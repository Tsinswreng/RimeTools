namespace RimeTools.Scripts;

/// Dks3 方案流水線命令：等同 Dks2，但多一步「回退缺音」——
/// 凡是布之道表裏缺音的字（在 Dks2 的 dks 裏不存在、或存在但全部碼為空），改用舊 Dks 流程的讀音補上。
/// 流程（骨架在本類 Impl，文件操作全在 DksPipeline）：
///   新側（正式目錄）＝布之道ToDkz → dkp 覆蓋 → DkzToDks（含產出驗證）；
///   舊側（<倉庫根>/_Dks3臨時）＝舊 Dks 流程整套，拿「原來的讀音」；
///   兩側互不相干，故一齊跑；匯合後回退缺音（新行在前原序、回退行追加在後，再驗證一次）；
///   後段與 Dks2 相同且併行：dks_v／dkn／dks_phrase／拷 dkp·dkz。
/// 臨時目錄在工作區內：<倉庫根>/_Dks3臨時/{src,user}（跑完留著，便於查；可自行刪）。
/// 可選參數（依序）：[UserDataDir] [SrcTableDir] [布之道DictPath]，缺省用 DksPaths 默認值。
/// 例：dotnet run --project proj/RimeTools.Scripts -- Dks3
internal static partial class Dks3{
	/// Args 是去除命令名後的剩餘參數（0~3 個，依序為 UserDataDir、SrcTableDir、布之道DictPath）。
	internal static partial Task Main(ISCtx Ctx, str[] Args, CT Ct);
}
