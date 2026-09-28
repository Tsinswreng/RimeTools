namespace RimeTools.Scripts;

/// Dks2 方案流水線命令（新流程：布之道擬音）。
/// 與 Dks 共用同一個 SvcDks，只換前段——「布之道擬音 → dkz」取代 saffes 中古倒推，「查表 dkz → dks」取代規則版；
/// 中間的 dkp 優先覆蓋與後段（dks_v／dkn／dks_phrase／拷 dkp·dkz）與 Dks 完全相同。
/// 流程骨架在本類 Impl，文件操作全在 DksPipeline；後段四件互不相干，一齊跑。
/// 可選參數（依序）：[UserDataDir] [SrcTableDir] [布之道DictPath]，缺省用 DksPaths 默認值：
///   UserDataDir = "D:/Program Files/Rime/User_Data"
///   SrcTableDir = "e:/_code/ngaq/src/backend/dict/原表"
///   布之道DictPath = "D:/Program Files/Rime/User_Data/OC_msoegDK.dict.yaml"
/// 例：dotnet run --project proj/RimeTools.Scripts -- Dks2
internal static partial class Dks2{
	/// Args 是去除命令名後的剩餘參數（0~3 個，依序為 UserDataDir、SrcTableDir、布之道DictPath）。
	internal static partial Task Main(ISCtx Ctx, str[] Args, CT Ct);
}
