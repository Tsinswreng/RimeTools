namespace Dks.Test.Domains.Freq;

using RimeTools.Shared.Freq;
using Tsinswreng.CsTreeTest;

/// 測試 EssayWordFreqSource(讀 essay 文件 → 頻率降序枚舉)。
public partial class TestEssayWordFreqSource:ITester{
	public ITestNode RegisterTestsInto(ITestNode? Node){
		Node ??= new TestNode();
		RegisterEnumerate(Node);
		return Node;
	}

	public void RegisterEnumerate(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestEssayWordFreqSource),
			[typeof(EssayWordFreqSource), typeof(IWordFreqSource)],
			[nameof(EssayWordFreqSource.Enumerate)],
			"Enumerate"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("讀文件按頻率降序", async O => {
			var tmpPath = System.IO.Path.GetTempFileName();
			try{
				// step 1: 造 essay.txt 樣本(故意亂序)。
				await System.IO.File.WriteAllTextAsync(tmpPath, "〇\t981\n一半\t53913\n〇〇\t658\n");
				var src = new EssayWordFreqSource(tmpPath);
				var list = new List<WordFreq>();
				await foreach(var wf in src.Enumerate(default)){
					list.Add(wf);
				}
				// step 2: 驗證降序 + 內容。
				T(list.Count == 3);
				T(list[0].Text == "一半" && list[0].Freq == 53913);
				T(list[1].Text == "〇" && list[1].Freq == 981);
				T(list[2].Text == "〇〇" && list[2].Freq == 658);
			}
			finally{
				System.IO.File.Delete(tmpPath);
			}
			return null;
		});
		R("跳過空行與無 tab 髒行", async O => {
			var tmpPath = System.IO.Path.GetTempFileName();
			try{
				await System.IO.File.WriteAllTextAsync(tmpPath, "\n〇\t981\n\n\n髒行沒有tab\n〇〇\t658\n");
				var src = new EssayWordFreqSource(tmpPath);
				var count = 0;
				await foreach(var _ in src.Enumerate(default)){
					count++;
				}
				T(count == 2);
			}
			finally{
				System.IO.File.Delete(tmpPath);
			}
			return null;
		});
	}
}