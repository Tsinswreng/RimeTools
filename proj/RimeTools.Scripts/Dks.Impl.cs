namespace RimeTools.Scripts;

using System.Globalization;
using System.Text;
using System.Text.Json;
using Tsinswreng.CsCtx;
using Tsinswreng.CsSh;
using static Tsinswreng.CsSh.ShGlobal;
using static System.IO.Path;
using DksCfg = global::Dks.Core.DksCfg;
using SvcDks = global::Dks.Core.Svc.SvcDks;
using 布之道拼式Parser = global::Dks.Core.布之道拼式Parser;
using EssayWordFreqSource = global::RimeTools.Shared.Freq.EssayWordFreqSource;

/// Dks 命令的實現（新）：四張 csx 表併行讀 → 整字合併 → dkz → dks → 三件後段併行。
/// 讀 csx 走 `dotnet script eval`：每個子進程一個表，stdout 只有 Export.csx 產出的 JSON。
/// 一切路徑都來自 DksFactPaths，本檔不寫死任何絕對路徑。
internal static partial class Dks{
	/// 一條讀音：字、音、權重（百分比原值字串，如 "10000%"）。
	private sealed record 讀音(str 字, str 音, str? 權重);

	internal static async partial Task Main(ISCtx Ctx, str[] Args, CT Ct){
		// step 1: 參數與真相源檢查。
		var P = DksFactPaths.FromArgs(Args);
		DksFactPaths.要求存在(P);
		Directory.CreateDirectory(P.OutDir);
		using IFnCtx fnCtx = new FnCtx();
		var svc = new SvcDks(new DksCfg{
			WordFreq = new EssayWordFreqSource(P.Essay),
		});
		var 錶 = System.Diagnostics.Stopwatch.StartNew();

		// step 2: 四張表併行讀；csx 各自起一個 dotnet-script，互不等待。
		var modTask = 讀Csx(P, P.TswgOcModed, "Mk魔改()", Ct);
		var zhengTask = 讀Csx(P, P.TswgOc, "Mk正音()", Ct);
		var buTask = 讀布之道(P, Ct);
		var zhongTask = 讀中古倒推(P, Ct);
		await Task.WhenAll(modTask, zhengTask, buTask, zhongTask);
		var mod = await modTask;
		var zheng = await zhengTask;
		var bu = await buTask;
		var zhong = await zhongTask;
		Console.WriteLine(
			$"事實源讀取 {錶.ElapsedMilliseconds}ms：魔改 {mod.Count}、正音 {zheng.Count}、布之道 {bu.Count}、中古倒推 {zhong.Count}");

		// step 3: 整字合併（魔改 > 正音 > 布之道 > 中古倒推）。
		錶.Restart();
		var merged = 合併([mod, zheng, bu, zhong]);
		var 字數 = merged.Select(x => x.字).Distinct().Count();
		Console.WriteLine($"合併 {錶.ElapsedMilliseconds}ms：{merged.Count} 條、{字數} 字");

		// step 4: 合併表 → dkz 文本 → dks（三鍵碼）。DkzToDks 內部逐行並行，且寫檔前會校驗。
		錶.Restart();
		var dkzText = MkDkzText(merged);
		str dksText;
		using(var reader = new StringReader(dkzText))
		using(var writer = new StringWriter()){
			await svc.DkzToDks(fnCtx, reader, writer, Ct);
			dksText = writer.ToString();
		}
		File.WriteAllText(P.OutDks, dksText, new UTF8Encoding(false));
		Console.WriteLine($"dks {錶.ElapsedMilliseconds}ms → {P.OutDks}");

		// step 5: 後段三件互不相干，一齊跑（各自讀同一份 dks 文本）。
		錶.Restart();
		await Task.WhenAll(
			產DksV(svc, fnCtx, P, dksText, Ct),
			產Dkn(svc, fnCtx, P, dksText, Ct),
			產DksPhrase(svc, fnCtx, P, dksText, Ct)
		);
		Console.WriteLine($"後段 {錶.ElapsedMilliseconds}ms");

		// step 6: 彙報產物。
		foreach(var f in new[]{P.OutDks, P.OutDksV, P.OutDkn, P.OutDksPhrase}){
			Console.WriteLine($"已產出: {f}");
		}
	}

