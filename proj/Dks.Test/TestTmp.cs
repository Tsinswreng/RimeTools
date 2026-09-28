namespace Dks.Test;

/// 測試用的臨時文件位置。
/// 一律落在**工作區內**：測試程序集輸出目錄（如 proj/Dks.Test/bin/Debug/net10.0）下的 `_tmp/`，
/// 不用系統 %TEMP%（免得到處留垃圾、也免得跨盤/權限問題）。
public static class TestTmp{
	/// 取（並創建）一個帶標籤的臨時目錄，如 `<輸出目錄>/_tmp/roundTrip`。
	public static str MkDir(str Tag){
		var dir = System.IO.Path.Combine(AppContext.BaseDirectory, "_tmp", Tag);
		System.IO.Directory.CreateDirectory(dir);
		return dir;
	}

	/// 取一個臨時文件路徑（所在目錄順手建好），如 `<輸出目錄>/_tmp/essay/essay.txt`。
	public static str MkPath(str Tag, str FileName){
		return System.IO.Path.Combine(MkDir(Tag), FileName);
	}
}
