namespace RimeTools.Shared.Freq;

/// IWordFreqSource 的默認文件實現：讀取 essay 詞頻文件（每行「詞\t頻率」，忽略空行）。
/// 本實現把全部條目載入內存後按頻率降序枚舉（essay.txt 約 4MB，內存可行）。
public partial class EssayWordFreqSource : IWordFreqSource{
	/// essay 文件的路徑。
	public str Path { get; set; }

	/// 指定 essay 文件路徑構建。
	public partial EssayWordFreqSource(str Path);

	/// 枚舉全部詞頻條目（頻率降序）。流只能消費一遍。
	public partial IAsyncEnumerable<WordFreq> Enumerate(CT Ct);
}