namespace RimeTools.Scripts;

using Core = global::Dks.Core.DksCfg;
using SvcDks = global::Dks.Core.Svc.SvcDks;
using Tsinswreng.CsCtx;

internal static partial class Dks4{
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

		using IFnCtx fnCtx = new FnCtx();
		var svc = new SvcDks(cfg);

		// step 2: 先按 Dks2 的步子跑出正式 dks（布之道擬音版本）。
		await svc.布之道ToDkz(fnCtx, Ct);
		await svc.UpdateDkzFile(fnCtx, Ct);
		await svc.DkzToDks(fnCtx, Ct);

		// step 3: 舊 Dks 流程（中古漢語倒推）跑到工作區臨時目錄，作為高頻字的讀音來源。
		var 臨時根 = System.IO.Path.Combine(Ctx.RootDir, "_Dks4臨時");
		var 臨時原表 = System.IO.Path.Combine(臨時根, "src");
		var 臨時用戶 = System.IO.Path.Combine(臨時根, "user");
		System.IO.Directory.CreateDirectory(臨時原表);
		System.IO.Directory.CreateDirectory(臨時用戶);
		System.IO.File.Copy(System.IO.Path.Combine(cfg.SrcTableDir, "saffes.dict.yaml"), System.IO.Path.Combine(臨時原表, "saffes.dict.yaml"), true);
		System.IO.File.Copy(System.IO.Path.Combine(cfg.SrcTableDir, "dkp.dict.yaml"), System.IO.Path.Combine(臨時原表, "dkp.dict.yaml"), true);
		var 臨時Cfg = new Core{
			UserDataDir = 臨時用戶,
			SrcTableDir = 臨時原表,
			WordFreq = cfg.WordFreq,
			布之道DictPath = cfg.布之道DictPath,
		};
		var 舊Svc = new SvcDks(臨時Cfg);
		var 錶 = System.Diagnostics.Stopwatch.StartNew();
		await 舊Svc.SaffesToDkz(fnCtx, Ct);
		await 舊Svc.UpdateDkzFile(fnCtx, Ct);
		await 舊Svc.UpdateDks(fnCtx, Ct);
		Console.WriteLine($"中古倒推（臨時目錄）完成: {錶.ElapsedMilliseconds}ms");

		// step 4: 按字頻擇源——dkp 最優先；其餘前 5000 名用中古倒推，5000 名外用布之道。
		錶.Restart();
		await svc.按頻擇源(fnCtx, System.IO.Path.Combine(臨時用戶, "dks.dict.yaml"), 高頻名次上限, Ct);
		Console.WriteLine($"按頻擇源完成(上限 {高頻名次上限}): {錶.ElapsedMilliseconds}ms");

		// step 5: 後段併行——四件只讀 dks（外加倉頡表、詞頻源、拷貝來源），彼此不相干。
		await Task.WhenAll(
			svc.AttachCangjie(fnCtx, Ct),
			svc.ToDkn(fnCtx, Ct),
			svc.MkDksPhrase(fnCtx, Ct),
			svc.CopyDkpDkz(fnCtx, Ct)
		);

		// step 6: 匯報產物。
		Console.WriteLine($"已產出: {System.IO.Path.Combine(cfg.UserDataDir, "dks.dict.yaml")}");
		Console.WriteLine($"已產出: {System.IO.Path.Combine(cfg.UserDataDir, "dks_v.dict.yaml")}");
		Console.WriteLine($"已產出: {System.IO.Path.Combine(cfg.UserDataDir, "dkn.dict.yaml")}");
		Console.WriteLine($"已產出: {System.IO.Path.Combine(cfg.UserDataDir, "dkp.dict.yaml")}");
		Console.WriteLine($"已產出: {System.IO.Path.Combine(cfg.UserDataDir, "dkz.dict.yaml")}");
		Console.WriteLine($"已產出: {System.IO.Path.Combine(cfg.UserDataDir, "dks_phrase.dict.yaml")}");
	}
}
