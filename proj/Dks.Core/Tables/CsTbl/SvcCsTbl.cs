namespace Dks.Core.Tables.CsTbl;

using System.Text;
using Tsinswreng.CsCtx;

/// <summary>
/// 綜合字表源（DSL）產出服務的實現。
/// 見 <see cref="ISvcCsTbl"/> 的職責與約定；實現分三個 partial 檔：
/// - 本檔：主流程（抽取帳 → 逐表抽取 → 校驗 → 寫兩份文本）。
/// - `SvcCsTbl.抽取.cs`：九家來源表的抽取器與共用小工具。
/// - `SvcCsTbl.輸出.cs`：`.cs`／`.csproj` 文本的組裝與生成端校驗。
/// </summary>
public partial class SvcCsTbl:ISvcCsTbl{
	/// 產出時每行都用 `\n` 換行：產物要進版本庫、要能跨平臺 diff，
	/// 不能跟著運行環境變成 CRLF（測試也要釘住這件事）。
	private const str 換行 = "\n";

	/// 縮進用一個 Tab：與 Dk.cs 的手寫風格一致，產物貼回 DSL 工程時不會整檔跑格。
	private const str 縮 = "\t";

	// ---- 主流程 ----

	public async Task<CsTbl產出報告> 產出(
		IFnCtx Ctx, CsTbl來源集 來源, str 表名,
		TextWriter ProgramCs, TextWriter DslCs, TextWriter Csproj,
		CT Ct
	){
		// step 1: 逐表抽取。字典用序數排序 ⇒ 產出順序穩定（按碼位），手工登記時好找、好 diff。
		var 字表 = new SortedDictionary<str, CsTbl字條>(StringComparer.Ordinal);
		var 帳s = new List<抽取帳>();

		await 抽TswgOcModed(Ctx, 來源.Dkp, Mk帳(字表, 帳s, "dkp（TswgOcModed）"), Ct);
		await 抽PolyMc(Ctx, 來源.Zyenpheng, Mk帳(字表, 帳s, "zyenpheng（PolyMc）"), Ct);
		await 抽Ptl(Ctx, 來源.廣韻形聲考字表, Mk帳(字表, 帳s, "廣韻形聲考主字表（Ptl）"), Ct);
		await 抽BsOc(Ctx, 來源.白沙官方Tsv, Mk帳(字表, 帳s, "白沙官方 TSV（BsOc）"), Ct);
		await 抽MsoegOc(Ctx, 來源.MsoegOc, Mk帳(字表, 帳s, "OC_msoeg（MsoegOc）"), Ct);
		await 抽SchOc(Ctx, 來源.SchuesslerOc, Mk帳(字表, 帳s, "OC_schuesslerOC（SchOc）"), Ct);
		await 抽DtOc(Ctx, 來源.ZhengzhangOc, Mk帳(字表, 帳s, "OC_zhengzhang（DtOc）"), Ct);
		await 抽StaOc(Ctx, 來源.StarostinOc, Mk帳(字表, 帳s, "qgsg_stn（StaOc）"), Ct);
		await 抽廣韻意(Ctx, 來源.廣韻字表, Mk帳(字表, 帳s, "廣韻字表 Dzih（廣韻意）"), Ct);

		// step 2: 排塊內順序。方法按 CsTbl方法.產出序、同方法內保持抽取順序（＝來源表行序）。
		foreach(var 字條 in 字表.Values){
			排塊內序(字條);
		}

		// step 3: 生成端校驗。有問題就不落盤——先把問題拋出去，兩個 Writer 一個字都不寫。
		var 問題 = new List<CsTbl問題>();
		foreach(var 帳 in 帳s){
			問題.AddRange(帳.問題);
		}
		抽全表校驗(字表, 帳s, 問題);
		if(問題.Count > 0){
			throw new InvalidOperationException(Mk問題訊息(問題));
		}

		// step 4: 寫三份文本：Program.cs（數據）／Dsl.cs（結構：定義與運行時）／.csproj。
		//         順序無所謂（三個 Writer 各自獨立），但先數據後結構方便對照。
		await 寫Program(Ctx, 表名, 字表, ProgramCs, Ct);
		await 寫Dsl(Ctx, 表名, DslCs, Ct);
		await 寫Csproj(Ctx, 表名, Csproj, Ct);

		// step 5: 交報告。
		var 統計 = new List<CsTbl來源統計>();
		foreach(var 帳 in 帳s){
			統計.Add(帳.Mk統計());
		}
		return new CsTbl產出報告(表名, 字表.Count, 帳s.Sum(z => z.調用.Count), 統計, Mk備註(字表, 帳s));
	}

