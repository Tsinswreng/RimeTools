namespace Dks.Core.Svc;

using System.Collections.Concurrent;
using System.Text;
using Dks.Core;
using RimeTools.Shared.Dict.Lookup;
using RimeTools.Shared.Dict.Models;
using RimeTools.Shared.Dict.Parser;
using RimeTools.Shared.Phrase;
using RimeTools.Tools;
using Tsinswreng.CsCtx;

/// ISvcDks 的文件實現：七步流水線，等效 ngaq dks.ts 的 Dks.run() + AttachCangjie.run + 後製 dkn。
/// 每一步直接操作 DksCfg 指向的目錄：原表目錄(SrcTableDir)是中間產物的家，
/// 用戶目錄(UserDataDir)是最終產物的家。
public class SvcDks(DksCfg Cfg):ISvcDks{
	// ---- 音節：拼式 ↔ 三拼碼（純查表，無模式匹配）----

	public Tswg上古漢語音節 Mk上古漢語音節FromDkp(str DkpSpelling){
		// 義符表查首字得聲母、聲符表查聲符字形得韻，再按最後一個元音切成介腹/尾調。
		return Dkp拼式Parser.Parse(DkpSpelling);
	}

	public Tswg上古漢語音節 Mk上古漢語音節From布之道(str 布之道Spelling){
		// 先按適配規則改寫布之道拼式（去括號、ɫ→j、末 h→s、清化→ʰ、韻尾 i/u→j/w 等），
		// 再走與 dkp 同一套純音切分，故兩邊得到的音節形態一致。
		return 布之道拼式Parser.Parse(布之道Spelling);
	}

	public str ToDks(Tswg上古漢語音節 z){
		// 三段各查一張鍵位表（DksKeyboard）；任何一段查不到即視為無法編碼，回空串。
		if(!DksKeyboard.首鍵Of聲母.TryGetValue(z.聲母, out var 首鍵)){
			return "";
		}
		// R 鍵(收 r/j)與 J 鍵(收 船 ml／邪 sl)都靠**介腹有沒有 r** 區分讀音：
		// 完整拼式裏不存在 jr（腳註[1]），也不存在 ml／sl 帶 r 的組合，故介腹那一格可以借來當標記。
		//   R 鍵：聲母 r ⇒ 介腹取帶 r 那一形；聲母 j ⇒ 用原形。
		//   J 鍵：聲母 ml ⇒ 介腹取帶 r 那一形（船）；聲母 sl ⇒ 用原形（邪）。
		// 例：辣 聲母 r、介腹 ˁa ⇒ 查 rˁa ⇒ 次鍵 Y ⇒ ryt；
		//     神 聲母 ml、介腹 i ⇒ 查 ri ⇒ 次鍵 E ⇒ jes（mlAks 的射 ⇒ 查 rA ⇒ H ⇒ jhg）；
		//     涎 聲母 sl、介腹 a ⇒ 用原形 ⇒ 次鍵 A ⇒ jad（與神 jes 區分得開）；
		//     礦 聲母 kʷ、介腹本來就帶 r ⇒ 原樣查 ⇒ iyw。
		var 介腹 = (z.聲母 == "r" || z.聲母 == "ml") ? "r" + z.介腹 : z.介腹;
		if(!DksKeyboard.次鍵Of介腹.TryGetValue(介腹, out var 次鍵)){
			return "";
		}
		if(!DksKeyboard.末鍵Of尾調.TryGetValue(z.尾調, out var 末鍵)){
			return "";
		}
		// dks.dict.yaml 的碼欄是小寫，故這裏統一轉小寫。
		return $"{首鍵}{次鍵}{末鍵}".ToLowerInvariant();
	}

	// ---- Dks2：布之道擬音 → 全表（第 1′、3′ 步；其餘步驟與 Dks 共用）----

