namespace Dks.Core;

using RimeTools.Shared.Dict.Parser;
using RimeTools.Shared.Freq;

/// Dks 流水線的配置：輸入/輸出路徑 + 可換的存儲後端（默認內存實現）。
public sealed partial class DksCfg{
	/// Rime 用戶目錄（dks/dkn/dks_v/dkp/dkz/dks_phrase 的輸出目標目錄）。
	public str UserDataDir { get; set; }

	/// 原表目錄（saffes/dkp/dkz/cangjie 等源碼表所在目錄）。
	public str SrcTableDir { get; set; }

	/// dict.yaml 解析器（可換；默認內存實現）。
	public IDictYamlParser Parser { get; set; }

	/// dict.yaml 寫出器（可換；默認內存實現）。
	public IDictYamlWriter Writer { get; set; }

	/// 詞頻源（可換；默認讀 UserDataDir/essay.txt 的內存實現）。
	public IWordFreqSource WordFreq { get; set; }

	/// 構造默認配置：Parser/Writer 用默認內存實現，WordFreq 指向 UserDataDir/essay.txt。
	public partial DksCfg();
}