namespace Dks.Core.Tables.CsTbl;

using System.Globalization;
using System.Text;
using Tsinswreng.CsCtx;

/// 綜合字表源產出服務：`.cs`／`.csproj` 文本的組裝，以及生成端的校驗與對帳。
/// 生成端只管「文本對不對」；跑起來之後的 JSON 校驗在生成出來的工程那一側（見 <see cref="CsTbl模板"/>）。
public partial class SvcCsTbl{
	/// 方法在塊內的排序權重（＝CsTbl方法.產出序 的下標）。
	private static readonly IReadOnlyDictionary<CsTbl方法Def, i32> 方法序 = Mk方法序();

	private static IReadOnlyDictionary<CsTbl方法Def, i32> Mk方法序(){
		var 表 = new Dictionary<CsTbl方法Def, i32>();
		for(var i = 0; i < CsTbl方法.產出序.Count; i++){
			表[CsTbl方法.產出序[i]] = i;
		}
		return 表;
	}

	// ---- 塊內排序 ----

	/// 排一個字條的調用順序：方法按產出序；**同一方法內部保持來源表行序**。
	/// 用 LINQ OrderBy（穩定排序）而不是 List.Sort（不穩定）：dkp 裏 三 的兩行必須維持 sˁəm 在前、srum 在後。
	private static void 排塊內序(CsTbl字條 字條){
		if(字條.調用.Count < 2){
			return;
		}
		var 排 = 字條.調用.OrderBy(z => 方法序[z.方法]).ToList();
		字條.調用.Clear();
		字條.調用.AddRange(排);
	}

	// ---- 校驗與對帳 ----

	/// 生成端校驗。這裡只放「抽取器本身難以保證」的整表級檢查；
	/// 逐行的檢查（欄數、單字、空音）貼在各抽取器裏，能直接帶出源檔行號。
	private static void 抽全表校驗(
		SortedDictionary<str, CsTbl字條> 字表, IReadOnlyList<抽取帳> 帳s, List<CsTbl問題> 問題
	){
		// 1) 來源檔讀不出正文：多半是 dict.yaml 少了 `...` 文檔頭結束標記，或檔根本是空的。
		//    這種情況必須報，否則會產出一張「少了一家」的表而無人察覺。
		foreach(var 帳 in 帳s){
			if(帳.資料行 == 0){
				問題.Add(new CsTbl問題(帳.來源, "", "讀不到任何資料行（來源檔為空，或 dict.yaml 缺 `...` 文檔頭標記）"));
			}
		}
		// 2) 字欄必須是單字（抽取器已逐行檢查，這裡再兜一層，防以後改抽取器時漏掉）。
		foreach(var 字條 in 字表.Values){
			if(!是單字(字條.字)){
				問題.Add(new CsTbl問題("產出", 字條.字, "Z 的鍵不是單字"));
			}
			if(字條.調用.Count == 0){
				問題.Add(new CsTbl問題("產出", 字條.字, "字條沒有任何調用"));
			}
		}
	}

	/// 把問題清單收成一段可直接讀的訊息：按出處分組，逐條列出（不截斷、不抽樣）。
	private static str Mk問題訊息(IReadOnlyList<CsTbl問題> 問題){
		var 文 = new StringBuilder();
		文.Append("綜合字表源產出失敗，共 ").Append(問題.Count).Append(" 條問題（未寫出任何檔案）：");
		foreach(var 組 in 問題.GroupBy(z => z.出處)){
			文.Append(換行).Append("【").Append(組.Key).Append("】").Append(組.Count()).Append(" 條");
			foreach(var 問 in 組){
				文.Append(換行).Append("  ").Append(問.字.Length > 0 ? 問.字 + "：" : "").Append(問.原因);
			}
		}
		return 文.ToString();
	}

	/// 產出報告的備註：把「這份表怎麼讀」與「哪些東西刻意沒做」寫清楚，避免使用者日後誤讀資料。
	private static IReadOnlyList<str> Mk備註(
		SortedDictionary<str, CsTbl字條> 字表, IReadOnlyList<抽取帳> 帳s
	){
		var 備註 = new List<str>{
			"同一個字的多個讀音＝同一個 Z 塊內多次調用（來源表一行一條）；不自創任何分隔符。",
			"空欄位不列出：某欄沒出現＝該字在該欄無值。",
			"字塊按字碼位（序數）排序，產出穩定、可 diff。",
		};
		// 完全相同的調用（同字同方法同值同附欄）：**不去重**，只報數——去重會讓「來源表本來就有兩行」這件事消失。
		var 重複 = 0;
		foreach(var 字條 in 字表.Values){
			var 見 = new HashSet<str>(StringComparer.Ordinal);
			foreach(var 調 in 字條.調用){
				var 鍵 = Mk調用鍵(調);
				if(!見.Add(鍵)){
					重複++;
				}
			}
		}
		if(重複 > 0){
			備註.Add($"完全相同的調用 {重複} 條（同字、同方法、同值、同附欄）：保留原樣未去重。");
		}
		foreach(var 帳 in 帳s){
			if(帳.跳過 > 0){
				var 尾 = 帳.跳過樣本.Count > 0 ? "；樣本：" + str.Join("、", 帳.跳過樣本) : "";
				備註.Add($"{帳.來源}：跳過 {帳.跳過} 行（{帳.跳過說明}{尾}）。");
			}
			if(帳.可疑字頭.Count > 0){
				備註.Add($"{帳.來源}：非漢字字頭（照收，建議核來源）：{str.Join("、", 帳.可疑字頭)}。");
			}
		}
		return 備註;
	}

