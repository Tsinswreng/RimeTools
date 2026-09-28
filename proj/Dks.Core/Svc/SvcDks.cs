namespace Dks.Core.Svc;

using System.Collections.Concurrent;
using Dks.Core;
using RimeTools.Shared.Dict.Models;
using RimeTools.Shared.Dict.Parser;
using RimeTools.Shared.Freq;
using RimeTools.Tools;
using Tsinswreng.CsCtx;

/// ISvcDks 的實現（本檔：公用件 + 單音節純函數 + 擇源兩步）。
/// 檔案分工（同一個 partial 類，藉 partial 分檔以便閱讀；有接口即聲明，故不是 Decl/Impl 拆分）：
///   - `SvcDks.cs`（本檔）：可換策略、表頭構造、鍵集/驗證、讀行工具、單音節純函數、擇源（回退缺音／按頻擇源）。
///   - `SvcDks.舊流程.cs`：saffes 中古倒推那三步（SaffesToDkz／UpdateDkzFile／UpdateDks），用規則檔 + SrsReplacer。
///   - `SvcDks.新流程.cs`：布之道那幾步（布之道ToDkz／DkzToDks／AttachCangjie／ToDkn／MkDksPhrase），走查表。
/// 所有步驟都只認 TextReader/TextWriter（或純內存對象），不認路徑、不碰文件系統；
/// 開檔/關檔/建目錄/複製檔案是端點（RimeTools.Scripts）的事。
public partial class SvcDks:ISvcDks{
	/// 可換策略（解析/寫出/詞頻源）；構造時注入，方法體內不再各帶配置。
	public DksCfg Cfg{get;}

	public SvcDks(DksCfg Cfg){
		this.Cfg = Cfg;
	}

	// ---- 單音節：拼式 ↔ 音節 ↔ 三拼碼（純函數）----

	public Tswg上古漢語音節 Mk上古漢語音節FromDkp(str DkpSpelling){
		// 義符表查首字得聲母、聲符表查聲符字形得韻，再按最後一個元音切成介腹/尾調。
		return Dkp拼式Parser.Parse(DkpSpelling);
	}

	public Tswg上古漢語音節 Mk上古漢語音節From布之道(str 布之道Spelling){
		// 先按適配規則改寫布之道拼式（去括號、ɫ→j、末 h→s、清化→ʰ、韻尾 i/u→j/w 等），
		// 再走與 dkp 同一套純音切分，故兩邊得到的音節形態一致。
		return 布之道拼式Parser.Parse(布之道Spelling);
	}

	public str? ToDks(Tswg上古漢語音節 z){
		// step 1: 首鍵。
		if(!DksKeyboard.首鍵Of聲母.TryGetValue(z.聲母, out var 首鍵)){
			return null;
		}
		// step 2: 次鍵——R 鍵(收 r/j)與 J 鍵(收 船 ml／邪 sl)都靠**介腹有沒有 r** 區分讀音：
		//   完整拼式裏不存在 jr（腳註[1]），也不存在 ml／sl 帶 r 的組合，故介腹那一格可以借來當標記。
		//     R 鍵：聲母 r ⇒ 介腹取帶 r 那一形；聲母 j ⇒ 用原形。
		//     J 鍵：聲母 ml ⇒ 介腹取帶 r 那一形（船）；聲母 sl ⇒ 用原形（邪）。
		//   例：辣 聲母 r、介腹 ˁa ⇒ 查 rˁa ⇒ 次鍵 Y ⇒ ryt；
		//       神 聲母 ml、介腹 i ⇒ 查 ri ⇒ 次鍵 E ⇒ jes；涎 聲母 sl、介腹 a ⇒ 原形 ⇒ 次鍵 A ⇒ jad；
		//       礦 聲母 kʷ、介腹本來就帶 r ⇒ 原樣查 ⇒ iyw。
		var 介腹 = (z.聲母 == "r" || z.聲母 == "ml") ? "r" + z.介腹 : z.介腹;
		if(!DksKeyboard.次鍵Of介腹.TryGetValue(介腹, out var 次鍵)){
			return null;
		}
		// step 3: 末鍵。
		if(!DksKeyboard.末鍵Of尾調.TryGetValue(z.尾調, out var 末鍵)){
			return null;
		}
		// step 4: dks.dict.yaml 的碼欄是小寫，故這裏統一轉小寫。
		return $"{首鍵}{次鍵}{末鍵}".ToLowerInvariant();
	}

