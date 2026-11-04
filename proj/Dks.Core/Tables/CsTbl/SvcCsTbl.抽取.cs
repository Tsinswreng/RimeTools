namespace Dks.Core.Tables.CsTbl;

using System.Runtime.CompilerServices;
using System.Text;
using Tsinswreng.CsCtx;

/// 綜合字表源產出服務：九家來源表的抽取器，以及各家共用的行級小工具。
/// 為甚麼要自己讀行、不走 <c>DictYamlParser</c>：dkp 的註釋（行內 `#` 與上方緊鄰註釋行）就是要拿去當 Note 的內容，
/// 而現行解析器會把 `#` 之後整段截掉（`BodyLinesAsync` 裏的行內註釋截斷）；這是使用者拍板過的取捨。
public partial class SvcCsTbl{

	// ============================================================
	// 共用小工具
	// ============================================================

	/// 判斷是不是「單字」。用 rune 數而不是 `str.Length`：
	/// CJK 擴展 B 以後的字是代理對（`Length == 2`），但它是一個字。
	/// 另外**排除 IDS 控制符（U+2FF0–U+2FFF）**：那些是「字形描述」的結構符號，不是字——
	/// 廣韻形聲考主字表裏實測有 2 行的字欄是 `⿰`（站方把不確定字寫成 `<⿰…>`），不能當 Z 的鍵。
	private static bool 是單字(str z){
		if(z.Length == 0){
			return false;
		}
		var 數 = 0;
		foreach(var r in z.EnumerateRunes()){
			數++;
			if(數 > 1){
				return false;
			}
			if(r.Value >= 0x2FF0 && r.Value <= 0x2FFF){
				return false;
			}
		}
		return 數 == 1;
	}

	/// 判斷是不是 IDS 控制符（U+2FF0–U+2FFF，如 ⿰⿱⿴）。它們是字形描述的結構符號，不是字，
	/// 但**是來源檔裏的真實內容**（站方把沒有碼位的字寫成 `<⿰…>`），所以要跳過而不是當成解析錯誤。
	private static bool 是Ids控制符(str z){
		var 數 = 0;
		foreach(var r in z.EnumerateRunes()){
			數++;
			if(r.Value < 0x2FF0 || r.Value > 0x2FFF){
				return false;
			}
		}
		return 數 == 1;
	}

	/// 判斷是不是 CJK 漢字。只覆蓋常見漢字區段，用來標出「可疑字頭」——
	/// 來源表裏若出現拉丁字母之類（實測 OC_msoeg 第 1726 行首欄是 `x`，疑為來源檔的手民之誤），
	/// 產出照樣收（不丟信息），但會在報告的備註裏点名，讓使用者去核來源。
	private static bool 是漢字(str z){
		foreach(var r in z.EnumerateRunes()){
			var v = r.Value;
			if(v >= 0x4E00 && v <= 0x9FFF){ continue; }   // CJK 基本區
			if(v >= 0x3400 && v <= 0x4DBF){ continue; }   // 擴展 A
			if(v >= 0xF900 && v <= 0xFAFF){ continue; }   // 兼容漢字
			if(v >= 0x20000 && v <= 0x323AF){ continue; } // 擴展 B 到 H（含兼容補充）
			if(v >= 0x3005 && v <= 0x3007){ continue; }   // 々〆〇
			return false;
		}
		return z.Length > 0;
	}

