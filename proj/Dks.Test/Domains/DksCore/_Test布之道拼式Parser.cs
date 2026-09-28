namespace Dks.Test.Domains.DksCore;

using Dks.Core;
using Tsinswreng.CsTreeTest;

/// 測試 布之道拼式Parser（布之道 IPA → 本方案音節的記法適配與切分）。
/// 純函數，無副作用，故整個節點可並行。
public partial class Test布之道拼式Parser:ITester{
	public ITestNode RegisterTestsInto(ITestNode? Node){
		Node ??= new TestNode();
		Node.IsParallelRecursive = true;
		RegisterAdapt(Node);
		RegisterParse(Node);
		return Node;
	}
}
