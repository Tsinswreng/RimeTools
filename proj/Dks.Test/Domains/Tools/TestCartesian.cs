namespace Dks.Test.Domains.Tools;

using RimeTools.Tools;
using Tsinswreng.CsTreeTest;

/// 測試 Cartesian.Product(笛卡爾積)。
public partial class TestCartesian:ITester{
	public ITestNode RegisterTestsInto(ITestNode? Node){
		Node ??= new TestNode();
		RegisterProduct(Node);
		return Node;
	}

	public void RegisterProduct(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestCartesian),
			[typeof(Cartesian)],
			[nameof(Cartesian.Product)],
			"Product"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("多組候選窮舉", async O => {
			var r = Cartesian.Product([["a", "b"], ["1"]]);
			T(r.Count == 2);
			T(r[0][0] == "a" && r[0][1] == "1");
			T(r[1][0] == "b" && r[1][1] == "1");
			return null;
		});
		R("空分組結果為空", async O => {
			var r = Cartesian.Product([[], ["1"]]);
			T(r.Count == 0);
			return null;
		});
	}
}