namespace RimeTools.Tools;

/// 正則替換工具：把一組規則依序應用於字符串。
/// 對應 ngaq 的 preprocess(regexReplacePair 流水線)：規則按序套用、前一條的輸出是後一條的輸入。
/// 例（saffesToOcRegex 的前兩步）：
///   Input="'A'Q" 規則1 {Pattern="'", Replacement=""} → "AQ"
///   再套規則2 {Pattern="(.)(.)(.)", Replacement="首一$1首二腹一$2腹二尾一$3尾二"} → "首一A首二腹一Q腹二尾一"（第三字符缺失時不匹配）
/// 規則與順序即音變規則表，順序敏感——不能打亂。
public static partial class RegexReplacer{
	/// 依序套用全部規則（每條規則對整個字符串做全表替換），返回最終結果。
	/// <param name="Input">原始字符串（如 saffes 碼 "AQ"）。</param>
	/// <param name="Rules">按序應用的規則表，前一條輸出是後一條輸入。</param>
	public static partial str ApplyAll(str Input, IReadOnlyList<RegexReplacePair> Rules);
}