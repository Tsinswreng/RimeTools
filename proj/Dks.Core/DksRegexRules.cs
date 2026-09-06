namespace Dks.Core;

using RimeTools.Tools;

/// Dks 方案的正則替換規則表。
/// 規則內容移植自 ngaq 的 TS 源碼（saffesToOcRegex.ts / ocToOc3.ts / cangjie.ts）與
/// 數據文件（聲符.txt / 義符.txt）；以內嵌資源形式隨 Dks.Core 分發，AOT 下亦可用。
/// 規則全部是「字面正則替換對」，順序敏感（前一條輸出是後一條輸入），由 RegexReplacer.ApplyAll 依序套用。
public static partial class DksRegexRules{
	/// saffes → OC 轉換規則（對應 ngaq saffesToOcRegex.ts）。
	/// 例（真實規則）：把碼先拆成 首/腹/尾 三段再逐段變音，如
	///   {Pattern="'", Replacement=""} —— 去除撇號
	///   {Pattern="首一(Y)首二腹一([QTIPHYCN])腹二尾一(.*?)尾二", Replacement="首一j首二腹一$2腹二尾一$3尾二"}
	/// 輸入是大寫的 saffes 碼，輸出仍是「首X腹X尾X」佔位串。
	public static partial IReadOnlyList<RegexReplacePair> SaffesToOc();

	/// OCR 拼音 → OC3 轉換規則（對應 ngaq ocToOc3.ts）。
	/// 例（真實規則）：
	///   {Pattern="g", Replacement="ɡ"}   —— 濁 g
	///   {Pattern="dz", Replacement="從"}   —— 先佔位再換回
	///   {Pattern="ˁ(æ|A)", Replacement="腹一.腹二"}
	/// 輸入是 saffesToDkz 輸出的佔位串，輸出是帶音標的 OC3 碼（可含 ˁ æ 等 IPA 字符）。
	public static partial IReadOnlyList<RegexReplacePair> OcToOc3();

	/// 倉頡碼整理規則（對應 ngaq cangjie.ts）。
	/// 例（真實規則）：把倉頡字根/拆碼中的部件替換成對應字母，如
	///   {Pattern="冫", Replacement="im"}
	///   {Pattern="門", Replacement="A"}
	/// 用於 AttachCangjie 前把倉頡碼標準化（字根→字母映射）。
	public static partial IReadOnlyList<RegexReplacePair> Cangjie();

	/// 聲符規則（對應 ngaq 聲符.txt，用於 dkz 表生成）。
	/// 文件格式「字\t音」，例：奴→ˁa、睪→æk、羊→æŋ。
	/// 讀入後轉成 RegexReplacePair{Pattern=字, Replacement=音}，用於把 dkp 表音替換成對應聲符音。
	public static partial IReadOnlyList<RegexReplacePair> ShengFu();

	/// 義符規則（對應 ngaq 義符.txt，用於 dkz 表生成）。
	/// 文件格式「字\t音」，例：手→nʰ、火→mʰ、木→mʰ。
	/// 讀入後轉成 RegexReplacePair{Pattern=字, Replacement=音}，用於把 dkp 表音替換成對應義符音。
	public static partial IReadOnlyList<RegexReplacePair> YiFu();
}