namespace RimeTools.Tools;

/// 碼表編碼變換工具。
public static partial class CodeTransformer{
	/// 取編碼的首字符與尾字符拼成新碼；長度為 1 的碼重複自身；空碼返回空串。
	/// 對應 Dks.sh 的 awk 轉換以及 dks_phrase 的「每字取首末碼」造詞策略（dkn 與 dks_phrase 共用）。
	public static partial str HeadTail(str Code);
}