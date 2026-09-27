namespace RimeTools.Shared.Phrase;

using RimeTools.Shared.Dict.Lookup;
using RimeTools.Shared.Dict.Models;
using RimeTools.Shared.Freq;
using Tsinswreng.CsCtx;

/// 造詞器：詞頻 × 反查 × 造詞策略 → 詞條流（text/code/weight）。
/// 對應 cli-tools 的 MkPhrase.start()，但把 IO（詞頻來源、碼表、輸出）全部抽成接口，本身只做組合。
/// 例（詞「一個」，頻 374279；一=qdk、個=...，策略 HeadTail）：
///   輸入 freq=("一個",374279)、lookup 查得每字碼、mkr 取首尾
///   輸出 → 詞條 一個	qkkn	374279
public partial class PhraseMaker{
	/// 造詞主流程：
	/// step 1: 枚舉詞頻（降序）
	/// step 2: 拆詞成字；每字查反查得碼組（一詞可能多組組合，做笛卡爾積）
	/// step 3: 每組經 IPhraseMkr 生成詞碼片段並拼接成詞碼
	/// step 4: 產出 DictLine{text=詞, code=詞碼, weight=頻率}
	/// 單字詞、查無碼的字、策略拒收的組合一律跳過。
	/// <param name="Ctx">函數上下文袋（本實現暫不讀取，留給日後掛日誌/事務/臨時表）。</param>
	/// <param name="Freq">詞頻源（降序）。</param>
	/// <param name="Lookup">字/詞 → 碼 反查。</param>
	/// <param name="Mkr">造詞策略（如 PhraseMkr_HeadEtTail）。</param>
	/// <param name="Ct">取消令牌。</param>
	public partial IAsyncEnumerable<DictLine> MakePhrases(
		IFnCtx Ctx, IWordFreqSource Freq, ICharCodeLookup Lookup, IPhraseMkr Mkr, CT Ct);
}