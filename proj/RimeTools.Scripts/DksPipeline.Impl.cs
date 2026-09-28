namespace RimeTools.Scripts;

using RimeTools.Shared.Freq;
using Core = global::Dks.Core.DksCfg;
using SvcDks = global::Dks.Core.Svc.SvcDks;
using Tsinswreng.CsCtx;
using static System.IO.Path;

internal static partial class DksPipeline{
	internal static partial SvcDks MkSvc(DksPaths P){
		// 解析/寫出用默認純文本實現；詞頻源指向端點目錄下的 essay.txt（庫不知道路徑，故在此注入）。
		var cfg = new Core{
			WordFreq = new EssayWordFreqSource(P.Essay),
		};
		return new SvcDks(cfg);
	}

	internal static async partial Task 跑一步(
		str Src, str Dst, Func<TextReader, TextWriter, Task> 步, CT Ct){
		// step 1: 同檔改寫 → 先寫臨時檔，寫完再原子改名覆蓋（免得寫出器截斷來源）。
		var 同檔 = string.Equals(
			System.IO.Path.GetFullPath(Src), System.IO.Path.GetFullPath(Dst), StringComparison.OrdinalIgnoreCase);
		var 目標 = 同檔 ? Dst + ".tmp" : Dst;
		// step 2: 開兩端 → 跑步 → 兩端各自關閉（異常也關）。
		using(var reader = new StreamReader(Src, new System.Text.UTF8Encoding(false))){
			using var writer = new StreamWriter(目標, false, new System.Text.UTF8Encoding(false));
			await 步(reader, writer);
		}
		if(同檔){
			System.IO.File.Move(目標, Dst, true);
		}
	}

	internal static async partial Task 跑兩入一步(
		str Src1, str Src2, str Dst, Func<TextReader, TextReader, TextWriter, Task> 步, CT Ct){
		var 目標 = Mk目標(Src1, Src2, Dst);
		using(var r1 = new StreamReader(Src1, new System.Text.UTF8Encoding(false))){
			using(var r2 = new StreamReader(Src2, new System.Text.UTF8Encoding(false))){
				using var w = new StreamWriter(目標, false, new System.Text.UTF8Encoding(false));
				await 步(r1, r2, w);
			}
		}
		if(目標 != Dst){
			System.IO.File.Move(目標, Dst, true);
		}
	}

	internal static async partial Task<T> 跑兩入一步<T>(
		str Src1, str Src2, str Dst, Func<TextReader, TextReader, TextWriter, Task<T>> 步, CT Ct){
		var 目標 = Mk目標(Src1, Src2, Dst);
		T ans;
		using(var r1 = new StreamReader(Src1, new System.Text.UTF8Encoding(false))){
			using(var r2 = new StreamReader(Src2, new System.Text.UTF8Encoding(false))){
				using var w = new StreamWriter(目標, false, new System.Text.UTF8Encoding(false));
				ans = await 步(r1, r2, w);
			}
		}
		if(目標 != Dst){
			System.IO.File.Move(目標, Dst, true);
		}
		return ans;
	}

	/// 判定「輸出是否與某個輸入同路徑」：是則返回臨時目標（寫完再改名）。
	private static str Mk目標(str Src1, str Src2, str Dst){
		var d = System.IO.Path.GetFullPath(Dst);
		var 撞 = string.Equals(d, System.IO.Path.GetFullPath(Src1), StringComparison.OrdinalIgnoreCase)
			|| string.Equals(d, System.IO.Path.GetFullPath(Src2), StringComparison.OrdinalIgnoreCase);
		return 撞 ? Dst + ".tmp" : Dst;
	}

