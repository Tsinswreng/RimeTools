namespace RimeTools.Tools;

/// 正則替換規則一對：Pattern 匹配、Replacement 替換（ngaq 的 RegexReplacePair 對應物）。
public sealed record RegexReplacePair(
	/// 正則表達式。
	str Pattern,
	/// 替換文本（支持 $1 等捕獲引用）。
	str Replacement
);