	/// 以第一個 `#` 為界切開一行：其前是欄位段，其後是註釋文字（回傳時剝掉 `#` 與其後緊跟的一個空白）。
	/// 這樣切是照 dkp 的實際寫法定的，涵蓋三種情形：
	/// - `有	wuʔ	# ts` ⇒ 欄位段 `有	wuʔ	`、註釋 `ts`；
	/// - `唯	wiʔ	0%	#諾也 以水切 又音惟八` ⇒ 註釋 `諾也 以水切 又音惟八`；
	/// - `賁	bul	# bs 主元音u	說文: 从貝卉聲` ⇒ 註釋 `bs 主元音u	說文: 从貝卉聲`
	///   （`#` 之後那一欄是作者沒用 `#` 開頭的續註，一併當註釋原文保留，不丟字）。
	private static (str 段, str? 註釋) 剖註釋(str 行){
		var i = 行.IndexOf('#');
		if(i < 0){
			return (行, null);
		}
		var 段 = 行[..i];
		var 註 = 行[(i + 1)..];
		if(註.StartsWith(' ')){
			註 = 註[1..];
		}
		return (段, 註.Length == 0 ? null : 註);
	}

	/// 判斷一段「去掉 `#` 之後」的文字屬於哪一類。這個分類是使用者定的取捨所要求的：
	/// 使用者說「字條後面或上面有註釋的就填進 Note」「別人的表裏有註釋的也要保留」，又說
	/// 「被 `#` 註釋掉的整條字條先不要了」——所以必須把「註釋」與「被停用的字條」分開。
	/// 判據（實測 dkp 131 條、zyenpheng 70 條、許思萊 0 條）：
	/// - 含 Tab ⇒ 這是被停用的字條（`#喇	rˁat	1%`）；
	/// - 去掉空白後只有一個 rune ⇒ 也是被停用的字條，只是作者連碼都沒寫（dkp 的 `# 蕪`、`# 俺`、`#@`）；
	/// - 其餘 ⇒ 真註釋（`#·開，小徐本《說文…》`、`# 不變`、`#<reset-weight>`、`# ts`）。
	private static bool 是被停用的字條(str 去井號){
		if(去井號.Contains('\t')){
			return true;
		}
		var 修 = 去井號.Trim();
		return 是單字(修);
	}

	/// 把「上方緊鄰註釋」與「行內註釋」合成一條 Note 值：上方在前、行內在後，用換行連接。
	/// 兩者都沒有 ⇒ null（⇒ 不產生 Note 附欄）。註釋**不截斷、不摘要**——dkp 裏「開」那條三百多字的考證照收。
	private static str? 併註釋(str? 上方, str? 行內){
		if(上方 is null){
			return 行內;
		}
		if(行內 is null){
			return 上方;
		}
		return 上方 + "\n" + 行內;
	}

	/// 從 `ASCII*IPA` 形式的碼欄取 IPA 段，並去掉整理者自加的漢拼夾註（全角括號）。
	/// 例：`x(r)yh*ʔ⁽ʴ⁾əh（yì）` ⇒ `ʔ⁽ʴ⁾əh`；`kXax*kâʔ` ⇒ `kâʔ`；`qyyds*qɯːds` ⇒ `qɯːds`。
	/// 沒有 `*` 的原樣回傳（斯塔羅斯金是純 IPA）。只認第一個 `*`。
	/// **不適用於白沙官方 TSV**：那張表 OC 欄開頭的 `*` 是「擬構形式」的記號、屬音值的一部分，故白沙另走 拆白沙括註。
	private static str 取Ipa(str 碼){
		var z = 碼.Trim();
		var i = z.IndexOf('（');
		if(i >= 0){
			z = z[..i];
		}
		var j = z.IndexOf('*');
		if(j >= 0){
			z = z[(j + 1)..];
		}
		return z.Trim();
	}

	/// 從 dict.yaml 的欄位取權重：第三欄優先，第三欄空時退到第四欄。
	/// 為甚麼要退：dkp 有 13 行寫成 `為	waj	⟨空⟩	0%`（多一個 Tab），只看第三欄會把 0% 漏掉。
	/// 值一律回傳**百分比原值數字**（`10000%` ⇒ `"10000"`、`1%` ⇒ `"1"`、`0%` ⇒ `"0"`），不換算成比值；
	/// 只有「數字（可帶一個 %）」才當權重，其餘（欄裏夾著說明）回 null，免得把說明當數字用。
	private static str? 取權重(str[] 欄){
		for(var i = 2; i < 欄.Length && i <= 3; i++){
			var v = 欄[i].Trim();
			if(v.Length == 0){
				continue;
			}
			if(v.EndsWith('%')){
				v = v[..^1].Trim();
			}
			if(v.Length > 0 && v.All(char.IsAsciiDigit)){
				return v;
			}
			return null;
		}
		return null;
	}

