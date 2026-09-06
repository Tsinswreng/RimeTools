namespace RimeTools.Shared.Dict.Parser;

using RimeTools.Shared.Dict.Models;

/// IDictYamlWriter 的默認文件實現：把表頭與正文行流寫成 Rime dict.yaml。
/// 表頭按 Items 原序寫出；正文以 Header.Columns（缺省 text/code/weight）為列序，
/// 逐行從 IDictLine 取鍵拼 Tab，行的尾列缺失時不補空 tab。
/// 例（dks_phrase.dict.yaml，真實格式）：
///   ---
///   name: dks_phrase
///   version: ""
///   sort: by_weight
///   columns:
///     - text
///     - code
///     - weight
///   use_preset_vocabulary: false
///   ...
///   一個	qkkn	374279
/// 對應正文行字典 {text:"一個", code:"qkkn", weight:"374279"}。
public partial class DictYamlWriter : IDictYamlWriter{
	/// 寫出一個 dict.yaml 文件。目標目錄不存在時自動創建。
	public partial Task<nil> Write(str Path, RimeDictHeader Header, IAsyncEnumerable<IDictLine> Body, CT Ct);
}