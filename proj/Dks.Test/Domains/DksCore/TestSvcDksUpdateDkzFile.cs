namespace Dks.Test.Domains.DksCore;

using Dks.Core.Svc;
using Tsinswreng.CsTreeTest;

/// 測試 SvcDks.UpdateDkzFile（dkp 覆蓋 dkz：解 dkp 表體、剔 dkp 已收字頭、回傳覆蓋字集）。
public partial class TestSvcDks{
	public void RegisterUpdateDkzFile(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestSvcDks),
			[typeof(ISvcDks), typeof(SvcDks)],
			[nameof(ISvcDks.UpdateDkzFile)],
			"UpdateDkzFile"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("dkp 表體按義符聲符解音、併在 dkz 之前", async O => {
			// 真實 dkp 行：誠	言成 ⇒ 義符 言→ŋ + 聲符 成→eŋ ⇒ ŋeŋ；挪	手ˁaj ⇒ nʰˁaj。
			// 行內註釋（# ts）由解析器截掉。
			var dkp = DictText.Mk("dkp",
				"誠\t言成\t# ts",
				"挪\t手ˁaj",
				"挪\tnˁaj");
			var dkz = DictText.Mk("dkz",
				"誠\tŋeŋ\t10000%",
				"挪\tnʰˁaj",
				"辣\trˁat\t10000%");
			using var r1 = new StringReader(dkp);
			using var r2 = new StringReader(dkz);
			using var w = TestText.MkWriter();
			await Svc.UpdateDkzFile(Ctx, r1, r2, w, default);
			var 行 = await DictText.Mk行(w.ToString());
			// step 1: dkp 表體在前、且同一字的多行都保留（挪 兩個讀音）。
			T(行.Count == 4, $"應為 3 行 dkp + 1 行未被覆蓋的 dkz，實得 {行.Count} 行");
			T(行[0].字 == "誠" && 行[0].碼 == "ŋeŋ", $"誠 應為 ŋeŋ，實得 [{行[0].碼}]");
			T(行[1].字 == "挪" && 行[1].碼 == "nʰˁaj", $"挪 應為 nʰˁaj，實得 [{行[1].碼}]");
			T(行[2].字 == "挪" && 行[2].碼 == "nˁaj", $"挪 的第二讀音應保留，實得 [{行[2].碼}]");
			// step 2: dkz 裏 dkp 已收的字頭（誠、挪）被剔除，未覆蓋的（辣）照原樣在後。
			T(行[3].字 == "辣" && 行[3].碼 == "rˁat", $"辣 應原樣保留在末尾，實得 [{行[3].字}={行[3].碼}]");
			return null;
		});
		R("退回 dkp 覆蓋字集", async O => {
			// 回傳值供擇源步驟判「該字是否 dkp 說了算」，免得調用方再解析一遍 dkp。
			var dkp = DictText.Mk("dkp", "誠\t言成", "挪\t手ˁaj");
			var dkz = DictText.Mk("dkz", "辣\trˁat");
			using var r1 = new StringReader(dkp);
			using var r2 = new StringReader(dkz);
			using var w = TestText.MkWriter();
			var 集 = await Svc.UpdateDkzFile(Ctx, r1, r2, w, default);
			T(集.Contains("誠") && 集.Contains("挪"), "覆蓋字集應含 dkp 全部字頭");
			T(!集.Contains("辣"), "未被 dkp 覆蓋的字不該在集裏");
			T(集.Count == 2, $"覆蓋字集應為 2 個字，實得 {集.Count}");
			return null;
		});
		R("無碼的 dkp 行照樣覆蓋字頭", async O => {
			// dkp 有純 text 行（該字保留但無碼）：它仍算「dkp 已收」，故 dkz 的那一行要被剔掉。
			var dkp = DictText.Mk("dkp", "𠀀");
			var dkz = DictText.Mk("dkz", "𠀀\tsruk", "辣\trˁat");
			using var r1 = new StringReader(dkp);
			using var r2 = new StringReader(dkz);
			using var w = TestText.MkWriter();
			var 集 = await Svc.UpdateDkzFile(Ctx, r1, r2, w, default);
			var 行 = await DictText.Mk行(w.ToString());
			T(集.Contains("𠀀"), "無碼的 dkp 行也算已收字頭");
			T(行.Count == 2, "dkp 那行 + 辣 那行");
			T(行[0].字 == "𠀀" && 行[0].碼 == "", "dkp 無碼行原樣寫出");
			T(行[1].字 == "辣", "dkz 裏被 dkp 收走的字要剔掉");
			return null;
		});
	}
}
