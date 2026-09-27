namespace RimeTools.Tools;

/// 笛卡爾積工具：多組候選各取其一，窮舉全部組合。
/// 例：Cartesian.Product([["a","b"], ["1"]]) →
///     [["a","1"], ["b","1"]]。
public static partial class Cartesian{
	/// 對多組候選求笛卡爾積；任一分組為空表時結果為空表。
	/// <param name="Groups">每組候選（組數 ≥ 0）。</param>
	public static partial IReadOnlyList<IReadOnlyList<str>> Product(IReadOnlyList<IReadOnlyList<str>> Groups);
}