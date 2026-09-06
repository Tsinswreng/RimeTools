namespace RimeTools.Shared.Dict.Lookup;

using RimeTools.Shared.Dict.Models;

/// ICharCodeLookup 的默認內存實現：把 dict 正文行收集進 「字/詞 → 碼表」字典，查詢走字典。
public partial class MemoryCharCodeLookup : ICharCodeLookup{
	/// 字/詞 → 全部編碼（一行多碼時保序追加）。
	protected Dictionary<str, List<str>> _Codes = new();

	/// 從已解析的 dict 文檔構建索引（消費一遍 Body 流；同名多行 = 多碼）。
	public static partial Task<MemoryCharCodeLookup> FromDocAsync(RimeDictDoc Doc, CT Ct);

	/// 按字/詞查全部編碼；未收錄返回空表。
	public partial IReadOnlyList<str> GetCodes(str Text);
}