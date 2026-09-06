using Tsinswreng.CsTreeTest;
using RimeTools.Test.Domains.Calculator;
namespace RimeTools.Test;

public class RimeToolsTestMgr:DiEtTestMgr{
	public static RimeToolsTestMgr Inst = new();
	public override ITestNode RegisterTestsInto(ITestNode? Node){
		Node = this.TestNode;
		this.RegisterTester<TestCalculator>();
		return Node;
	}
}
