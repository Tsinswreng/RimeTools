namespace Dks.Core;

using RimeTools.Tools;

/// Dks 方案的規則表載入器。
/// 規則數據以「每行 pattern<TAB>replacement」的文本資源隨 Dks.Core 嵌入(Rules/ 目錄),
/// 直接取自 ngaq 的 saffesToOcRegex.ts / ocToOc3.ts / cangjie.ts 與 聲符.txt / 義符.txt,
/// 可與 ngaq 源文件逐行 diff, 改規則只動資源不重編碼。
public static partial class DksRegexRules{
	/// saffes → OC 音變規則(對應 ngaq saffesToOcRegex.ts 啟用規則, 485 條)。
	/// 作用於把三拼鍵位碼包成的「首一X首二腹一Y腹二尾一Z尾二」格式, 依序全表替換。
	public static partial IReadOnlyList<SrRule> SaffesToOc();

	/// OC → OC3 音變規則(對應 ngaq ocToOc3.ts 啟用規則, 128 條)。
	/// 承接 SaffesToOc 的輸出, 再做細化與字形統一(g→ɡ、ˤ→ˁ 等)。
	public static partial IReadOnlyList<SrRule> OcToOc3();

	/// 倉頡碼整理規則(對應 ngaq cangjie.ts 啟用規則, 45 條)。
	/// 把倉頡字根/拆碼部件替換成字母(冫→im、門→A 等), 供 dks_v 合併前標準化。
	public static partial IReadOnlyList<SrRule> Cangjie();

	/// 聲符表(對應 ngaq 聲符.txt): 字 → 音(奴→ˁa 等)。用於把碼尾的聲符字形轉成音。
	public static partial IReadOnlyDictionary<str, str> ShengFu();

	/// 義符表(對應 ngaq 義符.txt): 字 → 音(手→nʰ 等)。用於把碼首的義符字形轉成音。
	public static partial IReadOnlyDictionary<str, str> YiFu();

	/// 聲符替換規則: pattern = 字 + `$`(尾錨)。對 dkp 碼尾的聲符字形做音轉。
	public static partial IReadOnlyList<SrRule> ShengFuRules();

	/// 義符替換規則: pattern = `^` + 字(首錨)。對 dkp 碼首的義符字形做音轉。
	public static partial IReadOnlyList<SrRule> YiFuRules();
}