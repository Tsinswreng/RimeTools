namespace Dks.Test.Domains.DksCore;

using Dks.Core;
using Dks.Core.Svc;
using Tsinswreng.CsTreeTest;

/// 測試 SvcDks.ToDks（音節 → 三拼碼；查不到鍵返回 null）。
public partial class TestSvcDks{
	/// 造一個音節（只填三段，Full 不重要）。
	private static Tswg上古漢語音節 Mk音(str 聲母, str 介腹, str 尾調){
		return new Tswg上古漢語音節{
			聲母 = 聲母,
			介腹 = 介腹,
			尾調 = 尾調,
			Full = 聲母 + 介腹 + 尾調,
		};
	}

	public void RegisterToDks(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestSvcDks),
			[typeof(ISvcDks), typeof(SvcDks)],
			[nameof(ISvcDks.ToDks)],
			"ToDks"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("三段各查一鍵並轉小寫", async O => {
			// 礦 kʷr'aŋʔ ⇒ 首 I、次 Y、末 W ⇒ iyw。
			T(Svc.ToDks(Mk音("kʷ", "r'a", "ŋʔ")) == "iyw", "礦 應為 iyw");
			// 辣 r'at ⇒ 首 r→R、次（帶 r 那一形）r'a→Y、末 t→T ⇒ ryt。
			T(Svc.ToDks(Mk音("r", "'a", "t")) == "ryt", "辣 應為 ryt");
			// 吾 ŋ'a ⇒ 首 ŋ→W、次 'a→Z、末（空）→J ⇒ wzj。
			T(Svc.ToDks(Mk音("ŋ", "'a", "")) == "wzj", "吾 應為 wzj");
			// 挪 nʰ'aj ⇒ 首 nʰ→,、次 'a→Z、末 j→H ⇒ ,zh。
			T(Svc.ToDks(Mk音("nʰ", "'a", "j")) == ",zh", "挪 應為 ,zh");
			return null;
		});
		R("聲母 r 的介腹取帶 r 那一形", async O => {
			// 完整拼式裏不存在 jr，故 R 鍵的兩讀音（r/j）借用介腹有沒有 r 區分：
			// 聲母 r ⇒ 介腹前面補 r；聲母 j ⇒ 用原形。
			T(Svc.ToDks(Mk音("r", "a", "")) == "rqj", "r + a 應查 ra（Q 鍵）");
			T(Svc.ToDks(Mk音("j", "a", "")) == "raj", "j + a 應查 a（A 鍵）");
			T(Svc.ToDks(Mk音("r", "'a", "t")) == "ryt", "r + 'a 應查 r'a（Y 鍵）");
			T(Svc.ToDks(Mk音("j", "'a", "t")) == "rzt", "j + 'a 應查 'a（Z 鍵）");
			return null;
		});
		R("聲母 ml／sl 的 J 鍵同法區分", async O => {
			// J 鍵收船母 ml 與邪母 sl；同樣靠介腹有沒有 r 區分：ml ⇒ 補 r，sl ⇒ 用原形。
			T(Svc.ToDks(Mk音("ml", "i", "ŋ")) == "jes", "ml + i 應查 ri（E 鍵）");
			T(Svc.ToDks(Mk音("sl", "a", "n")) == "jad", "sl + a 應查 a（A 鍵）");
			T(Svc.ToDks(Mk音("z", "a", "n")) == "jad", "z（邪）與 sl 同鍵");
			return null;
		});
		R("查不到鍵返回 null（不是空串）", async O => {
			// 空串在表裏另有含義（該行本無讀音），故查不到鍵必須用 null 區分。
			T(Svc.ToDks(Mk音("", "", "")) is null, "空聲母查不到鍵 ⇒ null");
			T(Svc.ToDks(Mk音("qq", "a", "")) is null, "未知聲母 ⇒ null");
			T(Svc.ToDks(Mk音("p", "zz", "")) is null, "未知介腹 ⇒ null");
			T(Svc.ToDks(Mk音("p", "a", "xx")) is null, "未知尾調 ⇒ null");
			return null;
		});
	}
}
