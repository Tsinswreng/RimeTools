namespace RimeTools.Shared.Freq;

/// 詞頻數據源：按頻率降序提供全部詞頻條目。
/// 這是存儲後端的切換點：當前從 essay.txt 讀取（內存實現 EssayWordFreqSource），
/// 未來可換 CsSql/數據庫實現，消費方（如造詞器）只依賴本接口（依賴倒置）。
/// 例（essay.txt 中按頻降序的頭幾條）：
///   （某字高頻）→ 〇(981) → 〇〇(658) → 〇九(616) → …
/// 降序是因為造詞時先處理高頻詞、詞條輸出亦按頻率排序。
public interface IWordFreqSource{
	/// 枚舉全部詞頻條目，按頻率降序。流只能消費一遍（需要多次消費時先物化）。
	/// <param name="Ct">取消令牌。</param>
	IAsyncEnumerable<WordFreq> Enumerate(CT Ct);
}