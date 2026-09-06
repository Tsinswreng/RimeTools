namespace RimeTools.Shared.Dict.Models;

/// dict.yaml 表頭（`---` 與 `...` 之間）的文檔模型。
/// 文檔型：鍵不固定、出現順序有意義、未知鍵原樣保留——不是固定六字段的關係型 schema。
/// 典型表頭（dks.dict.yaml）：
///   name: dks
///   version: ""
///   sort: by_weight
///   use_preset_vocabulary: true
///   import_tables:
///     - nonKanji
///     - chineseDict
/// 其他可能鍵：columns（dks_phrase: [text, code, weight]）等，一律按 HeaderItem 保序保留。
/// 便捷訪問器（Name/Version/Sort/...）只是常見鍵的快捷方式，等價於 GetScalar/GetList 的查找：
/// 鍵重複時取首項；鍵缺失或型別不符時返回各自缺省值（""/null/false）。
public sealed partial class RimeDictHeader{
	/// 保序鍵值項（寫出時按此順序）。
	private IReadOnlyList<HeaderItem> _Items = [];

	/// 從保序鍵值項構建表頭。Items 的順序即寫出時的順序。
	public partial RimeDictHeader(IReadOnlyList<HeaderItem> Items);

	/// 保序的全部鍵值項，寫出時按此順序。
	public partial IReadOnlyList<HeaderItem> Items{get;}

	/// 按鍵取標量值；無此鍵、該鍵為列表型或重複鍵的後續項時返回 null（取首項標量）。
	public partial str? GetScalar(str Key);

	/// 按鍵取列表值；無此鍵或該鍵為標量型時返回 null（取首項列表）。
	public partial IReadOnlyList<str>? GetList(str Key);

	/// 便捷：name 鍵（碼表名，如 dks / dkn / dks_phrase）；缺省 ""。
	public partial str Name{get;}

	/// 便捷：version 鍵；缺省 ""。
	public partial str Version{get;}

	/// 便捷：sort 鍵（by_weight / by_code）；缺省 null。
	public partial str? Sort{get;}

	/// 便捷：columns 鍵（正文列定義，如 [text, code, weight]）；缺省 null（= Rime 默認 text/code/weight 三列）。
	public partial IReadOnlyList<str>? Columns{get;}

	/// 便捷：use_preset_vocabulary 鍵；缺省 null（Rime 默認 false）。
	public partial bool? UsePresetVocabulary{get;}

	/// 便捷：import_tables 鍵（引入的其他碼表名列表）；缺省 null。
	public partial IReadOnlyList<str>? ImportTables{get;}
}