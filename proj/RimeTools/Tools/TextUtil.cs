namespace RimeTools.Tools;

/// 文本工具。
public static partial class TextUtil{
	/// 按 Unicode 碼點拆分字符串，避免把代理對（如 emoji）拆成兩半。
	/// 例：SplitByRune("辣一") → ["辣","一"]；SplitByRune("😀") → ["😀"]（不會拆成兩個代理項）。
	/// 返回空表代表輸入為空串。
	/// <param name="S">要拆分的字符串。</param>
	public static partial IReadOnlyList<str> SplitByRune(str S);
}