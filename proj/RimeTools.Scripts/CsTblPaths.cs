namespace RimeTools.Scripts;

using static System.IO.Path;

/// CsTbl 命令的路徑與名字集合。
/// **只有端點知道路徑**：Dks.Core 的 SvcCsTbl 只認 TextReader/TextWriter，完全不碰文件系統。
///
/// 事實源的位置不在本類：九張來源表的目錄與檔名集中在 `CsTblFacts`，
/// 本類只負責「這次要讀哪幾張表、產物寫到哪個文件夾」。
/// 故搬動事實源時不必動本檔，也不必動任何註釋。
internal sealed partial class CsTblPaths{
	/// 產出檔的預設表名（決定 .cs／.csproj／.json 的檔名）。
	internal const str Dflt表名 = "Dk";

	/// 目標文件夾：命令列第一個參數，**必填**；產出的三份文本寫在這裏。
	internal str OutDir{get;set;} = "";

	/// 表名：命令列第二個參數（可選）。
	internal str 表名{get;set;} = Dflt表名;

	/// 依命令列參數建立路徑集合（缺表名＝用 Dflt表名）。
	internal static partial CsTblPaths FromArgs(str[] Args);

	// ---- 來源側：路徑一律經 CsTblFacts.SrcPath 取得，本類不寫死任何目錄 ----

	/// 廣韻形聲考主字表（單字、九欄；Ptl 的來源）。
	internal str 形聲考字表 => CsTblFacts.SrcPath(CsTblFacts.File形聲考);
	/// 白-沙官方字表（11 欄；BsOc 的來源）。
	internal str 白沙官方Tsv => CsTblFacts.SrcPath(CsTblFacts.File白沙);
	/// dkp 手工表（TswgOcModed 的來源）。
	internal str Dkp => CsTblFacts.SrcPath(CsTblFacts.FileDkp);
	/// Polyhedron 中古全拼（PolyMc 的來源）。
	internal str Zyenpheng => CsTblFacts.SrcPath(CsTblFacts.FilePolyMc);
	/// msoeg 上古擬音（MsoegOc 的來源）。
	internal str Msoeg => CsTblFacts.SrcPath(CsTblFacts.FileMsoeg);
	/// 許思萊上古音（SchOc 的來源）。
	internal str Schuessler => CsTblFacts.SrcPath(CsTblFacts.FileSchuessler);
	/// 鄭張尚芳上古音（DtOc 的來源）。
	internal str Zhengzhang => CsTblFacts.SrcPath(CsTblFacts.FileZhengzhang);
	/// 斯塔羅斯金（StaOc 的來源）。
	internal str StaOc => CsTblFacts.SrcPath(CsTblFacts.FileStaOc);
	/// 廣韻全字表＋釋義（廣韻意 的來源）。
	internal str 廣韻 => CsTblFacts.SrcPath(CsTblFacts.File廣韻);

	// ---- 派生路徑：產出側 ----

	/// 產出的 `Program.cs`：**只放數據**——每字一個 `Z("字", a=>{…})` 塊，以及收尾的導出調用
	/// （導出調用必須是 top-level 語句，而 top-level 語句只能住 data 這一支，故它跟數據同檔）。
	/// 檔名固定 `Program.cs`，不跟表名走：這是 C# 專案裏放主流程的慣例名。
	internal str OutProgram => Combine(OutDir, "Program.cs");

	/// 產出的 `Dsl.cs`：**只放結構**——DSL 定義（`Global`／`CbArg` 及各 Cb 類）與運行時
	/// （收集器、JSON 導出、校驗、schemaVer）。與數據分檔，是為了手工登記時只動 Program.cs、
	/// 讓定義那支保持穩定、可整支替換。
	internal str OutDsl => Combine(OutDir, "Dsl.cs");

	/// 產出的 `.csproj`（零依賴主控台工程）。
	internal str OutCsproj => Combine(OutDir, 表名 + ".csproj");

	/// 使用者 `cd` 進目標文件夾 `dotnet run` 之後會得到的 JSON。
	internal str OutJson => Combine(OutDir, 表名 + ".json");
}
