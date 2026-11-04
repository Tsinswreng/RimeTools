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
	//數據結構版本。此常數與導出 JSON 頂層的 "schemaVer" 欄必須一致：
	//導出時把此值寫進 JSON，讀回 JSON 時斷言兩者相同，用來擋「表結構改了、手上的卻是舊 JSON」。
	public const string SchemaVer = "1";

	public static void Z(
		string 字, Action<CbArg> Cb
		
	){
		
	}
}

public class CbArg{
	
	//Tsinswreng 上古漢語擬音
	public void TswgOc(string 音){
		
	}
	
	//Tsinswreng 上古漢語魔改擬音
	public void TswgOcModed(string 音){
		
	}
	public void TswgOcModed(string 音, Action<CbArg_TswgOcModed> Cb){
		
	}
	
	//Polyhedron 中古拼音
	public void PolyMc(string 音){
		
	}
	public void PolyMc(string 音, Action<CbArg_PolyMc> Cb){
		
	}
	
	//白-沙
	public void BsOc(Action<CbArg_BsOc> Cb){
		
	}
	
	//許思萊
	public void SchOc(string 音){
		
	}
	public void SchOc(string 音, Action<CbArg_SchOc> Cb){
		
	}
	
	//Msoeg
	public void MsoegOc(string 音){
		
	}
	
	//斯塔羅斯金
	public void StaOc(string 音){
		
	}
	
	//鄭張尚芳
	public void DtOc(string 音){
		
	}
	
	//廣韻形聲考聲
	public void Ptl(Action<CbArg_Ptl> Cb){
		
	}
	
	//廣韻釋義
	public void 廣韻意(string S){
		
	}
	
	
	
	public class CbArg_BsOc{
		public void Oc(string S){
			
		}
		public void Gloss(string S){
			
		}
		
	}
	
	public class CbArg_SchOc{
		//來源表的原註釋。OC_schuesslerOC.dict.yaml 正文裏只有兩處節標題：
		//「# 原書詞組」（緊接 踟躕 那條之前）與「# Voice from Early China」（緊接 於 那條之前）。
		public void Note(string Note){
			
		}
	}
	
	public class CbArg_Ptl{
		public void 聲符(string S){
			
		}
		
		public void 諧聲域(string S){
			
		}
		
		public void 記號(string S){
			
		}
		
		public void 反切(string S){
			
		}
		
		public void 中古地位(string S){}
		
		public void 切拼(string S){
			
		}
		
		public void 上古音參考(string S){
			
		}
		public void 考釋(string S){
			
		}
	}
	
	public class CbArg_TswgOcModed{
		public void TestDsl_Note(Action<CbArg_TswgOcModed_TestDsl_Note> Cb){
			
		}
		
		public void Note(string Note){
			
		}
		
		//權重：來源表第三欄的百分比原值，不換算成比值。
		//dkp 例：辣	rˁat	10000% ⇒ Ratio(10000)；為	waj		0% ⇒ Ratio(0)；
		//該欄為空（如 込	kom）⇒ 不調用此方法。
		public void Ratio(double Ratio){
			
		}
		
		public class CbArg_TswgOcModed_TestDsl_Note{
			public void 仿中古音(){}
		}
	}
	
	public class CbArg_PolyMc{
		//權重：zyenpheng.dict.yaml 第三欄的百分比原值，不換算成比值。
		//例：谷	giak	1% ⇒ Ratio(1)；好	hauh	30% ⇒ Ratio(30)；
		//該欄為空（如 谷	kuk）⇒ 不調用此方法。
		public void Ratio(double Ratio){}
	}

}