	/// 加一個附欄；值為空（去掉空白後）就不加——檔頭有註明「空欄不列出」的約定。
	/// 這是廣韻形聲考那種「九欄固定、但常有大片空欄」的表能瘦下來的關鍵。
	private static void 加欄(List<CsTbl欄> 附, str 名, str 值){
		var v = 值.Trim();
		if(v.Length == 0){
			return;
		}
		附.Add(new CsTbl欄(名, v));
	}

	/// 拆廣韻形聲考的「諧聲域」欄：實測該欄 25,253 行裏有 6,174 行是複數值
	/// （2 個值 4917 行、3 個值 1138 行、4 個值 119 行），以半角空白分隔，如 `TUI TUN`、`KUI ɁUI ŊUI`。
	/// 使用者定「要分條目寫」，故拆成多個 token，呼叫方為每個 token 各出一條調用。
	/// 空欄回傳一個空 token，讓呼叫方照舊走「九欄全空」那條分支。
	/// 同時容忍全角空白（U+3000）：來源是網頁轉出，用哪種空白不以我們的喜好為準。
	private static IReadOnlyList<str> 拆諧聲域(str 值){
		var z = 值.Trim();
		if(z.Length == 0){
			return new[]{ "" };
		}
		return z.Split(new[]{ ' ', '\u3000' }, StringSplitOptions.RemoveEmptyEntries);
	}

	/// 拆白沙官方 OC 欄的圓括號註記：以 `~`／`?`／`or `／`<` 開頭的括號組是「或作／或者／源自」的別說，
	/// 抽出來當 Note；其餘括號（構詞前綴 `(mə-)`、可選成分 `(r)`／`(j)`／`(ŋ)`）留在音值裏。
	/// 實測樣本（`baxtersagart.tsv`）：
	/// - `*mə.rˤat (~ C.rˤat ?)` ⇒ 音 `*mə.rˤat`、別說 `~ C.rˤat ?`
	/// - `*məʔ (? or *mˤoʔ) ` ⇒ 音 `*məʔ`、別說 `? or *mˤoʔ`
	/// - `*[q](r)oŋ (< *[q](r)om)` ⇒ 音 `*[q](r)oŋ`、別說 `< *[q](r)om`
	/// - `*(mə-)[b](r)oŋ (~ *C.[b](r)oŋ)` ⇒ 音 `*(mə-)[b](r)oŋ`、別說 `~ *C.[b](r)oŋ`
	/// 音值開頭的 `*`（擬構記號）與 `[]`（可選成分）都原樣保留；只用圓括號配對掃描，不碰 `[]`／`<>`。
	private static (str 音, str? 別說) 拆白沙括註(str oc){
		var 音 = new StringBuilder(oc.Length);
		var 別 = new List<str>();
		var i = 0;
		while(i < oc.Length){
			if(oc[i] != '('){
				音.Append(oc[i]);
				i++;
				continue;
			}
			// step 1: 找與這個 `(` 配對的 `)`（容許嵌套；實測沒有嵌套，但成本極低）。
			var 深 = 1;
			var j = i + 1;
			while(j < oc.Length && 深 > 0){
				if(oc[j] == '('){
					深++;
				} else if(oc[j] == ')'){
					深--;
				}
				j++;
			}
			if(深 != 0){
				// 括號不成對：原樣收尾，不猜（寧可整串照抄，也不擅自刪）。
				音.Append(oc[i..]);
				break;
			}
			var 修 = oc[(i + 1)..(j - 1)].Trim();
			if(修.StartsWith('~') || 修.StartsWith('?') || 修.StartsWith('<') || 修.StartsWith("or ")){
				別.Add(修);
			} else {
				音.Append(oc[i..j]);
			}
			i = j;
		}
		return (音.ToString().Trim(), 別.Count == 0 ? null : str.Join("\n", 別));
	}

