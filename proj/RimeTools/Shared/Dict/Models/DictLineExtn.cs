namespace RimeTools.Shared.Dict.Models;

/// 默認三列語義的便捷訪問器：text/code/weight 就是字典鍵的強類型外觀（C#14 extension members）。
/// 只對「鍵名恰為 text/code/weight」的文檔有效；自定義 columns（如 essay 的 text/weight）時
/// code 鍵不存在 → 返回 ""，請用字典索引直接取。
public static class DictLineExtn{
	/// 擴展接收者：任意 IDictLine 都帶上下列便捷屬性。
	extension(IDictLine z){
		/// text 鍵：詞或字。等價 z["text"]；無鍵返回 ""。
		public str text{
			get => z["text"] as str ?? "";
			set => z["text"] = value;
		}

		/// code 鍵：編碼。例：一個→qkkn；essay 兩列表無此鍵 → ""。
		public str code{
			get => z["code"] as str ?? "";
			set => z["code"] = value;
		}

		/// weight 鍵：權重字符串（"10000%" / "374279"）；無鍵返回 null。
		public str? weight{
			get => z["weight"] as str;
			set => z["weight"] = value;
		}
	}
}