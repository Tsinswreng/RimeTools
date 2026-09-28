namespace Dks.Test.Domains.DksCore;

using Dks.Core;
using Tsinswreng.CsTreeTest;

/// 測試 布之道拼式Parser.Parse（適配之後接本方案同一套切韻）。
public partial class Test布之道拼式Parser{
	public void RegisterParse(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(Test布之道拼式Parser),
			[typeof(布之道拼式Parser)],
			[nameof(布之道拼式Parser.Parse)],
			"Parse"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("圓脣聲母與 r 介腹切三段", async O => {
			// 礦: kwrˤaŋʔ ⇒ kʷ（上標）+ r'a + ŋʔ。
			var z = 布之道拼式Parser.Parse("kwrˤaŋʔ");
			T(z.聲母 == "kʷ" && z.介腹 == "r'a" && z.尾調 == "ŋʔ");
			T(z.Full == "kʷr'aŋʔ", $"Full 應為 kʷr'aŋʔ，實得 [{z.Full}]");
			return null;
		});
		R("去聲改 s 後仍切出三段", async O => {
			var z = 布之道拼式Parser.Parse("pʰrˤuh");
			T(z.聲母 == "pʰ" && z.介腹 == "r'u" && z.尾調 == "s");
			return null;
		});
		R("括號刪掉後照樣能切", async O => {
			var z = 布之道拼式Parser.Parse("b(r)u");
			T(z.聲母 == "b" && z.介腹 == "u" && z.尾調 == "");
			var z2 = 布之道拼式Parser.Parse("m̥(r)uk");
			T(z2.聲母 == "mʰ" && z2.介腹 == "u" && z2.尾調 == "k");
			return null;
		});
		R("空串返回空音節", async O => {
			T(布之道拼式Parser.Parse("").Full == "");
			return null;
		});
	}
}
