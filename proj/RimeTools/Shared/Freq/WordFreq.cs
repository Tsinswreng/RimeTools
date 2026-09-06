namespace RimeTools.Shared.Freq;

/// 詞頻條目：詞/字 與其頻率。
public sealed record WordFreq(
	/// 詞或字。
	str Text,
	/// 頻率數值。
	long Freq
);