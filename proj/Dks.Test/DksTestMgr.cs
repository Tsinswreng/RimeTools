using Tsinswreng.CsTreeTest;

namespace Dks.Test;

/// Dks.Core 測試的管理器：把各測試域註冊進測試樹。
public class DksTestMgr:DiEtTestMgr{
	public static DksTestMgr Inst = new();
	public override ITestNode RegisterTestsInto(ITestNode? Node){
		Node = this.TestNode;
		// 測試域註冊點：Impl 階段在此掛載各 Domains 的測試類。
		//this.RegisterTester<...>();
		return Node;
	}
}