	public async Task<nil> 布之道ToDkz(IFnCtx Ctx, CT Ct){
		// step 1: 解析布之道原表（碼欄 `ASCII*IPA`，ASCII 段只供其方案自身打字用）。
		var doc = await Cfg.Parser.Parse(Cfg.布之道DictPath, Ct);
		var rows = new List<IDictLine>();
		await foreach(var line in doc.Body.WithCancellation(Ct)){
			rows.Add(line);
		}

		// step 2: 逐行取 `*` 後的 IPA 段 → 適配成本方案音節 → 碼欄寫 Full。
		//         並行: 這步是純 CPU + 查表、每行互不相干，故整批並行；行序用下標釘住。
		var made = new DictLine?[rows.Count];
		Parallel.For(0, rows.Count, i => {
			var line = rows[i];
			var text = line.text;
			if(string.IsNullOrEmpty(text) || !IsSingleChar(text)){
				return;
			}
			var ipa = Ipa段Of(line.code ?? "");
			if(ipa.Length == 0){
				return;
			}
			var 音節 = 布之道拼式Parser.Parse(ipa);
			if(音節.Full.Length == 0){
				return;
			}
			var outLine = new DictLine{
				["text"] = text,
				["code"] = 音節.Full,
			};
			if(line.weight is not null){
				outLine["weight"] = line.weight;
			}
			made[i] = outLine;
		});

		// step 3: 按原行序收集（跳過被過濾與解析不出者），寫出 dkz.dict.yaml。
		var body = new List<DictLine>();
		foreach(var one in made){
			if(one is not null){
				body.Add(one);
			}
		}
		var header = MakeSimpleHeader("dkz");
		await Cfg.Writer.Write(SrcPath("dkz.dict.yaml"), header, ToAsy(body), Ct);
		return NIL;
	}

	/// 取布之道碼欄 `ASCII*IPA` 中 `*` 之後的 IPA 段；無 `*` 時原樣返回。
	/// 例：prXu*prˤu → prˤu；pʰrˤuh → pʰrˤuh。
	private static str Ipa段Of(str Code){
		var k = Code.IndexOf('*');
		return k < 0 ? Code : Code[(k + 1)..];
	}

	public async Task<nil> DkzToDks(IFnCtx Ctx, CT Ct){
		// step 1: 解析 dkz（可能含布之道來的純音行與 dkp 併入的字形拼式行）。
		var dkzDoc = await Cfg.Parser.Parse(SrcPath("dkz.dict.yaml"), Ct);
		var rows = new List<IDictLine>();
		await foreach(var line in dkzDoc.Body.WithCancellation(Ct)){
			rows.Add(line);
		}

		// step 2: 逐行「解音節 → 查三層鍵位表」。並行: 純函數、無共享狀態。
		//         碼為空的行（有些字只有字頭無讀音）原樣保留空碼。
		//         產出驗證: 每行碼必須恰為 3 個 dks 鍵位字符;不合格的行收集起來,寫檔前一次報錯。
		var 鍵集 = MkDks鍵集();
		var 問題行 = new ConcurrentBag<str>();
		var made = new DictLine?[rows.Count];
		Parallel.For(0, rows.Count, i => {
			var line = rows[i];
			if(string.IsNullOrEmpty(line.text) || !IsSingleChar(line.text)){
				return;
			}
			var raw = line.code ?? "";
			var code = raw.Length == 0 ? "" : ToDks(Dkp拼式Parser.Parse(raw));
			var outLine = new DictLine{
				["text"] = line.text,
				["code"] = code,
			};
			if(line.weight is not null){
				outLine["weight"] = line.weight;
			}
			made[i] = outLine;
			// 產出驗證: dks.dict.yaml 的碼欄只有「三個鍵位字符」這一種形態。
			var 原因 = Mk碼問題(code, 鍵集);
			if(原因 is not null){
				var 音 = Dkp拼式Parser.Parse(raw);
				問題行.Add($"{line.text}\t全拼=[{raw}]\t音節=({音.聲母}|{音.介腹}|{音.尾調})\t碼=[{code}]\t{原因}");
			}
		});
		if(!問題行.IsEmpty){
			throw new InvalidOperationException(
				$"dks 產出驗證失敗: 共 {問題行.Count} 行的碼不是合法的三鍵碼(必須恰為 3 個 dks 方案用到的鍵)。\n"
				+ string.Join("\n", 問題行.OrderBy(x => x, StringComparer.Ordinal)));
		}

		// step 3: 按原行序收集，寫出 dks.dict.yaml（表頭與 UpdateDks 一致）。
		var body = new List<DictLine>();
		foreach(var one in made){
			if(one is not null){
				body.Add(one);
			}
		}
		var header = MakeDksHeader();
		await Cfg.Writer.Write(UserPath("dks.dict.yaml"), header, ToAsy(body), Ct);
		return NIL;
	}