	// ---- 讀行 ----

	/// 讀 dict.yaml 的正文：表頭（`#` 註釋、`---` … 鍵值 … `...`）整段丟掉（使用者已定「表頭註釋不要」），
	/// `...` 之後的每一行原樣吐出——**包含**正文裏的註釋行與空行，因為 dkp 的抽取器要靠它們判斷 Note。
	/// 只認第一個 `...`；`---`/`...` 這兩種文檔標記本身不吐（某些表尾會殘留）。
	private static async IAsyncEnumerable<str> 讀dict正文(TextReader 讀, [EnumeratorCancellation] CT Ct){
		var 進正文 = false;
		str? 行;
		while((行 = await 讀.ReadLineAsync(Ct)) is not null){
			Ct.ThrowIfCancellationRequested();
			if(!進正文){
				if(行.Trim() == "..."){
					進正文 = true;
				}
				continue;
			}
			var 修 = 行.Trim();
			if(修 == "..." || 修 == "---"){
				continue;
			}
			yield return 行;
		}
	}

	/// 讀沒有表頭的整份檔（Dzih.txt 與兩張 TSV）：逐行吐出並附 1 起算的行號，
	/// 行號只用於問題訊息（要能讓人直接去來源檔定位）。表頭行由各抽取器自己跳過。
	private static async IAsyncEnumerable<(i32 行號, str 行)> 讀全行(TextReader 讀, [EnumeratorCancellation] CT Ct){
		var 行號 = 0;
		str? 行;
		while((行 = await 讀.ReadLineAsync(Ct)) is not null){
			Ct.ThrowIfCancellationRequested();
			行號++;
			yield return (行號, 行);
		}
	}

	// ============================================================
	// 九家抽取器
	// ============================================================

	/// 抽 dkp.dict.yaml → <see cref="CsTbl方法.TswgOcModed"/>。
	/// 這張是用戶手工維護最重的一張：權重（Ratio）、行內註釋與上方緊鄰註釋都要留下。
	/// 例：`三	srum	# bs` ⇒ `a.TswgOcModed("srum", b=>{ b.Note("bs"); });`
	///     `辣	rˁat	10000%` ⇒ `a.TswgOcModed("rˁat", b=>{ b.Ratio(10000); });`
	private async Task<nil> 抽TswgOcModed(IFnCtx Ctx, TextReader 讀, 抽取帳 帳, CT Ct){
		str? 待用註釋 = null;
		await foreach(var 行 in 讀dict正文(讀, Ct)){
			if(行.Trim().Length == 0){
				// 空行打斷「上方緊鄰」的關係：空行之後的註釋不再算某條字條的上方註釋。
				待用註釋 = null;
				continue;
			}
			var 修 = 行.Trim();
			if(修.StartsWith('#')){
				var 去井 = 修[1..];
				if(是被停用的字條(去井)){
					帳.記跳過("被 # 停用的字條（使用者已定不收）", 修);
					continue;
				}
				待用註釋 = 剖註釋(修).註釋;
				continue;
			}
			var (碼段, 行內註釋) = 剖註釋(行);
			var 欄 = 碼段.Split('\t');
			if(欄.Length < 2){
				帳.記跳過("欄數不足 2 的行", "");
				continue;
			}
			var 字 = 欄[0].Trim();
			var 碼 = 欄[1].Trim();
			if(!是單字(字)){
				// 詞條（多字）：不進字表；**不消費**待用註釋，讓它掛到下面最近的單字行。
				帳.記跳過("非單字行（詞條）", 字);
				continue;
			}
			帳.資料行++;
			if(碼.Length == 0){
				// 空碼行：沒有音就沒有落點，註釋也無處可掛（該行的上方註釋一併作廢並在對帳裏可見）。
				帳.記跳過("空碼行", 字);
				待用註釋 = null;
				continue;
			}
			var 附 = new List<CsTbl欄>(2);
			var 註 = 併註釋(待用註釋, 行內註釋);
			if(註 is not null){
				附.Add(new CsTbl欄(CsTbl欄名.Note, 註));
			}
			var 權 = 取權重(欄);
			if(權 is not null){
				附.Add(new CsTbl欄(CsTbl欄名.Ratio, 權, 是數值: true));
			}
			帳.記(字, new CsTbl調用(CsTbl方法.TswgOcModed, 碼, 附));
			待用註釋 = null;
		}
		return NIL;
	}

