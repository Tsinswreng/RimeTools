namespace Dks.Core;

/// Dks 方案完整流水線：按 Dks.sh 的順序依次執行全部七個步驟。
/// 例（命令行等效流程，即 RimeTools.Scripts 的 Dks 命令所做）：
///   DksCfg{UserDataDir="D:/Program Files/Rime/User_Data", SrcTableDir="<ngaq>/src/backend/dict/原表"}
///   Run → 產出 UserDataDir 下的 dks/dkn/dks_v/dkp/dkz/dks_phrase 六個 dict.yaml。
public static partial class DksPipeline{
	/// 依序執行：SaffesToDkz → UpdateDkzFile → UpdateDks → AttachCangjie → CopyDkpDkz → ToDkn → MkDksPhrase。
	/// 任一步失敗即中止並向上拋出異常（不清理已產出的中間文件）。
	/// <param name="Cfg">流水線配置（路徑 + 可換後端實現）。</param>
	/// <param name="Ct">取消令牌。</param>
	public static partial Task<nil> Run(DksCfg Cfg, CT Ct);
}