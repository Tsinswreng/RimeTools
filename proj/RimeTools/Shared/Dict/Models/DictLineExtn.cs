namespace RimeTools.Shared.Dict.Models;

/// 默認三列語義的便捷訪問器：text/code/weight 就是字典鍵的強類型外觀（C#14 extension members）。
/// 只對「鍵名恰為 text/code/weight」的文檔有效；自定義 columns（如 essay 的 text/weight）時
/// code 鍵不存在 → 返回 ""，請用字典索引直接取。
public static class DictLineExtn{
	/// 擴展接收者：任意 IDictLine 都帶上下列便捷屬性。
	extension(IDictLine z){
		/// text 鍵：詞或字。等價 z[DictColumns.Text]；無鍵返回 ""。
		public str text{
			get => z.TryGetValue(DictColumns.Text, out var v) ? v as str ?? "" : "";
			set => z[DictColumns.Text] = value;
		}

		/// code 鍵：編碼。例：一個→qkkn；essay 兩列表無此鍵 → ""。
		public str code{
			get => z.TryGetValue(DictColumns.Code, out var v) ? v as str ?? "" : "";
			set => z[DictColumns.Code] = value;
		}

		/// weight 鍵：權重字符串（"10000%" / "374279"）；無鍵返回 null。
		public str? weight{
			get => z.TryGetValue(DictColumns.Weight, out var v) ? v as str : null;
			set => z[DictColumns.Weight] = value;
		}
	}
}