namespace RimeTools.Shared.Freq;

/// 詞頻數據源。
/// 這是存儲後端的切換點：當前從 essay.txt 讀取（內存實現），未來可換 CsSql/數據庫實現。
/// 詞頻流按頻率降序提供——需求方（如造詞器）依此決定輸出順序。
public interface IWordFreqSource{
	/// 枚舉全部詞頻條目，按頻率降序。流只能消費一遍。
	IAsyncEnumerable<WordFreq> Enumerate(CT Ct);
}