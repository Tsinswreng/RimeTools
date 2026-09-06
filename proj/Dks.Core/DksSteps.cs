namespace Dks.Core;

/// Dks 方案流水線的七個步驟：與 Dks.sh 的處理一一對應，可單獨調用、也可由 DksPipeline 依次執行。
public static partial class DksSteps{
	/// 步驟 1：saffes → dkz。
	/// 讀 saffes.dict.yaml，碼轉大寫後套用 SaffesToOc 規則，寫出 SrcTableDir/dkz.dict.yaml（供後續步驟讀取）。
	/// 對應 ngaq dks.ts 的 saffesToDkz。
	public static partial Task<nil> SaffesToDkz(DksCfg Cfg, CT Ct);

	/// 步驟 2：用 dkp 的聲符/義符剔除 dkz 字頭，並把 dkp 表體併入 dkz 表體，重寫 SrcTableDir/dkz.dict.yaml。
	/// 對應 ngaq dks.ts 的 putDkzInDb + updateDkzFile。
	public static partial Task<nil> UpdateDkzFile(DksCfg Cfg, CT Ct);

	/// 步驟 3：dkz → dks。
	/// 讀 SrcTableDir/dkz.dict.yaml，套用 OcToOc3 規則、碼轉小寫，帶 import_tables 表頭寫出 UserDataDir/dks.dict.yaml。
	/// 對應 ngaq dks.ts 的 updateDks。
	public static partial Task<nil> UpdateDks(DksCfg Cfg, CT Ct);

	/// 步驟 4：dks + 倉頡輔助碼 → dks_v。
	/// 讀 cangjie 表與 dks.dict.yaml，按字合併碼（左連接語義），寫出 UserDataDir/dks_v.dict.yaml。
	/// 對應 ngaq dks.ts 的 AttachCangjie。
	public static partial Task<nil> AttachCangjie(DksCfg Cfg, CT Ct);

	/// 步驟 5：拷貝 dkp/dkz 到 UserDataDir。
	/// 對應 ngaq dks.ts run() 中的 copyDkp + fs.copyFileSync(dkz)。
	public static partial Task<nil> CopyDkpDkz(DksCfg Cfg, CT Ct);

	/// 步驟 6：dks → dkn。
	/// 讀 dks.dict.yaml，每行碼取首尾字符（CodeTransformer.HeadTail）、name 改為 dkn，寫出 UserDataDir/dkn.dict.yaml。
	/// 對應 Dks.sh 的 awk + sed 步驟。
	public static partial Task<nil> ToDkn(DksCfg Cfg, CT Ct);

	/// 步驟 7：造詞 → dks_phrase。
	/// 以 dks.dict.yaml 為反查源、詞頻源為權重，對每個詞取每字首尾碼生成簡碼詞，
	/// 按頻率降序寫出 UserDataDir/dks_phrase.dict.yaml。
	/// 對應 cli-tools RimeTools 的 DksMkPhrase。
	public static partial Task<nil> MkDksPhrase(DksCfg Cfg, CT Ct);
}