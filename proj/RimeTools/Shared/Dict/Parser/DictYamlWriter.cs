namespace RimeTools.Shared.Dict.Parser;

using RimeTools.Shared.Dict.Models;

/// IDictYamlWriter 的默認實現：把表頭與正文行流寫成 Rime dict.yaml 文本。
/// 純文本適配：只寫傳入的 TextWriter，不碰文件系統（建目錄/開檔見 IDictYamlWriterExtn）。
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
	/// 寫出表頭與正文到 Writer；不關閉 Writer、不建目錄。
	/// <param name="Writer">目標文本寫出器。</param>
	/// <param name="Header">表頭文檔。</param>
	/// <param name="Body">正文行流（只能消費一遍）。</param>
	/// <param name="Ct">取消令牌。</param>
	public partial Task<nil> Write(TextWriter Writer, RimeDictHeader Header, IAsyncEnumerable<IDictLine> Body, CT Ct);
}
