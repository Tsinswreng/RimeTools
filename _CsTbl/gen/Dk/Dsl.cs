// 綜合字表源的 DSL 定義與運行時（收集器、JSON 導出、校驗、schemaVer）
// 由 RimeTools 的 CsTbl 命令產出；勿手改，重跑會被覆蓋
// 表名：Dk
// 數據在 Program.cs：每字一個 Z 塊；同一個字的多個讀音＝同一個 Z 塊內多次調用。

using System.Text; // StringBuilder（ImplicitUsings 不含 System.Text）

// ============================================================
// DSL 定義與運行時（本檔＝結構；數據在 Program.cs）
// ============================================================

/// 綜合字表源的收集器與導出器。
public class Global{
	/// 數據結構版本。與導出 JSON 頂層的 schemaVer 欄必須一致：
	/// 導出時寫的就是此常數，讀回 JSON 時（外部消費者）拿它比對，用來擋「表結構改了、手上的卻是舊 JSON」。
	public const string SchemaVer = "1";

	/// 全部字條，順序＝數據裏 Z 塊的出現順序（CsTbl 產出時已按碼位排好）。
	public static readonly List<字條> 字表 = new();

	/// 登記一個字。Z 塊內可以多次調用同一方法（＝該字有多個讀音）。
	public static void Z(string 字, Action<CbArg> Cb){
		var 條 = new 字條(字);
		字表.Add(條);
		Cb(new CbArg(條));
	}

	/// 校驗並導出 JSON。**校驗不過就不落盤**，並把全部問題一次列出（不留半成品）。
	public static void 導出Json(string 檔名){
		// step 1: 校驗。
		var 問題 = new List<string>();
		foreach(var 條 in 字表){
			if(條.字.Length == 0 || 條.字.EnumerateRunes().Count() != 1){
				問題.Add($"字欄不是單字：`{條.字}`");
			}
			foreach(var 調 in 條.調用){
				if(調.方法.Length == 0){
					問題.Add($"字 `{條.字}`：方法名為空");
				}
				foreach(var 欄 in 調.附欄){
					if(欄.名.Length == 0){
						問題.Add($"字 `{條.字}`／{調.方法}：附欄名為空");
					}
					if(欄.是數值 && !Is數值(欄.值)){
						問題.Add($"字 `{條.字}`／{調.方法}／{欄.名}：數值欄不是數字 `{欄.值}`");
					}
				}
			}
		}
		if(問題.Count > 0){
			throw new InvalidOperationException(
				$"綜合字表源校驗不通過（{問題.Count} 條）：" + Environment.NewLine + string.Join(Environment.NewLine, 問題)
			);
		}
		// step 2: 寫 JSON。手寫序列化——本工程零外部依賴，不用 System.Text.Json。
		var 文 = new StringBuilder();
		文.Append("{\n");
		文.Append("  \"schemaVer\": \"").Append(Json轉義(SchemaVer)).Append("\",\n");
		文.Append("  \"字數\": ").Append(字表.Count).Append(",\n");
		文.Append("  \"字表\": {\n");
		for(var i = 0; i < 字表.Count; i++){
			var 條 = 字表[i];
			文.Append("    \"").Append(Json轉義(條.字)).Append("\": [");
			if(條.調用.Count == 0){
				文.Append("],\n");
				continue;
			}
			文.Append('\n');
			for(var j = 0; j < 條.調用.Count; j++){
				var 調 = 條.調用[j];
				文.Append("      { \"方法\": \"").Append(Json轉義(調.方法)).Append('"');
				if(調.值 is not null){
					文.Append(", \"").Append(值鍵(調.方法)).Append("\": \"").Append(Json轉義(調.值)).Append('"');
				}
				foreach(var 欄 in 調.附欄){
					文.Append(", \"").Append(Json轉義(欄.名)).Append("\": ");
					if(欄.是數值){
						文.Append(欄.值);
					} else {
						文.Append('"').Append(Json轉義(欄.值)).Append('"');
					}
				}
				文.Append(" }");
				if(j + 1 < 條.調用.Count){
					文.Append(',');
				}
				文.Append('\n');
			}
			文.Append("    ],\n");
		}
		文.Append("  }\n}\n");
		// step 3: 落盤（UTF-8 無 BOM，跨平臺一致）。
		var 路徑 = Path.GetFullPath(檔名);
		File.WriteAllText(路徑, 文.ToString(), new UTF8Encoding(false));
		var 調用數 = 0;
		foreach(var 條 in 字表){
			調用數 += 條.調用.Count;
		}
		Console.WriteLine($"已導出 {路徑}：{字表.Count} 字、{調用數} 條調用、schemaVer={SchemaVer}");
	}