	/// 用 `dotnet script eval` 載入 Export.csx + 指定表，stdout 取 JSON。
	/// 加 nonce（Dsl／Export／表 的最大 mtime）：任一個改了都會破 dotnet-script 的編譯緩存。
	private static async Task<List<讀音>> 讀Csx(DksFactPaths P, str 表路徑, str 呼叫, CT Ct){
		var nonce = new[]{ P.Dsl, P.Export, 表路徑 }
			.Max(f => File.GetLastWriteTimeUtc(f).Ticks);
		// #load 的參數是普通 C# 字串字面量，Windows 路徑的 `\` 會被當轉義；統一換成正斜線。
		var code = $"// nonce {nonce}\n" + $"""
#load "{P.Export.Replace('\\', '/')}"
#load "{表路徑.Replace('\\', '/')}"
Console.WriteLine(Export.MkJson({呼叫}));
""";
		// 每張表用獨立的 dotnet-script build 目錄：四張表併行時若共用同一個目錄，
		// 首次編譯會互相踩（鎖競爭導致 exit 1）。目錄固定，之後可重用緩存。
		var cacheDir = Combine(Path.GetTempPath(), "RimeTools.Dks", Path.GetFileNameWithoutExtension(表路徑));
		Directory.CreateDirectory(cacheDir);
		using var cmd = Cmd("dotnet", ["script", "-p", cacheDir, "eval", code], Ct);
		CommandTextResult r;
		try{
			r = await cmd.Text(Ct);
		} catch(CommandFailedException e){
			throw new InvalidOperationException(
				$"讀事實源失敗（exit {e.Exit.ExitCode}）：{表路徑}。可單獨跑 dotnet script eval 看 stderr。", e);
		}
		return 解析Json(r.Stdout);
	}

	/// 布之道表讀入後，先按既有適配規則轉成本方案的寫法，才能和其它三層比較與轉碼。
	private static async Task<List<讀音>> 讀布之道(DksFactPaths P, CT Ct){
		var rows = await 讀Csx(P, P.PaasTvLuuq, "Mk布之道()", Ct);
		for(var i = 0; i < rows.Count; i++){
			rows[i] = rows[i] with { 音 = 布之道拼式Parser.Adapt(rows[i].音) };
		}
		return rows;
	}

	/// 中古層是舊規則推得的快照，音值記法比現行鍵盤多：只有 kʰ/k/ɡ/ŋ/ʔ/h/s 的 ʷ 保留，
	/// 其餘聲母的 ʷ 要丟掉（與舊 ocToOc3 規則一致），否則 DkzToDks 查不到鍵。
	private static async Task<List<讀音>> 讀中古倒推(DksFactPaths P, CT Ct){
		var rows = await 讀Csx(P, P.McToOc, "Mk中古倒推()", Ct);
		for(var i = 0; i < rows.Count; i++){
			rows[i] = rows[i] with { 音 = 正規化中古音(rows[i].音) };
		}
		return rows;
	}

	private static str 正規化中古音(str 音){
		// 船母在舊快照裏寫成 ʑ（等於 ml）。現行 ToDks 的 J 鍵只對 ml 補帶 r 的介腹，ʑ 不補，
		// 會把 ʑok 推成 jfk（正確是 jrk）；故先收斂成 ml。
		音 = 音.Replace("ʑ", "ml");
		// 舊快照把 r 聲母寫成 rr'…（聲母 r 之外，介腹又寫了一次 r）；現行 ToDks 對 r 聲母會再補一次 r，
		// 故先收斂成 r'…，避免變成 rr'… 查不到鍵。
		if(音.StartsWith("rr", StringComparison.Ordinal)){
			音 = 音[1..];
		}
		var 保留ʷ = 音.StartsWith("kʰʷ", StringComparison.Ordinal)
			|| 音.StartsWith("kʷ", StringComparison.Ordinal)
			|| 音.StartsWith("ɡʷ", StringComparison.Ordinal)
			|| 音.StartsWith("ŋʷ", StringComparison.Ordinal)
			|| 音.StartsWith("ʔʷ", StringComparison.Ordinal)
			|| 音.StartsWith("hʷ", StringComparison.Ordinal)
			|| 音.StartsWith("sʷ", StringComparison.Ordinal);
		return 保留ʷ ? 音 : 音.Replace("ʷ", "");
	}

