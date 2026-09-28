namespace Dks.Test.Domains.DksCore;

using Dks.Core.Svc;
using Tsinswreng.CsTreeTest;

/// 測試 SvcDks.ToDkn（dks → dkn：每碼取首尾字符）。
public partial class TestSvcDks{
	public void RegisterToDkn(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestSvcDks),
			[typeof(ISvcDks), typeof(SvcDks)],
			[nameof(ISvcDks.ToDkn)],
			"ToDkn"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("每碼取首尾成雙碼", async O => {
			// 辣 ryt ⇒ rt；吾 wzj ⇒ wj；,zh ⇒ ,h。
			var dks = DictText.Mk("dks", "辣\tryt\t10000%", "吾\twzj", "挪\t,zh");
			using var reader = new StringReader(dks);
			using var writer = TestText.MkWriter();
			await Svc.ToDkn(Ctx, reader, writer, default);
			var text = writer.ToString();
			T(text.Contains("name: dkn\n"), "dkn 的表頭 name 應為 dkn");
			var 表 = await DictText.Mk字到碼集(text);
			T(表["辣"][0] == "rt", $"辣 應為 rt，實得 [{表["辣"][0]}]");
			T(表["吾"][0] == "wj", $"吾 應為 wj，實得 [{表["吾"][0]}]");
			T(表["挪"][0] == ",h", $"挪 應為 ,h，實得 [{表["挪"][0]}]");
			return null;
		});
		R("權重與空碼行照舊", async O => {
			var dks = DictText.Mk("dks", "𠀀\t", "辣\tryt\t10000%");
			using var reader = new StringReader(dks);
			using var writer = TestText.MkWriter();
			await Svc.ToDkn(Ctx, reader, writer, default);
			var 行 = await DictText.Mk行(writer.ToString());
			T(行.Count == 2, $"應保留兩行，實得 {行.Count}");
			T(行[0].字 == "𠀀" && 行[0].碼 == "", "空碼行原樣留空");
			T(行[1].權重 == "10000%", "權重照舊帶過");
			return null;
		});
	}
}
