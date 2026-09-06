namespace RimeTools.Shared.Dict.Lookup;

/// 「字/詞 → 編碼」的查詢索引。
/// 這是存儲後端的切換點：當前提供內存實現，未來可換 CsSql/數據庫實現，業務邏輯只依賴本接口。
public interface ICharCodeLookup{
	/// 按單字/詞查全部編碼；未收錄時返回空表。
	IReadOnlyList<str> GetCodes(str Text);
}