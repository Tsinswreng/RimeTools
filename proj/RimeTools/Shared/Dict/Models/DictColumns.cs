namespace RimeTools.Shared.Dict.Models;

/// dict.yaml 正文的默認三列列名（Rime 沒寫 `columns` 時的語義）。
/// 解析器、寫出器、反查索引、造詞器都按這三個名字取列，故集中在這裏：
/// 名字散落各處時，改一處漏一處就會出現「寫出的表讀不回」這類問題。
/// 例：dks 的一行「辣	ryt	10000%」= Text="辣"、Code="ryt"、Weight="10000%"。
public static class DictColumns{
	/// 詞或字。
	public const str Text = "text";

	/// 編碼（dks 的三拼碼、dks_phrase 的詞碼）。
	public const str Code = "code";

	/// 權重（"10000%" 或詞頻數值）。
	public const str Weight = "weight";

	/// 默認三列、按此序（供表頭 columns 與正文列序的缺省值用）。
	public static readonly IReadOnlyList<str> Default = [Text, Code, Weight];
}
