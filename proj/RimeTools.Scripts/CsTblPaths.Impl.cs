namespace RimeTools.Scripts;

/// CsTblPaths 的實現：ISCtx ＋ 命令列參數 → 路徑集合。
internal sealed partial class CsTblPaths{
	/// 參數約定：Args[0]＝目標文件夾（必填）、Args[1]＝表名（可選，空或缺失＝Dflt表名）。
	/// 目標文件夾刻意**不給默認值**：使用者明確要求「輸出時要指定文件夾」，
	/// 免得哪天忘了傳參數就把產物寫到意料之外的地方。
	internal static partial CsTblPaths FromArgs(ISCtx Ctx, str[] Args){
		var ans = new CsTblPaths{
			RootDir = Ctx.RootDir,
		};
		// step 1: 目標文件夾（必填）。缺失時 OutDir 留空，由命令報用法。
		if(Args.Length >= 1 && !string.IsNullOrEmpty(Args[0])){
			ans.OutDir = Args[0];
		}
		// step 2: 表名（可選）。
		if(Args.Length >= 2 && !string.IsNullOrEmpty(Args[1])){
			ans.表名 = Args[1];
		}
		return ans;
	}
}
