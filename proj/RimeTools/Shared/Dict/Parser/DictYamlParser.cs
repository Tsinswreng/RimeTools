namespace RimeTools.Shared.Dict.Parser;

using RimeTools.Shared.Dict.Models;

/// IDictYamlParser 的默認實現：解析 Rime dict.yaml（表頭 `---`/`...` 段 + Tab 分列正文）。
/// 純文本適配：只讀傳入的 TextReader，不碰文件系統；正文流續讀同一 Reader（單趟掃描，不再重開文件）。
/// 例（對 dks.dict.yaml 解析，真實格式）：
///   #20260818214532           ← 文件頂註釋，跳過
///   ---
///   name: dks
///   version: ""
///   sort: by_weight
///   use_preset_vocabulary: true
///   import_tables:
///     - nonKanji
///     - chineseDict
///   ...
///   辣	ryt	10000%           ← 行尾可能殘留一個 tab，拆分時忽略尾空列
/// 產出 Header{Items=[(name,"dks"),(version,""),(sort,"by_weight"),(use_preset_vocabulary,"true"),(import_tables,[nonKanji,chineseDict])]}，
/// Body 首行 IDictLine{text:"辣", code:"ryt", weight:"10000%"}（正文不解析 YAML，僅按 Tab 拆列後映射列名）。
/// 表頭標量會還原 YAML 雙引號（與 DictYamlWriter.MkScalar 對稱）：`version: ""` 讀回來是空串而不是兩個引號。
public partial class DictYamlParser : IDictYamlParser{
	/// 從 Reader 解析；正文流續讀同一 Reader，故 Reader 在 Body 消費完前不得釋放。
	/// <param name="Reader">dict.yaml 文本源。</param>
	/// <param name="Ct">取消令牌。</param>
	public partial Task<RimeDictDoc> Parse(TextReader Reader, CT Ct);
}
