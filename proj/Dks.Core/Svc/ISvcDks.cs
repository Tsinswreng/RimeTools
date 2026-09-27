namespace Dks.Core.Svc;

using Tsinswreng.CsCtx;

/// Dks 方案的表生產服務（對應 ngaq dks.ts 的 Dks 類 + AttachCangjie + 後製 dkn）。
/// 所有方法的第一個參數統一為 IFnCtx（函數上下文袋），與 Ngan.Dict 的 Svc 規範一致。
/// 配置（路徑、可換後端）由實現在構造時注入 DksCfg，方法體內不再各帶配置參數。
public partial interface ISvcDks{
	
	[Doc(@$"從Dkp.dict.yaml的拼式解析得 {nameof(Tswg上古漢語音節)}")]
	public Tswg上古漢語音節 Mk上古漢語音節FromDkp(str DkpSpelling);
	
	
	public str ToDks(Tswg上古漢語音節 z);
	
	#region 全部不合格！爲甚麼不用TextReader/TextWriter之類的當輸入輸出?搞個Task<nil>是幾個意思?你tm會不會寫代碼的 有你這麼拉屎的嗎?
	/// 步驟 1：saffes → dkz。
	/// 讀 saffes.dict.yaml，碼轉大寫後套用 SaffesToOc 規則，寫出 dkz.dict.yaml（供後續步驟讀取）。
	Task<nil> SaffesToDkz(IFnCtx Ctx, CT Ct);

	/// 步驟 2：用 dkp 的聲符/義符剔除 dkz 字頭，並把 dkp 表體併入 dkz 表體，重寫 dkz.dict.yaml。
	Task<nil> UpdateDkzFile(IFnCtx Ctx, CT Ct);

	/// 步驟 3：dkz → dks。套用 OcToOc3 規則、碼轉小寫，帶 import_tables 表頭寫出 dks.dict.yaml。
	Task<nil> UpdateDks(IFnCtx Ctx, CT Ct);

	/// 步驟 4：dks + 倉頡輔助碼 → dks_v。按字左聯（dks 有而倉頡無的字保留原碼），寫出 dks_v.dict.yaml。
	Task<nil> AttachCangjie(IFnCtx Ctx, CT Ct);

	/// 步驟 5：拷貝 dkp/dkz 到 UserDataDir。
	Task<nil> CopyDkpDkz(IFnCtx Ctx, CT Ct);

	/// 步驟 6：dks → dkn。每行碼取首尾字符（CodeTransformer.HeadTail）、name 改為 dkn。
	Task<nil> ToDkn(IFnCtx Ctx, CT Ct);

	/// 步驟 7：造詞 → dks_phrase。
	/// 以 dks.dict.yaml 為反查源、詞頻源為權重，用 RimeTools.Shared.Phrase.PhraseMaker 生成簡碼詞，
	/// 按頻率降序寫出 dks_phrase.dict.yaml（columns: text/code/weight）。
	/// 例：詞「一個」（頻 374279）→ 一個	qkkn	374279
	Task<nil> MkDksPhrase(IFnCtx Ctx, CT Ct);
	#endregion 全部不合格！
}
