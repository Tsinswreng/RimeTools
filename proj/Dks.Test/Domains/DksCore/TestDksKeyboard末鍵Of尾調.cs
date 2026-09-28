namespace Dks.Test.Domains.DksCore;

using Dks.Core;
using Tsinswreng.CsTreeTest;

/// 測試 DksKeyboard.末鍵Of尾調（尾調 → 末鍵）。
public partial class TestDksKeyboard{
	public void Register末鍵Of尾調(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestDksKeyboard),
			[typeof(DksKeyboard)],
			[nameof(DksKeyboard.末鍵Of尾調)],
			"末鍵Of尾調"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("無尾調落 J（開音節）", async O => {
			T(DksKeyboard.末鍵Of尾調[""] == 'J', "開音節應落 J");
			return null;
		});
		R("鼻音與塞音尾各落其鍵", async O => {
			var 表 = DksKeyboard.末鍵Of尾調;
			T(表["ŋ"] == 'S' && 表["n"] == 'D' && 表["m"] == 'F');
			T(表["k"] == 'K' && 表["t"] == 'T' && 表["p"] == 'P');
			T(表["ʔ"] == 'U');
			return null;
		});
		R("上聲（ʔ 尾）與去聲（s 尾）各落其鍵", async O => {
			var 表 = DksKeyboard.末鍵Of尾調;
			T(表["ŋʔ"] == 'W' && 表["nʔ"] == 'E' && 表["mʔ"] == 'R' && 表["jʔ"] == 'Y' && 表["wʔ"] == 'Q');
			T(表["ls"] == '.' && 表["lʔ"] == 'O');
			T(表["s"] == 'M' && 表["js"] == 'N' && 表["ws"] == 'Z');
			return null;
		});
		R("去聲加塞尾與 s 尾複合", async O => {
			var 表 = DksKeyboard.末鍵Of尾調;
			T(表["ps"] == 'B' && 表["ts"] == 'I' && 表["ks"] == 'G' && 表["ms"] == 'V');
			T(表["ns"] == 'C' && 表["ŋs"] == 'X');
			return null;
		});
		R("半元音尾各落其鍵", async O => {
			var 表 = DksKeyboard.末鍵Of尾調;
			T(表["j"] == 'H' && 表["w"] == 'A' && 表["l"] == 'L');
			// 文檔把 wk/wks 併作一格（,）。
			T(表["wk"] == ',' && 表["wks"] == ',');
			return null;
		});
	}
}
