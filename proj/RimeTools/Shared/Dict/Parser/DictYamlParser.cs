namespace RimeTools.Shared.Dict.Parser;

using RimeTools.Shared.Dict.Models;

/// IDictYamlParser 的默認文件實現：解析 Rime dict.yaml（表頭 `---`/`...` 段 + 表格正文）。
/// 表頭按「鍵:值」逐行讀入，值為 `- ` 前綴的行歸入上一鍵的列表；
/// 正文按 Tab 拆列後，以 Header.Columns（缺省 text/code/weight）為列名建 IDictLine 字典。
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
public partial class DictYamlParser : IDictYamlParser{
	/// 解析一個 dict.yaml 文件。文件不存在或格式錯誤時拋出異常。
	public partial Task<RimeDictDoc> Parse(str Path, CT Ct);
}