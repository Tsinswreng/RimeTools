namespace RimeTools.Shared.Dict.Models;

/// dict.yaml 表頭（`---` 與 `...` 之間的鍵值段）的欄位。
/// 只收錄 Rime 碼表實際用到的鍵；未出現的鍵以 null 表示，寫出時省略。
public sealed record RimeDictHeader(
	/// name：碼表名。
	str Name,
	/// version：版本，常為 ""。
	str Version,
	/// sort：排序方式，如 by_weight / by_code。
	str? Sort,
	/// columns：列定義，如 ["text", "code", "weight"]；缺省時 Rime 用默認三列。
	IReadOnlyList<str>? Columns,
	/// use_preset_vocabulary：是否使用預置詞表。
	bool? UsePresetVocabulary,
	/// import_tables：引入的其他碼表名列表。
	IReadOnlyList<str>? ImportTables
);