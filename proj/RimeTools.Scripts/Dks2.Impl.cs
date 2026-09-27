namespace RimeTools.Scripts;

using Core = global::Dks.Core.DksCfg;
using SvcDks = global::Dks.Core.Svc.SvcDks;
using Tsinswreng.CsCtx;

internal static partial class Dks2{
	internal static async partial Task Main(ISCtx Ctx, str[] Args, CT Ct){
		// step 1: 依參數(可空)覆寫默認路徑；第三個參數是布之道原表。
		var cfg = new Core();
		if(Args.Length >= 1 && !string.IsNullOrEmpty(Args[0])){
			cfg.UserDataDir = Args[0];
		}
		if(Args.Length >= 2 && !string.IsNullOrEmpty(Args[1])){
			cfg.SrcTableDir = Args[1];
		}
		if(Args.Length >= 3 && !string.IsNullOrEmpty(Args[2])){
			cfg.布之道DictPath = Args[2];
		}

		// step 2: 前段必須串行——布之道→dkz、dkp 覆蓋、dkz→dks，後一步都要讀前一步的產物。
		using IFnCtx fnCtx = new FnCtx();
		var svc = new SvcDks(cfg);
		await svc.布之道ToDkz(fnCtx, Ct);
		await svc.UpdateDkzFile(fnCtx, Ct);
		await svc.DkzToDks(fnCtx, Ct);

		// step 3: 後段併行——四件都只讀 dks（外加倉頡表、詞頻源、拷貝來源），彼此不相干：
		//         dks_v 接倉頡、dkn 取首尾碼、dks_phrase 造詞、拷 dkp/dkz 到 User_Data。
		await Task.WhenAll(
			svc.AttachCangjie(fnCtx, Ct),
			svc.ToDkn(fnCtx, Ct),
			svc.MkDksPhrase(fnCtx, Ct),
			svc.CopyDkpDkz(fnCtx, Ct)
		);

		// step 4: 匯報產物。
		Console.WriteLine($"已產出: {System.IO.Path.Combine(cfg.UserDataDir, "dks.dict.yaml")}");
		Console.WriteLine($"已產出: {System.IO.Path.Combine(cfg.UserDataDir, "dks_v.dict.yaml")}");
		Console.WriteLine($"已產出: {System.IO.Path.Combine(cfg.UserDataDir, "dkn.dict.yaml")}");
		Console.WriteLine($"已產出: {System.IO.Path.Combine(cfg.UserDataDir, "dkp.dict.yaml")}");
		Console.WriteLine($"已產出: {System.IO.Path.Combine(cfg.UserDataDir, "dkz.dict.yaml")}");
		Console.WriteLine($"已產出: {System.IO.Path.Combine(cfg.UserDataDir, "dks_phrase.dict.yaml")}");
	}
}
