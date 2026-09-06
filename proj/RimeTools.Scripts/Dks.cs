namespace RimeTools.Scripts;

/// Dks 方案流水線命令：組裝 DksCfg 並執行 Dks.Core.DksPipeline.Run（等效 Dks.sh 全套）。
/// 可選參數（依序）：[UserDataDir] [SrcTableDir]，缺省用默認值。
internal static partial class Dks{
	/// Args 是去除命令名後的剩餘參數。
	internal static partial Task Main(ISCtx Ctx, str[] Args, CT Ct);
}