	/// 數值欄的合法形狀：只允許數字、小數點與開頭的負號。抽取器給的是百分比原值（如 10000），
	/// 手寫登記時若寫比值（如 0.01）也放行。
	private static bool Is數值(string z){
		if(z.Length == 0){
			return false;
		}
		var 點 = 0;
		for(var i = 0; i < z.Length; i++){
			var c = z[i];
			if(c == '-' && i == 0){
				continue;
			}
			if(c == '.'){
				點++;
				if(點 > 1){
					return false;
				}
				continue;
			}
			if(!char.IsAsciiDigit(c)){
				return false;
			}
		}
		return true;
	}

	/// 位置參數在 JSON 裏的鍵名。與 CsTbl 產出端的 CsTbl方法Def.值鍵 是同一個約定。
	private static string 值鍵(string 方法){
		if(方法 == "廣韻意"){
			return "釋義";
		}
		return "音";
	}

	/// 最小 JSON 字串轉義：只處理 JSON 規範要求轉義的字符，其餘原樣（IPA 與漢字都不需要轉義）。
	private static string Json轉義(string z){
		var 文 = new StringBuilder(z.Length + 8);
		foreach(var c in z){
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
					if(c < 0x20){
						文.Append("\\u").Append(((int)c).ToString("x4"));
					} else {
						文.Append(c);
					}
					break;
			}
		}
		return 文.ToString();
	}
}

/// 一個字條＝該字的全部調用（有序：方法按 CsTbl 的產出序、同方法按來源表行序）。
public class 字條{
	public string 字{get;}
	public List<調用> 調用{get;} = new();

	public 字條(string 字){
		this.字 = 字;
	}
}

/// 一條調用：方法名 + 位置參數（純附欄的方法沒有）+ 附欄（有序）。
public class 調用{
	public string 方法{get;}
	public string? 值{get;}
	public List<欄> 附欄{get;}

	public 調用(string 方法, string? 值, List<欄>? 附欄){
		this.方法 = 方法;
		this.值 = 值;
		this.附欄 = 附欄 ?? new List<欄>();
	}
}

/// 一個附欄。是數值 為 true 時 JSON 裏不加引號（如 Ratio）。
public class 欄{
	public string 名{get;}
	public string 值{get;}
	public bool 是數值{get;}

	public 欄(string 名, string 值, bool 是數值 = false){
		this.名 = 名;
		this.值 = 值;
		this.是數值 = 是數值;
	}
}

/// 一個字的登記入口。方法與 Dk.cs 定義一一對應；一個方法調用多次＝多個讀音。
public class CbArg{
	private readonly 字條 _條;

	internal CbArg(字條 條){
		this._條 = 條;
	}

	//Tsinswreng 上古漢語擬音
	public void TswgOc(string 音){
		_條.調用.Add(new 調用("TswgOc", 音, null));
	}

	//Tsinswreng 上古漢語魔改擬音
	public void TswgOcModed(string 音){
		_條.調用.Add(new 調用("TswgOcModed", 音, null));
	}
	public void TswgOcModed(string 音, Action<CbArg_TswgOcModed> Cb){
		var 調 = new 調用("TswgOcModed", 音, null);
		_條.調用.Add(調);
		Cb(new CbArg_TswgOcModed(調));
	}

	//Polyhedron 中古拼音
	public void PolyMc(string 音){
		_條.調用.Add(new 調用("PolyMc", 音, null));
	}
	public void PolyMc(string 音, Action<CbArg_PolyMc> Cb){
		var 調 = new 調用("PolyMc", 音, null);
		_條.調用.Add(調);
		Cb(new CbArg_PolyMc(調));
	}

	//白-沙
	public void BsOc(Action<CbArg_BsOc> Cb){
		var 調 = new 調用("BsOc", null, null);
		_條.調用.Add(調);
		Cb(new CbArg_BsOc(調));
	}

