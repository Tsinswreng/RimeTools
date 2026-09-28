namespace Dks.Core.Svc;

using Tsinswreng.CsCtx;

/// Dks 方案的表生產服務（對應 ngaq dks.ts 的 Dks 類 + AttachCangjie + 後製 dkn）。
/// 所有方法的第一個參數統一為 IFnCtx（函數上下文袋），與 Ngan.Dict 的 Svc 規範一致。
/// 配置（路徑、可換後端）由實現在構造時注入 DksCfg，方法體內不再各帶配置參數。
public partial interface ISvcDks{
	
	[Doc(@$"把 布之道 的上古漢語擬音 適配成 {nameof(Tswg上古漢語音節)}
	規則:
	- 非三等標記、清化、元音韻尾就不說了, 自己看 Tswg上古漢語音節 的文檔
	- ɫ 改成 j
	- 小括號及內容刪掉 如 b(r)u -> bu
	- 中括號刪掉 保內容 如 l[a]u -> lau
	- 去聲標記(即音節末尾的h)改成 s
	- ɬ原樣保留。 {nameof(ToDks)}的時候ɬ應對應s
	")]
	public Tswg上古漢語音節 Mk上古漢語音節From布之道(str 布之道Spelling);
	
	
	[Doc(@$"從Dkp.dict.yaml的拼式解析得 {nameof(Tswg上古漢語音節)}")]
	public Tswg上古漢語音節 Mk上古漢語音節FromDkp(str DkpSpelling);
	
	
	public str ToDks(Tswg上古漢語音節 z);
	
	/// 步驟 1′（Dks2 用，取代 SaffesToDkz）：布之道擬音 → dkz。
	/// 讀 Cfg.布之道DictPath 的表，碼欄形如 `ASCII*IPA`，只取 `*` 之後的 IPA 段，
	/// 用 Mk上古漢語音節From布之道 適配成本方案音節，把 Full 寫進 dkz 的碼欄。
	/// 單字過濾與表頭與 SaffesToDkz 一致，好讓後續步驟（dkp 覆蓋、dkz→dks）原樣沿用。
	/// 例：包 prXu*prˤu → 音節（聲母 p、介腹 r'u、尾調空）⇒ dkz 行「包」+Tab+「pr'u」。
	Task<nil> 布之道ToDkz(IFnCtx Ctx, CT Ct);

	/// 步驟 3′（Dks2 用，取代 UpdateDks）：dkz → dks，走查表而非規則。
	/// 每行碼先用 Dkp拼式Parser 解成音節（布之道來的行是純音；dkp 併入的行可能帶義符字形），
	/// 再用 ToDks 查三層鍵位表得小寫三拼碼；表頭與 UpdateDks 相同（name: dks + import_tables）。
	/// 例：dkz 行「辣	rˤat」⇒ 音節（聲母空、介腹 r'a、尾調 t）⇒ dks 行「辣	ryt」。
	Task<nil> DkzToDks(IFnCtx Ctx, CT Ct);
	
	/// 步驟 4′（Dks3 用）：回退缺音。
	/// 讀 Dks2 產出的那份 dks（UserDataDir/dks.dict.yaml）與舊 Dks 流程產出的那份（入參路徑）：
	/// 凡「布之道側缺音」的字——即在 Dks2 那份裏不存在、或存在但全部碼都是空——改用舊那份裏該字的行；
	/// 其餘一律保留 Dks2 的行與原序，回退行追加在末尾。寫回 dks.dict.yaml，並同樣做三鍵產出驗證。
	/// 例：你、個 這類布之道表裏沒有的字 ⇒ 用舊流程的 nku、kzn。
	Task<nil> 回退缺音(IFnCtx Ctx, str 舊Dks路徑, CT Ct);

	/// 步驟 4″（Dks4 用）：按字頻擇源（保證不缺字）。
	/// 讀 Dks2 產出的那份 dks（UserDataDir/dks.dict.yaml）與中古倒推（舊 Dks）產出的那份（入參路徑），
	/// 逐字決定採用哪一側的讀音：
	///   ① dkp 覆蓋到的字 → 保留 Dks2 那份（dkp 最優先，不受頻率影響）；
	///   ② 布之道沒有、中古倒推有的字 → 用中古倒推那份（不能缺字）；
	///   ③ 其餘的字，在 essay.txt 的**漢字頻率排名** ≤ 名次上限者 → 用中古倒推那份；
	///   ④ 其餘（排名在上限之外，或 essay.txt 裏沒有該字）→ 保留 Dks2 那份（布之道擬音）。
	/// 舊側缺該字時仍用新側（例：布之道有、saffes 沒有的字）。
	/// 寫回 dks.dict.yaml，並同樣做三鍵產出驗證。
	/// 例：名次上限 5000 時，買／個 這類高頻字走中古倒推的碼，罕見字走布之道擬音的碼，
	///     而布之道缺音的字（如 鼸、馱）一律由中古倒推補上，不會從表裏消失。
	Task<nil> 按頻擇源(IFnCtx Ctx, str 舊Dks路徑, i32 高頻名次上限, CT Ct);

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
