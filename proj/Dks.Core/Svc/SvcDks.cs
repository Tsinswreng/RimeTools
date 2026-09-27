namespace Dks.Core.Svc;

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