	// ---- 擇源：Dks3 回退缺音 ----

	public async Task<nil> 回退缺音(IFnCtx Ctx, TextReader 新表, TextReader 舊表, TextWriter 出, CT Ct){
		// step 1: 讀兩側（擇源要按字比對整表，故兩邊都物化；量級 ~2-4 萬行）。
		var 新行 = await Mk讀出行(新表, Ct);
		var 舊行 = await Mk讀出行(舊表, Ct);

		// step 2: 新側按字收集全部碼，用來判「布之道側缺音」：字不在新表，或在新表但全部碼都空。
		var 新碼 = new Dictionary<str, List<str>>();
		foreach(var line in 新行){
			var 字 = line.text;
			if(string.IsNullOrEmpty(字)){
				continue;
			}
			if(!新碼.TryGetValue(字, out var list)){
				list = new List<str>();
				新碼[字] = list;
			}
			list.Add(line.code ?? "");
		}

		// step 3: 收「缺音字的舊行」為回退塊（舊表本來就空碼的字頭行不回退——回退是為了補讀音）。
		var 回退 = new List<DictLine>();
		var 舊組 = Mk行分組(舊行);
		foreach(var kv in 舊組){
			var 缺 = true;
			if(新碼.TryGetValue(kv.Key, out var 碼s)){
				foreach(var 碼 in 碼s){
					if(碼.Length > 0){
						缺 = false;
						break;
					}
				}
			}
			if(!缺){
				continue;
			}
			foreach(var line in kv.Value){
				if(string.IsNullOrEmpty(line.code)){
					continue;
				}
				回退.Add(line);
			}
		}

		// step 4: 組最終行——新側全部保留（原序），回退塊追加在末尾。
		var 有序 = new List<DictLine>(新行.Count + 回退.Count);
		foreach(var line in 新行){
			有序.Add(MkCopy(line));
		}
		有序.AddRange(回退);

		// step 5: 產出驗證（與 DkzToDks 同一把尺），再寫出。
		MkAssert三鍵(有序, "回退缺音後");
		await Cfg.Writer.Write(出, MakeDksHeader(), ToAsy(有序), Ct);
		return NIL;
	}

	// ---- 擇源：Dks4 按字頻擇源 ----

	public async Task<nil> 按頻擇源(
		IFnCtx Ctx, TextReader 新表, TextReader 舊表, IReadOnlySet<str> Dkp覆蓋字,
		i32 高頻名次上限, TextWriter 出, CT Ct){
		// step 1: 讀兩側並按字分組。
		var 新行 = await Mk讀出行(新表, Ct);
		var 舊行 = await Mk讀出行(舊表, Ct);
		var 新組 = Mk行分組(新行);
		var 舊組 = Mk行分組(舊行);

		// step 2: essay.txt 的漢字頻率排名前 N（單字條目按頻數降序，詞條不佔名次）。
		var 高頻字 = await Mk高頻字集(要求詞頻(), 高頻名次上限, Ct);

		// step 3: 逐字定來源（走兩側字的併集，**保證不缺字**）：
		//   ① dkp 覆蓋者 → 新側（dkp 解出的碼），不受頻率影響；
		//   ② 否則新側沒有該字（布之道缺音）→ 舊側（中古倒推）——布之道沒有的字不能就此丟掉；
		//   ③ 否則字頻排名 ≤ 上限且有舊讀音 → 舊側（中古倒推）；
		//   ④ 其餘 → 新側（布之道擬音）。
		var 取舊 = new HashSet<str>();
		foreach(var 字 in 新組.Keys.Union(舊組.Keys)){
			if(Dkp覆蓋字.Contains(字)){
				continue;
			}
			if(!舊組.ContainsKey(字)){
				continue; // 舊側也沒這字 → 只能留在新側
			}
			var 新側無 = !新組.ContainsKey(字);
			var 高頻 = 高頻字.Contains(字);
			if(新側無 || 高頻){
				取舊.Add(字);
			}
		}

		// step 4: 組最終行——先按新側原序（跳過改用舊側讀音的字），再把那些字的舊行追加在末尾。
		var 終 = new List<DictLine>();
		foreach(var line in 新行){
			if(!string.IsNullOrEmpty(line.text) && 取舊.Contains(line.text)){
				continue;
			}
			終.Add(MkCopy(line));
		}
		foreach(var 字 in 取舊){
			foreach(var line in 舊組[字]){
				終.Add(line);
			}
		}

		// step 5: 產出驗證，再寫出。
		MkAssert三鍵(終, "按頻擇源後");
		await Cfg.Writer.Write(出, MakeDksHeader(), ToAsy(終), Ct);
		return NIL;
	}

