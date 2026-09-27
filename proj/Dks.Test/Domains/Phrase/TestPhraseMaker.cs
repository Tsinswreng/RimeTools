namespace Dks.Test.Domains.Phrase;

using RimeTools.Shared.Dict.Models;
using RimeTools.Shared.Phrase;
using Tsinswreng.CsCtx;
using Tsinswreng.CsTreeTest;

/// 測試 PhraseMaker 造詞主流程(詞頻 × 反查 × HeadTail → 詞條流)。
public partial class TestPhraseMaker:ITester{
	public ITestNode RegisterTestsInto(ITestNode? Node){
		Node ??= new TestNode();
		RegisterMakePhrases(Node);
		return Node;
	}

	public void RegisterMakePhrases(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestPhraseMaker),
			[typeof(PhraseMaker)],
			[nameof(PhraseMaker.MakePhrases)],
			"MakePhrases"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("雙字詞造出首尾簡碼", async O => {
			// step 1: 造詞頻源(一個 374279) + 反查(辣→ryt、一→qdk)。
			var freq = new TestMemFreqSrc{ Items = [new RimeTools.Shared.Freq.WordFreq("辣一", 374279)] };
			var lookup = new TestMemLookup{
				Map = {
					["辣"] = ["ryt"],
					["一"] = ["qdk"],
				}
			};
			// step 2: HeadTail 策略 → 每字首尾: 辣→rt、一→qk → 詞碼 rtqk。
			using IFnCtx ctx = new FnCtx();
			var maker = new PhraseMaker();
			var lines = new List<DictLine>();
			await foreach(var l in maker.MakePhrases(ctx, freq, lookup, new PhraseMkr_HeadEtTail(), default)){
				lines.Add(l);
			}
			T(lines.Count == 1);
			T(lines[0].text == "辣一");
			T(lines[0].code == "rtqk");
			T(lines[0].weight == "374279");
			return null;
		});
		R("多音字窮舉全部組合", async O => {
			// step 1: 辣有兩碼 rytYl/rytyl → 詞「辣一」應產出兩個詞條。
			var freq = new TestMemFreqSrc{ Items = [new RimeTools.Shared.Freq.WordFreq("辣一", 100)] };
			var lookup = new TestMemLookup{
				Map = {
					["辣"] = ["rytYl", "rytyl"],
					["一"] = ["qdk"],
				}
			};
			using IFnCtx ctx = new FnCtx();
			var maker = new PhraseMaker();
			var lines = new List<DictLine>();
			await foreach(var l in maker.MakePhrases(ctx, freq, lookup, new PhraseMkr_HeadEtTail(), default)){
				lines.Add(l);
			}
			T(lines.Count == 2);
			T(lines[0].code == "rlqk" && lines[1].code == "rlqk");
			return null;
		});
		R("查無碼的字 → 該詞跳過", async O => {
			var freq = new TestMemFreqSrc{ Items = [new RimeTools.Shared.Freq.WordFreq("辣?", 100)] };
			var lookup = new TestMemLookup{
				Map = {
					["辣"] = ["ryt"],
					// "?" 無碼
				}
			};
			using IFnCtx ctx = new FnCtx();
			var maker = new PhraseMaker();
			var count = 0;
			await foreach(var _ in maker.MakePhrases(ctx, freq, lookup, new PhraseMkr_HeadEtTail(), default)){
				count++;
			}
			T(count == 0);
			return null;
		});
	}
}