	/// 解析 Export.csx 的 JSON 陣列；只取字、音、權重。
	private static List<讀音> 解析Json(str Json){
		var ans = new List<讀音>();
		using var doc = JsonDocument.Parse(Json);
		foreach(var el in doc.RootElement.EnumerateArray()){
			var 字 = el.GetProperty("zvs").GetString() ?? "";
			var 音 = el.GetProperty("vm").GetString() ?? "";
			str? 權 = null;
			if(el.TryGetProperty("pct", out var pct)){
				權 = pct.GetDouble().ToString("0.######", CultureInfo.InvariantCulture) + "%";
			}
			ans.Add(new 讀音(字, 音, 權));
		}
		return ans;
	}

	/// 整字合併：按層的優先級，先到先得；同字的所有讀音整組保留。
	/// 最後按字碼位排序，讓產出穩定可 diff（同字內保持原行序）。
	private static List<讀音> 合併(IReadOnlyList<List<讀音>> Layers){
		var taken = new HashSet<str>();
		var ans = new List<讀音>();
		foreach(var layer in Layers){
			var byChar = new Dictionary<str, List<讀音>>();
			var order = new List<str>();
			foreach(var e in layer){
				if(!byChar.TryGetValue(e.字, out var list)){
					list = new List<讀音>();
					byChar[e.字] = list;
					order.Add(e.字);
				}
				list.Add(e);
			}
			foreach(var z in order){
				if(taken.Add(z)){
					ans.AddRange(byChar[z]);
				}
			}
		}
		return ans.OrderBy(x => x.字, StringComparer.Ordinal).ToList();
	}

	/// 合併結果 → dkz 文本（上古全拼 + 權重）；DkzToDks 吃這個格式。
	private static str MkDkzText(List<讀音> Rows){
		var sb = new StringBuilder(Rows.Count * 24);
		sb.Append("---\nname: dkz\nversion: \"\"\nsort: by_weight\nuse_preset_vocabulary: true\n...\n\n");
		foreach(var e in Rows){
			sb.Append(e.字).Append('\t').Append(e.音);
			if(e.權重 is not null){
				sb.Append('\t').Append(e.權重);
			}
			sb.Append('\n');
		}
		return sb.ToString();
	}

	/// dks_v：dks 左聯倉頡輔助碼。
	private static async Task 產DksV(SvcDks svc, IFnCtx fnCtx, DksFactPaths P, str DksText, CT Ct){
		using var dks = new StringReader(DksText);
		using var cj = new StreamReader(P.Cangjie, new UTF8Encoding(false));
		using var writer = new StringWriter();
		await svc.AttachCangjie(fnCtx, dks, cj, writer, Ct);
		File.WriteAllText(P.OutDksV, writer.ToString(), new UTF8Encoding(false));
	}

	/// dkn：dks 每碼取首尾。
	private static async Task 產Dkn(SvcDks svc, IFnCtx fnCtx, DksFactPaths P, str DksText, CT Ct){
		using var dks = new StringReader(DksText);
		using var writer = new StringWriter();
		await svc.ToDkn(fnCtx, dks, writer, Ct);
		File.WriteAllText(P.OutDkn, writer.ToString(), new UTF8Encoding(false));
	}

	/// dks_phrase：essay 詞頻 × dks 反查 × HeadTail 策略。
	private static async Task 產DksPhrase(SvcDks svc, IFnCtx fnCtx, DksFactPaths P, str DksText, CT Ct){
		using var dks = new StringReader(DksText);
		using var writer = new StringWriter();
		await svc.MkDksPhrase(fnCtx, dks, writer, Ct);
		File.WriteAllText(P.OutDksPhrase, writer.ToString(), new UTF8Encoding(false));
	}
}
