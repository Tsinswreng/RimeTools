namespace Dks.Test.Domains.DksCore;

using Dks.Core;
using Tsinswreng.CsTreeTest;

/// 測試 布之道拼式Parser.Adapt（只做記法轉換、不切分的那一步）。
public partial class Test布之道拼式Parser{
	public void RegisterAdapt(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(Test布之道拼式Parser),
			[typeof(布之道拼式Parser)],
			[nameof(布之道拼式Parser.Adapt)],
			"Adapt"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("小括號連內容刪掉、中括號只刪括號", async O => {
			T(布之道拼式Parser.Adapt("b(r)u") == "bu", $"b(r)u 應成 bu，實得 [{布之道拼式Parser.Adapt("b(r)u")}]");
			T(布之道拼式Parser.Adapt("l[a]u") == "law", $"l[a]u 應成 law（韻尾 u→w），實得 [{布之道拼式Parser.Adapt("l[a]u")}]");
			T(布之道拼式Parser.Adapt("[k.r]ˤa") == "k.rˤa", $"中括號內容保留，實得 [{布之道拼式Parser.Adapt("[k.r]ˤa")}]");
			return null;
		});
		R("末尾 h 是去聲 ⇒ 改成 s", async O => {
			T(布之道拼式Parser.Adapt("pʰrˤuh") == "pʰrˤus", "去聲標記 h 應改 s");
			T(布之道拼式Parser.Adapt("tˤah") == "tˤas", "去聲標記 h 應改 s");
			// 非末尾的 h（聲母或韻腹）不動。
			T(布之道拼式Parser.Adapt("hlak") == "hlak", "聲母的 h 不該被動");
			return null;
		});
		R("清化符改成後置 ʰ", async O => {
			// 組合下環/上環兩種寫法都要認；清化信息保留，不歸併成別的寫法。
			T(布之道拼式Parser.Adapt("w̥a") == "wʰa", $"w̥ 應成 wʰ，實得 [{布之道拼式Parser.Adapt("w̥a")}]");
			T(布之道拼式Parser.Adapt("n̥ˤu") == "nʰˤu", $"n̥ 應成 nʰ，實得 [{布之道拼式Parser.Adapt("n̥ˤu")}]");
			T(布之道拼式Parser.Adapt("m̊uk") == "mʰuk", $"m̊ 應成 mʰ，實得 [{布之道拼式Parser.Adapt("m̊uk")}]");
			return null;
		});
		R("ɫ 改成 j、ǝ 改成 ə", async O => {
			T(布之道拼式Parser.Adapt("ɫa") == "ja", "ɫ 應成 j");
			T(布之道拼式Parser.Adapt("kǝk") == "kək", "ǝ 應成 ə");
			return null;
		});
		R("元音後的 i/u 改成 j/w", async O => {
			T(布之道拼式Parser.Adapt("tˤui") == "tˤuj", "韻尾 i 應成 j");
			T(布之道拼式Parser.Adapt("dˤiuk") == "dˤiwk", "韻尾 u 應成 w");
			// 主元音 i/u 前面是輔音，不動。
			T(布之道拼式Parser.Adapt("di") == "di", "主元音 i 不該動");
			T(布之道拼式Parser.Adapt("du") == "du", "主元音 u 不該動");
			return null;
		});
		R("聲母段的 w 改成上標 ʷ", async O => {
			T(布之道拼式Parser.Adapt("kwrˤaŋʔ") == "kʷrˤaŋʔ", "聲母圓脣應寫上標");
			// 韻尾的 w 不該被改成上標。
			T(布之道拼式Parser.Adapt("ˤuw") == "ˤuw", "韻尾 w 不該動");
			// 單獨作聲母的 w 也不該動（i>0 才轉）。
			T(布之道拼式Parser.Adapt("wa") == "wa", "單 w 聲母不該動");
			return null;
		});
		R("ɬ 與 ʍ 原樣保留", async O => {
			// A 步不做歸併：它們落哪個鍵是 ToDks 的事。
			T(布之道拼式Parser.Adapt("ɬa") == "ɬa", "ɬ 應原樣保留");
			T(布之道拼式Parser.Adapt("ʍa") == "ʍa", "ʍ 應原樣保留");
			return null;
		});
		R("空串返回空串", async O => {
			T(布之道拼式Parser.Adapt("") == "");
			T(布之道拼式Parser.Adapt("  ") == "");
			return null;
		});
	}
}
