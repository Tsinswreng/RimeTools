namespace RimeTools.Shared.Dict.Lookup;

/// 「字/詞 → 編碼」的查詢索引：給一個字/詞，返回它在碼表中的全部編碼。
/// 這是存儲後端的切換點：當前提供內存實現（MemoryCharCodeLookup），
/// 未來可換 CsSql/數據庫實現，業務邏輯只依賴本接口（依賴倒置）。
/// 例（dks_v.dict.yaml 收錄「辣 rytYl」與「辣 rytyl」兩行——同字不同碼）：
///   GetCodes("辣") → ["rytYl", "rytyl"]  （同名多行即多碼，保序）
///   GetCodes("不存在的字") → []         （空表，不拋異常）
public interface ICharCodeLookup{
	/// 按字/詞查全部編碼；未收錄時返回空表（非 null）。
	/// <param name="Text">單字或詞。</param>
	IReadOnlyList<str> GetCodes(str Text);
}