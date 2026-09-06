namespace RimeTools.Shared.Dict.Models;

/// dict.yaml 正文的一行 = 一個文檔對象(jsonb 式)：列名 → 值的集合，不是固定槽位的關係型行。
/// 接口直接就是字典：鍵是列名(obj)、值是列值(obj?)，缺鍵即「無此列」語義。
/// 例(dks_phrase.dict.yaml, 表頭 columns: [text, code, weight])：
///   源行: 一個	qkkn	374279
///   文檔: {text:"一個", code:"qkkn", weight:"374279"}
/// 例(dks_v.dict.yaml 某行只有兩格，無 columns → Rime 默認三列語義)：
///   源行: 一	qdkm;
///   文檔: {text:"一", code:"qdkm;"}   → z["weight"] 返回 null（無此鍵）
/// 例(essay.dict.yaml, 表頭 columns: [text, weight])：
///   源行: 〇〇	658
///   文檔: {text:"〇〇", weight:"658"}  → z["code"] 返回 null（無此鍵）
public interface IDictLine:IDictionary<obj, obj?>{
}