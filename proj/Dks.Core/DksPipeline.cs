namespace Dks.Core;

/// Dks 方案完整流水線：按 Dks.sh 的順序依次執行全部七個步驟。
public static partial class DksPipeline{
	/// 依序執行：SaffesToDkz → UpdateDkzFile → UpdateDks → AttachCangjie → CopyDkpDkz → ToDkn → MkDksPhrase。
	/// 任一步失敗即中止並向上拋出異常。
	public static partial Task<nil> Run(DksCfg Cfg, CT Ct);
}