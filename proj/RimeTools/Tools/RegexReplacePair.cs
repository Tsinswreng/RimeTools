namespace RimeTools.Tools;

/// 正則替換規則一對：Pattern 匹配、Replacement 替換。
/// 對應 ngaq TS 的 RegexReplacePair（regex/replacement 一對）、RimeToolsOld 的 RegexReplacePair。
/// 例（真實規則，來自 saffesToOcRegex.ts）：
///   把 g 統一成 ɡ      → RegexReplacePair{Pattern="g", Replacement="ɡ"}（ocToOc3.ts）
///   把 ˤ 統一成 ˁ      → RegexReplacePair{Pattern="ˤ", Replacement="ˁ"}（ocToOc3.ts）
///   倉頡字根「冫」→ im  → RegexReplacePair{Pattern="冫", Replacement="im"}（cangjie.ts）
/// 注意：Pattern 是 .NET 正則語法；$1 等捕獲引用在 Replacement 中生效。
public sealed record RegexReplacePair(
	/// 正則表達式（.NET 語法，如 "g" 或 "(.)(.)"）。
	str Pattern,
	/// 替換文本（支持 $1 等捕獲引用，如 "首一$1尾二"）。
	str Replacement
);