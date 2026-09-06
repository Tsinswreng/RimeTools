namespace RimeTools.Shared.Dict.Parser;

using RimeTools.Shared.Dict.Models;

/// dict.yaml 解析器：把碼表文件解析為表頭 + 正文行流。
/// 這是「存儲可換」抽象的一部分：文件解析是純 IO 層，不感知數據庫。
/// 例（真實文件 dks.dict.yaml）：
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
	/// 解析一個 dict.yaml 文件，返回表頭與正文行流。
	/// 正文行流是惰性的，解析器只預先讀取表頭段（`---` 到 `...`），正文行按需流式產出。
	/// 處理邊界：
	/// - `---` 之前的 `#` 註釋行（時間戳、說明等）跳過，不入 Header
	/// - 表頭缺 `---`/`...` 閉合、正文行格式異常時拋出異常
	/// - 正文行去除行尾空白後按 Tab 拆列；行尾多餘空列忽略
	/// - 正文行格子數 ≤ Columns 數：逐格以 Columns 名為鍵收進字典；少格子即少鍵（如 dks_v 的「一	qdkm;」無 weight 鍵）
	/// - 正文行格子數 > Columns 數屬異常文件，解析器忽略多出的尾格
	/// <param name="Path">dict.yaml 文件絕對路徑（UTF-8 無 BOM）。</param>
	/// <param name="Ct">取消令牌。</param>
	Task<RimeDictDoc> Parse(str Path, CT Ct);
}