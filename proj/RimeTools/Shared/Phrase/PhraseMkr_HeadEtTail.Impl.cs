namespace RimeTools.Shared.Phrase;

using RimeTools.Tools;

public partial class PhraseMkr_HeadEtTail:IPhraseMkr{
	public partial IReadOnlyList<str> MkPhrase(IReadOnlyList<str> CodesPerChar, IReadOnlyList<str>? Chars){
		var ans = new List<str>();
		// step 1: 單字不成詞（只有一個字的詞不產簡碼詞）。
		if(CodesPerChar.Count <= 1){
			return ans;
		}
		// step 2: 每個字碼取首尾字符；空碼跳過。
		for(var i = 0; i < CodesPerChar.Count; i++){
			var code = CodesPerChar[i];
			if(string.IsNullOrEmpty(code)){
				continue;
			}
			ans.Add(CodeTransformer.HeadTail(code));
		}
		return ans;
	}
}