	/// 抽 zyenpheng.dict.yaml → <see cref="CsTbl方法.PolyMc"/>（Polyhedron 中古**拼音**，不是 IPA）。
	/// 這張表的正文實測沒有註釋（70 條 `#` 行全是被停用的字條），故不設 Note 落點。
	/// 例：`谷	giak	1%` ⇒ `a.PolyMc("giak", b=>{ b.Ratio(1); });`；`谷	luk` ⇒ `a.PolyMc("luk");`
	private async Task<nil> 抽PolyMc(IFnCtx Ctx, TextReader 讀, 抽取帳 帳, CT Ct){
		await foreach(var 行 in 讀dict正文(讀, Ct)){
			var 修 = 行.Trim();
			if(修.Length == 0){
				continue;
			}
			if(修.StartsWith('#')){
				// 全是被停用的字條（實測 70 條），照使用者「這種先不要了」整條丟。
				帳.記跳過("被 # 停用的字條（使用者已定不收）", 修);
				continue;
			}
			var (碼段, _) = 剖註釋(行);
			var 欄 = 碼段.Split('\t');
			if(欄.Length < 2){
				帳.記跳過("欄數不足 2 的行", "");
				continue;
			}
			var 字 = 欄[0].Trim();
			var 碼 = 欄[1].Trim();
			if(!是單字(字)){
				帳.記跳過("非單字行（詞條）", 字);
				continue;
			}
			帳.資料行++;
			if(碼.Length == 0){
				帳.記跳過("空碼行", 字);
				continue;
			}
			var 附 = new List<CsTbl欄>(1);
			var 權 = 取權重(欄);
			if(權 is not null){
				附.Add(new CsTbl欄(CsTbl欄名.Ratio, 權, 是數值: true));
			}
			帳.記(字, new CsTbl調用(CsTbl方法.PolyMc, 碼, 附));
		}
		return NIL;
	}

