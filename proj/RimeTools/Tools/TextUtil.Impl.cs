namespace RimeTools.Tools;

using System.Text;

public static partial class TextUtil{
	public static partial IReadOnlyList<str> SplitByRune(str Text){
		var ans = new List<str>();
		if(string.IsNullOrEmpty(Text)){
			return ans;
		}
		// step 1: 按 Unicode 碼點枚舉，一個碼點一個元素（代理對合成一個 Rune，不會被拆半）。
		foreach(var rune in Text.EnumerateRunes()){
			ans.Add(rune.ToString());
		}
		return ans;
	}
}