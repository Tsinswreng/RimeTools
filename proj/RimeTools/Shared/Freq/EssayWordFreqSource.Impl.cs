namespace RimeTools.Shared.Freq;

public partial class EssayWordFreqSource : IWordFreqSource{
	public partial EssayWordFreqSource(str Path){
		this.Path = Path;
	}

	public partial IAsyncEnumerable<WordFreq> Enumerate(CT Ct){
		return EnumerateImpl(Ct);
	}

	/// 內部實現：整讀文件 → 解析每行「詞\t頻率」 → 內存排序降序 → 流式產出。
	private async IAsyncEnumerable<WordFreq> EnumerateImpl([System.Runtime.CompilerServices.EnumeratorCancellation] CT Ct){
		// step 1: 逐行讀入並解析。
		var list = new List<WordFreq>();
		using var reader = new StreamReader(Path, new System.Text.UTF8Encoding(false));
		str? line;
		while((line = await reader.ReadLineAsync(Ct)) is not null){
			var trimmed = line.Trim();
			if(trimmed.Length == 0){
				continue;
			}
			var tabIdx = trimmed.IndexOf('\t');
			if(tabIdx < 0){
				continue; // 無 tab 的行視為髒行跳過
			}
			var text = trimmed[..tabIdx];
			if(!long.TryParse(trimmed[(tabIdx + 1)..], out var freq)){
				continue;
			}
			list.Add(new WordFreq(text, freq));
		}

		// step 2: 頻率降序排序後逐條產出（流只能消費一遍）。
		list.Sort((a, b) => b.Freq.CompareTo(a.Freq));
		foreach(var wf in list){
			Ct.ThrowIfCancellationRequested();
			yield return wf;
		}
	}
}