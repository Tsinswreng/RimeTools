namespace Dks.Test.Domains.Dict;

using RimeTools.Shared.Dict.Parser;
using Tsinswreng.CsTreeTest;

/// 測試 DictYamlParser（表頭形狀、正文列映射、髒行的處理）。
public partial class TestDictYamlParser:ITester{
	IDictYamlParser Parser = new DictYamlParser();

	public ITestNode RegisterTestsInto(ITestNode? Node){
		Node ??= new TestNode();
		RegisterReadHeader(Node);
		RegisterBodyLines(Node);
		return Node;
	}

	/// 直接讀 TextReader（新接口），不碰文件系統。
	internal Task<RimeTools.Shared.Dict.Models.RimeDictDoc> Parse(str Text){
		return Parser.Parse(new StringReader(Text), default);
	}
}
