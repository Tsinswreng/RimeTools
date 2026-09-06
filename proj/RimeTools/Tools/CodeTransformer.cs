namespace RimeTools.Tools;

/// 碼表編碼變換工具：把一個完整編碼變換成另一個（簡碼策略）。
public static partial class CodeTransformer{
	/// 取編碼的首字符與尾字符拼成新碼；長度為 1 的碼重複自身；空碼返回空串。
	/// 例：HeadTail("ryt") → "rt"；HeadTail("a") → "aa"；HeadTail("") → ""。
	/// 對應 Dks.sh 的 awk 轉換（dkn 全表首尾碼）與 dks_phrase 的「每字取首末碼」造詞策略。
	/// <param name="Code">完整編碼（如 "ryt"）。</param>
	public static partial str HeadTail(str Code);
}