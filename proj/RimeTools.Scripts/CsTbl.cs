namespace RimeTools.Scripts;

/// 綜合字表源產出命令（CsTbl）。
/// 做一件事：讀九張來源表，抽出「字 → 各家讀音與欄位」，在**指定文件夾**裏寫出三份文本——
///   ① `Program.cs`：**只放數據**——每字一個 `Z` 塊，收尾一行導出調用（top-level 語句只允許一支檔案，故與數據同檔）；
///   ② `Dsl.cs`：**只放結構**——DSL 定義（`Global`／`CbArg` 及各 Cb 類）與運行時（收集器、JSON 導出、校驗、schemaVer）；
///   ③ `<表名>.csproj`：零外部依賴的 net10.0 主控台工程。
/// 之後使用者 `cd` 到該文件夾 `dotnet run`，即導出 `<表名>.json`（頂層帶 schemaVer；校驗不過不落盤）。
///
/// 參數：`CsTbl &lt;目標文件夾&gt; [表名]`；目標文件夾必填（相對路徑按當前工作目錄解析），表名默認 Dk。
/// 九張來源表的位置集中在 `CsTblFacts`（本命令不在別處散落路徑）。
internal static partial class CsTbl{
	/// Args 是去除命令名後的剩餘參數（0~2 個：目標文件夾、表名）。
	internal static partial Task Main(ISCtx Ctx, str[] Args, CT Ct);
}
