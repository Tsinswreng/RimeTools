namespace Dks.Core;

/// Dks 方案流水線的七個步驟：與 Dks.sh 的處理一一對應，可單獨調用、也可由 DksPipeline 依次執行。
/// 每一步的輸入輸出路徑都取自 DksCfg：原表目錄(SrcTableDir)是中間產物的家，用戶目錄(UserDataDir)是最終產物的家。
public static partial class DksSteps{
	/// 步驟 1：saffes → dkz。
	/// 讀 SrcTableDir/saffes.dict.yaml，碼轉大寫後套用 SaffesToOc 規則，
	/// 寫出 SrcTableDir/dkz.dict.yaml（供步驟 2/3 讀取）。
	/// 例：saffes 行「辣	y	10000%」→ 碼轉大寫「Y」→ 套規則 → dkz 行「辣	ˁa	10000%」。
	/// 對應 ngaq dks.ts 的 saffesToDkz。
	public static partial Task<nil> SaffesToDkz(DksCfg Cfg, CT Ct);

	/// 步驟 2：用 dkp 的聲符/義符剔除 dkz 字頭，並把 dkp 表體併入 dkz 表體，重寫 SrcTableDir/dkz.dict.yaml。
	/// 對應 ngaq dks.ts 的 putDkzInDb + updateDkzFile。
	public static partial Task<nil> UpdateDkzFile(DksCfg Cfg, CT Ct);

	/// 步驟 3：dkz → dks。
	/// 讀 SrcTableDir/dkz.dict.yaml，套用 OcToOc3 規則、碼轉小寫，帶 import_tables 表頭
	/// 寫出 UserDataDir/dks.dict.yaml。
	/// 對應 ngaq dks.ts 的 updateDks。
	public static partial Task<nil> UpdateDks(DksCfg Cfg, CT Ct);

	/// 步驟 4：dks + 倉頡輔助碼 → dks_v。
	/// 讀 cangjie 表與 dks.dict.yaml，按字合併碼（左連接語義：dks 有而倉頡無的字保留原碼），
	/// 寫出 UserDataDir/dks_v.dict.yaml。
	/// 對應 ngaq dks.ts 的 AttachCangjie。
	public static partial Task<nil> AttachCangjie(DksCfg Cfg, CT Ct);

	/// 步驟 5：拷貝 dkp/dkz 到 UserDataDir。
	/// 對應 ngaq dks.ts run() 中的 copyDkp + fs.copyFileSync(dkz)。
	public static partial Task<nil> CopyDkpDkz(DksCfg Cfg, CT Ct);

	/// 步驟 6：dks → dkn。
	/// 讀 dks.dict.yaml，每行碼取首尾字符（CodeTransformer.HeadTail）、name 改為 dkn，
	/// 寫出 UserDataDir/dkn.dict.yaml。
	/// 例：dks 行「辣	ryt	10000%」→ dkn 行「辣	rt	10000%」。
	/// 對應 Dks.sh 的 awk + sed 步驟。
	public static partial Task<nil> ToDkn(DksCfg Cfg, CT Ct);

	/// 步驟 7：造詞 → dks_phrase。
	/// 以 dks.dict.yaml 為反查源（ICharCodeLookup）、詞頻源為權重，對每個詞取每字首尾碼生成簡碼詞，
	/// 按頻率降序寫出 UserDataDir/dks_phrase.dict.yaml（columns: text/code/weight）。
	/// 例：詞「一個」（頻 374279，一→y 個→gk）→ 「一個	y	…」→ 首尾碼拼「qkkn」：
	///   一個	qkkn	374279
	/// 對應 cli-tools RimeTools 的 DksMkPhrase。
	public static partial Task<nil> MkDksPhrase(DksCfg Cfg, CT Ct);
}