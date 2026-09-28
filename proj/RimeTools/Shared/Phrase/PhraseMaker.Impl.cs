namespace RimeTools.Shared.Phrase;

using RimeTools.Shared.Dict.Lookup;
using RimeTools.Shared.Dict.Models;
using RimeTools.Shared.Freq;
using RimeTools.Tools;
using Tsinswreng.CsCtx;

public partial class PhraseMaker{
	public partial IAsyncEnumerable<DictLine> MakePhrases(
		IFnCtx Ctx, IWordFreqSource Freq, ICharCodeLookup Lookup, IPhraseMkr Mkr, CT Ct){
		return MakePhrasesImpl(Freq, Lookup, Mkr, Ct);
	}

	/// 內部實現：詞頻(降序) → 拆詞 → 每字查碼 → 笛卡爾積 → 策略 → 詞條。
	private async IAsyncEnumerable<DictLine> MakePhrasesImpl(
		IWordFreqSource Freq, ICharCodeLookup Lookup, IPhraseMkr Mkr,
		[System.Runtime.CompilerServices.EnumeratorCancellation] CT Ct){
		// step 1: 枚舉詞頻。
		await foreach(var wf in Freq.Enumerate(Ct)){
			Ct.ThrowIfCancellationRequested();
			var word = wf.Text;
			if(string.IsNullOrEmpty(word)){
				continue;
			}

			// step 2: 拆詞成字。
			var chars = TextUtil.SplitByRune(word);
			if(chars.Count <= 1){
				continue; // 單字詞不造簡碼詞
			}

			// step 3: 每字查反查，得每字的碼組（多讀音 = 多個候選碼）。
			var codeGroups = new List<IReadOnlyList<str>>();
			var skip = false;
			foreach(var ch in chars){
				var codes = Lookup.GetCodes(ch);
				if(codes.Count == 0){
					skip = true; // 某字無碼 → 此詞不成詞
					break;
				}
				codeGroups.Add(codes);
			}
			if(skip){
				continue;
			}

			// step 4: 對每組候選做笛卡爾積，窮舉發音組合。
			var combos = Cartesian.Product(codeGroups);
			foreach(var combo in combos){
				// step 5: 策略生成每字片段，拼接成詞碼。
				var parts = Mkr.MkPhrase(combo, chars);
				if(parts.Count == 0){
					continue;
				}
				var phraseCode = string.Join("", parts);
				// step 6: 產出詞條（權重 = 頻率字符串）。
				var line = new DictLine{
					[DictColumns.Text] = word,
					[DictColumns.Code] = phraseCode,
					[DictColumns.Weight] = wf.Freq.ToString(),
				};
				yield return line;
			}
		}
	}
}