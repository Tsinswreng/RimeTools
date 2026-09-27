namespace Dks.Test.Domains.Tools;

using RimeTools.Tools;
using Tsinswreng.CsTreeTest;

/// 測試 TextUtil.SplitByRune(按 Unicode 碼點拆字)。
public partial class TestTextUtil:ITester{
	public ITestNode RegisterTestsInto(ITestNode? Node){
		Node ??= new TestNode();
		RegisterSplitByRune(Node);
		return Node;
	}

	public void RegisterSplitByRune(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestTextUtil),
			[typeof(TextUtil)],
			[nameof(TextUtil.SplitByRune)],
			"SplitByRune"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("拆中文詞成字", async O => {
			var r = TextUtil.SplitByRune("一個");
			T(r.Count == 2 && r[0] == "一" && r[1] == "個");
			return null;
		});
		R("不拆代理對", async O => {
			var r = TextUtil.SplitByRune("😀");
			T(r.Count == 1 && r[0] == "😀");
			return null;
		});
		R("空串返回空表", async O => {
			T(TextUtil.SplitByRune("").Count == 0);
			return null;
		});
	}
}