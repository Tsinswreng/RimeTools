namespace RimeTools.Scripts;

/// Dks4 方案流水線命令：在 Dks2 的基礎上，按**漢字頻率排名**決定每個字用哪一側的讀音。
/// 規則（**保證不缺字**；dkp 永遠最優先，不受頻率影響）：
///   ① dkp 覆蓋到的字 → 用 Dks2 那份（dkp 解出的碼）；
///   ② 布之道沒有、中古倒推有的字 → 用**中古漢語倒推**（舊 Dks 流程）的碼（補缺字）；
///   ③ 其餘的字，在 essay.txt 的漢字頻率排名 ≤ 5000 者 → 用**中古漢語倒推**的碼；
///   ④ 其餘（排名 5000 之外，或 essay.txt 裏沒有該字）→ 用**布之道擬音**（Dks2）的碼。
/// 為何這樣分：高頻字保留舊讀音 ⇒ 不破壞既有肌肉記憶；罕見字改採布之道新擬音；
/// 而布之道沒收的字一律由中古倒推接住，換源不會讓整表缺字。
/// 流程與 Dks3 相同（骨架在本類 Impl，文件操作全在 DksPipeline）：
///   新側（正式目錄）＝布之道ToDkz → dkp 覆蓋 → DkzToDks（含產出驗證）；
///   舊側（<倉庫根>/_Dks4臨時）＝舊 Dks 流程整套；
///   兩側互不相干，故一齊跑；匯合後按上述規則擇源寫回 dks（再做一次產出驗證）；
///   最後 dks_v／dkn／dks_phrase／拷檔四件併行。
/// 臨時目錄：<倉庫根>/_Dks4臨時/{src,user}（跑完留著便於查，可自行刪）。
/// 可選參數（依序）：[UserDataDir] [SrcTableDir] [布之道DictPath]，缺省用 DksPaths 默認值。
/// 例：dotnet run --project proj/RimeTools.Scripts -- Dks4
internal static partial class Dks4{
	/// 排名上限：essay.txt 單字頻數降序，前這個數目以內者用中古倒推。
	internal const i32 高頻名次上限 = 5000;

	/// Args 是去除命令名後的剩餘參數（0~3 個，依序為 UserDataDir、SrcTableDir、布之道DictPath）。
	internal static partial Task Main(ISCtx Ctx, str[] Args, CT Ct);
}