	// ---- Dks3：回退缺音 ----

	public async Task<nil> 回退缺音(IFnCtx Ctx, str 舊Dks路徑, CT Ct){
		// step 1: 讀 Dks2 那份（UserDataDir/dks.dict.yaml）與舊流程那份（入參路徑）。
		var 新行 = new List<IDictLine>();
		var 新Doc = await Cfg.Parser.Parse(UserPath("dks.dict.yaml"), Ct);
		await foreach(var line in 新Doc.Body.WithCancellation(Ct)){
			新行.Add(line);
		}
		var 舊行 = new List<IDictLine>();
		var 舊Doc = await Cfg.Parser.Parse(舊Dks路徑, Ct);
		await foreach(var line in 舊Doc.Body.WithCancellation(Ct)){
			舊行.Add(line);
		}

		// step 2: 新表按字收集全部碼，用來判「布之道側缺音」：字不在新表，或在新表但全部碼都空。
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

		// step 3: 組最終行——新表全部保留（原序），再把缺音字的舊行追加在末尾。
		var 終 = new List<DictLine>();
		var 舊轉新 = new Dictionary<str, List<DictLine>>();   // 舊行按字分組，免得逐行重複掃
		foreach(var line in 舊行){
			var 字 = line.text;
			if(string.IsNullOrEmpty(字)){
				continue;
			}
			if(!舊轉新.TryGetValue(字, out var list)){
				list = new List<DictLine>();
				舊轉新[字] = list;
			}
			var copy = new DictLine();
			foreach(var kv in line){
				copy[kv.Key] = kv.Value;
			}
			list.Add(copy);
		}
		var 回退字 = new HashSet<str>();
		var 回退行 = 0;
		foreach(var kv in 舊轉新){
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
			// 舊表裏本來就空碼的行（無讀音字頭行）不回退——回退是為了「補讀音」，
			// 補一個空碼沒有意義，也會讓產出驗證報錯。
			foreach(var line in kv.Value){
				if(string.IsNullOrEmpty(line.code)){
					continue;
				}
				終.Add(line);
				回退行++;
				回退字.Add(kv.Key);
			}
		}

		// step 4: 產出驗證（與 DkzToDks 同一把尺），再把新行接在後面。
		var 鍵集 = MkDks鍵集();
		var 問題行 = new List<str>();
		foreach(var line in 新行){
			var 碼 = line.code ?? "";
			var 原因 = Mk碼問題(碼, 鍵集);
			if(原因 is not null){
				問題行.Add($"{line.text}\t碼=[{碼}]\t{原因}");
			}
		}
		foreach(var line in 終){
			var 碼 = line.code ?? "";
			var 原因 = Mk碼問題(碼, 鍵集);
			if(原因 is not null){
				問題行.Add($"{line.text}\t碼=[{碼}]\t{原因}");
			}
		}
		if(問題行.Count > 0){
			throw new InvalidOperationException(
				$"dks 產出驗證失敗(回退缺音後): 共 {問題行.Count} 行的碼不是合法的三鍵碼。\n"
				+ string.Join("\n", 問題行.OrderBy(x => x, StringComparer.Ordinal)));
		}

		// step 5: 寫回 dks.dict.yaml：Dks2 的行在前（原序），回退的行在後。
		var body = new List<DictLine>();
		foreach(var line in 新行){
			var copy = new DictLine();
			foreach(var kv in line){
				copy[kv.Key] = kv.Value;
			}
			body.Add(copy);
		}
		body.AddRange(終);
		var header = MakeDksHeader();
		await Cfg.Writer.Write(UserPath("dks.dict.yaml"), header, ToAsy(body), Ct);
		return NIL;
	}

	/// dks 方案「用到的鍵」集（小寫）= 三張鍵位表裏出現過的全部鍵。
	/// 例：Q..P/A..L/;/Z..M/`,`/`.` 共 29 個；由表推得,不另立硬編碼字母表。
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
	private static str? Mk碼問題(str 碼, HashSet<char> 鍵集){
		if(碼.Length == 3){
			foreach(var ch in 碼){
				if(!鍵集.Contains(char.ToLowerInvariant(ch))){
					return $"含非 dks 鍵的字符 [{ch}]";
				}
			}
			return null;
		}
		if(碼.Length == 0){
			return "碼為空(查不到鍵,或該行本無讀音)";
		}
		return $"碼長 {碼.Length} ≠ 3";
	}