	/// 一條調用的比較鍵，只用來偵測「完全相同的調用」。
	private static str Mk調用鍵(CsTbl調用 調){
		var 文 = new StringBuilder();
		文.Append(調.方法.名).Append('\u0001').Append(調.值 ?? "").Append('\u0001');
		foreach(var 欄 in 調.附欄){
			文.Append(欄.名).Append('\u0002').Append(欄.是數值 ? "N" : "S").Append(欄.值).Append('\u0001');
		}
		return 文.ToString();
	}

	// ---- 寫 .cs ----

	/// 寫一段文字。`TextWriter` 沒有 `WriteAsync(string, CT)` 這個重載（只有 `WriteAsync(ReadOnlyMemory<char>, CT)`），
	/// 故所有寫出都統一走這裡，免得每個調用點各自 `AsMemory()`。
	private static Task 寫文(TextWriter 寫, str 文, CT Ct){
		return 寫.WriteAsync(文.AsMemory(), Ct);
	}

	/// 寫 `Program.cs`：**只放數據**——檔頭 → 每字一個 Z 塊 → 收尾的導出調用。
	/// 導出調用為甚麼跟數據同檔：它必須是 top-level 語句，而 C# 一個編譯單元只允許**一支**檔案有 top-level 語句，
	/// 那一支就是這裡（結構那一支 `Dsl.cs` 只放型別宣告）。
	/// 逐塊寫出、不整份拼成一個大字串：這張表有兩萬多字，全部拼起來再寫會多吃一份內存。
	private static async Task<nil> 寫Program(
		IFnCtx Ctx, str 表名, SortedDictionary<str, CsTbl字條> 字表, TextWriter 寫, CT Ct
	){
		// step 1: 檔頭（產出者、約定、using）。
		await 寫文(寫, MkProgram檔頭(表名), Ct);
		// step 2: 數據。
		var 塊 = new StringBuilder(1024);
		foreach(var 字條 in 字表.Values){
			Ct.ThrowIfCancellationRequested();
			塊.Clear();
			MkZ塊(字條, 塊);
			await 寫文(寫, 塊.ToString(), Ct);
		}
		// step 3: 收尾的導出調用（top-level 語句，只能住這一支）。
		await 寫文(寫, $"{換行}Global.導出Json({Mk字串(表名)} + \".json\");{換行}", Ct);
		return NIL;
	}

	/// `Program.cs` 的檔頭。這裡刻意**不寫版本號**：schemaVer 的唯一出處是 `Dsl.cs` 裏的 `Global.SchemaVer`，
	/// 兩處都寫就會有一天對不上。
	private static str MkProgram檔頭(str 表名){
		var 文 = new StringBuilder();
		文.Append("// 綜合字表源（數據）——由 RimeTools 的 CsTbl 命令產出；勿手改，重跑會被覆蓋").Append(換行);
		文.Append("// 表名：").Append(表名).Append(換行);
		文.Append("// 結構定義與運行時在 Dsl.cs（Global／CbArg 及各 Cb 類、JSON 導出、校驗）。").Append(換行);
		文.Append("// 數據結構版本見 Global.SchemaVer（與導出 JSON 頂層的 schemaVer 同一個值）。").Append(換行);
		文.Append("// 約定：").Append(換行);
		文.Append("// - 同一個字的多個讀音＝同一個 Z 塊內多次調用（來源表一行一條），不自創分隔符。").Append(換行);
		文.Append("// - 空欄位不列出：某欄沒出現＝該字在該欄無值。").Append(換行);
		文.Append("// - 來源：dkp／zyenpheng／廣韻形聲考主字表／白沙官方 TSV／OC_msoeg／OC_schuesslerOC／OC_zhengzhang／qgsg_stn／廣韻字表 Dzih。").Append(換行);
		文.Append("// 用法：在本目錄執行 dotnet run，即導出 ").Append(表名).Append(".json；校驗不過不會落盤。").Append(換行);
		文.Append(換行);
		文.Append("using static Global;").Append(換行);
		文.Append(換行);
		return 文.ToString();
	}

	// ---- 寫 Dsl.cs（結構：定義與運行時） ----

