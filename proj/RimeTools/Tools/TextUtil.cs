namespace RimeTools.Tools;

/// 文本工具。
public static partial class TextUtil{
	/// 按 Unicode 碼點拆分字符串，避免把代理對（如 emoji）拆成兩半。返回空表代表輸入為空串。
	public static partial IReadOnlyList<str> SplitByRune(str S);
}