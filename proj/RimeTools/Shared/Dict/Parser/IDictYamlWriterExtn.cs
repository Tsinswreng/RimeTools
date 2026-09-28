namespace RimeTools.Shared.Dict.Parser;

using RimeTools.Shared.Dict.Models;

/// `IDictYamlWriter` 的路徑版簡便寫法（端點與測試用）。
/// 成員只認 `TextWriter`（流式、無文件系統概念）；建目錄、開檔、關檔是這裏額外補的一層，
/// 屬「寫法上的簡化」而非能力上的擴展，故按規範放在擴展類而非接口成員。
/// 例：端點要寫 User_Data/dks.dict.yaml 時用它，目標目錄不存在時自動創建。
public static class IDictYamlWriterExtn{
	/// 開檔寫出：自動建目錄，寫完（或中途異常/取消）自動關檔。
	/// <param name="z">寫出器實現。</param>
	/// <param name="Path">目標文件路徑。</param>
	/// <param name="Header">表頭文檔。</param>
	/// <param name="Body">正文行流（只能消費一遍）。</param>
	/// <param name="Ct">取消令牌。</param>
	public static async Task<nil> Write(
		this IDictYamlWriter z, str Path, RimeDictHeader Header,
		IAsyncEnumerable<IDictLine> Body, CT Ct){
		// step 1: 目標目錄不存在則創建（端點的雜務，不進庫）。
		var dir = System.IO.Path.GetDirectoryName(Path);
		if(!string.IsNullOrEmpty(dir)){
			System.IO.Directory.CreateDirectory(dir);
		}
		// step 2: 開檔 → 交給成員寫 → finally 關檔（異常與取消也關，不留半截句柄）。
		using var writer = new StreamWriter(Path, false, new System.Text.UTF8Encoding(false));
		await z.Write(writer, Header, Body, Ct);
		return NIL;
	}
}
