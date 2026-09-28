namespace RimeTools.Shared.Dict.Parser;

using RimeTools.Shared.Dict.Models;

/// dict.yaml 寫出器：把表頭與正文行流寫成 Rime 碼表**文本**。
/// 本接口只認 `TextWriter`——不認路徑、不建目錄、不開檔；開檔與關檔是端點的事
/// （路徑版簡便寫法見 `IDictYamlWriterExtn.Write(this IDictYamlWriter, str Path, …)`）。
/// 表頭按 Items 原序輸出（標量寫 `鍵: 值`、列表寫 `鍵:` 後逐項 `  - 項`）：
/// 空標量必須寫成 `""`——Rime 的 `DictSettings::LoadDictHeader` 要求 name/version 非 null，
/// 寫成 `version: `（空標量 = YAML null）會被判 `incomplete dict header`、整張表編譯失敗。
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
	/// 把表頭與正文寫進 Writer。Body 是惰性流、只能消費一遍；寫完即返回，不關閉 Writer。
	/// <param name="Writer">目標文本寫出器（生命週期歸調用方）。</param>
	/// <param name="Header">表頭文檔（保序鍵值）。</param>
	/// <param name="Body">正文行流。</param>
	/// <param name="Ct">取消令牌。</param>
	Task<nil> Write(TextWriter Writer, RimeDictHeader Header, IAsyncEnumerable<IDictLine> Body, CT Ct);
}
