namespace RimeTools.Scripts;

using SvcDks = global::Dks.Core.Svc.SvcDks;
using Tsinswreng.CsCtx;

/// Dks4 命令的流程實現：dkp 最優先；其餘按 essay.txt 的字頻排名擇源（前 N 名用中古倒推，其餘用布之道）。
/// 舊側整套跑到工作區臨時目錄，不動正式產物；新舊兩側互不相干故併行。
internal static partial class Dks4{
	internal static async partial Task Main(ISCtx Ctx, str[] Args, CT Ct){
		// step 1: 參數 → 路徑集合；建 Svc。
		var P = DksPaths.FromArgs(Args);
		using IFnCtx fnCtx = new FnCtx();
		var svc = DksPipeline.MkSvc(P);
		var 錶 = System.Diagnostics.Stopwatch.StartNew();

		// step 2: 兩側併行（互不相干，且舊側慢得多）：
		//   新側 = 布之道 → dkz →（dkp 覆蓋）→ dks，落正式目錄（P.Dks）；
		//   舊側 = saffes → dkz →（dkp 覆蓋）→ dks，落 <倉庫根>/_Dks4臨時/user。
		var 臨時根 = System.IO.Path.Combine(Ctx.RootDir, "_Dks4臨時");
		var 新側 = Mk新側(P, svc, fnCtx, Ct);
		var 舊側 = DksPipeline.跑舊流程到目錄(臨時根, P, fnCtx, Ct);
		await Task.WhenAll(新側, 舊側);
		var dkp字 = await 新側;
		var 舊Dks = await 舊側;
		Console.WriteLine($"新舊兩側完成: {錶.ElapsedMilliseconds}ms（舊讀音取自 {舊Dks}）");

		// step 3: 按頻擇源——dkp 覆蓋字用新側（即 dkp 解出的碼）；布之道缺音的用舊側；
		//         其餘在 essay 字頻前 N 名內者用舊側、名次之外者用新側。寫回 P.Dks（自動走臨時檔＋改名）。
		錶.Restart();
		await DksPipeline.跑兩入一步(
			P.Dks, 舊Dks, P.Dks,
			(新, 舊, w) => svc.按頻擇源(fnCtx, 新, 舊, dkp字, 高頻名次上限, w, Ct), Ct);
		Console.WriteLine($"按頻擇源完成(上限 {高頻名次上限}): {錶.ElapsedMilliseconds}ms");

		// step 4: 後段四件併行 + 彙報產物。
		await DksPipeline.跑後段(P, svc, fnCtx, Ct);
		Console.WriteLine($"全部完成: {錶.ElapsedMilliseconds}ms");
		foreach(var f in DksPipeline.產物清單(P)){
			Console.WriteLine($"已產出: {f}");
		}
	}

	/// 新側：布之道 → dkz →（dkp 覆蓋）→ dks；回傳 dkp 覆蓋到的字集（按頻擇源要用）。
	private static async Task<IReadOnlySet<str>> Mk新側(DksPaths P, SvcDks svc, IFnCtx fnCtx, CT Ct){
		var dkp字 = await DksPipeline.跑新前段(P, svc, fnCtx, Ct);
		await DksPipeline.跑一步(P.Dkz, P.Dks, (r, w) => svc.DkzToDks(fnCtx, r, w, Ct), Ct);
		return dkp字;
	}
}
