namespace Dks.Test.Domains.DksCore;

using System.Text;
using RimeTools.Shared.Dict.Models;
using RimeTools.Shared.Dict.Parser;

/// 測試用 dict.yaml 文本構造器：拼出「表頭 + 正文行」的文本，餵給只吃 TextReader 的 Svc 步驟。
/// 表頭刻意寫成真實 dks 的形狀（`version: ""`——空標量必須寫成空串是 Rime 的硬要求），
/// 正文每行形如 `字\t碼` 或 `字\t碼\t權重`。
public static class DictText{
	/// 造一份 dict.yaml 文本。
	/// <param name="Name">表頭 name（如 dks、dkz、dkp）。</param>
	/// <param name="Lines">正文行（已含 Tab 的原始行，如 "辣\tryt\t10000%"）。</param>
	public static str Mk(str Name, params str[] Lines){
		var sb = new StringBuilder();
		sb.Append("---\n");
		sb.Append($"name: {Name}\n");
		sb.Append("version: \"\"\n");
		sb.Append("sort: by_weight\n");
		sb.Append("use_preset_vocabulary: true\n");
		sb.Append("...\n");
		foreach(var line in Lines){
			sb.Append(line).Append('\n');
		}
		return sb.ToString();
	}

	/// 把寫出的 dict.yaml 文本解析成「字 → 碼列表（保序）」。
	/// 斷言用它，順帶驗證寫出的文本能被解析器讀回（表頭 + 正文都合法）。
	public static async Task<Dictionary<str, List<str>>> Mk字到碼集(str Text){
		var ans = new Dictionary<str, List<str>>();
		foreach(var (字, 碼, _) in await Mk行(Text)){
			if(!ans.TryGetValue(字, out var list)){
				list = new List<str>();
				ans[字] = list;
			}
			list.Add(碼);
		}
		return ans;
	}

	/// 把寫出的 dict.yaml 文本解析成 (字, 碼, 權重) 三元組列表（保序）；缺列以空串補。
	public static async Task<List<(str 字, str 碼, str 權重)>> Mk行(str Text){
		var parser = new DictYamlParser();
		var doc = await parser.Parse(new StringReader(Text), default);
		var ans = new List<(str, str, str)>();
		await foreach(var line in doc.Body){
			ans.Add((line.text ?? "", line.code ?? "", line.weight ?? ""));
		}
		return ans;
	}
}
