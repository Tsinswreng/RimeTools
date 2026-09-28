namespace RimeTools.Shared.Dict.Parser;

using RimeTools.Shared.Dict.Models;

/// dict.yaml 解析器：把碼表**文本**解析為表頭 + 正文行流。
/// 本接口只認 `TextReader`——不認路徑、不開檔、不關檔；開檔與關檔是端點的事
/// （路徑版簡便寫法見 `IDictYamlParserExtn.Parse(this IDictYamlParser, str Path, CT)`）。
/// 例（真實文件 dks.dict.yaml，餵給 Reader 的內容）：
///   #20260818214532            ← 文件頂註釋（生成時間戳等，位於 --- 之前，跳過）
///   ---
///   name: dks
///   version: ""
///   sort: by_weight
///   use_preset_vocabulary: true
///   import_tables:
///     - nonKanji
///     - chineseDict
///   ...
///   辣	ryt	10000%
/// Parse 結果：Header.Items 保序含 name/version/sort/use_preset_vocabulary/import_tables 五項；
/// Body 首行是 IDictLine{text:"辣", code:"ryt", weight:"10000%"}（字典鍵即列名，訪問走 DictLineExtn）。
public interface IDictYamlParser{
	/// 從 Reader 解析：先讀完表頭段（`---` 到 `...`），正文由返回文檔的 Body **續讀同一個 Reader**。
	/// 因此 Body 消費完（或提前中止）之前 Reader 必須保持可用；解析器不負責 Dispose 它。
	/// 處理邊界：
	/// - `---` 之前的 `#` 註釋行（時間戳、說明等）跳過，不入 Header
	/// - 表頭行缺冒號、或走到 EOF 仍未見 `...` 時拋 `FormatException`
	/// - 正文行去除首尾空白後按 Tab 拆列；行內 `#` 之後的內容丟棄（如權重 `0% # 舍他音` → `0%`）
	/// - 正文行格子數 ≤ Columns 數：逐格以 Columns 名為鍵收進字典；少格子即少鍵（如 dks_v 的「一	qdkm;」無 weight 鍵）
	/// - 正文行格子數 > Columns 數屬異常文件，解析器忽略多出的尾格
	/// - 正文中形如 `...`／`---` 的文檔分隔行跳過（永不當數據）
	/// <param name="Reader">dict.yaml 文本源（UTF-8；生命週期歸調用方）。</param>
	/// <param name="Ct">取消令牌。</param>
	Task<RimeDictDoc> Parse(TextReader Reader, CT Ct);
}
