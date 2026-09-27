namespace Dks.Test.Domains.Dict;

using RimeTools.Shared.Dict.Lookup;
using RimeTools.Shared.Dict.Models;
using Tsinswreng.CsTreeTest;

/// 測試 MemoryCharCodeLookup(把 dict 行收集成 字→多碼 索引)。
public partial class TestMemoryCharCodeLookup:ITester{
	public ITestNode RegisterTestsInto(ITestNode? Node){
		Node ??= new TestNode();
		RegisterFromDoc(Node);
		return Node;
	}

	public void RegisterFromDoc(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestMemoryCharCodeLookup),
			[typeof(MemoryCharCodeLookup), typeof(ICharCodeLookup)],
			[nameof(MemoryCharCodeLookup.FromDocAsync), nameof(MemoryCharCodeLookup.GetCodes)],
			"Lookup"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("同名多行聚合為多碼", async O => {
			var body = new List<DictLine>{
				new(){ ["text"]="辣", ["code"]="rytYl", ["weight"]="10000%" },
				new(){ ["text"]="辣", ["code"]="rytyl", ["weight"]="10000%" },
			};
			var doc = new RimeDictDoc(
				new RimeDictHeader([new HeaderItem("name", "dks_v", null)]),
				TestAsy.ToAsy(body));
			var lookup = await MemoryCharCodeLookup.FromDocAsync(doc, default);
			var codes = lookup.GetCodes("辣");
			T(codes.Count == 2);
			T(codes[0] == "rytYl" && codes[1] == "rytyl");
			return null;
		});
		R("未收錄返回空表", async O => {
			var body = new List<DictLine>{
				new(){ ["text"]="辣", ["code"]="ryt", ["weight"]="10000%" },
			};
			var doc = new RimeDictDoc(
				new RimeDictHeader([new HeaderItem("name", "dks", null)]),
				TestAsy.ToAsy(body));
			var lookup = await MemoryCharCodeLookup.FromDocAsync(doc, default);
			T(lookup.GetCodes("不存在的字").Count == 0);
			return null;
		});
	}
}