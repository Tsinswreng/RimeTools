namespace RimeTools.Scripts;

using Core = global::Dks.Core.DksCfg;

/// Dks 方案流水線命令：組裝 DksCfg、new SvcDks(Cfg)，依序跑 ISvcDks 七步（等效 Dks.sh 全套）。
/// 注意：本類名 Dks 與命名空間 Dks 同前綴，類內引用 Dks.Core 類型一律走 using 別名（Core）。
/// 可選參數（依序）：[UserDataDir] [SrcTableDir]，缺省用 Core 默認值：
///   UserDataDir = "D:/Program Files/Rime/User_Data"
///   SrcTableDir = "e:/_code/ngaq/src/backend/dict/原表"
/// 例：dotnet run --project proj/RimeTools.Scripts -- Dks
///     或用自定義路徑：-- Dks D:/Rime/User_Data e:/tables
internal static partial class Dks{
	/// Args 是去除命令名後的剩餘參數（0~2 個，依序為 UserDataDir、SrcTableDir）。
	internal static partial Task Main(ISCtx Ctx, str[] Args, CT Ct);
}