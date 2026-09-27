namespace RimeTools.Shared.Phrase;

using RimeTools.Tools;

/// 造詞策略：取每字碼的首尾兩字符（對應 cli-tools 的 PhraseMker_HeadEtTail）。
/// 例：MkPhrase(["ryt","qdk"]) → ["rt","qk"]。
/// 單字碼（CodesPerChar 只有一項）不成詞 → 返回空表；
/// 空碼/空串字 跳過（不產片段）。
public partial class PhraseMkr_HeadEtTail:IPhraseMkr{
	/// 見 IPhraseMkr.MkPhrase。
	public partial IReadOnlyList<str> MkPhrase(IReadOnlyList<str> CodesPerChar, IReadOnlyList<str>? Chars);
}