	// ---- 公用件（表頭 / 驗證 / 讀行 / 分組）----

	/// 讀一份 dict.yaml 文本為行列表（供需要整表比對或並行處理的步驟用）。
	/// 例：擇源類步驟要按字比對兩側整表，故必須物化；純變換類步驟（如 DkzToDks）也物化以便 Parallel.For。
	private async Task<List<IDictLine>> Mk讀出行(TextReader Reader, CT Ct){
		var rows = new List<IDictLine>();
		var doc = await Cfg.Parser.Parse(Reader, Ct);
		await foreach(var line in doc.Body.WithCancellation(Ct)){
			rows.Add(line);
		}
		return rows;
	}

	/// 取詞頻源；未注入時拋出點明原因的異常（而不是讓 null 傳播成 NullReference）。
	private IWordFreqSource 要求詞頻(){
		return Cfg.WordFreq ?? throw new InvalidOperationException(
			"本次步驟需要詞頻源：請在 DksCfg.WordFreq 注入 IWordFreqSource（如 EssayWordFreqSource）。");
	}

	/// dks 方案「用到的鍵」集（小寫）= 三張鍵位表裏出現過的全部鍵。
	/// 例：Q..P/A..L/;/Z..M/`,`/`.` 共 29 個；由表推得，不另立硬編碼字母表。
	private static HashSet<char> MkDks鍵集(){
		var ans = new HashSet<char>();
		foreach(var k in DksKeyboard.首鍵Of聲母.Values){
			ans.Add(char.ToLowerInvariant(k));
		}
		foreach(var k in DksKeyboard.次鍵Of介腹.Values){
			ans.Add(char.ToLowerInvariant(k));
		}
		foreach(var k in DksKeyboard.末鍵Of尾調.Values){
			ans.Add(char.ToLowerInvariant(k));
		}
		return ans;
	}

	/// 檢查一個 dks 碼；合格返回 null，否則返回原因。
	/// 合格 = 恰好 3 個字符，且每個字符都在鍵集裏（例：iyw、ryt、,zh；大寫亦算合格）。
	private static str? Mk碼問題(str? 碼, HashSet<char> 鍵集){
		var v = 碼 ?? "";
		if(v.Length == 3){
			foreach(var ch in v){
				if(!鍵集.Contains(char.ToLowerInvariant(ch))){
					return $"含非 dks 鍵的字符 [{ch}]";
				}
			}
			return null;
		}
		if(v.Length == 0){
			return "碼為空(查不到鍵,或該行本無讀音)";
		}
		return $"碼長 {v.Length} ≠ 3";
	}

