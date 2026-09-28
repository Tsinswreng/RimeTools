namespace RimeTools.Scripts;

using static System.IO.Path;

/// 端點層的路徑集合：**只有端點知道路徑**（Dks.Core 只認 reader/writer）。
/// 默認值對應作者本機的 Dks.sh 佈局；任一命令都可用參數覆寫其中任意幾項。
/// 派生屬性把「某一步的輸入/輸出在哪」集中定義，命令只寫流程骨架，不再各自拼路徑（避免路徑散落多處）。
internal sealed partial class DksPaths{
	/// 默認 Rime 用戶目錄（產物 dks/dks_v/dkn/dkz/dks_phrase/dkp 的落點；essay.txt 也在這）。
	internal const str DfltUserDataDir = "D:/Program Files/Rime/User_Data";

	/// 默認原表目錄（輸入側：saffes/dkp/cangjie 等）。
	internal const str DfltSrcTableDir = "e:/_code/ngaq/src/backend/dict/原表";

	/// 默認布之道（msoeg 體系）擬音原表：Dks2/Dks3/Dks4 的輸入側，碼欄形如 `ASCII*IPA`。
	internal const str Dflt布之道Dict = "D:/Program Files/Rime/User_Data/OC_msoegDK.dict.yaml";

	/// 用於比對的 cangjie 表檔名（含五六相異者）。
	internal const str CangjieFileName = "cangjie7-1含五六相異者.yaml";

	/// Rime 用戶目錄。
	internal str UserDataDir{get;set;} = DfltUserDataDir;

	/// 原表目錄。
	internal str SrcTableDir{get;set;} = DfltSrcTableDir;

	/// 布之道擬音原表（完整路徑，不在 SrcTableDir 下）。
	internal str 布之道Dict{get;set;} = Dflt布之道Dict;

	/// 依命令列參數覆寫（依序：UserDataDir、SrcTableDir、布之道Dict；缺省或空串＝沿用默認）。
	internal static partial DksPaths FromArgs(str[] Args);

	// ---- 派生路徑：輸入側 ----

	/// saffes 原表（舊 Dks 流程的源頭）。
	internal str Saffes => Combine(SrcTableDir, "saffes.dict.yaml");
	/// dkp 手工聲符義符表（兩條流程都用作「最優先覆蓋」）。
	internal str Dkp => Combine(SrcTableDir, "dkp.dict.yaml");
	/// 倉頡表（造 dks_v 的輔助碼來源）。
	internal str Cangjie => Combine(SrcTableDir, CangjieFileName);
	/// dkz 中間表（全拼形態；既讀又寫，端點用「臨時檔＋改名」避免讀寫同檔）。
	internal str Dkz => Combine(SrcTableDir, "dkz.dict.yaml");
	/// 布之道擬音原表。
	internal str 布之道 => 布之道Dict;

	// ---- 派生路徑：產物側 ----

	/// dks 主表（三拼碼；整條流水線的核心產物）。
	internal str Dks => Combine(UserDataDir, "dks.dict.yaml");
	/// dks_v（dks + 倉頡輔助碼，供反查/輔助輸入）。
	internal str DksV => Combine(UserDataDir, "dks_v.dict.yaml");
	/// dkn（每碼取首尾的雙碼表）。
	internal str Dkn => Combine(UserDataDir, "dkn.dict.yaml");
	/// dks_phrase（自動造詞表）。
	internal str DksPhrase => Combine(UserDataDir, "dks_phrase.dict.yaml");
	/// 拷貝到用戶目錄的 dkp（Rime 端 dkp 方案要用）。
	internal str DkpInUser => Combine(UserDataDir, "dkp.dict.yaml");
	/// 拷貝到用戶目錄的 dkz（Rime 端 dkz 方案要用）。
	internal str DkzInUser => Combine(UserDataDir, "dkz.dict.yaml");
	/// 詞頻源（essay.txt，造詞與按字頻擇源的權重來源）。
	internal str Essay => Combine(UserDataDir, "essay.txt");
}
