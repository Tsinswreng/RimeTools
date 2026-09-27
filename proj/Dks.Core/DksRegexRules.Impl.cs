namespace Dks.Core;

using System.Reflection;
using RimeTools.Tools;

public static partial class DksRegexRules{
	// 資源名: Dks.Core 程序集內嵌資源的邏輯名 = 預設命名空間.Rules.檔案名
	// (csproj 的 EmbeddedResource Include="Rules/*.txt" 無 LogicalName, 故路徑轉點)。

	public static partial IReadOnlyList<SrRule> SaffesToOc()
		=> LoadRules("saffesToOcRegex.txt");

	public static partial IReadOnlyList<SrRule> OcToOc3()
		=> LoadRules("ocToOc3.txt");

	public static partial IReadOnlyList<SrRule> Cangjie()
		=> LoadRules("cangjie.txt");

	public static partial IReadOnlyDictionary<str, str> ShengFu()
		=> LoadTable("聲符.txt");

	public static partial IReadOnlyDictionary<str, str> YiFu()
		=> LoadTable("義符.txt");

	public static partial IReadOnlyList<SrRule> ShengFuRules()
		=> ToRules(ShengFu(), isYiFu: false);

	public static partial IReadOnlyList<SrRule> YiFuRules()
		=> ToRules(YiFu(), isYiFu: true);

	/// 把「字 → 音」表轉成帶錨規則：義符用首錨 `^字`（碼首是義符字形），聲符用尾錨 `字$`（碼尾是聲符字形）。
	private static IReadOnlyList<SrRule> ToRules(IReadOnlyDictionary<str, str> Table, bool isYiFu){
		var ans = new List<SrRule>();
		foreach(var kv in Table){
			var pat = isYiFu ? "^" + kv.Key : kv.Key + "$";
			ans.Add(new SrRule(pat, kv.Value));
		}
		return ans;
	}

	/// 讀規則資源: 每行「pattern\t替換」, 忽略空行。
	private static IReadOnlyList<SrRule> LoadRules(str FileName){
		var text = ReadEmbedded(FileName);
		var ans = new List<SrRule>();
		foreach(var rawLine in text.Split('\n')){
			var line = rawLine.TrimEnd('\r');
			if(line.Length == 0){
				continue;
			}
			var tabIdx = line.IndexOf('\t');
			if(tabIdx < 0){
				// 無替換(空替換)的規則: pattern 後直接換行。
				ans.Add(new SrRule(line, ""));
				continue;
			}
			ans.Add(new SrRule(line[..tabIdx], line[(tabIdx + 1)..]));
		}
		return ans;
	}

	/// 讀「字\t音」表資源。
	private static IReadOnlyDictionary<str, str> LoadTable(str FileName){
		var text = ReadEmbedded(FileName);
		var ans = new Dictionary<str, str>();
		foreach(var rawLine in text.Split('\n')){
			var line = rawLine.TrimEnd('\r');
			if(line.Length == 0){
				continue;
			}
			var tabIdx = line.IndexOf('\t');
			if(tabIdx <= 0){
				continue;
			}
			ans[line[..tabIdx]] = line[(tabIdx + 1)..];
		}
		return ans;
	}

	/// 取內嵌資源文本; 資源名按「Dks.Core.Rules.{File}」解析。
	private static str ReadEmbedded(str FileName){
		var asm = typeof(DksRegexRules).Assembly;
		var name = asm.GetName().Name + ".Rules." + FileName;
		using var stream = asm.GetManifestResourceStream(name);
		if(stream is null){
			// 資源名解析失敗時, 列出可用資源幫助定位(不應發生)。
			var names = string.Join(", ", asm.GetManifestResourceNames());
			throw new InvalidOperationException($"找不到內嵌規則資源 {name}; 可用: {names}");
		}
		using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
		return reader.ReadToEnd();
	}
}