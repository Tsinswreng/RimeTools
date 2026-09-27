using RimeTools.Shared.Dict.Models;

namespace Dks.Core;

// public record 上古漢語聲母(str Value){
// 	public str Value{get;set;} = Value;
// 	public static implicit operator string(上古漢語聲母 z){
// 		return z.Value;
// 	}
// }



public partial class Tswg上古漢語音節{
	
	public override partial str ToString(){
		return this.Full;
	}
}

public static partial class Tswg上古漢語音節Extn{
	
	public static partial bool Verify(this Tswg上古漢語音節 z){
		return true;
	}
	
	[Doc("非原地 正規化")]
	public static partial Tswg上古漢語音節 ToNormalized(this Tswg上古漢語音節 z){
		return z;
	}
}
