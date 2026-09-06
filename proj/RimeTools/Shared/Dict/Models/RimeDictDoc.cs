namespace RimeTools.Shared.Dict.Models;

/// dict.yaml 的整體模型：表頭（文檔型鍵值）+ 正文行流。
/// 例（dks.dict.yaml 本體）：
///   ---
///   name: dks
///   version: ""
///   sort: by_weight
///   ...
///   辣	ryt	10000%
///   一	y	10000%
/// Header 即 `---`/`...` 之間的表頭段；Body 為表頭之後每行一個 IDictLine（文檔型：列名→值字典）的惰性流。
/// Body 只能消費一遍；需要多次訪問時應先物化成列表。
public sealed record RimeDictDoc(
	/// 表頭文檔（保序鍵值）。
	RimeDictHeader Header,
	/// 正文行流（惰性，僅可消費一次）。每行以 IDictLine 呈現：text/code/weight 等列名即字典鍵。
	IAsyncEnumerable<IDictLine> Body
);