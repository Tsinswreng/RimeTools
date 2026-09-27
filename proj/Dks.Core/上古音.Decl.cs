using RimeTools.Shared.Dict.Models;

namespace Dks.Core;

// public record 上古漢語聲母(str Value){
// 	public str Value{get;set;} = Value;
// 	public static implicit operator string(上古漢語聲母 z){
// 		return z.Value;
// 	}
// }

[Doc(@"
表示完整的上古漢語音節
如 礦: kʷr'aŋʔ
")]
public partial class Tswg上古漢語音節: DictLine{
	
	public str Full{get;set;} = "";
	
	[Doc(@"
	圓脣 用 上標 ʷ
	送氣 用 上標 ʰ
	同時有圓脣和送氣時, 先送氣後圓脣 kʰʷ
	不使用清化符,
	用ʰ表示清化。
	如 不採用n̥的寫法 而是用 nʰ的寫法
	")]
	public str 聲母{get;set;} = "";
	[Doc(@"
	非三等標記統一用  '
	非三等標記必須緊標在主元音之前
	")]
	public str 介腹{get;set;} = "";
	
	[Doc(@"
	有 j w l
	去聲用 s韻尾 不用h
	")]
	public str 尾調{get;set;} = "";
	
	[Doc("轉完整拼式")]
	public override partial str ToString();
	
}

public static partial class Tswg上古漢語音節Extn{
	[Doc("驗證是否爲合法音節")]
	public static partial bool Verify(this Tswg上古漢語音節 z);
	
	[Doc("非原地 正規化")]
	public static partial Tswg上古漢語音節 ToNormalized(this Tswg上古漢語音節 z);
}
