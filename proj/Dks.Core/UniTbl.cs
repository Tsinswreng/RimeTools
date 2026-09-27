namespace _;

public class Keys{
	
}

public interface IInitable{
	
}

public class Base:Dictionary<obj,obj?>,IInitable{}

public class Rec:Base{
	
}

public static class RecExtn{
	extension(Rec z){
		public f64 八股文字頻{
			get=>(f64)z["八股文字頻"]!;
			set=>z["八股文字頻"]=value;
		}
	}
	extension<T>(T z)
		where T:IInitable
	{
		public Action<T> Init{
			set{
				value(z);
			}
		}
	}
}

public class Tswg上古:Base{
	
}

public static class Tswg上古Extn{
	extension(Tswg上古 z){
		//public 
	}
}

public class UniTbl{
	
	public void Init(){
		this.A("字", o=>{
			o.八股文字頻 = 1;
			
		})
		;
	}
	// public UniTbl A(str Key, Rec A){
	// 	return this;
	// }
	public UniTbl A(str Key, Action<Rec> A){
		return this;
	}
}
