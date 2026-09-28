namespace Dks.Test.Domains.DksCore;

using Dks.Core;
using Tsinswreng.CsTreeTest;

/// 測試 DksKeyboard（三層鍵位表：讀音 → 鍵）。
/// 都是純字典查詢、無副作用，故整個節點設為可遞歸並行。
public partial class TestDksKeyboard:ITester{
	public ITestNode RegisterTestsInto(ITestNode? Node){
		Node ??= new TestNode();
		Node.IsParallelRecursive = true;
		Register首鍵Of聲母(Node);
		Register次鍵Of介腹(Node);
		Register末鍵Of尾調(Node);
		Register鍵集(Node);
		return Node;
	}
}
