namespace Dks.Test.Domains.DksCore;

using Dks.Core.Svc;
using Tsinswreng.CsTreeTest;

/// 測試 SvcDks.回退缺音（Dks3：新側缺音的字改用舊側讀音，其餘保留新側）。
public partial class TestSvcDks{
	public void Register回退缺音(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestSvcDks),
			[typeof(ISvcDks), typeof(SvcDks)],
			[nameof(ISvcDks.回退缺音)],
			"回退缺音"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("新側沒有的字用舊側補上", async O => {
			// 新側（布之道）沒有 買、所；舊側（中古倒推）有 ⇒ 補在末尾，其餘保持新側原序。
			var 新 = DictText.Mk("dks", "辣\tryt\t10000%");
			var 舊 = DictText.Mk("dks", "買\tmpu", "辣\tryt", "所\tsqu");
			using var r1 = new StringReader(新);
			using var r2 = new StringReader(舊);
			using var w = TestText.MkWriter();
			await Svc.回退缺音(Ctx, r1, r2, w, default);
			var text = w.ToString();
			T(text.Contains("name: dks\n"), "表頭仍應是 dks");
			T(text.Contains("- nonKanji\n"), "dks 應帶 import_tables");
			var 行 = await DictText.Mk行(text);
			T(行.Count == 3, $"新側 1 行 + 回退 2 行 = 3 行，實得 {行.Count}");
			// 新側原序在前。
			T(行[0].字 == "辣" && 行[0].碼 == "ryt", "新側的行原序保留在前");
			// 回退塊在後：買、所（舊組按舊表行序）。
			T(行[1].字 == "買" && 行[1].碼 == "mpu", $"買 應回退成 mpu，實得 [{行[1].字}={行[1].碼}]");
			T(行[2].字 == "所" && 行[2].碼 == "squ", $"所 應回退成 squ，實得 [{行[2].字}={行[2].碼}]");
			return null;
		});
		R("新側有音的字的舊讀音不參與", async O => {
			// 辣 新舊都有 ⇒ 只留新側那一行（回退是補缺，不是換源）。
			var 新 = DictText.Mk("dks", "辣\tpuj");
			var 舊 = DictText.Mk("dks", "辣\tryt");
			using var r1 = new StringReader(新);
			using var r2 = new StringReader(舊);
			using var w = TestText.MkWriter();
			await Svc.回退缺音(Ctx, r1, r2, w, default);
			var 行 = await DictText.Mk行(w.ToString());
			T(行.Count == 1, $"應只留新側一行，實得 {行.Count}");
			T(行[0].碼 == "puj", $"應保留新側讀音，實得 [{行[0].碼}]");
			return null;
		});
		R("舊側空碼的行不算回退", async O => {
			// 回退是為了補讀音，故舊側該字若本來無碼，就不該把空碼行搬過來（否則產出驗證會攔下）。
			var 新 = DictText.Mk("dks", "辣\tryt");
			var 舊 = DictText.Mk("dks", "𠀀\t", "所\tsqu");
			using var r1 = new StringReader(新);
			using var r2 = new StringReader(舊);
			using var w = TestText.MkWriter();
			await Svc.回退缺音(Ctx, r1, r2, w, default);
			var 表 = await DictText.Mk字到碼集(w.ToString());
			T(!表.ContainsKey("𠀀"), "舊側空碼的行不該回退");
			T(表["所"][0] == "squ", "舊側有碼的照樣回退");
			return null;
		});
		R("同一字多個回退讀音都保留", async O => {
			var 新 = DictText.Mk("dks", "辣\tryt");
			var 舊 = DictText.Mk("dks", "買\tmpu", "買\tnzh");
			using var r1 = new StringReader(新);
			using var r2 = new StringReader(舊);
			using var w = TestText.MkWriter();
			await Svc.回退缺音(Ctx, r1, r2, w, default);
			var 表 = await DictText.Mk字到碼集(w.ToString());
			T(表["買"].Count == 2, $"買 的兩個舊讀音都該回退，實得 {表["買"].Count}");
			T(表["買"][0] == "mpu" && 表["買"][1] == "nzh", "回退行保舊表原序");
			return null;
		});
	}
}