	/// 產出驗證：dks 每一行碼必須恰為 3 個鍵位字符；不合格即拋出並列出全部問題行。
	/// 這是 Rime 側的硬約束（dks.dict.yaml 的碼欄只有這一種形態），故在寫檔前攔住，不會寫出半成品。
	private static void MkAssert三鍵(IReadOnlyList<DictLine> 行, str 步驟名){
		var 鍵集 = MkDks鍵集();
		var 問題行 = new List<str>();
		foreach(var line in 行){
			var 原因 = Mk碼問題(line.code, 鍵集);
			if(原因 is not null){
				問題行.Add($"{line.text}\t碼=[{line.code}]\t{原因}");
			}
		}
		if(問題行.Count > 0){
			throw new InvalidOperationException(
				$"dks 產出驗證失敗({步驟名}): 共 {問題行.Count} 行的碼不是合法的三鍵碼"
				+ "(必須恰為 3 個 dks 方案用到的鍵)。\n"
				+ string.Join("\n", 問題行.OrderBy(x => x, StringComparer.Ordinal)));
		}
	}

	/// 複製一行（DictLine 是字典，直接 AddRange 會共用引用；這裏逐鍵拷貝）。
	private static DictLine MkCopy(IDictLine Line){
		var ans = new DictLine();
		foreach(var kv in Line){
			ans[kv.Key] = kv.Value;
		}
		return ans;
	}

	/// 把一串行按首列字分組（同一字的多個讀音保序）。
	private static Dictionary<str, List<DictLine>> Mk行分組(IEnumerable<IDictLine> Lines){
		var ans = new Dictionary<str, List<DictLine>>();
		foreach(var line in Lines){
			var 字 = line.text;
			if(string.IsNullOrEmpty(字)){
				continue;
			}
			if(!ans.TryGetValue(字, out var list)){
				list = new List<DictLine>();
				ans[字] = list;
			}
			list.Add(MkCopy(line));
		}
		return ans;
	}

	/// 從詞頻源取「漢字頻率排名前 N」的字集：按頻數降序枚舉，數到第 N 個**單字**條目為止。
	/// 例：Limit=5000 ⇒ 取 essay.txt 裏頻數最高的 5000 個單字（詞條不佔名次）。
	private static async Task<HashSet<str>> Mk高頻字集(IWordFreqSource Freq, i32 Limit, CT Ct){
		var ans = new HashSet<str>();
		if(Limit <= 0){
			return ans;
		}
		await foreach(var wf in Freq.Enumerate(Ct)){
			if(TextUtil.SplitByRune(wf.Text).Count != 1){
				continue;
			}
			ans.Add(wf.Text);
			if(ans.Count >= Limit){
				break;
			}
		}
		return ans;
	}

	/// 是否為單字（按 Unicode 碼點計，CJK 擴展區字符是代理對、UTF-16 Length=2，不能用 Length 判）。
	private static bool IsSingleChar(str Text){
		return TextUtil.SplitByRune(Text).Count == 1;
	}

	/// 通用簡表頭（對應 Dict.getSimpleHead(name)）。
	private static RimeDictHeader MakeSimpleHeader(str Name){
		return new RimeDictHeader([
			new HeaderItem("name", Name, null),
			new HeaderItem("version", "", null),
			new HeaderItem("sort", "by_weight", null),
			new HeaderItem("use_preset_vocabulary", "true", null),
		]);
	}

	/// dks 專用表頭（帶 import_tables）。
	private static RimeDictHeader MakeDksHeader(){
		return new RimeDictHeader([
			new HeaderItem("name", "dks", null),
			new HeaderItem("version", "", null),
			new HeaderItem("sort", "by_weight", null),
			new HeaderItem("use_preset_vocabulary", "true", null),
			new HeaderItem("import_tables", null, ["nonKanji", "chineseDict"]),
		]);
	}

	/// dks_phrase 表頭（columns 三列）。
	private static RimeDictHeader MakePhraseHeader(){
		return new RimeDictHeader([
			new HeaderItem("name", "dks_phrase", null),
			new HeaderItem("version", "", null),
			new HeaderItem("sort", "by_weight", null),
			new HeaderItem("columns", null, DictColumns.Default),
			new HeaderItem("use_preset_vocabulary", "false", null),
		]);
	}

	/// 把一個已物化的集合轉成惰性流（寫出器只吃 IAsyncEnumerable）。
	private static async IAsyncEnumerable<T> ToAsy<T>(IEnumerable<T> Items){
		foreach(var it in Items){
			yield return it;
		}
		await Task.CompletedTask;
	}
}
