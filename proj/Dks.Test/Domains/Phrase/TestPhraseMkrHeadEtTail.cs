namespace Dks.Test.Domains.Phrase;

using RimeTools.Shared.Dict.Lookup;
using RimeTools.Shared.Dict.Models;
using RimeTools.Shared.Freq;
using RimeTools.Shared.Phrase;
using Tsinswreng.CsTreeTest;

/// 測試 PhraseMkr_HeadEtTail(造詞策略:每字碼取首尾)。
public partial class TestPhraseMkrHeadEtTail:ITester{
	public ITestNode RegisterTestsInto(ITestNode? Node){
		Node ??= new TestNode();
		RegisterMkPhrase(Node);
		return Node;
	}

	public void RegisterMkPhrase(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestPhraseMkrHeadEtTail),
			[typeof(PhraseMkr_HeadEtTail), typeof(IPhraseMkr)],
			[nameof(PhraseMkr_HeadEtTail.MkPhrase)],
			"MkPhrase"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("雙字詞每字取首尾", async O => {
			var mkr = new PhraseMkr_HeadEtTail();
			var r = mkr.MkPhrase(["ryt", "qdk"], ["辣", "一"]);
			T(r.Count == 2);
			T(r[0] == "rt" && r[1] == "qk");
			return null;
		});
		R("單字詞不成詞返回空表", async O => {
			var mkr = new PhraseMkr_HeadEtTail();
			var r = mkr.MkPhrase(["ryt"], ["辣"]);
			T(r.Count == 0);
			return null;
		});
		R("空碼字跳過", async O => {
			var mkr = new PhraseMkr_HeadEtTail();
			var r = mkr.MkPhrase(["ryt", ""], ["辣", ""]);
			T(r.Count == 1 && r[0] == "rt");
			return null;
		});
	}
}

/// 造詞測試用的內存詞頻源：預置若干 WordFreq，構造後直接枚舉（已降序由調用方保證）。
public class TestMemFreqSrc:IWordFreqSource{
	public List<WordFreq> Items = [];
	public IAsyncEnumerable<WordFreq> Enumerate(CT Ct)
		=> TestAsy.ToAsy(Items);
}

/// 造詞測試用的內存反查：字 → 碼。
public class TestMemLookup:ICharCodeLookup{
	public Dictionary<str, List<str>> Map = [];
	public IReadOnlyList<str> GetCodes(str Text)
		=> Map.TryGetValue(Text, out var l) ? l : [];
}