	// ---- 步驟 1：saffes → dkz ----

	public async Task<nil> SaffesToDkz(IFnCtx Ctx, CT Ct){
		// step 1: 解析 saffes 原表（大寫前不做單字過濾——原碼對「碼長度 3」之外的行也保留，
		//         但 DictRaw 默認只取首列單字的行，故此處以單字過濾對齊）。
		var saffesDoc = await Cfg.Parser.Parse(SrcPath("saffes.dict.yaml"), Ct);

		// step 2: 對每個字：碼轉大寫 → 套 SaffesToOc 音變規則（自研引擎，全表替換語義）。
		var rules = DksRegexRules.SaffesToOc();
		var body = new List<DictLine>();
		await foreach(var line in saffesDoc.Body.WithCancellation(Ct)){
			var text = line.text;
			var code = line.code;
			if(string.IsNullOrEmpty(text) || !IsSingleChar(text) || string.IsNullOrEmpty(code)){
				continue; // 只處理單字行（對齊 DictRaw.singleCharMode）
			}
			var upper = code.ToUpperInvariant();
			var oc = SrsReplacer.ApplyAll(upper, rules);
			var outLine = new DictLine{
				["text"] = text,
				["code"] = oc,
			};
			if(line.weight is not null){
				outLine["weight"] = line.weight;
			}
			body.Add(outLine);
		}

		// step 3: 寫出 dkz.dict.yaml（表頭 name: dkz）。
		var header = MakeSimpleHeader("dkz");
		await Cfg.Writer.Write(SrcPath("dkz.dict.yaml"), header, body.ToAsyncEnumerable(), Ct);
		return NIL;
	}

	// ---- 步驟 2：用 dkp 的聲符/義符剔除 dkz 字頭，並把 dkp 表體併入 dkz 表體 ----

	public async Task<nil> UpdateDkzFile(IFnCtx Ctx, CT Ct){
		// step 1: 解析 dkp 原表（手工聲符義符擴充表，碼 = 義符字形 + 聲符字形，如 「言成」「手令」）。
		var dkpDoc = await Cfg.Parser.Parse(SrcPath("dkp.dict.yaml"), Ct);

		// step 2: dkp 表體每行碼：先套義符規則(碼首字形→音, 如 ^言→ŋ)、再套聲符規則(碼尾字形→音, 如 成$→eŋ)。
		//         取處理後首列字集作為「dkp 已收字頭」，同時保留處理後行供併入。
		var yiFuRules = DksRegexRules.YiFuRules();
		var shengFuRules = DksRegexRules.ShengFuRules();
		var dkpLines = new List<IDictLine>();
		var dkpCharSet = new HashSet<str>();
		await foreach(var line in dkpDoc.Body.WithCancellation(Ct)){
			if(string.IsNullOrEmpty(line.text) || !IsSingleChar(line.text)){
				continue;
			}
			// step 2a: 碼可空（dkp 有純 text 行如「謇」，表示該字保留但無碼）；
			//          非空碼才套義符(碼首字形→音)與聲符(碼尾字形→音)規則；純音碼如 lʰək 天然不被命中。
			var code = line.code ?? "";
			if(code.Length > 0){
				code = SrsReplacer.ApplyAll(code, yiFuRules);
				code = SrsReplacer.ApplyAll(code, shengFuRules);
			}

			var outLine = new DictLine{
				["text"] = line.text,
				["code"] = code,
			};
			if(line.weight is not null){
				outLine["weight"] = line.weight;
			}
			dkpLines.Add(outLine);
			dkpCharSet.Add(line.text);
		}

		// step 3: 解析 dkz（saffes 表轉換後的單字表），剔除 dkp 已收的字頭。
		var dkzDoc = await Cfg.Parser.Parse(SrcPath("dkz.dict.yaml"), Ct);
		var kept = new List<IDictLine>();
		await foreach(var line in dkzDoc.Body.WithCancellation(Ct)){
			if(!string.IsNullOrEmpty(line.text) && !dkpCharSet.Contains(line.text)){
				kept.Add(line);
			}
		}

		// step 4: 新 dkz 表體 = dkp 表體在前 + 剩餘 dkz 表體在後。
		var outBody = new List<IDictLine>();
		outBody.AddRange(dkpLines);
		outBody.AddRange(kept);

		// step 5: 寫回 SrcTableDir/dkz.dict.yaml（原 dks.ts 是覆寫同一 dkz 檔）。
		var header = MakeSimpleHeader("dkz");
		await Cfg.Writer.Write(SrcPath("dkz.dict.yaml"), header, ToAsy(outBody), Ct);
		return NIL;
	}

