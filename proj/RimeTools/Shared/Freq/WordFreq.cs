namespace RimeTools.Shared.Freq;

/// 詞頻條目：詞/字 與其頻率數值。
/// 例（essay.txt 真實行）：「〇」頻率 981 → WordFreq{Text="〇", Freq=981}；
/// 「〇〇」頻率 658 → WordFreq{Text="〇〇", Freq=658}。
public sealed record WordFreq(
	/// 詞或字。
	str Text,
	/// 頻率數值（越大越常用）。
	long Freq
);