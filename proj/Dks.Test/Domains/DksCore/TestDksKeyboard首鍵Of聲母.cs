namespace Dks.Test.Domains.DksCore;

using Dks.Core;
using Tsinswreng.CsTreeTest;

/// 測試 DksKeyboard.首鍵Of聲母（聲母 → 首鍵）。
public partial class TestDksKeyboard{
	public void Register首鍵Of聲母(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestDksKeyboard),
			[typeof(DksKeyboard)],
			[nameof(DksKeyboard.首鍵Of聲母)],
			"首鍵Of聲母"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("文檔 Tbl 的聲母各落其鍵", async O => {
			var 表 = DksKeyboard.首鍵Of聲母;
			T(表["ʔ"] == 'Q', "ʔ 應落 Q");
			T(表["ŋ"] == 'W', "ŋ 應落 W");
			T(表["tʰ"] == 'E', "tʰ 應落 E");
			T(表["r"] == 'R', "r 應落 R");
			T(表["j"] == 'R', "j 應落 R（與 r 同鍵，靠介腹帶 r 區分）");
			T(表["t"] == 'T', "t 應落 T");
			T(表["w"] == 'Y', "w 應落 Y");
			T(表["kʷ"] == 'I', "kʷ 應落 I");
			T(表["lʰ"] == 'O', "lʰ 應落 O");
			T(表["p"] == 'P', "p 應落 P");
			T(表["s"] == 'S', "s 應落 S");
			T(表["d"] == 'D', "d 應落 D");
			T(表["pʰ"] == 'F', "pʰ 應落 F");
			T(表["ɡ"] == 'G', "ɡ 應落 G");
			T(表["h"] == 'H', "h 應落 H");
			T(表["k"] == 'K', "k 應落 K");
			T(表["l"] == 'L', "l 應落 L");
			T(表["dz"] == 'Z', "dz 應落 Z");
			T(表["kʰ"] == 'X', "kʰ 應落 X");
			T(表["ts"] == 'C', "ts 應落 C");
			T(表["tsʰ"] == 'V', "tsʰ 應落 V");
			T(表["b"] == 'B', "b 應落 B");
			T(表["n"] == 'N', "n 應落 N");
			T(表["m"] == 'M', "m 應落 M");
			T(表["mʰ"] == '.', "mʰ 應落 .");
			T(表["st"] == ';', "st 應落 ;");
			return null;
		});
		R("一鍵收多個聲母（多對一）", async O => {
			var 表 = DksKeyboard.首鍵Of聲母;
			// U 鍵：濁圓脣與濁流音/鼻音。
			T(表["ɡʷ"] == 'U' && 表["ŋʷ"] == 'U' && 表["ɡl"] == 'U' && 表["ɡj"] == 'U');
			T(表["ŋj"] == 'U' && 表["ɦ"] == 'U');
			// A 鍵：圓脣/帶 l/j 的送氣與喉塞。
			T(表["kʰʷ"] == 'A' && 表["kʰl"] == 'A' && 表["ʔʷ"] == 'A' && 表["kʰj"] == 'A');
			// ' , 鍵：清鼻音與 sN 系列。
			T(表["nʰ"] == ',' && 表["sn"] == ',' && 表["sʷ"] == ',' && 表["sm"] == ',');
			// ; 鍵：s+塞音/鼻音。
			T(表["st"] == ';' && 表["sŋ"] == ';' && 表["stʰ"] == ';' && 表["ŋʰ"] == ';');
			return null;
		});
		R("J 鍵收邪船兩系的音值", async O => {
			var 表 = DksKeyboard.首鍵Of聲母;
			// 文檔 J 欄寫中古母名「邪/船」，對應本方案音值 z/ʑ（邪）與 ml/sl（船）。
			T(表["邪"] == 'J' && 表["船"] == 'J');
			T(表["z"] == 'J' && 表["ʑ"] == 'J');
			T(表["ml"] == 'J' && 表["sl"] == 'J');
			return null;
		});
		R("適配新歸併的聲母各落其鍵", async O => {
			// 這幾條是「布之道記法 → 本方案」新增的落鍵：A 步原樣保留，B 步（查鍵）才歸鍵。
			var 表 = DksKeyboard.首鍵Of聲母;
			T(表["ɬ"] == 'S', "ɬ 按 s 那一鍵");
			T(表["hl"] == 'H' && 表["hʰ"] == 'H', "清化流音 hl 與 hʰ 按 h 那一鍵");
			T(表["bl"] == 'B', "bl 按 b 那一鍵");
			T(表["wʰ"] == '.' && 表["ʍ"] == '.', "清化圓脣半元音按 hʷ 那一鍵");
			T(表["rʰ"] == 'E', "rʰ 按 tʰ 那一鍵");
			T(表["sk"] == 'K', "sk 按 k 那一鍵");
			return null;
		});
		R("不設空聲母條目", async O => {
			// 完整拼式裏不存在空聲母；R/J 兩鍵的分歧改用「介腹有沒有 r」標記，故不設空聲母鍵。
			T(!DksKeyboard.首鍵Of聲母.ContainsKey(""), "不該有空聲母鍵位");
			T(!DksKeyboard.首鍵Of聲母.ContainsKey("q"));
			T(!DksKeyboard.首鍵Of聲母.ContainsKey("r'"));
			return null;
		});
	}
}
