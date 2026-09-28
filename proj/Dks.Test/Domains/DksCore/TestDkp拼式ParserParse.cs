namespace Dks.Test.Domains.DksCore;

using Dks.Core;
using Tsinswreng.CsTreeTest;

/// 測試 Dkp拼式Parser（義符/聲符字形與純音切分、正規化、最長前綴取聲母）。
public partial class TestDkp拼式Parser:ITester{
	public ITestNode RegisterTestsInto(ITestNode? Node){
		Node ??= new TestNode();
		Node.IsParallelRecursive = true;
		RegisterParse(Node);
		return Node;
	}

	public void RegisterParse(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestDkp拼式Parser),
			[typeof(Dkp拼式Parser)],
			[nameof(Dkp拼式Parser.Parse)],
			"Parse"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("以最後一個元音為界切介腹與尾調", async O => {
			var z = Dkp拼式Parser.Parse("mrˁeʔ");
			T(z.聲母 == "m", $"聲母應為 m，實得 [{z.聲母}]");
			T(z.介腹 == "r'e", $"介腹應為 r'e，實得 [{z.介腹}]");
			T(z.尾調 == "ʔ", $"尾調應為 ʔ，實得 [{z.尾調}]");
			return null;
		});
		R("義符與聲符字形各查表", async O => {
			// 手→nʰ（義符表）、成→eŋ（聲符表）⇒ 言成 得 ŋeŋ、手ˁaj 得 nʰ'a j。
			var a = Dkp拼式Parser.Parse("言成");
			T(a.聲母 == "ŋ" && a.介腹 == "e" && a.尾調 == "ŋ");
			T(a.Full == "ŋeŋ", $"Full 應為 ŋeŋ，實得 [{a.Full}]");
			var b = Dkp拼式Parser.Parse("手ˁaj");
			T(b.聲母 == "nʰ", $"手 應查得 nʰ，實得 [{b.聲母}]");
			T(b.介腹 == "'a" && b.尾調 == "j");
			return null;
		});
		R("最長可行前綴當聲母", async O => {
			// rˁat ⇒ 聲母取 r（而非空）⇒ 介腹 'a、尾調 t；kʷrˁaŋʔ ⇒ 聲母取 kʷ（k 帶不出介腹）。
			var a = Dkp拼式Parser.Parse("rˁat");
			T(a.聲母 == "r" && a.介腹 == "'a" && a.尾調 == "t");
			var b = Dkp拼式Parser.Parse("kʷrˁaŋʔ");
			T(b.聲母 == "kʷ" && b.介腹 == "r'a" && b.尾調 == "ŋʔ");
			return null;
		});
		R("無元音的韻段整段當介腹", async O => {
			// sŋ 這種整串輔音：沒有元音可切，故介腹為整段、尾調空。
			var z = Dkp拼式Parser.Parse("sŋ");
			T(z.聲母 == "sŋ", $"聲母應取最長前綴 sŋ，實得 [{z.聲母}]");
			T(z.介腹 == "" && z.尾調 == "");
			return null;
		});
		R("正規化：非三等、送氣圓脣次序、濁塞音字形", async O => {
			T(Dkp拼式Parser.Parse("rˁat").介腹 == "'a", "ˁ 應寫成 '");
			T(Dkp拼式Parser.Parse("kʷʰat").聲母 == "kʰʷ", "ʷʰ 應寫成 ʰʷ（送氣在前）");
			T(Dkp拼式Parser.Parse("gat").聲母 == "ɡ", "g 應寫成 ɡ");
			return null;
		});
		R("空拼式返回空音節", async O => {
			T(Dkp拼式Parser.Parse("").Full == "");
			T(Dkp拼式Parser.Parse("   ").Full == "");
			return null;
		});
	}
}
