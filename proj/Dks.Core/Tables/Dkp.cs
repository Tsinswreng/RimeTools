public class Reason{
	public str Value{get;set;}
	public static Reason 改音調{get;set;} = new();
}
public record class Z{
	public Z(str Id){
		this.Id = Id;
	}
	public str Id{get;set;} = "";
	public str? O{get;set;}
	public str? U{get;set;}
	public str? D{get;set;}
	public Reason? R{get;set;}
	
	public static void Init(){
		List<Z> l = [
			new("忠"){
				O="t",
				U="ru",
				D="ŋʔ",
				R=Reason.改音調
			},
			new("喵"){
				O="m",
				U="A",
				D="w",
			}
			
		];
	}
}