	// ---- 步驟 3：dkz → dks ----

	public async Task<nil> UpdateDks(IFnCtx Ctx, CT Ct){
		// step 1: 解析 dkz（已含 dkp 表體）。
		var dkzDoc = await Cfg.Parser.Parse(SrcPath("dkz.dict.yaml"), Ct);
		var rules = DksRegexRules.OcToOc3();

		// step 2: 每行碼：套 OcToOc3 → 小寫；只保留單字行。
		var body = new List<DictLine>();
		await foreach(var line in dkzDoc.Body.WithCancellation(Ct)){
			if(string.IsNullOrEmpty(line.text) || !IsSingleChar(line.text)){
				continue;
			}
			// step 2: 碼可空（dkz 有空碼行，表示該字保留但無讀音），套 OcToOc3 後小寫。
			var raw = line.code ?? "";
			var oc3 = raw.Length == 0 ? "" : SrsReplacer.ApplyAll(raw, rules).ToLowerInvariant();
			var outLine = new DictLine{
				["text"] = line.text,
				["code"] = oc3,
			};
			if(line.weight is not null){
				outLine["weight"] = line.weight;
			}
			body.Add(outLine);
		}

		// step 3: 寫出 dks.dict.yaml（帶 import_tables 表頭，dks 依賴 nonKanji/chineseDict）。
		var header = MakeDksHeader();
		await Cfg.Writer.Write(UserPath("dks.dict.yaml"), header, body.ToAsyncEnumerable(), Ct);
		return NIL;
	}

	// ---- 步驟 4：dks + 倉頡輔助碼 → dks_v ----

	public async Task<nil> AttachCangjie(IFnCtx Ctx, CT Ct){
		// step 1: 解析倉頡原表並套 Cangjie 整理規則。
		//         倉頡行分兩種：普通碼行與空碼行(表示無輔助碼)。同一字可有多行(多碼)，保留重複行——
		//         SQL left join 語義不去重。規則含「取首尾簡碼」收斂(^(.)(.)(.)$→$1$3 等)。
		var cangjieDoc = await Cfg.Parser.Parse(SrcPath("cangjie7-1含五六相異者.yaml"), Ct);
		var cjRules = DksRegexRules.Cangjie();
		var cjMap = new Dictionary<str, List<str>>();
		await foreach(var line in cangjieDoc.Body.WithCancellation(Ct)){
			if(string.IsNullOrEmpty(line.text)){
				continue;
			}
			var raw = line.code ?? "";
			var cj = SrsReplacer.ApplyAll(raw, cjRules);
			if(!cjMap.TryGetValue(line.text, out var list)){
				list = new List<str>();
				cjMap[line.text] = list;
			}
			list.Add(cj); // 不去重：SQL join 保留重複行
		}

		// step 2: 解析 dks（單字表），每字左聯倉頡碼——
		//         dks 的每一行(含空碼行)與該字倉頡全部行 join：
		//         結果 code = 主碼 + 倉頡碼(倉頡碼為空時即主碼本身)；權重沿用 dks 行。
		var dksDoc = await Cfg.Parser.Parse(UserPath("dks.dict.yaml"), Ct);
		var body = new List<DictLine>();
		await foreach(var line in dksDoc.Body.WithCancellation(Ct)){
			if(string.IsNullOrEmpty(line.text)){
				continue;
			}
			var mainCode = line.code ?? "";
			if(!cjMap.TryGetValue(line.text, out var cjs) || cjs.Count == 0){
				// 倉頡完全無此字 → 只保留主碼行。
				body.Add(MkDksVLine(line.text, mainCode, line.weight));
				continue;
			}
			foreach(var cj in cjs){
				body.Add(MkDksVLine(line.text, mainCode + cj, line.weight));
			}
		}

		// step 3: 寫出 dks_v.dict.yaml（表頭 name: dks_v）。
		var header = MakeSimpleHeader("dks_v");
		await Cfg.Writer.Write(UserPath("dks_v.dict.yaml"), header, body.ToAsyncEnumerable(), Ct);
		return NIL;
	}

