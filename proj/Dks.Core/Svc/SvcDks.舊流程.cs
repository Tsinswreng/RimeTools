namespace Dks.Core.Svc;

using System.Collections.Concurrent;
using Dks.Core;
using RimeTools.Shared.Dict.Models;
using RimeTools.Tools;
using Tsinswreng.CsCtx;

/// ISvcDks 的實現（本檔：**舊 Dks 流程**——saffes 中古倒推 + dkp 覆蓋 + 規則轉三拼）。
/// 這三步用規則檔（Rules/*.txt）與自研替換引擎 SrsReplacer；新流程見 SvcDks.新流程.cs。
/// 只認 reader/writer，不碰文件系統。
public partial class SvcDks{
	public async Task<nil> SaffesToDkz(IFnCtx Ctx, TextReader Saffes, TextWriter Dkz, CT Ct){
		// step 1: 解析 saffes 原表（大寫前不做單字過濾——原碼對「碼長度 3」之外的行也保留，
		//         但 DictRaw 默認只取首列單字的行，故此處以單字過濾對齊）。
		var doc = await Cfg.Parser.Parse(Saffes, Ct);

		// step 2: 對每個字：碼轉大寫 → 套 SaffesToOc 音變規則（自研引擎，全表替換語義）。
		//         並行: 逐行只做「轉大寫 + 規則鏈替換」，互不共享狀態；行序用下標釘住。
		var rules = DksRegexRules.SaffesToOc();
		var rows = new List<IDictLine>();
		await foreach(var line in doc.Body.WithCancellation(Ct)){
			rows.Add(line);
		}
		var made = new DictLine?[rows.Count];
		Parallel.For(0, rows.Count, i => {
			var line = rows[i];
			var text = line.text;
			var code = line.code;
			if(string.IsNullOrEmpty(text) || !IsSingleChar(text) || string.IsNullOrEmpty(code)){
				return; // 只處理單字行（對齊 DictRaw.singleCharMode）
			}
			var upper = code.ToUpperInvariant();
			var oc = SrsReplacer.ApplyAll(upper, rules);
			var outLine = new DictLine{
				[DictColumns.Text] = text,
				[DictColumns.Code] = oc,
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

	public async Task<IReadOnlySet<str>> UpdateDkzFile(
		IFnCtx Ctx, TextReader Dkp, TextReader Dkz, TextWriter DkzOut, CT Ct){
		// step 1: 解析 dkp 原表（手工聲符義符擴充表，碼 = 義符字形 + 聲符字形，如 「言成」「手令」）。
		var dkpDoc = await Cfg.Parser.Parse(Dkp, Ct);

		// step 2: dkp 表體每行碼：先套義符規則(碼首字形→音, 如 ^言→ŋ)、再套聲符規則(碼尾字形→音, 如 成$→eŋ)。
		//         取處理後首列字集作為「dkp 已收字頭」，同時保留處理後行供併入。
		//         並行: 逐行只是兩段規則替換，互不共享狀態。
		var yiFuRules = DksRegexRules.YiFuRules();
		var shengFuRules = DksRegexRules.ShengFuRules();
		var dkpRows = new List<IDictLine>();
		await foreach(var line in dkpDoc.Body.WithCancellation(Ct)){
			dkpRows.Add(line);
		}
		var dkpMade = new DictLine?[dkpRows.Count];
		Parallel.For(0, dkpRows.Count, i => {
			var line = dkpRows[i];
			var text = line.text;
			if(string.IsNullOrEmpty(text) || !IsSingleChar(text)){
				return;
			}
			// 碼可空（dkp 有純 text 行如「謇」，表示該字保留但無碼）；
			// 非空碼才套義符(碼首字形→音)與聲符(碼尾字形→音)規則；純音碼如 lʰək 天然不被命中。
			var code = line.code ?? "";
			if(code.Length > 0){
				code = SrsReplacer.ApplyAll(code, yiFuRules);
				code = SrsReplacer.ApplyAll(code, shengFuRules);
			}
			var outLine = new DictLine{
				[DictColumns.Text] = text,
				[DictColumns.Code] = code,
			};
			if(line.weight is not null){
				outLine[DictColumns.Weight] = line.weight;
			}
			dkpMade[i] = outLine;
		});
		var dkpLines = Mk收集(dkpMade);
		var dkpCharSet = new HashSet<str>();
		foreach(var line in dkpLines){
			dkpCharSet.Add(line.text);
		}

		// step 3: 解析 dkz（saffes 表轉換後的單字表），剔除 dkp 已收的字頭。
		//         並行: 逐行只做「取 text/code/weight」+ 集合判定。
		var dkzDoc = await Cfg.Parser.Parse(Dkz, Ct);
		var dkzRows = new List<IDictLine>();
		await foreach(var line in dkzDoc.Body.WithCancellation(Ct)){
			dkzRows.Add(line);
		}
		var keptMade = new DictLine?[dkzRows.Count];
		Parallel.For(0, dkzRows.Count, i => {
			var line = dkzRows[i];
			if(string.IsNullOrEmpty(line.text) || dkpCharSet.Contains(line.text)){
				return;
			}
			keptMade[i] = MkCopy(line);
		});
		var kept = Mk收集(keptMade);

		// step 4: 新 dkz 表體 = dkp 表體在前 + 剩餘 dkz 表體在後。
		var outBody = new List<DictLine>(dkpLines.Count + kept.Count);
		outBody.AddRange(dkpLines);
		outBody.AddRange(kept);

		// step 5: 寫出新 dkz 文檔；回傳 dkp 覆蓋字集（供擇源類步驟用，免得調用方再解析一遍 dkp）。
		await Cfg.Writer.Write(DkzOut, MakeSimpleHeader("dkz"), ToAsy(outBody), Ct);
		return dkpCharSet;
	}

	public async Task<nil> UpdateDks(IFnCtx Ctx, TextReader Dkz, TextWriter Dks, CT Ct){
		// step 1: 解析 dkz（已含 dkp 表體）。
		var dkzDoc = await Cfg.Parser.Parse(Dkz, Ct);
		var rules = DksRegexRules.OcToOc3();
		var rows = new List<IDictLine>();
		await foreach(var line in dkzDoc.Body.WithCancellation(Ct)){
			rows.Add(line);
		}

		// step 2: 每行碼：套 OcToOc3 → 小寫；只保留單字行。並行同上。
		var made = new DictLine?[rows.Count];
		Parallel.For(0, rows.Count, i => {
			var line = rows[i];
			if(string.IsNullOrEmpty(line.text) || !IsSingleChar(line.text)){
				return;
			}
			// 碼可空（dkz 有空碼行，表示該字保留但無讀音），套 OcToOc3 後小寫。
			var raw = line.code ?? "";
			var oc3 = raw.Length == 0 ? "" : SrsReplacer.ApplyAll(raw, rules).ToLowerInvariant();
			var outLine = new DictLine{
				[DictColumns.Text] = line.text,
				[DictColumns.Code] = oc3,
			};
			if(line.weight is not null){
				outLine[DictColumns.Weight] = line.weight;
			}
			made[i] = outLine;
		});

		// step 3: 寫出 dks 文檔（帶 import_tables 表頭，dks 依賴 nonKanji/chineseDict）。
		var body = Mk收集(made);
		await Cfg.Writer.Write(Dks, MakeDksHeader(), ToAsy(body), Ct);
		return NIL;
	}

	/// 把並行步驟的「按下標釘序」結果收成保序列表（跳過被過濾的 null）。
	private static List<DictLine> Mk收集(DictLine?[] Made){
		var ans = new List<DictLine>(Made.Length);
		foreach(var one in Made){
			if(one is not null){
				ans.Add(one);
			}
		}
		return ans;
	}
}
