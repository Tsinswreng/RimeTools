namespace RimeTools.Scripts;

/// DksFactPaths 的實現：命令列參數 → 路徑集合；以及真相源存在性檢查。
internal sealed partial class DksFactPaths{
	/// 參數約定：`-i <真相源目錄>`、`-o <產物目錄>`；可任意省略，省略則用默認值。
	/// 不支援位置參數，避免與舊 Dks 的 `[UserDataDir] [SrcTableDir]` 混淆。
	internal static partial DksFactPaths FromArgs(str[] Args){
		var ans = new DksFactPaths();
		for(var i = 0; i < Args.Length; i++){
			switch(Args[i]){
				case "-i":
				case "--in":
					if(i + 1 >= Args.Length){
						throw new ArgumentException("-i 後面需要一個真相源目錄。用法：Dks [-i 真相源目錄] [-o 產物目錄]");
					}
					ans.InDir = Args[++i];
					break;
				case "-o":
				case "--out":
					if(i + 1 >= Args.Length){
						throw new ArgumentException("-o 後面需要一個產物目錄。用法：Dks [-i 真相源目錄] [-o 產物目錄]");
					}
					ans.OutDir = Args[++i];
					break;
				default:
					throw new ArgumentException($"未知參數：{Args[i]}。用法：Dks [-i 真相源目錄] [-o 產物目錄]");
			}
		}
		return ans;
	}

	/// 缺任何一個必需檔就一次列全，避免「修一個、再撞下一個」。
	internal static partial void 要求存在(DksFactPaths P){
		str[] 必需 = [
			P.Dsl, P.Export,
			P.TswgOcModed, P.TswgOc, P.PaasTvLuuq, P.McToOc,
			P.Cangjie, P.Essay,
		];
		var 缺 = new List<str>();
		foreach(var f in 必需){
			if(!File.Exists(f)){
				缺.Add(f);
			}
		}
		if(缺.Count > 0){
			throw new FileNotFoundException("缺以下真相源檔案：\n" + string.Join("\n", 缺));
		}
	}
}
