namespace RimeTools.Scripts;

using SvcDks = global::Dks.Core.Svc.SvcDks;
using Tsinswreng.CsCtx;

/// 端點共用的編排件：**全項目唯一做文件操作的地方**（庫內一律 TextReader/TextWriter）。
/// 命令只寫自己那條流程的骨架，開檔／關檔／同檔改寫／複製機制收在這一層。
/// 每個方法都對應原來 Dks.sh 的一段，命名與 SvcDks 的步驟一一對應。
internal static partial class DksPipeline{
	/// 建 SvcDks：注入可換策略（解析/寫出默認實現；詞頻源由端點按自己的目錄約定給）。
	internal static partial SvcDks MkSvc(DksPaths P);

	/// 開一個輸入、一個輸出，跑「reader → writer」的步驟。
	/// 若輸入與輸出同一路徑（如 UpdateDkzFile 就地改寫 dkz），先寫 `<目標>.tmp` 再改名覆蓋——
	/// 否則寫出器會在讀者仍持有該檔時截斷它（Windows 共享衝突／讀到半截）。
	internal static partial Task 跑一步(
		str Src, str Dst, Func<TextReader, TextWriter, Task> 步, CT Ct);

	/// 兩個輸入、一個輸出，跑「reader+reader → writer」的步驟（UpdateDkzFile／AttachCangjie）。
	internal static partial Task 跑兩入一步(
		str Src1, str Src2, str Dst, Func<TextReader, TextReader, TextWriter, Task> 步, CT Ct);

	/// 兩個輸入、一個輸出且**有回傳值**（UpdateDkzFile 要回 dkp 覆蓋字集）。
	internal static partial Task<T> 跑兩入一步<T>(
		str Src1, str Src2, str Dst, Func<TextReader, TextReader, TextWriter, Task<T>> 步, CT Ct);

	/// 產出 dks 的前段（不含後段四件）：舊流程（saffes 中古倒推）——回傳 dkp 覆蓋字集。
	internal static partial Task<IReadOnlySet<str>> 跑舊前段(DksPaths P, SvcDks Svc, IFnCtx Ctx, CT Ct);

	/// 產出 dks 的前段（不含後段四件）：新流程（布之道擬音）——回傳 dkp 覆蓋字集。
	internal static partial Task<IReadOnlySet<str>> 跑新前段(DksPaths P, SvcDks Svc, IFnCtx Ctx, CT Ct);

	/// 舊流程整套跑到**指定目錄**（供 Dks3/Dks4/AuditDks 取「原來的讀音」）：
	/// 在該目錄下備好 src（複製 saffes+dkp）與 user，跑完舊三步，回傳其 dks 路徑。
	internal static partial Task<str> 跑舊流程到目錄(str 目錄, DksPaths P, IFnCtx Ctx, CT Ct);

	/// 後段四件併行：dks_v（接倉頡）、dkn（首尾雙碼）、dks_phrase（造詞）、把 dkp/dkz 拷進用戶目錄。
	internal static partial Task 跑後段(DksPaths P, SvcDks Svc, IFnCtx Ctx, CT Ct);

	/// 產物清單（六個落點），供各命令末尾統一一句「已產出: …」——避免每個命令各抄一份文件名。
	internal static partial IReadOnlyList<str> 產物清單(DksPaths P);

	/// 建目錄（跑流程前先把落點備好）。收在端點層，庫內一律不管目錄。
	internal static partial void 備目錄(params str[] 目錄);
}