	/// 造 dks_v 行：text + code + 可選 weight。
	private static DictLine MkDksVLine(str Text, str Code, str? Weight){
		var line = new DictLine{
			["text"] = Text,
			["code"] = Code,
		};
		if(Weight is not null){
			line["weight"] = Weight;
		}
		return line;
	}

	// ---- 步驟 5：拷貝 dkp/dkz 到 UserDataDir ----

	public Task<nil> CopyDkpDkz(IFnCtx Ctx, CT Ct){
		System.IO.File.Copy(SrcPath("dkp.dict.yaml"), UserPath("dkp.dict.yaml"), true);
		System.IO.File.Copy(SrcPath("dkz.dict.yaml"), UserPath("dkz.dict.yaml"), true);
		return Task.FromResult(NIL);
	}

	// ---- 步驟 6：dks → dkn ----

	public async Task<nil> ToDkn(IFnCtx Ctx, CT Ct){
		// step 1: 解析 dks。
		var dksDoc = await Cfg.Parser.Parse(UserPath("dks.dict.yaml"), Ct);

		// step 2: 每行碼取首尾碼（HeadTail）；表頭 name 改為 dkn；保留權重與空碼行（對齊 Dks.sh 的 awk：只改第二欄）。
		var body = new List<DictLine>();
		await foreach(var line in dksDoc.Body.WithCancellation(Ct)){
			if(string.IsNullOrEmpty(line.text) || !IsSingleChar(line.text)){
				continue;
			}
			var code = line.code ?? "";
			var outLine = new DictLine{
				["text"] = line.text,
				["code"] = code.Length == 0 ? "" : CodeTransformer.HeadTail(code),
			};
			if(line.weight is not null){
				outLine["weight"] = line.weight;
			}
			body.Add(outLine);
		}

		// step 3: 寫出 dkn.dict.yaml。
		var header = MakeSimpleHeader("dkn");
		await Cfg.Writer.Write(UserPath("dkn.dict.yaml"), header, body.ToAsyncEnumerable(), Ct);
		return NIL;
	}

	// ---- 步驟 7：造詞 → dks_phrase ----

	public async Task<nil> MkDksPhrase(IFnCtx Ctx, CT Ct){
		// step 1: 以 dks.dict.yaml 建反查索引（字 → 碼）。
		var dksDoc = await Cfg.Parser.Parse(UserPath("dks.dict.yaml"), Ct);
		var lookup = await MemoryCharCodeLookup.FromDocAsync(dksDoc, Ct);

		// step 2: 造詞（詞頻 × 反查 × HeadTail 策略 → 詞條流）。
		var maker = new PhraseMaker();
		var mkr = new PhraseMkr_HeadEtTail();
		var phraseBody = maker.MakePhrases(Ctx, Cfg.WordFreq, lookup, mkr, Ct);

		// step 3: 寫出 dks_phrase.dict.yaml（columns: text/code/weight，use_preset_vocabulary: false）。
		var header = MakePhraseHeader();
		await Cfg.Writer.Write(UserPath("dks_phrase.dict.yaml"), header, phraseBody, Ct);
		return NIL;
	}

	// ---- 內部工具 ----

	private str SrcPath(str FileName) => System.IO.Path.Combine(Cfg.SrcTableDir, FileName);
	private str UserPath(str FileName) => System.IO.Path.Combine(Cfg.UserDataDir, FileName);

	/// 是否為單字（按 Unicode 碼點計，CJK 擴展區字符是代理對、UTF-16 Length=2，不能用 Length 判）。
	private static bool IsSingleChar(str Text)
		=> TextUtil.SplitByRune(Text).Count == 1;

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
			new HeaderItem("columns", null, ["text", "code", "weight"]),
			new HeaderItem("use_preset_vocabulary", "false", null),
		]);
	}

	private static async IAsyncEnumerable<T> ToAsy<T>(IEnumerable<T> Items){
		foreach(var it in Items){
			yield return it;
		}
		await Task.CompletedTask;
	}
}