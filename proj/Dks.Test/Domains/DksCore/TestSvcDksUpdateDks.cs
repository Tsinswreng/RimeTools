namespace Dks.Test.Domains.DksCore;

using Dks.Core.Svc;
using Tsinswreng.CsTreeTest;

/// 測試 SvcDks.UpdateDks（dkz → dks：走 Rules/ocToOc3.txt 的音變規則、碼轉小寫）。
public partial class TestSvcDks{
	public void RegisterUpdateDks(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestSvcDks),
			[typeof(ISvcDks), typeof(SvcDks)],
			[nameof(ISvcDks.UpdateDks)],
			"UpdateDks"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("全拼轉成三拼鍵位碼", async O => {
			// 真實舊流程 dkz 行：辣 rˁat ⇒ ryt；縮 sruk ⇒ stk。
			var dkz = DictText.Mk("dkz", "辣\trˁat\t10000%", "縮\tsruk");
			using var reader = new StringReader(dkz);
			using var writer = TestText.MkWriter();
			await Svc.UpdateDks(Ctx, reader, writer, default);
			var 表 = await DictText.Mk字到碼集(writer.ToString());
			T(表["辣"][0] == "ryt", $"辣 應為 ryt，實得 [{表["辣"][0]}]");
			T(表["縮"][0] == "stk", $"縮 應為 stk，實得 [{表["縮"][0]}]");
			return null;
		});
		R("表頭帶 import_tables 且 name 為 dks", async O => {
			// dks 依賴 nonKanji/chineseDict 兩張表，表頭錯了 Rime 端就查不全。
			var dkz = DictText.Mk("dkz", "辣\trˁat");
			using var reader = new StringReader(dkz);
			using var writer = TestText.MkWriter();
			await Svc.UpdateDks(Ctx, reader, writer, default);
			var text = writer.ToString();
			T(text.Contains("name: dks\n"), "name 應為 dks");
			T(text.Contains("- nonKanji\n") && text.Contains("- chineseDict\n"), "應帶 import_tables");
			return null;
		});
		R("空碼行原樣留空、權重帶過", async O => {
			// dkz 的空碼行表示該字保留但無讀音；此步不該給它編出碼來。
			var dkz = DictText.Mk("dkz", "𠀀\t", "辣\trˁat\t10000%");
			using var reader = new StringReader(dkz);
			using var writer = TestText.MkWriter();
			await Svc.UpdateDks(Ctx, reader, writer, default);
			var 行 = await DictText.Mk行(writer.ToString());
			T(行.Count == 2, $"應保留兩行，實得 {行.Count}");
			T(行[0].字 == "𠀀" && 行[0].碼 == "", "空碼行應原樣留空");
			T(行[1].權重 == "10000%", $"權重應原樣帶過，實得 [{行[1].權重}]");
			return null;
		});
	}
}