	// ---- 抽取帳：一家來源在抽取過程中累積的調用、對帳與問題 ----

	/// 建立一家來源的抽取帳，並登記進全域清單（抽取器只碰帳，不碰字典以外的東西）。
	private static 抽取帳 Mk帳(
		SortedDictionary<str, CsTbl字條> 字表, List<抽取帳> 帳s, str 來源
	){
		var 帳 = new 抽取帳(字表, 來源);
		帳s.Add(帳);
		return 帳;
	}

	/// 一家來源的抽取帳。
	/// 抽出的一條調用會立刻掛到該字的字條上（沒有對應字條就新建），
	/// 故字典本身就是最終產物，抽取階段不必再存一份中間清單。
	internal sealed class 抽取帳{
		/// 全域字表：字 → 字條。多張來源共用同一個字典，調用會累積到同一個字條上。
		private readonly SortedDictionary<str, CsTbl字條> _字表;
		/// 本家已抽出的調用（與 _字表 裏掛著的是同一批物件，只是為了報告計數與順序而留一份引用）。
		internal List<CsTbl調用> 調用{get;} = new();
		/// 本家讀到的正文資料行數（不含註釋、空行、表頭）。
		internal i32 資料行{get;set;}
		/// 本家跳過的行數。
		internal i32 跳過{get;set;}
		/// 跳過原因的簡述（最後一次為準；同一家通常只有一兩種原因）。
		internal str 跳過說明{get;set;} = "";
		/// 跳過的樣本（最多 5 條原樣字面），供報告的備註示範「到底跳過了甚麼」。
		internal List<str> 跳過樣本{get;} = new();
		/// 非漢字字頭樣本（最多 5 條）：照收不誤，但在報告裏点名，讓使用者去核來源。
		internal List<str> 可疑字頭{get;} = new();

		internal str 來源{get;}

		internal 抽取帳(SortedDictionary<str, CsTbl字條> 字表, str 來源){
			this._字表 = 字表;
			this.來源 = 來源;
		}

		/// 記一次跳過：累加計數、記下原因，並留最多 5 條不重複樣本。
		/// 留樣本的理由：使用者要求「不丟信息」，那至少要看得見被跳過的是些甚麼東西。
		internal void 記跳過(str 原因, str 樣本){
			跳過++;
			跳過說明 = 原因;
			if(樣本.Length > 0 && 跳過樣本.Count < 5 && !跳過樣本.Contains(樣本)){
				跳過樣本.Add(樣本);
			}
		}

		/// 記一條調用：掛到該字的字條上。
		/// 值為 null 且附欄為空時不記（＝該行沒有任何可登記的內容），回傳 false 讓調用方計數。
		internal bool 記(str 字, CsTbl調用 一調用){
			if(一調用.值 is null && 一調用.附欄.Count == 0){
				return false;
			}
			if(!_字表.TryGetValue(字, out var 字條)){
				字條 = new CsTbl字條(字);
				_字表[字] = 字條;
			}
			字條.調用.Add(一調用);
			調用.Add(一調用);
			if(!是漢字(字)){
				var 樣本 = Mk字頭樣本(字);
				if(可疑字頭.Count < 5 && !可疑字頭.Contains(樣本)){
					可疑字頭.Add(樣本);
				}
			}
			return true;
		}

		/// 記一個問題（生成端校驗用；有問題就不落盤）。
		internal void 記問題(str 字, str 原因){
			問題.Add(new CsTbl問題(來源, 字, 原因));
		}

		/// 可疑字頭的顯示形式：私有區（U+E000–F8FF，來源整理者拿它當無碼位字的佔位）與控制字符
		/// 在控制台裏只顯示成一片空白，故改成標碼位，讓人看得見到底是甚麼。
		private static str Mk字頭樣本(str 字){
			foreach(var r in 字.EnumerateRunes()){
				if((r.Value >= 0xE000 && r.Value <= 0xF8FF) || r.Value < 0x20){
					return $"U+{r.Value:X4}";
				}
			}
			return 字;
		}

		internal List<CsTbl問題> 問題{get;} = new();

		/// 依帳面數字收攏成一條對帳統計。
		internal CsTbl來源統計 Mk統計(){
			return new CsTbl來源統計(來源, 資料行, 調用.Count, 跳過, 跳過說明);
		}
	}
}
