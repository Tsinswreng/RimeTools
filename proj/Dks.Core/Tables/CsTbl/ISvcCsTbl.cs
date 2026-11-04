namespace Dks.Core.Tables.CsTbl;

using Tsinswreng.CsCtx;

/// 綜合字表源（DSL）的產出服務。
/// 職責：讀九張來源表 → 抽成「字 → 各家讀音與欄位」→ 校驗 → 寫出**三份文本**：
/// 1. `Program.cs`＝**只放數據**：每字一個 `Z("字", a=>{ … })` 塊，收尾一行導出調用。
/// 2. `Dsl.cs`＝**只放結構**：DSL 定義（`Global`／`CbArg` 及各 Cb 類）與運行時（收集器、JSON 導出、校驗、schemaVer）。
/// 3. `.csproj`＝零外部依賴的 net10.0 主控台工程；使用者 `cd` 進該目錄 `dotnet run` 即導出 JSON。
///
/// 設計約定（沿用 RimeTools 既有分層）：
/// - **本接口不認路徑、不碰文件系統**：輸入是九個 TextReader，輸出是三個 TextWriter；
///   開檔、建目錄、決定檔名一律由端點（RimeTools.Scripts 的 CsTbl 命令）負責。
/// - **產出前校驗**：任何問題都不落盤，拋 InvalidOperationException 並列出全部問題。
/// - 家的集合與方法名以 CsTbl來源集／CsTbl方法 為單一出處，不在此重複列舉。
///
/// 例（端點怎麼用）：
/// `using var p = new StringWriter(); using var d = new StringWriter(); using var j = new StringWriter();`
/// `await svc.產出(ctx, 來源, "Dk", p, d, j, ct);`
public partial interface ISvcCsTbl{
	/// 產出綜合字表源的三份文本。
	/// <param name="Ctx">函數上下文袋（Svc 規範：首參一律 IFnCtx）。</param>
	/// <param name="來源">九張來源表的 reader 集合。</param>
	/// <param name="表名">
	/// 表名：用於 `.csproj` 的 AssemblyName／RootNamespace，以及生成程式的導出檔名（`<表名>.json`）。
	/// `Program.cs`／`Dsl.cs` 的檔名固定，不跟表名走。
	/// </param>
	/// <param name="ProgramCs">接收 `Program.cs`（數據）文本的寫者。</param>
	/// <param name="DslCs">接收 `Dsl.cs`（結構：定義與運行時）文本的寫者。</param>
	/// <param name="Csproj">接收 `.csproj` 文本的寫者。</param>
	/// <param name="Ct">取消令牌（最後一個參數為 CT ⇒ 異步方法）。</param>
	/// <returns>產出統計：字數、調用數、各來源的對帳（讀了幾行、產出幾條、跳過幾行與原因）。</returns>
	Task<CsTbl產出報告> 產出(
		IFnCtx Ctx, CsTbl來源集 來源, str 表名,
		TextWriter ProgramCs, TextWriter DslCs, TextWriter Csproj,
		CT Ct
	);
}
