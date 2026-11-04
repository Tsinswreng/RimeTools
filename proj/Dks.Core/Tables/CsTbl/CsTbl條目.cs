namespace Dks.Core.Tables.CsTbl;

/// 一個附欄＝一次 Cb 調用的實參。例：`b.Note("ts")`、`b.Ratio(10000)`。
public sealed class CsTbl欄{
	/// 附欄名。必須取自 CsTbl欄名 的常量（禁止在抽取器裏硬寫字串）。
	public str 名{get;}
	/// 附欄值。數值欄存原值字串（如 "10000"），此處不做任何數值換算——
	/// 換算與解釋留給使用者的 DSL 定義，避免抽取器偷改數值。
	public str 值{get;}
	/// 是否數值欄：true 時產出不加引號（Ratio），false 時寫成 C# 字串字面量。
	public bool 是數值{get;}

	public CsTbl欄(str 名, str 值, bool 是數值 = false){
		this.名 = 名;
		this.值 = 值;
		this.是數值 = 是數值;
	}
}

/// 一條要寫進 DSL 的調用＝來源表的一行。
/// 例：dkp 的 `三	srum	# bs` ⇒ 方法 TswgOcModed、值 "srum"、附欄 [Note("bs")]。
public sealed class CsTbl調用{
	/// 這條調用打在哪個 DSL 方法上。
	public CsTbl方法Def 方法{get;}
	/// 位置參數（單值／值加附欄才有）；純附欄為 null。
	public str? 值{get;}
	/// 附欄，保持加入順序（產出時照此順序寫）。
	public IReadOnlyList<CsTbl欄> 附欄{get;}

	public CsTbl調用(CsTbl方法Def 方法, str? 值, IReadOnlyList<CsTbl欄>? 附欄 = null){
		this.方法 = 方法;
		this.值 = 值;
		this.附欄 = 附欄 ?? Array.Empty<CsTbl欄>();
	}
}

/// 一個字條＝該字的全部讀音與欄位。
/// 同一家的多個讀音就是多條調用（使用者定的：同一 API 在同一塊內重複調用，不自創分隔符）。
public sealed class CsTbl字條{
	public str 字{get;}
	/// 該字的全部調用；順序由產出器統一排（方法按 CsTbl方法.產出序、同方法內按來源表行序）。
	public List<CsTbl調用> 調用{get;} = new();

	public CsTbl字條(str 字){
		this.字 = 字;
	}
}

/// 一次產出所需的九張來源表。
/// 設計約定：**庫不認路徑、不碰文件系統**——開檔關檔由端點（RimeTools.Scripts 的 CsTbl 命令）負責，
/// 故此處只收 TextReader。九張表＝使用者拍板的來源清單，逐一對應 Dk.cs 定義裏的一個方法。
public sealed class CsTbl來源集{
	/// dkp.dict.yaml（原表目錄）：Tsinswreng 上古漢語魔改擬音 + 權重 + 註釋。
	public required TextReader Dkp{get;init;}
	/// zyenpheng.dict.yaml：Polyhedron 中古拼音（拼音，非 IPA）+ 權重。
	public required TextReader Zyenpheng{get;init;}
	/// 廣韻形聲考主字表 TSV（25253 行、九欄）：Ptl 的八個附欄。
	public required TextReader 廣韻形聲考字表{get;init;}
	/// 白沙官方字表 TSV（11 欄）：BsOc 的上古音與英文釋義，括註進 Note。
	public required TextReader 白沙官方Tsv{get;init;}
	/// msoeg 2018 擬音：MsoegOc（碼欄形如 `ASCII*IPA（漢拼）`，只取 IPA 段）。
	public required TextReader MsoegOc{get;init;}
	/// 許思萊上古音：SchOc（碼欄形如 `ASCII*IPA`，只取 IPA 段）。
	public required TextReader SchuesslerOc{get;init;}
	/// 鄭張尚芳上古音：DtOc（碼欄形如 `ASCII*IPA`，只取 IPA 段；多音分行）。
	public required TextReader ZhengzhangOc{get;init;}
	/// 斯塔羅斯金上古音：StaOc（純 IPA，無 `*` 分隔；空音行很多）。
	public required TextReader StarostinOc{get;init;}
	/// 廣韻字表 Dzih.txt（空格分隔，第 2/3 欄是韻典網自編的小韻序號與組內序，使用者已定不收）：
	/// 廣韻意 的釋義。
	public required TextReader 廣韻字表{get;init;}
}

/// 生成端（CsTbl 命令）的校驗問題。只要存在任何一條，就**不落盤**並把全部問題列出來，
/// 沿用 DkzToDks 的既有做法（不合格不留半成品）。
public sealed record CsTbl問題(
	/// 問題出在哪張來源表。
	str 出處,
	/// 相關的字（無關時為空串）。
	str 字,
	/// 問題原因，要能讓人直接定位與修資料。
	str 原因
);

/// 單一來源的對帳統計。使用者要求「不丟信息」，所以要看得見每張表讀了多少行、
/// 產出多少條調用、跳過多少行以及為甚麼跳過。
public sealed record CsTbl來源統計(
	str 來源,
	i32 資料行,
	i32 產出調用,
	i32 跳過,
	str 跳過說明
);

/// 一次產出的統計報告，供端點打印（也是對帳憑據）。
public sealed record CsTbl產出報告(
	str 表名,
	i32 字數,
	i32 調用數,
	IReadOnlyList<CsTbl來源統計> 來源統計,
	IReadOnlyList<str> 備註
);
