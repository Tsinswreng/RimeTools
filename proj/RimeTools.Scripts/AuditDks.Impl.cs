namespace RimeTools.Scripts;

using Core = global::Dks.Core.DksCfg;
using SvcDks = global::Dks.Core.Svc.SvcDks;
using RimeTools.Shared.Freq;
using RimeTools.Tools;
using Tsinswreng.CsCtx;

internal static partial class AuditDks{
	internal static async partial Task Main(ISCtx Ctx, str[] Args, CT Ct){
		// step 1: 輸出根（工作區內；默認 <倉庫根>/_AuditDks），四條目錄一次建好。
		var 根 = Args.Length >= 1 && !string.IsNullOrEmpty(Args[0])
			? Args[0]
			: System.IO.Path.Combine(Ctx.RootDir, "_AuditDks");
		var 舊根 = System.IO.Path.Combine(根, "舊");
		var 新根 = System.IO.Path.Combine(根, "新");
		var 舊原表 = System.IO.Path.Combine(舊根, "src");
		var 舊用戶 = System.IO.Path.Combine(舊根, "user");
		var 新原表 = System.IO.Path.Combine(新根, "src");
		var 新用戶 = System.IO.Path.Combine(新根, "user");
		foreach(var d in new[]{舊原表, 舊用戶, 新原表, 新用戶}){
			System.IO.Directory.CreateDirectory(d);
		}

		// step 2: 各流程只備自己會讀的原表——saffes 只有舊流程要，dkp 兩邊都要；
		//         布之道原表由 Cfg.布之道DictPath 直接指向真 User_Data，不複製。
		var 真原表 = Core.DefaultSrcTableDir;
		System.IO.File.Copy(System.IO.Path.Combine(真原表, "saffes.dict.yaml"), System.IO.Path.Combine(舊原表, "saffes.dict.yaml"), true);
		System.IO.File.Copy(System.IO.Path.Combine(真原表, "dkp.dict.yaml"), System.IO.Path.Combine(舊原表, "dkp.dict.yaml"), true);
		System.IO.File.Copy(System.IO.Path.Combine(真原表, "dkp.dict.yaml"), System.IO.Path.Combine(新原表, "dkp.dict.yaml"), true);

		using IFnCtx fnCtx = new FnCtx();
		var 詞頻路徑 = System.IO.Path.Combine(Core.DefaultUserDataDir, "essay.txt");

		// step 3: 舊 Dks 流程構建 dks（SaffesToDkz → dkp 覆蓋 → UpdateDks）。
		var 舊Cfg = new Core{
			UserDataDir = 舊用戶,
			SrcTableDir = 舊原表,
			WordFreq = new EssayWordFreqSource(詞頻路徑),
		};
		var 舊Svc = new SvcDks(舊Cfg);
		var 錶 = System.Diagnostics.Stopwatch.StartNew();
		await 舊Svc.SaffesToDkz(fnCtx, Ct);
		await 舊Svc.UpdateDkzFile(fnCtx, Ct);
		await 舊Svc.UpdateDks(fnCtx, Ct);
		Console.WriteLine($"舊 Dks 流程構建 dks 完成: {錶.ElapsedMilliseconds}ms");

		// step 4: 新 Dks2 流程構建 dks（布之道ToDkz → dkp 覆蓋 → DkzToDks）。
		//         產出驗證不過就打印問題清單並中止對比（新流程不會寫出半成品）。
		var 新Cfg = new Core{
			UserDataDir = 新用戶,
			SrcTableDir = 新原表,
			WordFreq = new EssayWordFreqSource(詞頻路徑),
		};
		var 新Svc = new SvcDks(新Cfg);
		錶.Restart();
		try{
			await 新Svc.布之道ToDkz(fnCtx, Ct);
			await 新Svc.UpdateDkzFile(fnCtx, Ct);
			await 新Svc.DkzToDks(fnCtx, Ct);
		}catch(InvalidOperationException ex){
			Console.WriteLine("新 Dks2 流程構建失敗（產出驗證）——先修好再對比：");
			Console.WriteLine(ex.Message);
			return;
		}
		Console.WriteLine($"新 Dks2 流程構建 dks 完成: {錶.ElapsedMilliseconds}ms");

		// step 5: 讀兩份 dks，取 字 → 碼集（去重、升序），行序不計。
		var 舊表 = Mk字到碼集(System.IO.Path.Combine(舊用戶, "dks.dict.yaml"));
		var 新表 = Mk字到碼集(System.IO.Path.Combine(新用戶, "dks.dict.yaml"));

		// step 6: essay.txt 的漢字頻率。
		var (字頻率, 總頻) = Mk字頻率(詞頻路徑);

		// step 7: 收有變化的字，按頻率降序（同頻按字序）排出，寫報告並打印。
		var 變化 = new List<(str 字, str 舊碼, str 新碼, double 頻率)>();
		foreach(var 字 in 舊表.Keys.Union(新表.Keys)){
			var 舊碼 = 舊表.TryGetValue(字, out var a) ? string.Join("/", a) : "";
			var 新碼 = 新表.TryGetValue(字, out var b) ? string.Join("/", b) : "";
			if(舊碼 == 新碼){
				continue;
			}
			變化.Add((字, 舊碼, 新碼, 字頻率.TryGetValue(字, out var f) ? f : 0.0));
		}
		變化.Sort((x, y) => {
			var c = y.頻率.CompareTo(x.頻率);
			return c != 0 ? c : string.CompareOrdinal(x.字, y.字);
		});

		var 報告 = new System.Text.StringBuilder();
		報告.AppendLine("dks 對比：舊 Dks 流程 vs 新 Dks2 流程（只列有變化的字，按漢字頻率降序）");
		報告.AppendLine($"頻率定義：該字在 essay.txt 的單字條目頻數 ÷ 全部單字條目頻數之和（合計 {總頻}），再乘 100000");
		報告.AppendLine($"舊表 {舊表.Count} 字、新表 {新表.Count} 字、有變化 {變化.Count} 字");
		報告.AppendLine("頻率(十萬分之,3位小數)\t字\t舊碼(Dks)\t新碼(Dks2)");
		foreach(var (字, 舊碼, 新碼, 頻率) in 變化){
			報告.AppendLine($"{頻率 * 100000:F3}\t{字}\t{舊碼}\t{新碼}");
		}
		var 報告路徑 = System.IO.Path.Combine(根, "差異.txt");
		System.IO.File.WriteAllText(報告路徑, 報告.ToString(), new System.Text.UTF8Encoding(false));
		Console.WriteLine($"報告已寫: {報告路徑}");
		Console.Write(報告.ToString());
	}

