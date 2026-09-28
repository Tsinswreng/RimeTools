namespace RimeTools.Scripts;

/// 臨時命令：審查 dks。
/// 分別用**舊 Dks 流程**與**新 Dks2 流程**各構建一份 dks.dict.yaml（兩側互不相干，故併行），
/// 逐字對比兩者的碼集，再按 essay.txt 的漢字頻率**降序**列出**有變化**的字
/// （頻率＝該字單字條目頻數÷全部單字條目頻數之和；dkp 覆蓋到的字不列，因為兩側都由 dkp 說了算）。
/// 兩條流程都只跑到 dks 為止（dks_v／dkn／dks_phrase 與本次對比無關，不跑，省時間）。
/// 一切產物與報告都寫在工作區內：默認輸出根 = <倉庫根>/_AuditDks，
/// 內含 舊/user/dks.dict.yaml、新/user/dks.dict.yaml 與 差異.txt（同報告內容）。
/// 例：dotnet run --project proj/RimeTools.Scripts -- AuditDks
///     指定輸出根：-- AuditDks E:/_code/Ngan/RimeTools/_AuditDks
/// 亦可在輸出根之後接 Dks 命令那三個參數（UserDataDir、SrcTableDir、布之道DictPath）覆寫默認路徑。
internal static partial class AuditDks{
	/// Args 是去除命令名後的剩餘參數（0~4 個：輸出根、UserDataDir、SrcTableDir、布之道DictPath）。
	internal static partial Task Main(ISCtx Ctx, str[] Args, CT Ct);
}