	/// 抽廣韻形聲考**主字表**（`guangyun_xingshengkao_char.tsv`，25253 行、九欄）→ <see cref="CsTbl方法.Ptl"/>。
	/// 欄序＝聲符／諧聲域／字／記號形式／反切／中古地位／切拼／上古音參考／考釋備註，與 CbArg_Ptl 一一對應；
	/// 字本身是 Z 的鍵，故九欄裏除「字」外的八欄都進附欄。使用者只要這張主字表（`group`／`appendix` 不收）。
	/// 注意：中古地位那一欄是古音小鏡調整過的（站方自述韻類名統一、開合由站方調整），不是布之道原值。
	private async Task<nil> 抽Ptl(IFnCtx Ctx, TextReader 讀, 抽取帳 帳, CT Ct){
		var 首行 = true;
		await foreach(var (行號, 行) in 讀全行(讀, Ct)){
			if(首行){
				首行 = false; // 表頭：聲符 諧聲域 字 記號形式 反切 中古地位 切拼 上古音參考 考釋備註
				continue;
			}
			if(行.Trim().Length == 0){
				continue;
			}
			var 欄 = 行.Split('\t');
			if(欄.Length != 9){
				帳.記問題("", $"第 {行號} 行有 {欄.Length} 欄，應為 9 欄");
				continue;
			}
			var 字 = 欄[2].Trim();
			if(!是單字(字)){
				// 主字表的字欄理應是單字；實測只有 2 行是 IDS 控制符（`⿰`，站方寫成 `<⿰…>` 的不確定字），
				// 那不是解析錯誤而是來源原貌，故跳過並留樣本。除此之外的非單字才算問題。
				if(是Ids控制符(字)){
					帳.記跳過("IDS 字形描述字頭（非單字）", 字);
				} else {
					帳.記問題(字, $"第 {行號} 行的字欄不是單字");
				}
				continue;
			}
			帳.資料行++;
			// 諧聲域一欄實測有四種情形：1 個值 19079 行、2 個值 4917 行、3 個值 1138 行、4 個值 119 行
			// （例：`甶` 的 `KUI ɁUI ŊUI`，`𦎫` 的 `TUI TUN`）。使用者定：這不是一個值、要**分條目**寫——
			// 每個諧聲域各出一條 Ptl 調用，其餘欄位（反切／中古地位／切拼／上古音參考／考釋）照抄。
			// 附帶一個硬約束：JSON 的調用是一條一個物件，同一物件裏出現兩個同名附欄會成為重複鍵，
			// 所以「兩個諧聲域」只能落在兩條調用上，不能塞進同一條。
			var 產出數 = 0;
			foreach(var 域 in 拆諧聲域(欄[1])){
				var 附 = new List<CsTbl欄>(8);
				加欄(附, CsTbl欄名.聲符, 欄[0]);
				加欄(附, CsTbl欄名.諧聲域, 域);
				加欄(附, CsTbl欄名.記號, 欄[3]);
				加欄(附, CsTbl欄名.反切, 欄[4]);
				加欄(附, CsTbl欄名.中古地位, 欄[5]);
				加欄(附, CsTbl欄名.切拼, 欄[6]);
				加欄(附, CsTbl欄名.上古音參考, 欄[7]);
				加欄(附, CsTbl欄名.考釋, 欄[8]);
				if(附.Count == 0){
					continue;
				}
				帳.記(字, new CsTbl調用(CsTbl方法.Ptl, null, 附));
				產出數++;
			}
			if(產出數 == 0){
				帳.記跳過("九欄全空（該字在形聲考無資料）", 字);
			}
		}
		return NIL;
	}

	/// 抽白沙官方字表（`baxtersagart.tsv`，4968 行、11 欄）→ <see cref="CsTbl方法.BsOc"/>。
	/// 只取第 5 欄（OC）與第 6 欄（gloss）；中古音／拼音／GSR／漢語大字典／部首／筆畫／Unicode 使用者已定不要
	/// （理由：廣韻音系各家相同，留一套即留全部；其餘與音韻無關）。
	/// 同字多行＝多音（如 縫 兩行、安 三行同音不同義）照樣多次調用，不合併、不作判斷。
	private async Task<nil> 抽BsOc(IFnCtx Ctx, TextReader 讀, 抽取帳 帳, CT Ct){
		var 首行 = true;
		await foreach(var (行號, 行) in 讀全行(讀, Ct)){
			if(首行){
				首行 = false; // 表頭：zi py MC ⟨無名欄⟩ OC gloss GSR HYDZD rad str Unicode
				continue;
			}
			if(行.Trim().Length == 0){
				continue;
			}
			var 欄 = 行.Split('\t');
			if(欄.Length == 12){
				// 實測 4967 行裏恰有 1 行是壞的：列（第 2107 行）的 GSR 欄多了一個 `"`，
				// 使該行變 12 欄、後面全部右移一格。多出來的那一欄就是那個引號，剔掉即還原。
				欄 = 剔多餘欄(欄, 行號, 帳);
			}
			if(欄.Length != 11){
				帳.記問題("", $"第 {行號} 行有 {欄.Length} 欄，應為 11 欄");
				continue;
			}
			var 字 = 欄[0].Trim();
			if(!是單字(字)){
				帳.記問題(字, $"第 {行號} 行的字欄不是單字");
				continue;
			}
			帳.資料行++;
			var (音, 別說) = 拆白沙括註(欄[4]);
			var 附 = new List<CsTbl欄>(3);
			if(音.Length > 0){
				附.Add(new CsTbl欄(CsTbl欄名.Oc, 音));
			}
			加欄(附, CsTbl欄名.Gloss, 欄[5]);
			if(別說 is not null){
				附.Add(new CsTbl欄(CsTbl欄名.Note, 別說));
			}
			if(附.Count == 0){
				帳.記跳過("OC 與 gloss 都空", 字);
				continue;
			}
			帳.記(字, new CsTbl調用(CsTbl方法.BsOc, null, 附));
		}
		return NIL;
	}

