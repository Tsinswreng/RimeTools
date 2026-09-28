namespace Dks.Core.Svc;

using System.Collections.Concurrent;
using Dks.Core;
using RimeTools.Shared.Dict.Lookup;
using RimeTools.Shared.Dict.Models;
using RimeTools.Shared.Phrase;
using RimeTools.Tools;
using Tsinswreng.CsCtx;

/// ISvcDks 的實現（本檔：**新 Dks2 流程**——布之道擬音 → 查表轉三拼 → dks_v／dkn／dks_phrase）。
/// 轉換全走查表（DksKeyboard 三層鍵位表、Dkp拼式Parser、布之道拼式Parser），不用正則。
/// 只認 reader/writer，不碰文件系統。
public partial class SvcDks{
	public async Task<nil> 布之道ToDkz(IFnCtx Ctx, TextReader 布之道表, TextWriter Dkz, CT Ct){
		// step 1: 解析布之道原表（碼欄 `ASCII*IPA`，ASCII 段只供其方案自身打字用）。
		var doc = await Cfg.Parser.Parse(布之道表, Ct);
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
			var ipa = MkIpa段(line.code ?? "");
			if(ipa.Length == 0){
				return;
			}
			var 音節 = 布之道拼式Parser.Parse(ipa);
			if(音節.Full.Length == 0){
				return;
			}
			var outLine = new DictLine{
				[DictColumns.Text] = text,
				[DictColumns.Code] = 音節.Full,
			};
			if(line.weight is not null){
				outLine[DictColumns.Weight] = line.weight;
			}
			made[i] = outLine;
		});

		// step 3: 按原行序收集，寫出 dkz 文檔（表頭 name: dkz）。
		var body = Mk收集(made);
		await Cfg.Writer.Write(Dkz, MakeSimpleHeader("dkz"), ToAsy(body), Ct);
		return NIL;
	}

	public async Task<nil> DkzToDks(IFnCtx Ctx, TextReader Dkz, TextWriter Dks, CT Ct){
		// step 1: 解析 dkz（可能含布之道來的純音行與 dkp 併入的字形拼式行）。
		var doc = await Cfg.Parser.Parse(Dkz, Ct);
		var rows = new List<IDictLine>();
		await foreach(var line in doc.Body.WithCancellation(Ct)){
			rows.Add(line);
		}

		// step 2: 逐行「解音節 → 查三層鍵位表」。並行: 純函數、無共享狀態。
		//         產出驗證: 每行碼必須恰為 3 個鍵位字符，不合格者收集起來，寫檔前一次報錯。
		//         碼為空的 dkz 行（該字本無讀音）原樣留空——空碼是合法形態，與「查不到鍵」不同。
		var 鍵集 = MkDks鍵集();
		var 問題行 = new ConcurrentBag<str>();
		var made = new DictLine?[rows.Count];
		Parallel.For(0, rows.Count, i => {
			var line = rows[i];
			if(string.IsNullOrEmpty(line.text) || !IsSingleChar(line.text)){
				return;
			}
			var raw = line.code ?? "";
			var code = "";
			if(raw.Length > 0){
				var 音節 = Dkp拼式Parser.Parse(raw);
				code = ToDks(音節) ?? "";
				if(code.Length == 0){
					問題行.Add($"{line.text}\t全拼=[{raw}]\t音節=({音節.聲母}|{音節.介腹}|{音節.尾調})\t查不到鍵");
				}
			}
			var outLine = new DictLine{
				[DictColumns.Text] = line.text,
				[DictColumns.Code] = code,
			};
			if(line.weight is not null){
				outLine[DictColumns.Weight] = line.weight;
			}
			made[i] = outLine;
		});
		if(!問題行.IsEmpty){
			throw new InvalidOperationException(
				$"dks 產出驗證失敗(DkzToDks): 共 {問題行.Count} 行的碼不是合法的三鍵碼"
				+ "(必須恰為 3 個 dks 方案用到的鍵)。\n"
				+ string.Join("\n", 問題行.OrderBy(x => x, StringComparer.Ordinal)));
		}

		// step 3: 按原行序收集，寫出 dks 文檔（表頭與 UpdateDks 一致）。
		var body = Mk收集(made);
		await Cfg.Writer.Write(Dks, MakeDksHeader(), ToAsy(body), Ct);
		return NIL;
	}

	public async Task<nil> AttachCangjie(
		IFnCtx Ctx, TextReader Dks, TextReader Cangjie, TextWriter DksV, CT Ct){
		// step 1: 解析倉頡原表並套 Cangjie 整理規則。
		//         倉頡行分兩種：普通碼行與空碼行(表示無輔助碼)。同一字可有多行(多碼)，保留重複行——
		//         SQL left join 語義不去重。規則含「取首尾簡碼」收斂(^(.)(.)(.)$→$1$3 等)。
		var cjDoc = await Cfg.Parser.Parse(Cangjie, Ct);
		var cjRows = new List<IDictLine>();
		await foreach(var line in cjDoc.Body.WithCancellation(Ct)){
			cjRows.Add(line);
		}
		var cjRules = DksRegexRules.Cangjie();
		var cjMade = new str?[cjRows.Count];
		var cjTexts = new str?[cjRows.Count];
		Parallel.For(0, cjRows.Count, i => {
			var line = cjRows[i];
			if(string.IsNullOrEmpty(line.text)){
				return;
			}
			cjTexts[i] = line.text;
			cjMade[i] = SrsReplacer.ApplyAll(line.code ?? "", cjRules);
		});
		// step 2: 合成「字 → 倉頡碼列表」（保留重複行 = join 語義）。
		var cjMap = new Dictionary<str, List<str>>();
		for(var i = 0; i < cjRows.Count; i++){
			var text = cjTexts[i];
			if(text is null){
				continue;
			}
			if(!cjMap.TryGetValue(text, out var list)){
				list = new List<str>();
				cjMap[text] = list;
			}
			list.Add(cjMade[i] ?? "");
		}

		// step 3: 解析 dks（單字表），每字左聯倉頡碼——dks 的每一行(含空碼行)與該字倉頡全部行 join：
		//         結果 code = 主碼 + 倉頡碼(倉頡碼為空時即主碼本身)；權重沿用 dks 行。
		//         並行: 逐行查表 + 拼接，只讀 cjMap。
		var dksDoc = await Cfg.Parser.Parse(Dks, Ct);
		var dksRows = new List<IDictLine>();
		await foreach(var line in dksDoc.Body.WithCancellation(Ct)){
			dksRows.Add(line);
		}
		var made = new List<DictLine>?[dksRows.Count];
		Parallel.For(0, dksRows.Count, i => {
			var line = dksRows[i];
			if(string.IsNullOrEmpty(line.text)){
				return;
			}
			var mainCode = line.code ?? "";
			var list = new List<DictLine>();
			if(!cjMap.TryGetValue(line.text, out var cjs) || cjs.Count == 0){
				// 倉頡完全無此字 → 只保留主碼行。
				list.Add(MkDksVLine(line.text, mainCode, line.weight));
			}
			else{
				foreach(var cj in cjs){
					list.Add(MkDksVLine(line.text, mainCode + cj, line.weight));
				}
			}
			made[i] = list;
		});
		var body = new List<DictLine>();
		foreach(var one in made){
			if(one is not null){
				body.AddRange(one);
			}
		}

		// step 4: 寫出 dks_v 文檔（表頭 name: dks_v）。
		await Cfg.Writer.Write(DksV, MakeSimpleHeader("dks_v"), ToAsy(body), Ct);
		return NIL;
	}

	/// 造 dks_v 行：text + code + 可選 weight。
	private static DictLine MkDksVLine(str Text, str Code, str? Weight){
		var line = new DictLine{
			[DictColumns.Text] = Text,
			[DictColumns.Code] = Code,
		};
		if(Weight is not null){
			line[DictColumns.Weight] = Weight;
		}
		return line;
	}

	public async Task<nil> ToDkn(IFnCtx Ctx, TextReader Dks, TextWriter Dkn, CT Ct){
		// step 1: 解析 dks。
		var doc = await Cfg.Parser.Parse(Dks, Ct);
		var rows = new List<IDictLine>();
		await foreach(var line in doc.Body.WithCancellation(Ct)){
			rows.Add(line);
		}

		// step 2: 每行碼取首尾碼（HeadTail）；表頭 name 改為 dkn；保留權重與空碼行（對齊 Dks.sh 的 awk：只改第二欄）。
		//         並行: 純字串變換。
		var made = new DictLine?[rows.Count];
		Parallel.For(0, rows.Count, i => {
			var line = rows[i];
			if(string.IsNullOrEmpty(line.text) || !IsSingleChar(line.text)){
				return;
			}
			var code = line.code ?? "";
			var outLine = new DictLine{
				[DictColumns.Text] = line.text,
				[DictColumns.Code] = code.Length == 0 ? "" : CodeTransformer.HeadTail(code),
			};
			if(line.weight is not null){
				outLine[DictColumns.Weight] = line.weight;
			}
			made[i] = outLine;
		});

		// step 3: 寫出 dkn 文檔。
		var body = Mk收集(made);
		await Cfg.Writer.Write(Dkn, MakeSimpleHeader("dkn"), ToAsy(body), Ct);
		return NIL;
	}

	public async Task<nil> MkDksPhrase(IFnCtx Ctx, TextReader Dks, TextWriter Phrase, CT Ct){
		// step 1: 以讀入的 dks 表建反查索引（字 → 碼）。
		var doc = await Cfg.Parser.Parse(Dks, Ct);
		var lookup = await MemoryCharCodeLookup.FromDocAsync(doc, Ct);

		// step 2: 造詞（詞頻 × 反查 × HeadTail 策略 → 詞條流）；詞頻源由 Cfg 注入。
		var maker = new PhraseMaker();
		var mkr = new PhraseMkr_HeadEtTail();
		var phraseBody = maker.MakePhrases(Ctx, 要求詞頻(), lookup, mkr, Ct);

		// step 3: 寫出 dks_phrase 文檔（columns: text/code/weight，use_preset_vocabulary: false）。
		await Cfg.Writer.Write(Phrase, MakePhraseHeader(), phraseBody, Ct);
		return NIL;
	}

	/// 取布之道碼欄 `ASCII*IPA` 中 `*` 之後的 IPA 段；無 `*` 時原樣返回。
	/// 例：prXu*prˤu → prˤu；pʰrˤuh → pʰrˤuh。
	private static str MkIpa段(str Code){
		var k = Code.IndexOf('*');
		return k < 0 ? Code : Code[(k + 1)..];
	}
}
