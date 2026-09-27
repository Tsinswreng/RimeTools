namespace RimeTools.Scripts;

using Core = global::Dks.Core.DksCfg;
using SvcDks = global::Dks.Core.Svc.SvcDks;
using Tsinswreng.CsCtx;

/// Dks 命令的流程實現：解析路徑參數 → 組 DksCfg → new SvcDks → 依序跑七步 → 打印產物清單。
internal static partial class Dks{
	internal static async partial Task Main(ISCtx Ctx, str[] Args, CT Ct){
		// step 1: 依參數(可空)覆寫默認路徑。
		var cfg = new Core();
		if(Args.Length >= 1 && !string.IsNullOrEmpty(Args[0])){
			cfg.UserDataDir = Args[0];
		}
		if(Args.Length >= 2 && !string.IsNullOrEmpty(Args[1])){
			cfg.SrcTableDir = Args[1];
		}

		// step 2: 建服務並依 Dks.sh 順序跑七步。
		using IFnCtx fnCtx = new FnCtx();
		var svc = new SvcDks(cfg);
		await svc.SaffesToDkz(fnCtx, Ct);
		await svc.UpdateDkzFile(fnCtx, Ct);
		await svc.UpdateDks(fnCtx, Ct);
		await svc.AttachCangjie(fnCtx, Ct);
		await svc.CopyDkpDkz(fnCtx, Ct);
		await svc.ToDkn(fnCtx, Ct);
		await svc.MkDksPhrase(fnCtx, Ct);

		// step 3: 匯報產物（與 Dks.sh 完成後的 User_Data 對拍用）。
		Console.WriteLine($"已產出: {System.IO.Path.Combine(cfg.UserDataDir, "dks.dict.yaml")}");
		Console.WriteLine($"已產出: {System.IO.Path.Combine(cfg.UserDataDir, "dks_v.dict.yaml")}");
		Console.WriteLine($"已產出: {System.IO.Path.Combine(cfg.UserDataDir, "dkn.dict.yaml")}");
		Console.WriteLine($"已產出: {System.IO.Path.Combine(cfg.UserDataDir, "dkp.dict.yaml")}");
		Console.WriteLine($"已產出: {System.IO.Path.Combine(cfg.UserDataDir, "dkz.dict.yaml")}");
		Console.WriteLine($"已產出: {System.IO.Path.Combine(cfg.UserDataDir, "dks_phrase.dict.yaml")}");
	}
}