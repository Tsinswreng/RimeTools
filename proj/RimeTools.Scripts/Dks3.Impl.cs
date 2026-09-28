namespace RimeTools.Scripts;

using SvcDks = global::Dks.Core.Svc.SvcDks;
using Tsinswreng.CsCtx;

/// Dks3 命令的流程實現：布之道擬音為主，布之道缺音的字用中古倒推回退（保證不缺字）。
/// 舊側整套跑到工作區臨時目錄，不動正式產物；新舊兩側互不相干故併行。
internal static partial class Dks3{
	internal static async partial Task Main(ISCtx Ctx, str[] Args, CT Ct){
		// step 1: 參數 → 路徑集合；建 Svc。
		var P = DksPaths.FromArgs(Args);
		using IFnCtx fnCtx = new FnCtx();
		var svc = DksPipeline.MkSvc(P);
		var 錶 = System.Diagnostics.Stopwatch.StartNew();

		// step 2: 兩側併行（互不相干，且舊側慢得多，串行等它純屬浪費）：
		//   新側 = 布之道 → dkz →（dkp 覆蓋）→ dks，落正式目錄（P.Dks）；
		//   舊側 = saffes → dkz →（dkp 覆蓋）→ dks，落 <倉庫根>/_Dks3臨時/user（跑完留著便於查）。
		var 臨時根 = System.IO.Path.Combine(Ctx.RootDir, "_Dks3臨時");
		var 新側 = Mk新側(P, svc, fnCtx, Ct);
		var 舊側 = DksPipeline.跑舊流程到目錄(臨時根, P, fnCtx, Ct);
		await Task.WhenAll(新側, 舊側);
		var 舊Dks = await 舊側;
		Console.WriteLine($"新舊兩側完成: {錶.ElapsedMilliseconds}ms（舊讀音取自 {舊Dks}）");

		// step 3: 回退缺音——新側「無此行、或此行全部碼皆空」的字改用舊側，其餘保留新側；
		//         輸入含輸出同一個檔，DksPipeline 自動走「臨時檔＋改名」。
		錶.Restart();
		await DksPipeline.跑兩入一步(P.Dks, 舊Dks, P.Dks, (新, 舊, w) => svc.回退缺音(fnCtx, 新, 舊, w, Ct), Ct);
		Console.WriteLine($"回退缺音完成: {錶.ElapsedMilliseconds}ms");

		// step 4: 後段四件併行 + 彙報產物。
		await DksPipeline.跑後段(P, svc, fnCtx, Ct);
		Console.WriteLine($"全部完成: {錶.ElapsedMilliseconds}ms");
		foreach(var f in DksPipeline.產物清單(P)){
			Console.WriteLine($"已產出: {f}");
		}
	}

	/// 新側：布之道 → dkz →（dkp 覆蓋）→ dks，寫進 P 指的產物目錄；回傳 dkp 覆蓋到的字集。
	private static async Task<IReadOnlySet<str>> Mk新側(DksPaths P, SvcDks svc, IFnCtx fnCtx, CT Ct){
		var dkp字 = await DksPipeline.跑新前段(P, svc, fnCtx, Ct);
		await DksPipeline.跑一步(P.Dkz, P.Dks, (r, w) => svc.DkzToDks(fnCtx, r, w, Ct), Ct);
		return dkp字;
	}
}
