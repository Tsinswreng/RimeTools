namespace RimeTools.Shared.Dict.Lookup;

using RimeTools.Shared.Dict.Models;

/// ICharCodeLookup 的默認內存實現：把 dict 正文行收集進「字/詞 → 碼表」字典，查詢走字典。
/// 例（dks_v.dict.yaml 真實兩行，同一字兩碼）：
///   辣	rytYl	10000%
///   辣	rytyl	10000%
/// 構建後 _Codes["辣"] = ["rytYl", "rytyl"]。
public partial class MemoryCharCodeLookup : ICharCodeLookup{
	/// 字/詞 → 全部編碼（同名多行時保序追加）。
	protected Dictionary<str, List<str>> _Codes = new();

	/// 從已解析的 dict 文檔構建索引。消費一遍 Body 流；同名多行即一詞多碼（如「辣」兩碼 rytYl/rytyl）。
	/// <param name="Doc">解析自 dict.yaml 的文檔。</param>
	/// <param name="Ct">取消令牌。</param>
	public static partial Task<MemoryCharCodeLookup> FromDocAsync(RimeDictDoc Doc, CT Ct);

	/// 按字/詞查全部編碼；未收錄返回空表。見 ICharCodeLookup.GetCodes。
	public partial IReadOnlyList<str> GetCodes(str Text);
}