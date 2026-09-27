namespace RimeTools.Shared.Phrase;

using RimeTools.Tools;

/// 造詞策略：把詞拆出的每字碼列表 → 詞碼片段（每個字在詞碼中佔的片段）。
/// 例（HeadTail 策略，詞「車輛」拆出 車=che1、輛=liang4）：
///   MkPhrase(["che1","liang4"]) → ["c1","l4"]   （每碼取首尾）
/// 返回空表 = 該組字碼不成詞（如單字詞）；由造詞器據此跳過。
/// 策略可換：不同輸入法方案可注入不同規則（對應 cli-tools 的 I_mkPhrase / PhraseMker_HeadEtTail）。
public interface IPhraseMkr{
	/// 給定詞內每字已選定的一組碼（等長於字數），生成詞碼的每字片段。
	/// <param name="CodesPerChar">每字一碼，如 ["che1","liang4"]。</param>
	/// <param name="Chars">詞拆出的字（可為 null 或空表，供需要看字形的策略用）。</param>
	IReadOnlyList<str> MkPhrase(IReadOnlyList<str> CodesPerChar, IReadOnlyList<str>? Chars);
}