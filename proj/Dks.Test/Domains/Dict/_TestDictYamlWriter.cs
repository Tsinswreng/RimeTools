namespace Dks.Test.Domains.Dict;

using RimeTools.Shared.Dict.Models;
using RimeTools.Shared.Dict.Parser;
using Tsinswreng.CsTreeTest;

/// 測試 DictYamlWriter（寫出的文本形態：表頭、標量引號、正文列序）。
public partial class TestDictYamlWriter:ITester{
	IDictYamlWriter Writer = new DictYamlWriter();

	public ITestNode RegisterTestsInto(ITestNode? Node){
		Node ??= new TestNode();
		Register寫表頭(Node);
		Register寫正文(Node);
		return Node;
	}
}
