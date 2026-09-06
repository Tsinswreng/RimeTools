namespace Dks.Core;

using RimeTools.Shared.Dict.Parser;
using RimeTools.Shared.Freq;

/// Dks 流水線的配置：輸入/輸出路徑 + 可換的存儲後端（默認內存實現）。
/// 典型使用（對應 Dks.sh 的默認路徑）：
///   UserDataDir = "D:/Program Files/Rime/User_Data"
///   SrcTableDir = "e:/_code/ngaq/src/backend/dict/原表"
public sealed partial class DksCfg{
	/// Rime 用戶目錄：dks/dkn/dks_v/dkp/dkz/dks_phrase 的輸出目標目錄；essay.txt 的默認所在目錄。
	/// 例：D:/Program Files/Rime/User_Data。與 Dks.sh 的 cd 目標一致。
	public str UserDataDir { get; set; }

	/// 原表目錄：saffes/dkp/dkz/cangjie 等源碼表所在目錄（流水線的輸入側）。
	/// 例：e:/_code/ngaq/src/backend/dict/原表。
	public str SrcTableDir { get; set; }

	/// dict.yaml 解析器（可換；默認內存實現 DictYamlParser）。
	public IDictYamlParser Parser { get; set; }

	/// dict.yaml 寫出器（可換；默認內存實現 DictYamlWriter）。
	public IDictYamlWriter Writer { get; set; }

	/// 詞頻源（可換；默認讀 UserDataDir/essay.txt 的內存實現 EssayWordFreqSource）。
	public IWordFreqSource WordFreq { get; set; }

	/// 構造默認配置：Parser/Writer 用默認內存實現，WordFreq 指向 UserDataDir/essay.txt。
	/// 需先用相對路徑填 UserDataDir/SrcTableDir 再調用；路徑在構造後可改。
	public partial DksCfg();
}