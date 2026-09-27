namespace Dks.Test.Domains.Dict;

using RimeTools.Shared.Dict.Models;
using Tsinswreng.CsTreeTest;

/// 測試 IDictLine 文檔型語義(行 = 列名→值字典)與 DictLineExtn 便捷器。
public partial class TestIDictLine:ITester{
	public ITestNode RegisterTestsInto(ITestNode? Node){
		Node ??= new TestNode();
		RegisterDocSemantics(Node);
		return Node;
	}

	public void RegisterDocSemantics(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestIDictLine),
			[typeof(IDictLine), typeof(DictLine)],
			[nameof(IDictLine.ContainsKey), nameof(DictLineExtn)],
			"DocLine"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("標準三列可經 text/code/weight 訪問", async O => {
			var line = new DictLine{ ["text"]="一個", ["code"]="qkkn", ["weight"]="374279" };
			T(line.text == "一個");
			T(line.code == "qkkn");
			T(line.weight == "374279");
			return null;
		});
		R("缺列 = 無此鍵", async O => {
			var line = new DictLine{ ["text"]="一", ["code"]="qdkm;" };
			T(line.ContainsKey("weight") == false);
			T(line.weight is null);
			T(line.text == "一");
			return null;
		});
		R("essay 兩列無 code", async O => {
			var line = new DictLine{ ["text"]="〇〇", ["weight"]="658" };
			T(line.ContainsKey("code") == false);
			T(line.code == "");
			return null;
		});
	}
}