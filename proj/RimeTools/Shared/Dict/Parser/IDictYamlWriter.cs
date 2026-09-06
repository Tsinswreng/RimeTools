namespace RimeTools.Shared.Dict.Parser;

using RimeTools.Shared.Dict.Models;

/// dict.yaml 寫出器：把表頭與正文行流寫成 Rime 碼表文件。
/// 表頭按 Items 原序輸出（標量寫 `鍵: 值`、列表寫 `鍵:` 後逐項 `  - 項`）。
/// 正文以 Header.Columns（缺省 text/code/weight）為列序，逐行從 IDictLine 取對應鍵拼 Tab 輸出——
/// 字典枚舉順序不參與寫出；行的尾列缺失時不補空 tab（如「一	qdkm;」無 weight 鍵照樣兩格輸出）。
/// 例（寫出 dks_phrase.dict.yaml，真實格式）：
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
public interface IDictYamlWriter{
	/// 寫出一個 dict.yaml 文件。Header 寫為表頭（`---` 起、`...` 收），Body 逐條寫為正文行。
	/// Body 是惰性流、只能消費一遍；消費完畢前文件句柄保持打開。
	/// <param name="Path">目標文件絕對路徑；目錄不存在時自動創建。</param>
	/// <param name="Header">表頭文檔（保序鍵值）。</param>
	/// <param name="Body">正文行流。</param>
	/// <param name="Ct">取消令牌。</param>
	Task<nil> Write(str Path, RimeDictHeader Header, IAsyncEnumerable<IDictLine> Body, CT Ct);
}