	/// 讀一份 dks.dict.yaml，取 字 → 碼集（同一字的碼去重後升序；含空碼）。
	private static Dictionary<str, List<str>> Mk字到碼集(str 路徑){
		var ans = new Dictionary<str, List<str>>();
		foreach(var line in System.IO.File.ReadAllLines(路徑)){
			if(line.Length == 0 || line[0] == '#'){
				continue;
			}
			var c = line.Split('\t');
			if(c.Length < 2 || c[0].Length == 0){
				continue;
			}
			if(!ans.TryGetValue(c[0], out var list)){
				list = new List<str>();
				ans[c[0]] = list;
			}
			var 碼 = c[1].Trim();
			if(!list.Contains(碼)){
				list.Add(碼);
			}
		}
		foreach(var list in ans.Values){
			list.Sort(StringComparer.Ordinal);
		}
		return ans;
	}

	/// 讀 essay.txt，算單字頻率：該字頻數 ÷ 全部單字條目頻數之和（同字多條目先合併）。
	/// 返回 (字→頻率, 單字條目頻數合計)。
	private static (Dictionary<str, double>, long) Mk字頻率(str 路徑){
		var 頻數 = new Dictionary<str, long>();
		long 合計 = 0;
		foreach(var line in System.IO.File.ReadAllLines(路徑)){
			var t = line.Trim();
			var k = t.IndexOf('\t');
			if(k <= 0){
				continue;
			}
			var text = t[..k];
			if(!long.TryParse(t[(k + 1)..], out var n)){
				continue;
			}
			// 只取單字條目；CJK 擴展區是代理對，故按碼點判而非 UTF-16 長度。
			if(TextUtil.SplitByRune(text).Count != 1){
				continue;
			}
			頻數[text] = 頻數.TryGetValue(text, out var old) ? old + n : n;
			合計 += n;
		}
		var ans = new Dictionary<str, double>();
		foreach(var kv in 頻數){
			ans[kv.Key] = 合計 == 0 ? 0.0 : (double)kv.Value / 合計;
		}
		return (ans, 合計);
	}
}
