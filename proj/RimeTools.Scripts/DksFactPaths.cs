namespace RimeTools.Scripts;

using static System.IO.Path;

/// 新 Dks 命令的路徑配置：**只有這裏知道真相源與產物的位置**。
/// 真相源全部集中在同一個目錄（默認 `...\_\Facts`），產物全部寫到同一個目錄（默認 User_Data）。
/// 可用 `-i` / `-o` 覆寫；改檔名或搬家時只改本檔的常量。
internal sealed partial class DksFactPaths{
	/// 默認真相源目錄。
	internal const str DfltInDir = @"D:\Program Files\Rime\User_Data\_\Facts";

	/// 默認產物目錄。
	internal const str DfltOutDir = @"D:\Program Files\Rime\User_Data";

	// ── 真相源檔名 ──

	/// 魔改表（整字覆寫）。
	internal const str FileTswgOcModed = "TswgOcModed.csx";

	/// 正音表（非魔改）。
	internal const str FileTswgOc = "TswgOc.csx";

	/// 布之道原表（原拼寫）。
	internal const str FilePaasTvLuuq = "PaasTvLuuq.csx";

	/// 中古倒推表（最低優先級；由舊 Dks 規則推得的快照）。
	internal const str FileMcToOc = "McToOc.csx";

	/// 倉頡表（dks_v 的輔助碼來源）。
	internal const str FileCangjie = "cangjie3c含五六相異者.yaml";

	/// 詞頻源（dks_phrase 的詞與權重來源）。
	internal const str FileEssay = "essay.txt";

	/// csx 表共用的序列化工具。
	internal const str FileExport = "Export.csx";

	/// csx 表共用的型別定義。
	internal const str FileDsl = "Dsl.csx";

	// ── 產物檔名 ──

	internal const str FileOutDks = "dks.dict.yaml";
	internal const str FileOutDksV = "dks_v.dict.yaml";
	internal const str FileOutDkn = "dkn.dict.yaml";
	internal const str FileOutDksPhrase = "dks_phrase.dict.yaml";

	/// 真相源目錄。
	internal str InDir{get;set;} = DfltInDir;

	/// 產物目錄。
	internal str OutDir{get;set;} = DfltOutDir;

	internal str Dsl => Combine(InDir, FileDsl);
	internal str Export => Combine(InDir, FileExport);
	internal str TswgOcModed => Combine(InDir, FileTswgOcModed);
	internal str TswgOc => Combine(InDir, FileTswgOc);
	internal str PaasTvLuuq => Combine(InDir, FilePaasTvLuuq);
	internal str McToOc => Combine(InDir, FileMcToOc);
	internal str Cangjie => Combine(InDir, FileCangjie);
	internal str Essay => Combine(InDir, FileEssay);

	internal str OutDks => Combine(OutDir, FileOutDks);
	internal str OutDksV => Combine(OutDir, FileOutDksV);
	internal str OutDkn => Combine(OutDir, FileOutDkn);
	internal str OutDksPhrase => Combine(OutDir, FileOutDksPhrase);

	/// 解析 `-i <真相源目錄>` 與 `-o <產物目錄>`；兩者皆可省略。
	internal static partial DksFactPaths FromArgs(str[] Args);

	/// 檢查所有必需的真相源檔是否存在；缺哪個就一次列全。
	internal static partial void 要求存在(DksFactPaths P);
}
