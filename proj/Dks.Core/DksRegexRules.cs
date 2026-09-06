namespace Dks.Core;

using RimeTools.Tools;

/// Dks 方案的正則替換規則表。
/// 規則內容移植自 ngaq 的 TS 源碼（saffesToOcRegex.ts / ocToOc3.ts / cangjie.ts）與
/// 數據文件（聲符.txt / 義符.txt）；以內嵌資源形式隨 Dks.Core 分發，AOT 下亦可用。
public static partial class DksRegexRules{
	/// saffes → OC 轉換規則（對應 ngaq saffesToOcRegex.ts）。
	public static partial IReadOnlyList<RegexReplacePair> SaffesToOc();

	/// OCR 拼音 → OC3 轉換規則（對應 ngaq ocToOc3.ts）。
	public static partial IReadOnlyList<RegexReplacePair> OcToOc3();

	/// 倉頡碼整理規則（對應 ngaq cangjie.ts）。
	public static partial IReadOnlyList<RegexReplacePair> Cangjie();

	/// 聲符規則（對應 ngaq 聲符.txt，用於 dkz 表生成）。
	public static partial IReadOnlyList<RegexReplacePair> ShengFu();

	/// 義符規則（對應 ngaq 義符.txt，用於 dkz 表生成）。
	public static partial IReadOnlyList<RegexReplacePair> YiFu();
}