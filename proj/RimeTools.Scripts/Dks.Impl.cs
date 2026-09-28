namespace RimeTools.Scripts;

using Tsinswreng.CsCtx;

/// Dks 命令的流程實現（舊．saffes 中古倒推）：
/// 前段（saffes→dkz、dkp 覆蓋）→ dkz→dks（規則版）→ 後段四件併行 → 彙報產物。
/// 一切文件操作都走 DksPipeline（本項目唯一碰文件系統的地方）。
internal static partial class Dks{
	internal static async partial Task Main(ISCtx Ctx, str[] Args, CT Ct){
		// step 1: 參數 → 路徑集合；建 Svc（注入解析/寫出/詞頻源策略）。
		var P = DksPaths.FromArgs(Args);
		using IFnCtx fnCtx = new FnCtx();
		var svc = DksPipeline.MkSvc(P);

		// step 2: 前段 —— saffes 表 + dkp 覆蓋，得到 dkz 中介表。
		var 錶 = System.Diagnostics.Stopwatch.StartNew();
		await DksPipeline.跑舊前段(P, svc, fnCtx, Ct);

		// step 3: dkz → dks（套 OcToOc3 規則、碼轉小寫）。
		await DksPipeline.跑一步(P.Dkz, P.Dks, (r, w) => svc.UpdateDks(fnCtx, r, w, Ct), Ct);
		Console.WriteLine($"dks 產出: {錶.ElapsedMilliseconds}ms");

		// step 4: 後段四件併行（dks_v/ dkn/ dks_phrase/ 拷 dkp·dkz）。
		await DksPipeline.跑後段(P, svc, fnCtx, Ct);
		Console.WriteLine($"全部完成: {錶.ElapsedMilliseconds}ms");

		// step 5: 彙報產物（與 Dks.sh 完成後的 User_Data 對拍用）。
		foreach(var f in DksPipeline.產物清單(P)){
			Console.WriteLine($"已產出: {f}");
		}
	}
}
