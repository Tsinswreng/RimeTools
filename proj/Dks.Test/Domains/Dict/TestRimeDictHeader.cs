namespace Dks.Test.Domains.Dict;

using RimeTools.Shared.Dict.Models;
using Tsinswreng.CsTreeTest;

/// 測試 RimeDictHeader 文檔型表頭(保序鍵值、GetScalar/GetList、便捷訪問器)。
public partial class TestRimeDictHeader:ITester{
	public ITestNode RegisterTestsInto(ITestNode? Node){
		Node ??= new TestNode();
		RegisterDocHeader(Node);
		return Node;
	}

	public void RegisterDocHeader(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestRimeDictHeader),
			[typeof(RimeDictHeader), typeof(HeaderItem)],
			[nameof(RimeDictHeader.GetScalar), nameof(RimeDictHeader.GetList), nameof(RimeDictHeader.ImportTables)],
			"Header"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("標量與列表混合解析", async O => {
			var header = new RimeDictHeader([
				new HeaderItem("name", "dks", null),
				new HeaderItem("version", "", null),
				new HeaderItem("import_tables", null, ["nonKanji", "chineseDict"]),
			]);
			T(header.GetScalar("name") == "dks");
			T(header.Name == "dks");
			T(header.Version == "");
			T(header.ImportTables!.Count == 2 && header.ImportTables[1] == "chineseDict");
			return null;
		});
		R("未知鍵原樣保留", async O => {
			var header = new RimeDictHeader([
				new HeaderItem("custom_key", "v", null),
			]);
			T(header.Items.Count == 1);
			T(header.GetScalar("custom_key") == "v");
			return null;
		});
		R("型別不符返回缺省", async O => {
			var header = new RimeDictHeader([
				new HeaderItem("import_tables", null, ["x"]),
			]);
			T(header.GetScalar("import_tables") is null);
			return null;
		});
	}
}