namespace RimeTools.Scripts;

using static System.IO.Path;

/// CsTbl 的事實源配置。
/// **這是全專案唯一寫死「事實源在哪」的地方**：換目錄只改 `Dir`，某張表換檔名只改對應的 `File*` 常量。
/// 其他代碼一律經 `SrcPath(檔名)` 取完整路徑，不再自行拼目錄，也不在別處出現絕對路徑。
///
/// 為甚麼集中：事實源原本散在倉庫內外多處，每搬一次家就要改多個屬性與多段註釋；
/// 收成「一個目錄常量 ＋ 一組檔名常量 → SrcPath」之後，路徑知識只剩這一份。
/// 註釋只寫「這份表是甚麼、歸哪個方法用」，不寫它此刻放在哪個磁碟目錄——
/// 位置會變，角色不會；位置寫進註釋就等於多了一處需要同步的真相。
internal static class CsTblFacts{
	/// 事實源目錄：九張來源表都放在這一個目錄下。
	internal const str Dir = @"D:\Program Files\Rime\User_Data\_\Facts";

	// ── 檔名常量：只寫檔名、不寫目錄，由 SrcPath 與 Dir 組合 ──

	/// Tsinswreng 手工表：字、音、權重；行內與上方緊鄰的註釋都要保留。
	internal const str FileDkp = "dkp.dict.yaml";

	/// Polyhedron 中古全拼：中古音用拼音表示（不是 IPA），另帶權重欄。
	internal const str FilePolyMc = "Polyhedron中古全拼.dict.yaml";

	/// 廣韻形聲考主字表：單字、九欄 TSV。
	/// 同目錄另有 group／appendix 兩份，是本表的另兩種切法與附錄，本命令不收。
	internal const str File形聲考 = "廣韻形聲考_char.tsv";

	/// 白-沙官方字表：11 欄 TSV，音只取上古音欄。
	internal const str File白沙 = "baxtersagart.tsv";

	/// msoeg 上古擬音：碼欄形如 `ASCII*IPA`，只取 `*` 之後的 IPA。
	internal const str FileMsoeg = "OC_msoeg.dict.yaml";

	/// 許思萊上古音：碼欄同樣是 `ASCII*IPA`。
	internal const str FileSchuessler = "OC_schuesslerOC.dict.yaml";

	/// 鄭張尚芳上古音：多音分行。
	internal const str FileZhengzhang = "OC_鄭張.dict.yaml";

	/// 斯塔羅斯金（qgsg_stn 版）：大量行是空音，空音不調用。
	internal const str FileStaOc = "qgsg_stn.dict.yaml";

	/// 廣韻全字表＋釋義：一行的格式是「字 小韻序號 小韻內序 釋義」（分隔符是空格，不是 Tab）。
	internal const str File廣韻 = "廣韻.txt";

	/// 由檔名組出完整路徑；**所有事實源路徑的唯一入口**。
	internal static str SrcPath(str FileName){
		return Combine(Dir, FileName);
	}
}
