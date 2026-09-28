namespace RimeTools.Scripts;

/// Dks 方案流水線命令（舊流程：saffes 中古倒推）。
/// 流程骨架全在本類的 Impl 裏，文件操作（開檔/關檔/建目錄/複製/同檔改寫）全在 DksPipeline：
///   ① saffes → dkz；② dkp 覆蓋 dkz；③ dkz → dks（規則版）；④ 後段四件併行（dks_v／dkn／dks_phrase／拷 dkp·dkz）。
/// 注意：本類名 Dks 與命名空間 Dks 同前綴，類內引用 Dks.Core 類型一律走 using 別名。
/// 可選參數（依序）：[UserDataDir] [SrcTableDir]，缺省用 DksPaths 默認值：
///   UserDataDir = "D:/Program Files/Rime/User_Data"
///   SrcTableDir = "e:/_code/ngaq/src/backend/dict/原表"
/// 例：dotnet run --project proj/RimeTools.Scripts -- Dks
///     或用自定義路徑：-- Dks D:/Rime/User_Data e:/tables
internal static partial class Dks{
	/// Args 是去除命令名後的剩餘參數（0~2 個，依序為 UserDataDir、SrcTableDir）。
	internal static partial Task Main(ISCtx Ctx, str[] Args, CT Ct);
}
