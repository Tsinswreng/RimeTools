namespace RimeTools.Scripts;

/// DksPaths 的實現：命令列參數 → 路徑集合。
internal sealed partial class DksPaths{
	internal static partial DksPaths FromArgs(str[] Args){
		var ans = new DksPaths();
		// step 1: 依序覆寫：Args[0]=UserDataDir、Args[1]=SrcTableDir、Args[2]=布之道Dict。
		//         空串或缺失＝沿用默認（方便只改其中一兩項）。
		if(Args.Length >= 1 && !string.IsNullOrEmpty(Args[0])){
			ans.UserDataDir = Args[0];
		}
		if(Args.Length >= 2 && !string.IsNullOrEmpty(Args[1])){
			ans.SrcTableDir = Args[1];
		}
		if(Args.Length >= 3 && !string.IsNullOrEmpty(Args[2])){
			ans.布之道Dict = Args[2];
		}
		return ans;
	}
}
