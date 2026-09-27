namespace Dks.Test.Domains.Tools;

using RimeTools.Tools;
using Tsinswreng.CsTreeTest;

/// 測試 CodeTransformer.HeadTail(取編碼首尾碼)。
public partial class TestCodeTransformer:ITester{
	public ITestNode RegisterTestsInto(ITestNode? Node){
		Node ??= new TestNode();
		RegisterHeadTail(Node);
		return Node;
	}

	public void RegisterHeadTail(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestCodeTransformer),
			[typeof(CodeTransformer)],
			[nameof(CodeTransformer.HeadTail)],
			"HeadTail"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("多字碼取首尾", async O => {
			T(CodeTransformer.HeadTail("ryt") == "rt");
			return null;
		});
		R("單字碼重複自身", async O => {
			T(CodeTransformer.HeadTail("a") == "aa");
			return null;
		});
		R("空碼返回空", async O => {
			T(CodeTransformer.HeadTail("") == "");
			return null;
		});
	}
}