namespace Dks.Test.Domains.DksCore;

using Dks.Core.Svc;
using Tsinswreng.CsTreeTest;

/// 測試 SvcDks.AttachCangjie（dks 左聯倉頡輔助碼 → dks_v）。
public partial class TestSvcDks{
	public void RegisterAttachCangjie(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestSvcDks),
			[typeof(ISvcDks), typeof(SvcDks)],
			[nameof(ISvcDks.AttachCangjie)],
			"AttachCangjie"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("主碼後接倉頡整理後的碼", async O => {
			// 真實倉頡行：辣	辛dl ⇒ 辛→Y、再取首尾（^(.)(.)(.)$→$1$3）⇒ Yl ⇒ dks_v 碼 rytYl。
			// 另一行 辣	yldl ⇒ 四碼取首尾 ⇒ yl ⇒ rytyl。
			var dks = DictText.Mk("dks", "辣\tryt\t10000%");
			var cj = DictText.Mk("cangjie", "辣\t辛dl", "辣\tyldl\t2401");
			using var r1 = new StringReader(dks);
			using var r2 = new StringReader(cj);
			using var w = TestText.MkWriter();
			await Svc.AttachCangjie(Ctx, r1, r2, w, default);
			var text = w.ToString();
			T(text.Contains("name: dks_v\n"), "dks_v 的表頭 name 應為 dks_v");
			var 行 = await DictText.Mk行(text);
			T(行.Count == 2, $"倉頡兩行都該 join 進來，實得 {行.Count} 行");
			T(行[0].碼 == "rytYl", $"辣 第一碼應為 rytYl，實得 [{行[0].碼}]");
			T(行[1].碼 == "rytyl", $"辣 第二碼應為 rytyl，實得 [{行[1].碼}]");
			return null;
		});
		R("權重沿用 dks 行而非倉頡行", async O => {
			// 倉頡行的權重（2401）不參與：dks_v 的權重是主表該字的權重。
			var dks = DictText.Mk("dks", "辣\tryt\t10000%");
			var cj = DictText.Mk("cangjie", "辣\tyldl\t2401");
			using var r1 = new StringReader(dks);
			using var r2 = new StringReader(cj);
			using var w = TestText.MkWriter();
			await Svc.AttachCangjie(Ctx, r1, r2, w, default);
			var 行 = await DictText.Mk行(w.ToString());
			T(行[0].權重 == "10000%", $"權重應沿用 dks 行，實得 [{行[0].權重}]");
			return null;
		});
		R("倉頡無此字則只留主碼", async O => {
			var dks = DictText.Mk("dks", "𠀀\tabc\t5", "辣\tryt");
			var cj = DictText.Mk("cangjie", "辣\tyldl");
			using var r1 = new StringReader(dks);
			using var r2 = new StringReader(cj);
			using var w = TestText.MkWriter();
			await Svc.AttachCangjie(Ctx, r1, r2, w, default);
			var 行 = await DictText.Mk行(w.ToString());
			T(行.Count == 2, $"每字一行，實得 {行.Count}");
			T(行[0].字 == "𠀀" && 行[0].碼 == "abc", $"無倉頡碼者保留主碼，實得 [{行[0].碼}]");
			T(行[0].權重 == "5", "權重照舊帶過");
			return null;
		});
		R("倉頡空碼行等於無輔助碼", async O => {
			// 倉頡表裏的空碼行（有字無碼）join 後不該多出碼來。
			var dks = DictText.Mk("dks", "辣\tryt");
			var cj = DictText.Mk("cangjie", "辣\t");
			using var r1 = new StringReader(dks);
			using var r2 = new StringReader(cj);
			using var w = TestText.MkWriter();
			await Svc.AttachCangjie(Ctx, r1, r2, w, default);
			var 行 = await DictText.Mk行(w.ToString());
			T(行.Count == 1, $"應只一行，實得 {行.Count}");
			T(行[0].碼 == "ryt", $"空倉頡碼 ⇒ 只留主碼，實得 [{行[0].碼}]");
			return null;
		});
	}
}
