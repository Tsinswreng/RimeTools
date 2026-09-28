namespace Dks.Test.Domains.DksCore;

using Dks.Core.Svc;
using Tsinswreng.CsTreeTest;

/// 測試 SvcDks.SaffesToDkz（中古三拼鍵位碼 → 上古全拼 dkz；走 Rules/saffesToOcRegex.txt）。
public partial class TestSvcDks{
	public void RegisterSaffesToDkz(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestSvcDks),
			[typeof(ISvcDks), typeof(SvcDks)],
			[nameof(ISvcDks.SaffesToDkz)],
			"SaffesToDkz"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("三拼鍵位碼倒推成上古全拼", async O => {
			// 真實 saffes 行：縮	smk ⇒ dkz 的 sruk（該字不在 dkp 裏，故這一轉換由 saffes 規則決定）。
			var 源 = DictText.Mk("saffes", "縮\tsmk");
			using var reader = new StringReader(源);
			using var writer = TestText.MkWriter();
			await Svc.SaffesToDkz(Ctx, reader, writer, default);
			var 表 = await DictText.Mk字到碼集(writer.ToString());
			T(表["縮"][0] == "sruk", $"縮 應為 sruk，實得 [{表["縮"][0]}]");
			return null;
		});
		R("表頭為 dkz、權重原樣帶過", async O => {
			var 源 = DictText.Mk("saffes", "縮\tsmk\t10000%");
			using var reader = new StringReader(源);
			using var writer = TestText.MkWriter();
			await Svc.SaffesToDkz(Ctx, reader, writer, default);
			var text = writer.ToString();
			T(text.Contains("name: dkz\n"), "dkz 中介表的 name 應為 dkz");
			var 行 = await DictText.Mk行(text);
			T(行[0].權重 == "10000%", $"權重應原樣帶過，實得 [{行[0].權重}]");
			return null;
		});
		R("只收單字行、空碼行丟掉", async O => {
			var 源 = DictText.Mk("saffes",
				"縮縮\tsmk",
				"𠀀\t",
				"縮\tsmk");
			using var reader = new StringReader(源);
			using var writer = TestText.MkWriter();
			await Svc.SaffesToDkz(Ctx, reader, writer, default);
			var 行 = await DictText.Mk行(writer.ToString());
			T(行.Count == 1, $"應只留單字有碼的一行，實得 {行.Count} 行");
			T(行[0].字 == "縮");
			return null;
		});
	}
}
