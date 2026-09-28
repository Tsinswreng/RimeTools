namespace Dks.Test.Domains.DksCore;

using Dks.Core;
using Tsinswreng.CsTreeTest;

/// 測試 DksKeyboard.次鍵Of介腹（介腹 → 次鍵）。
public partial class TestDksKeyboard{
	public void Register次鍵Of介腹(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestDksKeyboard),
			[typeof(DksKeyboard)],
			[nameof(DksKeyboard.次鍵Of介腹)],
			"次鍵Of介腹"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("無介腹落 ;（文檔的 ∅）", async O => {
			T(DksKeyboard.次鍵Of介腹[""] == ';', "空介腹應落 ;");
			return null;
		});
		R("帶 r 的七個介腹各落其鍵", async O => {
			var 表 = DksKeyboard.次鍵Of介腹;
			T(表["ra"] == 'Q' && 表["re"] == 'W' && 表["ri"] == 'E' && 表["ro"] == 'R' && 表["ru"] == 'T');
			T(表["r'a"] == 'Y' && 表["r'u"] == 'U' && 表["r'i"] == 'I' && 表["r'o"] == 'O' && 表["r'e"] == 'P');
			return null;
		});
		R("三等與非三等分屬不同鍵", async O => {
			var 表 = DksKeyboard.次鍵Of介腹;
			// 同一元音：非三等（帶 '）在 Y/U/I/O/P，三等在 A/S/D/F/G。
			T(表["a"] == 'A' && 表["e"] == 'S' && 表["i"] == 'D' && 表["o"] == 'F' && 表["u"] == 'G');
			T(表["'a"] == 'Z' && 表["'e"] == 'X' && 表["'i"] == 'C' && 表["'o"] == 'V' && 表["'u"] == 'B');
			return null;
		});
		R("ə 系列", async O => {
			var 表 = DksKeyboard.次鍵Of介腹;
			T(表["ə"] == 'J' && 表["rə"] == 'K' && 表["'ə"] == 'L' && 表["r'ə"] == 'M');
			return null;
		});
		R("文檔的 ja/æ/A 各家寫法同鍵", async O => {
			var 表 = DksKeyboard.次鍵Of介腹;
			T(表["ja"] == 'N' && 表["æ"] == 'N' && 表["A"] == 'N' && 表["ʲa"] == 'N' && 表["ˡa"] == 'N');
			T(表["rja"] == 'H' && 表["ræ"] == 'H' && 表["rA"] == 'H' && 表["rʲa"] == 'H' && 表["rˡa"] == 'H');
			return null;
		});
		R("帶 ' 的 ja/æ/A 落 , 與 .", async O => {
			var 表 = DksKeyboard.次鍵Of介腹;
			T(表["'ja"] == ',' && 表["'æ"] == ',' && 表["'A"] == ',');
			T(表["r'ja"] == '.' && 表["r'æ"] == '.' && 表["r'A"] == '.');
			return null;
		});
		R("適配新歸併的 ji 同 i", async O => {
			// 布之道寫 ji，本方案寫 i；A 步不歸併，故兩者都留在表裏。
			T(DksKeyboard.次鍵Of介腹["ji"] == 'D', "ji 與 i 同鍵");
			return null;
		});
	}
}