	/// 剔掉白沙那行多出來的欄：只在欄數為 12 且第 8 欄（1 起算）是孤立引號時動手，其餘情形原樣回傳。
	/// 抽成獨立函數是為了讓「壞行的處理」有單一出處，且出問題時能在報告裏留下痕跡。
	private static str[] 剔多餘欄(str[] 欄, i32 行號, 抽取帳 帳){
		var 去 = new List<str>(11);
		for(var i = 0; i < 欄.Length; i++){
			if(i == 7 && 欄[i].Trim() == "\""){
				continue;
			}
			去.Add(欄[i]);
		}
		if(去.Count != 11){
			帳.記問題("", $"第 {行號} 行是 12 欄但不是預期的多餘引號，無法自動對齊");
		}
		return 去.ToArray();
	}

	/// 抽 msoeg 2018 擬音 → <see cref="CsTbl方法.MsoegOc"/>。
	/// 碼欄形如 `ASCII*IPA（漢拼）`，取 `*` 之後的 IPA 段並剝掉全角括號漢拼（實測有 -l 韻尾 140 處，屬該家特徵）。
	private Task<nil> 抽MsoegOc(IFnCtx Ctx, TextReader 讀, 抽取帳 帳, CT Ct){
		return 抽Oc系(Ctx, 讀, 帳, CsTbl方法.MsoegOc, 收上方註釋: false, Ct);
	}

	/// 抽許思萊上古音 → <see cref="CsTbl方法.SchOc"/>。
	/// 碼欄形如 `ASCII*IPA`；正文實測有 260 條詞條（踟躕、差池…）與 2 條節標題註釋
	/// （`# 原書詞組`、`# Voice from Early China`），故這一家開上方註釋 → 進 SchOc 的 Note。
	private Task<nil> 抽SchOc(IFnCtx Ctx, TextReader 讀, 抽取帳 帳, CT Ct){
		return 抽Oc系(Ctx, 讀, 帳, CsTbl方法.SchOc, 收上方註釋: true, Ct);
	}

	/// 抽鄭張尚芳上古音 → <see cref="CsTbl方法.DtOc"/>。
	/// 取的是收藏集原件（18500 行／13118 字），多音是**分行**而非 `|` 併寫——因此本抽取器不做任何分隔符拆解。
	private Task<nil> 抽DtOc(IFnCtx Ctx, TextReader 讀, 抽取帳 帳, CT Ct){
		return 抽Oc系(Ctx, 讀, 帳, CsTbl方法.DtOc, 收上方註釋: false, Ct);
	}

	/// 抽斯塔羅斯金上古音 → <see cref="CsTbl方法.StaOc"/>。
	/// 碼欄是純 IPA（無 `*` 前綴）；這張表空音很多（實測 5450 行裏 3424 行空），空音一律不調用。
	private Task<nil> 抽StaOc(IFnCtx Ctx, TextReader 讀, 抽取帳 帳, CT Ct){
		return 抽Oc系(Ctx, 讀, 帳, CsTbl方法.StaOc, 收上方註釋: false, Ct);
	}