	/// 寫 `Dsl.cs`：**只放結構**——檔頭 ＋ 定義與運行時模板（收集器、JSON 導出、校驗、schemaVer）。
	/// 與數據分檔的用意：手工登記只動 `Program.cs`，`Dsl.cs` 可以整支替換而不碰數據。
	private static async Task<nil> 寫Dsl(IFnCtx Ctx, str 表名, TextWriter 寫, CT Ct){
		await 寫文(寫, MkDsl檔頭(表名), Ct);
		await 寫文(寫, CsTbl模板.定義模板, Ct);
		return NIL;
	}

	/// `Dsl.cs` 的檔頭：`using System.Text;`（StringBuilder 用；ImplicitUsings 不含 System.Text）＋角色說明。
	private static str MkDsl檔頭(str 表名){
		var 文 = new StringBuilder();
		文.Append("// 綜合字表源的 DSL 定義與運行時（收集器、JSON 導出、校驗、schemaVer）").Append(換行);
		文.Append("// 由 RimeTools 的 CsTbl 命令產出；勿手改，重跑會被覆蓋").Append(換行);
		文.Append("// 表名：").Append(表名).Append(換行);
		文.Append("// 數據在 Program.cs：每字一個 Z 塊；同一個字的多個讀音＝同一個 Z 塊內多次調用。").Append(換行);
		文.Append(換行);
		文.Append("using System.Text; // StringBuilder（ImplicitUsings 不含 System.Text）").Append(換行);
		文.Append(換行);
		return 文.ToString();
	}

	/// 產生一個 `Z("字", a=>{ … });` 塊。
	/// 三種方法形對應三種寫法：單值寫一行；值加附欄在有附欄時才展開成 Cb；純附欄一律展開成 Cb。
	private static void MkZ塊(CsTbl字條 字條, StringBuilder 文){
		文.Append("Z(").Append(Mk字串(字條.字)).Append(", a=>{").Append(換行);
		foreach(var 調 in 字條.調用){
			var 名 = 調.方法.名;
			if(調.方法.形 == ECsTbl方法形.純附欄){
				文.Append(縮).Append("a.").Append(名).Append("(b=>{").Append(換行);
				Mk附欄(調, 文);
				文.Append(縮).Append("});").Append(換行);
				continue;
			}
			if(調.方法.形 == ECsTbl方法形.值加附欄 && 調.附欄.Count > 0){
				文.Append(縮).Append("a.").Append(名).Append('(').Append(Mk字串(調.值!)).Append(", b=>{").Append(換行);
				Mk附欄(調, 文);
				文.Append(縮).Append("});").Append(換行);
				continue;
			}
			// 單值；以及「值加附欄但沒有附欄」退化成的單值寫法。
			文.Append(縮).Append("a.").Append(名).Append('(').Append(Mk字串(調.值!)).Append(");").Append(換行);
		}
		文.Append("});").Append(換行);
	}

	/// 產生 Cb 裏的附欄調用。數值欄不加引號（`b.Ratio(10000);`），字串欄走 C# 字串字面量。
	private static void Mk附欄(CsTbl調用 調, StringBuilder 文){
		foreach(var 欄 in 調.附欄){
			文.Append(縮).Append(縮).Append("b.").Append(欄.名).Append('(');
			if(欄.是數值){
				文.Append(欄.值);
			} else {
				文.Append(Mk字串(欄.值));
			}
			文.Append(");").Append(換行);
		}
	}

	/// 把一個值寫成 C# 字串字面量（含引號）。
	/// 一律用普通字面量加轉義、不用原始字串字面量：值裏可能出現 `"`、`\`、換行（註釋就是多行的），
	/// 普通字面量的轉義是確定的，不必去猜幾個引號當界符才安全。
	private static str Mk字串(str 值){
		var 文 = new StringBuilder(值.Length + 2);
		文.Append('"');
		foreach(var c in 值){
			switch(c){
				case '"':
					文.Append("\\\"");
					break;
				case '\\':
					文.Append("\\\\");
					break;
				case '\n':
					文.Append("\\n");
					break;
				case '\r':
					文.Append("\\r");
					break;
				case '\t':
					文.Append("\\t");
					break;
				default:
					if(char.IsControl(c)){
						文.Append("\\u").Append(((i32)c).ToString("x4", CultureInfo.InvariantCulture));
					} else {
						文.Append(c);
					}
					break;
			}
		}
		文.Append('"');
		return 文.ToString();
	}

	// ---- 寫 .csproj ----

	/// 寫 `.csproj`：零外部依賴的 net10.0 主控台工程，`cd` 進來 `dotnet run` 即導出 JSON。
	private static async Task<nil> 寫Csproj(IFnCtx Ctx, str 表名, TextWriter 寫, CT Ct){
		var 文 = str.Format(CultureInfo.InvariantCulture, CsTbl模板.Csproj, 表名);
		await 寫文(寫, 文, Ct);
		return NIL;
	}
}
