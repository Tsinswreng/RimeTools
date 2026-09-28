namespace RimeTools.Scripts;

using Tsinswreng.CsCtx;

/// Dks2 命令的流程實現（布之道擬音源）：骨架與 Dks 同一條，只換前段——布之道 → dkz → dks。
/// 一切文件操作走 DksPipeline。
internal static partial class Dks2{
	internal static async partial Task Main(ISCtx Ctx, str[] Args, CT Ct){
		// step 1: 參數 → 路徑集合；建 Svc（注入解析/寫出/詞頻源策略）。
		var P = DksPaths.FromArgs(Args);
		using IFnCtx fnCtx = new FnCtx();
		var svc = DksPipeline.MkSvc(P);

		// step 2: 前段三步必須串行（後一步讀前一步的產物）：
		//   ① 布之道擬音 → dkz；② dkp 覆蓋（就地改寫 dkz）；③ dkz → dks（查三層鍵位表 + 三鍵產出驗證）。
		var 錶 = System.Diagnostics.Stopwatch.StartNew();
		await DksPipeline.跑新前段(P, svc, fnCtx, Ct);
		await DksPipeline.跑一步(P.Dkz, P.Dks, (r, w) => svc.DkzToDks(fnCtx, r, w, Ct), Ct);
		Console.WriteLine($"dks 產出: {錶.ElapsedMilliseconds}ms");

		// step 3: 後段四件併行（dks_v／dkn／dks_phrase／拷 dkp·dkz）。
		await DksPipeline.跑後段(P, svc, fnCtx, Ct);
		Console.WriteLine($"全部完成: {錶.ElapsedMilliseconds}ms");

		// step 4: 彙報產物。
		foreach(var f in DksPipeline.產物清單(P)){
			Console.WriteLine($"已產出: {f}");
		}
	}
}