	internal static async partial Task<IReadOnlySet<str>> 跑舊前段(
		DksPaths P, SvcDks Svc, IFnCtx Ctx, CT Ct){
		// step 1: saffes → dkz（碼轉大寫 + 套規則）。
		await 跑一步(P.Saffes, P.Dkz, (r, w) => Svc.SaffesToDkz(Ctx, r, w, Ct), Ct);
		// step 2: dkp 覆蓋（就地改寫 dkz）：剔除 dkp 已收字頭、dkp 表體併在前。
		return await 跑兩入一步(P.Dkp, P.Dkz, P.Dkz, (a, b, w) => Svc.UpdateDkzFile(Ctx, a, b, w, Ct), Ct);
	}

	internal static async partial Task<IReadOnlySet<str>> 跑新前段(
		DksPaths P, SvcDks Svc, IFnCtx Ctx, CT Ct){
		// step 1′: 布之道擬音 → dkz（適配成自己的擬音）。
		await 跑一步(P.布之道, P.Dkz, (r, w) => Svc.布之道ToDkz(Ctx, r, w, Ct), Ct);
		// step 2: dkp 覆蓋（與舊流程同一函數，保證兩條流程的 dkp 語義一致）。
		return await 跑兩入一步(P.Dkp, P.Dkz, P.Dkz, (a, b, w) => Svc.UpdateDkzFile(Ctx, a, b, w, Ct), Ct);
	}

	internal static async partial Task<str> 跑舊流程到目錄(str 目錄, DksPaths P, IFnCtx Ctx, CT Ct){
		// step 1: 在目標目錄下備好 src/user 兩層：舊流程要讀 saffes+dkp，故複製過去（不動原表）。
		var 原表 = Combine(目錄, "src");
		var 用戶 = Combine(目錄, "user");
		備目錄(原表, 用戶);
		System.IO.File.Copy(P.Saffes, Combine(原表, "saffes.dict.yaml"), true);
		System.IO.File.Copy(P.Dkp, Combine(原表, "dkp.dict.yaml"), true);
		var 臨時路徑 = new DksPaths{
			UserDataDir = 用戶,
			SrcTableDir = 原表,
			布之道Dict = P.布之道Dict,
		};
		// step 2: 跑舊前段（saffes → dkz → dkp 覆蓋）+ 舊轉碼（dkz → dks）。
		var svc = MkSvc(臨時路徑);
		await 跑舊前段(臨時路徑, svc, Ctx, Ct);
		await 跑一步(臨時路徑.Dkz, 臨時路徑.Dks, (r, w) => svc.UpdateDks(Ctx, r, w, Ct), Ct);
		return 臨時路徑.Dks;
	}

	internal static async partial Task 跑後段(DksPaths P, SvcDks Svc, IFnCtx Ctx, CT Ct){
		// 四件互不相干，一齊跑：
		//   ① dks_v：dks 左聯倉頡輔助碼（讀 dks + 倉頡）；
		//   ② dkn：每碼取首尾（讀 dks）；
		//   ③ dks_phrase：造詞（讀 dks + essay 詞頻）；
		//   ④ 把 dkp/dkz 拷進用戶目錄（Rime 端方案要用）。
		await Task.WhenAll(
			跑兩入一步(P.Dks, P.Cangjie, P.DksV, (a, b, w) => Svc.AttachCangjie(Ctx, a, b, w, Ct), Ct),
			跑一步(P.Dks, P.Dkn, (r, w) => Svc.ToDkn(Ctx, r, w, Ct), Ct),
			跑一步(P.Dks, P.DksPhrase, (r, w) => Svc.MkDksPhrase(Ctx, r, w, Ct), Ct),
			Task.Run(() => {
				System.IO.File.Copy(P.Dkp, P.DkpInUser, true);
				System.IO.File.Copy(P.Dkz, P.DkzInUser, true);
			}, Ct)
		);
	}

	internal static partial IReadOnlyList<str> 產物清單(DksPaths P){
		return new[]{P.Dks, P.DksV, P.Dkn, P.DkpInUser, P.DkzInUser, P.DksPhrase};
	}

	internal static partial void 備目錄(params str[] 目錄){
		foreach(var d in 目錄){
			System.IO.Directory.CreateDirectory(d);
		}
	}
}
