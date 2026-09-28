namespace Dks.Test.Domains.DksCore;

using Dks.Core;
using Dks.Core.Svc;
using RimeTools.Shared.Freq;
using Tsinswreng.CsTreeTest;

/// 測試 SvcDks.MkDksPhrase（dks 反查 + 詞頻 → dks_phrase 詞條流）。
public partial class TestSvcDks{
	public void RegisterMkDksPhrase(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestSvcDks),
			[typeof(ISvcDks), typeof(SvcDks)],
			[nameof(ISvcDks.MkDksPhrase)],
			"MkDksPhrase"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("以 dks 為反查造出簡碼詞", async O => {
			// 反查來自讀入的 dks 表：辣→ryt、一→qdk；詞頻 辣一(374279)。
			// HeadTail 策略每字取首尾 ⇒ rt + qk ⇒ 詞碼 rtqk。
			var svc = new SvcDks(new DksCfg{
				WordFreq = new MemWordFreq(new WordFreq("辣一", 374279)),
			});
			var dks = DictText.Mk("dks", "辣\tryt", "一\tqdk");
			using var reader = new StringReader(dks);
			using var writer = TestText.MkWriter();
			await svc.MkDksPhrase(Ctx, reader, writer, default);
			var text = writer.ToString();
			// step 1: 表頭要是 dks_phrase + columns 三列（寫不出 columns 就讀不回三格）。
			T(text.Contains("name: dks_phrase\n"), "表頭 name 應為 dks_phrase");
			T(text.Contains("  - text\n  - code\n  - weight\n"), "columns 應為 text/code/weight");
			// step 2: 正文一行，碼與權重都對。
			var 行 = await DictText.Mk行(text);
			T(行.Count == 1, $"應造出一個詞條，實得 {行.Count}");
			T(行[0].字 == "辣一", $"詞應為 辣一，實得 [{行[0].字}]");
			T(行[0].碼 == "rtqk", $"詞碼應為 rtqk，實得 [{行[0].碼}]");
			T(行[0].權重 == "374279", $"權重應為詞頻，實得 [{行[0].權重}]");
			return null;
		});
		R("dks 表沒有的字 ⇒ 該詞跳過", async O => {
			// 反查缺字時整詞跳過（造出殘詞沒有意義）。
			var svc = new SvcDks(new DksCfg{
				WordFreq = new MemWordFreq(new WordFreq("辣𠀀", 100)),
			});
			var dks = DictText.Mk("dks", "辣\tryt");
			using var reader = new StringReader(dks);
			using var writer = TestText.MkWriter();
			await svc.MkDksPhrase(Ctx, reader, writer, default);
			var 行 = await DictText.Mk行(writer.ToString());
			T(行.Count == 0, $"查無碼的詞應跳過，實得 {行.Count} 行");
			return null;
		});
	}
}
