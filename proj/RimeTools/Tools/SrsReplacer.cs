namespace RimeTools.Tools;

/// 串替換規則引擎: 執行 ngaq 規則鏈的「逐條全表替換」語義, 不依賴 System.Text.RegularExpressions。
/// 語義等價: JS `str.replace(/pattern/g, replacement)` 依序套用整張規則表。
/// Pattern 受限語法(ngaq 三份規則實測全集):
///   - 字面文本(含「首一/首二/腹一/腹二/尾一/尾二」標記、IPA 字符 ˁ æ ŋ 等)
///   - `.` 任意單字符
///   - `[...]` 純字符集(字符列舉, 無範圍無 `^`), 如 [QTIPHYCN]
///   - `( ... )` 捕獲組, 組內可為: `.`、`[字符集]`、`a|b|c` 交替、`.*`(貪婪)/`.*?`(非貪婪)
///   - `^`/`$` 錨點; `;+` 一次以上(規則中僅此一處)
///   - 組數上限 9(替換 `$1`..`$9`); 組內不支持嵌套組
/// 替換文本支持 `$N` 捕獲引用與字面 `$$`(轉義為單個 `$`)。
/// 例: 規則 {Pattern="(.)(.)(.)", Replacement="首一$1首二腹一$2腹二尾一$3尾二"}
///   對 "pmk" → "首一p首二腹一m腹二尾一k尾二"
public static partial class SrsReplacer{
	/// 依序對 Input 套用全部規則(每條全表替換, 前一條輸出是後一條輸入)。
	/// <param name="Input">要變換的碼串。</param>
	/// <param name="Rules">規則表(順序敏感)。</param>
	public static partial str ApplyAll(str Input, IReadOnlyList<SrRule> Rules);

	/// 單條規則全表替換(等價 JS replace 帶 g)。
	public static partial str ReplaceAll(str Input, SrRule Rule);
}