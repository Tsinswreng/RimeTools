namespace Dks.Core;

using RimeTools.Shared.Dict.Parser;
using RimeTools.Shared.Freq;

/// Dks 流水線的配置：只放**可換策略**（解析/寫出/詞頻源）。
/// 路徑（UserDataDir/SrcTableDir/布之道表）**不在這裏**——本庫不認路徑，
/// 開檔與關檔由端點（RimeTools.Scripts 的命令）負責，路徑也在那裏定義。
/// 典型使用：
///   var cfg = new DksCfg { WordFreq = new EssayWordFreqSource(essay 路徑) };   // 端點負責給路徑
///   await svc.MkDksPhrase(ctx, dksReader, phraseWriter, ct);                   // 庫只認 reader/writer
public sealed partial class DksCfg{
	/// dict.yaml 解析器（可換；默認純文本實現 DictYamlParser，只吃 TextReader）。
	public IDictYamlParser Parser { get; set; }

	/// dict.yaml 寫出器（可換；默認純文本實現 DictYamlWriter，只吃 TextWriter）。
	public IDictYamlWriter Writer { get; set; }

	/// 詞頻源（可換）。**只有需要詞頻的兩步會用到**：造詞 MkDksPhrase、按字頻擇源 按頻擇源；
	/// 這兩步在它為 null 時拋 InvalidOperationException（錯誤信息點明缺哪個策略），其餘步驟不受影響。
	/// 例：new EssayWordFreqSource("D:/Program Files/Rime/User_Data/essay.txt")。
	public IWordFreqSource? WordFreq { get; set; }

	/// 構造默認配置：解析/寫出用默認實現；詞頻源留給端點注入（端點才知道路徑）。
	public partial DksCfg();
}
