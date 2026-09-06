namespace RimeTools.Shared.Dict.Models;

/// dict.yaml 表頭（`---` 與 `...` 之間）的一項鍵值。
/// 文檔型：值要麼是標量字符串、要麼是字符串列表（如 import_tables / columns），
/// 本模型不丟棄未知鍵，寫出時按原序重寫。
/// 例（dks.dict.yaml 表頭）：
///   name: dks            → 標量
///   import_tables:       → 列表
///     - nonKanji
///     - chineseDict
public sealed record HeaderItem(
	/// 鍵名（如 "name" / "sort" / "import_tables"）。
	str Key,
	/// 標量值（如 "dks"）；列表型時為 null。
	str? Scalar,
	/// 列表值（如 ["nonKanji", "chineseDict"]）；標量型時為 null。
	IReadOnlyList<str>? List
);