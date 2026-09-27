namespace Dks.Core;

using RimeTools.Shared.Dict.Parser;
using RimeTools.Shared.Freq;

/// Dks 流水線的配置：輸入/輸出路徑 + 可換的存儲後端（默認內存實現）。
/// 典型使用（對應 Dks.sh 的默認路徑）：
///   new DksCfg() → UserDataDir = "D:/Program Files/Rime/User_Data"
///                 SrcTableDir = "e:/_code/ngaq/src/backend/dict/原表"
///                 WordFreq 指向 UserDataDir/essay.txt
/// 若調用方改 UserDataDir/SrcTableDir，需同步改 WordFreq（或整體用對象初始化器重設）。
public sealed partial class DksCfg{
	/// 默認 Rime 用戶目錄（對應 Dks.sh 的 rime_dir）。
	public const str DefaultUserDataDir = "D:/Program Files/Rime/User_Data";

	/// 默認原表目錄（對應 ngaq 的原表目錄）。
	public const str DefaultSrcTableDir = "e:/_code/ngaq/src/backend/dict/原表";

	/// 默認布之道原表路徑（Dks2 的輸入側）。
	public const str Default布之道DictPath = "D:/Program Files/Rime/User_Data/OC_msoegDK.dict.yaml";

	/// Rime 用戶目錄：dks/dkn/dks_v/dkp/dkz/dks_phrase 的輸出目標目錄；essay.txt 的默認所在目錄。
	/// 例：D:/Program Files/Rime/User_Data。
	public str UserDataDir { get; set; }

	/// 原表目錄：saffes/dkp/dkz/cangjie 等源碼表所在目錄（流水線的輸入側）。
	/// 例：e:/_code/ngaq/src/backend/dict/原表。
	public str SrcTableDir { get; set; }

	/// 布之道（msoeg 體系）原表路徑：Dks2 的輸入側。
	/// 該表碼欄形如 `ASCII*IPA`（例：包	prXu*prˤu），轉換只取 `*` 之後的 IPA 段。
	/// 例：D:/Program Files/Rime/User_Data/OC_msoegDK.dict.yaml。
	public str 布之道DictPath { get; set; }

	/// dict.yaml 解析器（可換；默認內存實現 DictYamlParser）。
	public IDictYamlParser Parser { get; set; }

	/// dict.yaml 寫出器（可換；默認內存實現 DictYamlWriter）。
	public IDictYamlWriter Writer { get; set; }

	/// 詞頻源（可換；默認讀 UserDataDir/essay.txt 的內存實現 EssayWordFreqSource）。
	public IWordFreqSource WordFreq { get; set; }

	/// 構造默認配置：路徑用 DefaultUserDataDir/DefaultSrcTableDir，後端用默認內存實現。
	public partial DksCfg();
}