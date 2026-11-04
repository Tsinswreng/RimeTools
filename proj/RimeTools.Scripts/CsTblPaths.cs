namespace RimeTools.Scripts;

using static System.IO.Path;

/// CsTbl 命令的路徑與名字集合。
/// **只有端點知道路徑**：Dks.Core 的 SvcCsTbl 只認 TextReader/TextWriter，完全不碰文件系統。
///
/// 默認值對應作者本機的實際佈局：
/// - 兩張 TSV（廣韻形聲考主字表、白沙官方字表）已隨倉庫放在 `<倉庫根>/_CsTbl/src/`；
/// - dkp 是使用者手工維護的原表，在 ngaq 的原表目錄；
/// - 其餘五張在 Rime 的收藏集與 `User_Data/Stuff`（廣韻字表 Dzih.txt 是使用者自己下載放置的）。
internal sealed partial class CsTblPaths{
	/// dkp：Tsinswreng 手工表（TswgOcModed 的來源）。
	internal const str DfltDkp = "e:/_code/ngaq/src/backend/dict/原表/dkp.dict.yaml";

	/// zyenpheng：Polyhedron 中古拼音（PolyMc 的來源；用拼音，不是 IPA）。
	internal const str DfltZyenpheng = "D:/Program Files/Rime/User_Data/zyenpheng.dict.yaml";

	/// RIME_OC_collections 收藏集原件目錄（msoeg／許思萊／鄭張三家都取這裏的原件，不用 Stuff 裏的節本）。
	internal const str Dflt收藏集 = "D:/Program Files/Rime/_Download/RIME_OC_collections-main/RIME_OC_collections-main";

	/// 斯塔羅斯金（qgsg_stn 版；與 starostin.dict.yaml 只差兩行，取沒有加作者表頭的那份）。
	internal const str DfltStaOc = "D:/Program Files/Rime/User_Data/Stuff/qgsg_stn.dict.yaml";

	/// 廣韻字表（韻典網 Dzih.txt，25,333 行）：廣韻意 的來源。
	internal const str Dflt廣韻 = "D:/Program Files/Rime/User_Data/Stuff/Dzih.txt";

	/// 產出檔的預設表名（決定 .cs／.csproj／.json 的檔名）。
	internal const str Dflt表名 = "Dk";

	/// 倉庫根（來自 ISCtx），用來定位隨倉庫走的 `_CsTbl/src/` 那兩張 TSV。
	internal str RootDir{get;set;} = "";

	/// 目標文件夾：命令列第一個參數，**必填**；產出的兩份文本寫在這裏。
	internal str OutDir{get;set;} = "";

	/// 表名：命令列第二個參數（可選）。
	internal str 表名{get;set;} = Dflt表名;

	/// 依 ISCtx 與命令列參數建立路徑集合（缺表名＝用 Dflt表名）。
	internal static partial CsTblPaths FromArgs(ISCtx Ctx, str[] Args);

	// ---- 派生路徑：來源側 ----

	/// 廣韻形聲考主字表（25,253 行、九欄）。
	internal str 形聲考字表 => Combine(RootDir, @"_CsTbl\src\guangyun_xingshengkao_char.tsv");
	/// 白沙官方字表（4,967 行、11 欄）。
	internal str 白沙官方Tsv => Combine(RootDir, @"_CsTbl\src\baxtersagart.tsv");
	/// dkp 原表。
	internal str Dkp => DfltDkp;
	/// zyenpheng。
	internal str Zyenpheng => DfltZyenpheng;
	/// msoeg 2018 擬音（收藏集原件）。
	internal str Msoeg => Combine(Dflt收藏集, "OC_msoeg.dict.yaml");
	/// 許思萊上古音（收藏集原件）。
	internal str Schuessler => Combine(Dflt收藏集, "OC_schuesslerOC.dict.yaml");
	/// 鄭張尚芳上古音（收藏集原件，18500 行／13118 字；Stuff 裏的是節本）。
	internal str Zhengzhang => Combine(Dflt收藏集, "OC_zhengzhang.dict.yaml");
	/// 斯塔羅斯金。
	internal str StaOc => DfltStaOc;
	/// 廣韻字表。
	internal str 廣韻 => Dflt廣韻;

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
