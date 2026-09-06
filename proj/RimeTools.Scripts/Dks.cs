namespace RimeTools.Scripts;

using Dks.Core;

/// Dks 方案流水線命令：組裝 DksCfg 並執行 DksPipeline.Run（等效 Dks.sh 全套）。
/// 可選參數（依序）：[UserDataDir] [SrcTableDir]，缺省用下列默認值：
///   UserDataDir = "D:/Program Files/Rime/User_Data"
///   SrcTableDir = "e:/_code/ngaq/src/backend/dict/原表"
/// 例：dotnet run --project proj/RimeTools.Scripts -- Dks
///     或用自定義路徑：-- Dks D:/Rime/User_Data e:/tables
internal static partial class Dks{
	/// Args 是去除命令名後的剩餘參數（0~2 個，依序為 UserDataDir、SrcTableDir）。
	internal static partial Task Main(ISCtx Ctx, str[] Args, CT Ct);
}