	//許思萊
	public void SchOc(string 音){
		_條.調用.Add(new 調用("SchOc", 音, null));
	}
	public void SchOc(string 音, Action<CbArg_SchOc> Cb){
		var 調 = new 調用("SchOc", 音, null);
		_條.調用.Add(調);
		Cb(new CbArg_SchOc(調));
	}

	//Msoeg
	public void MsoegOc(string 音){
		_條.調用.Add(new 調用("MsoegOc", 音, null));
	}

	//斯塔羅斯金
	public void StaOc(string 音){
		_條.調用.Add(new 調用("StaOc", 音, null));
	}

	//鄭張尚芳
	public void DtOc(string 音){
		_條.調用.Add(new 調用("DtOc", 音, null));
	}

	//廣韻形聲考聲
	public void Ptl(Action<CbArg_Ptl> Cb){
		var 調 = new 調用("Ptl", null, null);
		_條.調用.Add(調);
		Cb(new CbArg_Ptl(調));
	}

	//廣韻釋義
	public void 廣韻意(string S){
		_條.調用.Add(new 調用("廣韻意", S, null));
	}
}

/// 白-沙的附欄。
public class CbArg_BsOc{
	private readonly 調用 _調;

	internal CbArg_BsOc(調用 調){
		this._調 = 調;
	}

	/// 白沙官方的上古音（含開頭的 `*` 擬構記號與 `[]` 可選成分）。
	public void Oc(string S){
		_調.附欄.Add(new 欄("Oc", S));
	}

	/// 白沙官方的英文釋義。
	public void Gloss(string S){
		_調.附欄.Add(new 欄("Gloss", S));
	}

	/// 音值欄裏的別說（`~ X`／`? or X`／`< X`）抽出來放這裡。
	public void Note(string Note){
		_調.附欄.Add(new 欄("Note", Note));
	}
}

/// 許思萊的附欄。
public class CbArg_SchOc{
	private readonly 調用 _調;

	internal CbArg_SchOc(調用 調){
		this._調 = 調;
	}

	/// 來源表的原註釋（該表正文只有兩處節標題）。
	public void Note(string Note){
		_調.附欄.Add(new 欄("Note", Note));
	}
}

/// 廣韻形聲考的附欄（與主字表 TSV 的九欄一一對應；字本身是 Z 的鍵）。
public class CbArg_Ptl{
	private readonly 調用 _調;

	internal CbArg_Ptl(調用 調){
		this._調 = 調;
	}

	public void 聲符(string S){
		_調.附欄.Add(new 欄("聲符", S));
	}
	public void 諧聲域(string S){
		_調.附欄.Add(new 欄("諧聲域", S));
	}
	public void 記號(string S){
		_調.附欄.Add(new 欄("記號", S));
	}
	public void 反切(string S){
		_調.附欄.Add(new 欄("反切", S));
	}
	public void 中古地位(string S){
		_調.附欄.Add(new 欄("中古地位", S));
	}
	public void 切拼(string S){
		_調.附欄.Add(new 欄("切拼", S));
	}
	public void 上古音參考(string S){
		_調.附欄.Add(new 欄("上古音參考", S));
	}
	public void 考釋(string S){
		_調.附欄.Add(new 欄("考釋", S));
	}
}

/// TswgOcModed 的附欄。
public class CbArg_TswgOcModed{
	private readonly 調用 _調;

	internal CbArg_TswgOcModed(調用 調){
		this._調 = 調;
	}

	/// 來源表裏的原註釋（dkp 的行內 `#` 註釋與上方緊鄰註釋行），不截斷、不摘要。
	public void Note(string Note){
		_調.附欄.Add(new 欄("Note", Note));
	}

	/// 權重：來源表第三欄的百分比原值（如 10000、1、0），不換算成比值。
	public void Ratio(double Ratio){
		_調.附欄.Add(new 欄("Ratio", Ratio.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture), true));
	}
}

/// PolyMc 的附欄。
public class CbArg_PolyMc{
	private readonly 調用 _調;

	internal CbArg_PolyMc(調用 調){
		this._調 = 調;
	}

	/// 權重：zyenpheng.dict.yaml 第三欄的百分比原值。
	public void Ratio(double Ratio){
		_調.附欄.Add(new 欄("Ratio", Ratio.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture), true));
	}
}