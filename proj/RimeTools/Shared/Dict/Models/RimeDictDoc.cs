namespace RimeTools.Shared.Dict.Models;

/// dict.yaml 的解析結果：表頭 + 正文行流。
/// Body 是惰性流，只能消費一遍；需要多次訪問時應先物化成列表。
public sealed record RimeDictDoc(
	/// 表頭元數據。
	RimeDictHeader Header,
	/// 正文行流（惰性，僅可消費一次）。
	IAsyncEnumerable<DictLine> Body
);