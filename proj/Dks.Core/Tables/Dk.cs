using static Global;


Z("有", a=>{
	a.TswgOcModed("wuʔ", b=>{
		b.Note("仿中古音");
	});
	a.TswgOc("wəʔ");
	a.Ptl(b=>{
		b.聲符("");
	});
	a.BsOc(b=>{
		b.Oc("");
		b.Gloss("");
	});
});
Z("只", a=>{
	a.TswgOcModed("kejʔ", b=>{
		
	});
	a.TswgOc("keʔ");
	//...
});

public class Global{
	public static void Z(
		str 字, Action<CbArg> Cb
		
	){
		
	}
}

public class CbArg{
	
	//Tsinswreng 上古漢語擬音
	public void TswgOc(str 音){
		
	}
	
	//Tsinswreng 上古漢語魔改擬音
	public void TswgOcModed(str 音, Action<CbArg_TswgOcModed> Cb){
		
	}
	
	//Polyhedron 中古拼音
	public void PolyMc(str 音){
		
	}
	public void PolyMc(str 音, Action<CbArg_PolyMc> Cb){
		
	}
	
	//白-沙
	public void BsOc(Action<CbArg_BsOc> Cb){
		
	}
	
	//許思萊
	public void SchOc(str 音){
		
	}
	
	//Msoeg
	public void MsoegOc(str 音){
		
	}
	
	//斯塔羅斯金
	public void StaOc(str 音){
		
	}
	
	//鄭張尚芳
	public void DtOc(str 音){
		
	}
	
	//廣韻形聲考聲
	public void Ptl(Action<CbArg_Ptl> Cb){
		
	}
	
	//廣韻釋義
	public void 廣韻意(str S){
		
	}
	
	
	
	public class CbArg_BsOc{
		public void Oc(str S){
			
		}
		public void Gloss(str S){
			
		}
		
	}
	
	public class CbArg_Ptl{
		public void 聲符(str S){
			
		}
		
		public void 諧聲域(str S){
			
		}
		
		public void 記號(str S){
			
		}
		
		public void 反切(str S){
			
		}
		
		public void 中古地位(str S){}
		
		public void 切拼(str S){
			
		}
		
		public void 上古音參考(str S){
			
		}
		public void 考釋(str S){
			
		}
	}
	
	public class CbArg_TswgOcModed{
		public void TestDsl_Note(Action<CbArg_TswgOcModed_TestDsl_Note> Cb){
			
		}
		
		public void Note(str Note){
			
		}
		
		public class CbArg_TswgOcModed_TestDsl_Note{
			public void 仿中古音(){}
		}
	}
	
	public class CbArg_PolyMc{
		public void Ratio(f64 Ratio){}
	}

}
