namespace RimeTools.Tools;

/// 正則替換工具：把一組規則依序應用於字符串（對應 ngaq 的 preprocess/RegexReplacePair 流水線）。
public static partial class RegexReplacer{
	/// 依序套用全部規則（每條規則對整個字符串做全表替換），返回最終結果。
	public static partial str ApplyAll(str Input, IReadOnlyList<RegexReplacePair> Rules);
}