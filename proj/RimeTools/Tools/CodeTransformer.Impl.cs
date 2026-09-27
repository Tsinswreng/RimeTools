namespace RimeTools.Tools;

public static partial class CodeTransformer{
	public static partial str HeadTail(str Code){
		if(string.IsNullOrEmpty(Code)){
			return "";
		}
		if(Code.Length == 1){
			return Code + Code;
		}
		return Code[..1] + Code[^1..];
	}
}