namespace RimeTools.Shared.Dict.Models;

/// IDictLine 的默認實現：直接繼承 Dictionary<obj,obj?>，字典成員零成本齊備。
/// 解析器把「列名 ↔ 格子」配對後塞進本字典即可；不做任何列數/鍵的預設。
/// 例：
///   var line = new DictLine{ ["text"]="一個", ["code"]="qkkn", ["weight"]="374279" };
/// 訪問: line.text 走 DictLineExtn → "一個"。
public sealed partial class DictLine:Dictionary<obj,obj?>,IDictLine{
	/// 創建空行。
	public partial DictLine();
}