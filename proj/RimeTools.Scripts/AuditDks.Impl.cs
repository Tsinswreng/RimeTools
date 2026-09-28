namespace RimeTools.Scripts;

using RimeTools.Tools;
using Tsinswreng.CsCtx;

internal static partial class AuditDks{
	internal static async partial Task Main(ISCtx Ctx, str[] Args, CT Ct){
		// step 1: 輸出根（工作區內；默認 <倉庫根>/_AuditDks）；其後可選參數與 Dks 命令同序，
		//         用來覆寫默認路徑集合（UserDataDir、SrcTableDir、布之道DictPath）。
		var 根 = Args.Length >= 1 && !string.IsNullOrEmpty(Args[0])
			? Args[0]
			: System.IO.Path.Combine(Ctx.RootDir, "_AuditDks");
		var P = DksPaths.FromArgs(Args.Length > 1 ? Args[1..] : []);
		using IFnCtx fnCtx = new FnCtx();

		// step 2: 兩側各寫自己的目錄，互不相干故併行（舊側要 40 秒上下，串行等它純屬浪費）：
		//   舊側 = 舊 Dks 流程整套（內部自備 src/user，複製 saffes·dkp）
		//          → <根>/舊/user/dks.dict.yaml；
		//   新側 = 布之道前段 → dkz → dks（只備 dkp）→ <根>/新/user/dks.dict.yaml。
		var 新路徑 = new DksPaths{
			UserDataDir = System.IO.Path.Combine(根, "新", "user"),
			SrcTableDir = System.IO.Path.Combine(根, "新", "src"),
			布之道Dict = P.布之道Dict,
		};
		var 錶 = System.Diagnostics.Stopwatch.StartNew();
		var 舊任務 = DksPipeline.跑舊流程到目錄(System.IO.Path.Combine(根, "舊"), P, fnCtx, Ct);
		var 新任務 = Mk新側(新路徑, P, fnCtx, Ct);
		var 舊Dks = await 舊任務;
		IReadOnlySet<str> dkp字;
		try{
			// 產出驗證不過就打印問題清單並中止對比（新流程不會留下半成品）。
			dkp字 = await 新任務;
		}catch(InvalidOperationException ex){
			Console.WriteLine("新 Dks2 流程構建失敗（產出驗證）——先修好再對比：");
			Console.WriteLine(ex.Message);
			return;
		}
		Console.WriteLine($"兩側構建 dks 完成: {錶.ElapsedMilliseconds}ms");
		Console.WriteLine($"  舊側(Dks):  {舊Dks}");
		Console.WriteLine($"  新側(Dks2): {新路徑.Dks}");

		// step 3: 讀兩份 dks，取 字 → 碼集（去重、升序），行序不計。
		var 舊表 = Mk字到碼集(舊Dks);
		var 新表 = Mk字到碼集(新路徑.Dks);

		// step 4: essay.txt 的漢字頻率（詞頻源路徑由 DksPaths 統一給）。
		var (字頻率, 總頻) = Mk字頻率(P.Essay);

		// step 5: 收有變化的字，按頻率降序（同頻按字序）排出，寫報告並打印。
		//         凡 dkp 覆蓋到的字一律不列：兩條流程都用 dkp 的那幾行，差別沒有意義（要改就去改 dkp）。
		//         dkp字 直接用新側前段回傳的覆蓋字集（同一份 dkp 解析結果，不再重讀一遍）。
		var 變化 = new List<(str 字, str 舊碼, str 新碼, double 頻率)>();
		var 略過dkp = 0;
		foreach(var 字 in 舊表.Keys.Union(新表.Keys)){
			var 舊碼 = 舊表.TryGetValue(字, out var a) ? string.Join("/", a) : "";
			var 新碼 = 新表.TryGetValue(字, out var b) ? string.Join("/", b) : "";
			if(舊碼 == 新碼){
				continue;
			}
			if(dkp字.Contains(字)){
				略過dkp++;
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
		報告.AppendLine($"舊表 {舊表.Count} 字、新表 {新表.Count} 字、有變化 {變化.Count} 字（另有 {略過dkp} 個字有差異但被 dkp 覆蓋，未列入）");
		報告.AppendLine("頻率(十萬分之,3位小數)\t字\t舊碼(Dks)\t新碼(Dks2)");
		foreach(var (字, 舊碼, 新碼, 頻率) in 變化){
			報告.AppendLine($"{頻率 * 100000:F3}\t{字}\t{舊碼}\t{新碼}");
		}
		var 報告路徑 = System.IO.Path.Combine(根, "差異.txt");
		System.IO.File.WriteAllText(報告路徑, 報告.ToString(), new System.Text.UTF8Encoding(false));
		Console.WriteLine($"報告已寫: {報告路徑}");
		Console.Write(報告.ToString());
	}

	/// 新側：在 新 指定的目錄下跑「布之道 → dkz →（dkp 覆蓋）→ dks」；
	/// 只複製 dkp（新流程不讀 saffes），回傳 dkp 覆蓋到的字集。
	private static async Task<IReadOnlySet<str>> Mk新側(DksPaths 新, DksPaths 源, IFnCtx fnCtx, CT Ct){
		DksPipeline.備目錄(新.SrcTableDir, 新.UserDataDir);
		System.IO.File.Copy(源.Dkp, 新.Dkp, true);
		var svc = DksPipeline.MkSvc(新);
		var dkp字 = await DksPipeline.跑新前段(新, svc, fnCtx, Ct);
		await DksPipeline.跑一步(新.Dkz, 新.Dks, (r, w) => svc.DkzToDks(fnCtx, r, w, Ct), Ct);
		return dkp字;
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
