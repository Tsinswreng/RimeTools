namespace Dks.Core.Svc;

using Tsinswreng.CsCtx;

/// Dks 方案的表生產服務。
/// 設計約定（2026-09-28 按代碼審查結論重整）：
/// - **本接口不認路徑、不碰文件系統**：表相關的每一步都是「讀者進、寫者出」（TextReader/TextWriter），
///   開檔／關檔／建目錄／複製檔案一律由端點（RimeTools.Scripts 的命令）負責。
/// - 單音節那一組是**純函數**（無 IO、無 Ct），可自由併行調用。
/// - 可換策略（dict.yaml 解析/寫出、詞頻源）由實現在構造時注入 DksCfg，方法體內不再各帶配置。
/// - 所有方法的第一個參數統一為 IFnCtx（函數上下文袋），與 Ngan.Dict 的 Svc 規範一致。
/// 例（端點怎麼用）：`using var r = new StreamReader(src); using var w = new StreamWriter(dst); await svc.DkzToDks(ctx, r, w, ct);`
public partial interface ISvcDks{

	// ---- 單音節：拼式 ↔ 音節 ↔ 三拼碼（純函數）----

	[Doc(@$"把 布之道 的上古漢語擬音 適配成 {nameof(Tswg上古漢語音節)}
	規則:
	- 非三等標記、清化、元音韻尾就不說了, 自己看 Tswg上古漢語音節 的文檔
	- ɫ 改成 j
	- 小括號及內容刪掉 如 b(r)u -> bu
	- 中括號刪掉 保內容 如 l[a]u -> lau
	- 去聲標記(即音節末尾的h)改成 s
	- ɬ原樣保留。 {nameof(ToDks)}的時候ɬ應對應s
	- 本步只做記法轉換、不做歸併：hl/hm/hn/hŋ/hr、ʍ／w̥、ml 一律原樣保留
	- 聲母段緊跟輔音的 w 寫成上標 ʷ（布之道圓脣寫法）
	")]
	public Tswg上古漢語音節 Mk上古漢語音節From布之道(str 布之道Spelling);

	[Doc(@$"從Dkp.dict.yaml的拼式解析得 {nameof(Tswg上古漢語音節)}")]
	public Tswg上古漢語音節 Mk上古漢語音節FromDkp(str DkpSpelling);

	/// 音節 → 上古三拼碼（dks 主表 code 欄那種三鍵碼，小寫）。
	/// 三段各查一張鍵位表（DksKeyboard）；聲母為 r 或 ml 時介腹取「帶 r 那一形」（R 鍵／J 鍵的區分標記）。
	/// 任何一段查不到鍵 ⇒ 返回 **null**（不是空串：空串在表裏另有含義 = 該字本無讀音）。
	/// 例：礦 kʷr'aŋʔ ⇒ iyw；辣 r'at ⇒ ryt；神 ml 系 ⇒ jes；涎 sl 系 ⇒ jad。
	public str? ToDks(Tswg上古漢語音節 z);

	// ---- 表流水線：每步只認 讀者/寫者（舊 Dks 流程，saffes 中古倒推）----

	/// 舊步驟 1：saffes → dkz。
	/// 讀 saffes 表，碼轉大寫後套 SaffesToOc 規則（Rules/saffesToOcRegex.txt），寫出 dkz 文檔。
	/// 只處理單字行；權重原樣帶過。
	Task<nil> SaffesToDkz(IFnCtx Ctx, TextReader Saffes, TextWriter Dkz, CT Ct);

	/// 舊步驟 2（新流程也用）：用 dkp 的聲符/義符解出 dkp 表體，覆蓋 dkz 字頭並併入 dkz 表體，寫出新 dkz。
	/// 語義 = 「dkp 最優先」：凡 dkp 收了的字，dkz 原型的那幾行一律剔除，改用 dkp 解出的行（dkp 表體在前）。
	/// 返回值 = dkp 覆蓋到的字集（供擇源類步驟判「該字是否 dkp 說了算」，免得調用方為此再解析一遍 dkp）。
	Task<IReadOnlySet<str>> UpdateDkzFile(IFnCtx Ctx, TextReader Dkp, TextReader Dkz, TextWriter DkzOut, CT Ct);

	/// 舊步驟 3：dkz → dks。套 OcToOc3 規則（Rules/ocToOc3.txt）、碼轉小寫，帶 import_tables 表頭寫出 dks 文檔。
	Task<nil> UpdateDks(IFnCtx Ctx, TextReader Dkz, TextWriter Dks, CT Ct);

	// ---- 表流水線：新 Dks2 流程（布之道擬音 → 查表轉三拼）----

	/// 新步驟 1′：布之道擬音 → dkz。
	/// 讀 OC_msoegDK 表，碼欄形如 `ASCII*IPA`，只取 `*` 之後的 IPA 段，
	/// 用 Mk上古漢語音節From布之道 適配成本方案音節，把 Full 寫進 dkz 的碼欄。
	/// 單字過濾與表頭與 SaffesToDkz 一致，好讓後續步驟（dkp 覆蓋、dkz→dks）原樣沿用。
	/// 例：包 prXu*prˤu → 音節（聲母 p、介腹 r'u、尾調空）⇒ dkz 行「包」+Tab+「pr'u」。
	Task<nil> 布之道ToDkz(IFnCtx Ctx, TextReader 布之道表, TextWriter Dkz, CT Ct);

	/// 新步驟 3′：dkz → dks，走查表而非規則。
	/// 每行碼先用 Dkp拼式Parser 解成音節（布之道來的行是純音；dkp 併入的行可能帶義符字形），
	/// 再用 ToDks 查三層鍵位表得小寫三拼碼；表頭與 UpdateDks 相同（name: dks + import_tables）。
	/// **產出驗證**：每行碼必須恰為 3 個 dks 鍵位字符，否則拋 InvalidOperationException 並列出全部問題行。
	/// 例：dkz 行「辣	rˤat」⇒ 音節（聲母 r、介腹 'a、尾調 t）⇒ dks 行「辣	ryt」。
	Task<nil> DkzToDks(IFnCtx Ctx, TextReader Dkz, TextWriter Dks, CT Ct);

	/// 新步驟 4：dks + 倉頡輔助碼 → dks_v。按字左聯（dks 有而倉頡無的字保留主碼），寫出 dks_v 文檔。
	Task<nil> AttachCangjie(IFnCtx Ctx, TextReader Dks, TextReader Cangjie, TextWriter DksV, CT Ct);

	/// 新步驟 5：dks → dkn。每行碼取首尾字符（CodeTransformer.HeadTail）、name 改為 dkn。
	Task<nil> ToDkn(IFnCtx Ctx, TextReader Dks, TextWriter Dkn, CT Ct);

	/// 新步驟 6：造詞 → dks_phrase。
	/// 以讀入的 dks 表為反查源、Cfg.WordFreq 為權重，用 RimeTools.Shared.Phrase.PhraseMaker 生成簡碼詞，
	/// 按頻率降序寫出 dks_phrase 文檔（columns: text/code/weight）。
	/// 例：詞「一個」（頻 374279）→ 一個	qkkn	374279
	Task<nil> MkDksPhrase(IFnCtx Ctx, TextReader Dks, TextWriter Phrase, CT Ct);

	// ---- 擇源：把新舊兩側的 dks 按規則合成一份（reader 進、writer 出）----

	/// Dks3：回退缺音。
	/// 「布之道側缺音」的字——即新側表裏不存在、或存在但全部碼都是空——改用舊側（中古倒推）該字的行；
	/// 其餘一律保留新側的行與原序，回退行追加在末尾。寫出後同樣做三鍵產出驗證。
	/// 例：你、鼸 這類布之道沒收的字 ⇒ 用舊流程的 nku、gxr。
	Task<nil> 回退缺音(IFnCtx Ctx, TextReader 新表, TextReader 舊表, TextWriter 出, CT Ct);

	/// Dks4：按字頻擇源（保證不缺字）。
	///   ① dkp 覆蓋到的字（Dkp覆蓋字 入參） → 保留新側（dkp 最優先，不受頻率影響）；
	///   ② 布之道沒有、中古倒推有的字 → 用舊側（不能缺字）；
	///   ③ 其餘的字，在 essay.txt 的漢字頻率排名 ≤ 名次上限者 → 用舊側；
	///   ④ 其餘（排名在上限之外，或 essay.txt 裏沒有該字）→ 保留新側（布之道擬音）。
	/// 寫出後同樣做三鍵產出驗證。
	/// 例：上限 5000 時，買／個 這類高頻字走中古倒推的碼，罕見字走布之道，布之道缺音的由中古倒推接住。
	Task<nil> 按頻擇源(
		IFnCtx Ctx, TextReader 新表, TextReader 舊表, IReadOnlySet<str> Dkp覆蓋字,
		i32 高頻名次上限, TextWriter 出, CT Ct);
}
