namespace RimeTools.Shared.Freq;

/// IWordFreqSource 的默認文件實現：讀取 essay 詞頻文件並按頻率降序枚舉。
/// 文件格式：每行「詞\t頻率」，忽略空行；詞可含中文字符（〇〇）或標點等任意文本。
/// 例（essay.txt 真實行）：
///   〇	981
///   〇〇	658
/// 本實現把全部條目載入內存後排序（essay.txt 約 4MB、數十萬行，內存可行）。
public partial class EssayWordFreqSource : IWordFreqSource{
	/// essay 文件的路徑（構造時指定，如 UserDataDir/essay.txt）。
	public str Path { get; set; }

	/// 指定 essay 文件路徑構建。
	/// <param name="Path">essay.txt 絕對路徑。</param>
	public partial EssayWordFreqSource(str Path);

	/// 枚舉全部詞頻條目（頻率降序）。流只能消費一遍；文件不存在時拋出異常。
	public partial IAsyncEnumerable<WordFreq> Enumerate(CT Ct);
}