	/// `OC_*` 這一族的共同抽取器：dict.yaml、兩欄（字 + 碼）、多音分行、空音跳過。
	/// 參數化的只有 DSL 方法與「要不要收上方註釋」；`收上方註釋` 只有許思萊要（它正文裏真有兩條節標題）。
	private async Task<nil> 抽Oc系(
		IFnCtx Ctx, TextReader 讀, 抽取帳 帳, CsTbl方法Def 方法, bool 收上方註釋, CT Ct
	){
		str? 待用註釋 = null;
		await foreach(var 行 in 讀dict正文(讀, Ct)){
			if(行.Trim().Length == 0){
				待用註釋 = null;
				continue;
			}
			var 修 = 行.Trim();
			if(修.StartsWith('#')){
				if(是被停用的字條(修[1..])){
					帳.記跳過("被 # 停用的字條（使用者已定不收）", 修);
					continue;
				}
				if(收上方註釋){
					待用註釋 = 剖註釋(修).註釋;
				}
				continue;
			}
			var (碼段, _) = 剖註釋(行);
			var 欄 = 碼段.Split('\t');
			if(欄.Length < 2){
				帳.記跳過("欄數不足 2 的行", "");
				continue;
			}
			var 字 = 欄[0].Trim();
			var 音 = 取Ipa(欄[1]);
			if(!是單字(字)){
				帳.記跳過("非單字行（詞條）", 字);
				continue;
			}
			帳.資料行++;
			if(音.Length == 0){
				帳.記跳過("空音行", 字);
				待用註釋 = null;
				continue;
			}
			var 附 = new List<CsTbl欄>(1);
			if(收上方註釋 && 待用註釋 is not null){
				附.Add(new CsTbl欄(CsTbl欄名.Note, 待用註釋));
			}
			帳.記(字, new CsTbl調用(方法, 音, 附));
			待用註釋 = null;
		}
		return NIL;
	}

	/// 抽廣韻字表（韻典網 `Dzih.txt`）→ <see cref="CsTbl方法.廣韻意"/>。
	/// 格式是**空格分隔**（不是 Tab）：`字 小韻序號 組內序 釋義`，全檔 25333 行、19636 個字頭、其中 5697 行是同字多行。
	/// 使用者已定不收那兩個數字（它們是韻典網自編的小韻索引，不是廣韻原文），故只取釋義。
	/// 釋義欄本身可能含空格（實測 3 行，如 `烏[枚嗚  /枝鳴]`），所以第 4 欄之後要**全部併回**。
	private async Task<nil> 抽廣韻意(IFnCtx Ctx, TextReader 讀, 抽取帳 帳, CT Ct){
		await foreach(var (行號, 行) in 讀全行(讀, Ct)){
			if(行.Trim().Length == 0){
				continue;
			}
			var 欄 = 行.Split(' ');
			if(欄.Length < 4){
				帳.記問題("", $"第 {行號} 行不足 4 欄");
				continue;
			}
			var 字 = 欄[0].Trim();
			if(!是單字(字)){
				// 韻典網把沒有 Unicode 碼位的字寫成字形描述（IDS），實測 5 條：
				// `⿱𥫗瀸`（上同）、`⿱⿰来攵正`（俗）、`⿰隺犬`（至也髙也）、`⿱芖雨`（俗）、`⿱入疌`（織⿱入?疌）。
				// 它們不是單字、當不了 Z 的鍵，故跳過並在報告裏留樣本（不是問題，整批不該因此失敗）。
				帳.記跳過("IDS 字形描述字頭（非單字）", 字);
				continue;
			}
			帳.資料行++;
			var 釋義 = str.Join(" ", 欄[3..]).Trim();
			if(釋義.Length == 0){
				帳.記跳過("釋義為空", 字);
				continue;
			}
			帳.記(字, new CsTbl調用(CsTbl方法.廣韻意, 釋義));
		}
		return NIL;
	}
}
