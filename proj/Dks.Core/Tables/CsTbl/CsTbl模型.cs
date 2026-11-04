namespace Dks.Core.Tables.CsTbl;

/// 「綜合字表源」DSL 的方法形狀：決定這個方法在產出的 C# 文本裏寫成哪一種調用。
/// 三種形狀恰好覆蓋 Dk.cs 定義裏的三類方法：
/// - 單值：只有一個位置參數。例：`a.TswgOc("wəʔ");`、`a.廣韻意("榮華又姓漢有榮啓期永兵切六");`
/// - 值加附欄：位置參數 + 可選的 Cb。附欄為空時退化為單值寫法。例：`a.TswgOcModed("wuʔ", b=>{ b.Note("ts"); b.Ratio(10000); });`
/// - 純附欄：沒有位置參數，所有欄位都在 Cb 裏。例：`a.Ptl(b=>{ b.聲符("又"); b.諧聲域("WƏ"); });`
public enum ECsTbl方法形{
	單值,
	值加附欄,
	純附欄,
}

/// 一個 DSL 方法（＝一家來源在綜合字表源裏的落點）的定義。
/// 集中定義的目的：產出器與 JSON 導出都只認這些常量，方法名與欄名不散落各處（禁止魔法字符串）。
/// 名 必須與 Dk.cs 定義裏的方法名逐字相同——產出時是把它當 C# 代碼原樣寫進 .cs 的。
/// 值鍵 是 JSON 裏「位置參數」用的鍵名（只有單值／值加附欄才有）；純附欄為 null。
public sealed record CsTbl方法Def(
	str 名,
	ECsTbl方法形 形,
	str? 值鍵
);

/// 各家的附欄名。值必須與 Dk.cs 定義裏的 Cb 方法名逐字相同。
public static class CsTbl欄名{
	/// 來源表裏的原註釋：dkp 的行內 `#` 註釋與上方緊鄰註釋行；許思萊的兩處節標題。
	public const str Note = nameof(Note);
	/// 白沙官方的上古音欄。注意它在 Cb 裏（`b.Oc(...)`），不是方法的位置參數。
	public const str Oc = nameof(Oc);
	/// 白沙官方的英文釋義欄。
	public const str Gloss = nameof(Gloss);
	/// 權重。**數值欄**：產出時不加引號，寫成 `Ratio(10000)`。
	public const str Ratio = nameof(Ratio);

	// ---- 廣韻形聲考主字表的九欄：字本身是 Z 的鍵，其餘八欄在此 ----

	public const str 聲符 = nameof(聲符);
	public const str 諧聲域 = nameof(諧聲域);
	public const str 記號 = nameof(記號);
	public const str 反切 = nameof(反切);
	public const str 中古地位 = nameof(中古地位);
	public const str 切拼 = nameof(切拼);
	public const str 上古音參考 = nameof(上古音參考);
	public const str 考釋 = nameof(考釋);
}

/// DSL 方法的全集，以及同一個 Z 塊內的固定排列序。
public static class CsTbl方法{
	/// Tsinswreng 上古漢語擬音。**不由 CsTbl 產出**（使用者已定「TswgOc 不填」），
	/// 列在此處只為了產出序完整、以及讓 JSON 的鍵名有單一出處。
	public static readonly CsTbl方法Def TswgOc = new("TswgOc", ECsTbl方法形.單值, "音");
	public static readonly CsTbl方法Def TswgOcModed = new("TswgOcModed", ECsTbl方法形.值加附欄, "音");
	public static readonly CsTbl方法Def PolyMc = new("PolyMc", ECsTbl方法形.值加附欄, "音");
	public static readonly CsTbl方法Def BsOc = new("BsOc", ECsTbl方法形.純附欄, null);
	public static readonly CsTbl方法Def SchOc = new("SchOc", ECsTbl方法形.值加附欄, "音");
	public static readonly CsTbl方法Def MsoegOc = new("MsoegOc", ECsTbl方法形.單值, "音");
	public static readonly CsTbl方法Def StaOc = new("StaOc", ECsTbl方法形.單值, "音");
	public static readonly CsTbl方法Def DtOc = new("DtOc", ECsTbl方法形.單值, "音");
	public static readonly CsTbl方法Def Ptl = new("Ptl", ECsTbl方法形.純附欄, null);
	public static readonly CsTbl方法Def 廣韻意 = new("廣韻意", ECsTbl方法形.單值, "釋義");

	/// 同一個 Z 塊內多次調用的排列順序＝Dk.cs 定義裏的聲明序（使用者看慣的順序）。
	/// 同一方法內部的順序＝來源表的行序（例如 dkp 裏 三 的兩行，sˁəm 在前、srum 在後）。
	public static readonly IReadOnlyList<CsTbl方法Def> 產出序 = new[]{
		TswgOc, TswgOcModed, PolyMc, BsOc, SchOc, MsoegOc, StaOc, DtOc, Ptl, 廣韻意,
	};
}
