namespace Dks.Test.Domains.DksCore;

using Dks.Core;
using Dks.Core.Svc;
using Tsinswreng.CsTreeTest;

/// 測試 SvcDks.Mk上古漢語音節FromDkp（dkp 表拼式 → 上古漢語音節）。
/// 用例取真實 dkp.dict.yaml 的拼式，期望值對得上倉庫現有的 dks 表。
public partial class TestSvcDks{
	public void RegisterMk上古漢語音節FromDkp(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestSvcDks),
			[typeof(ISvcDks), typeof(SvcDks)],
			[nameof(ISvcDks.Mk上古漢語音節FromDkp)],
			"Mk音節FromDkp"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("純音拼式切出聲母介腹尾調", async O => {
			// 辣	rˁat（真實 dkp 行）⇒ 聲母 r、介腹 'a、尾調 t（非三等標記 ˁ 歸一成 '）。
			var z = Svc.Mk上古漢語音節FromDkp("rˁat");
			T(z.聲母 == "r", $"聲母應為 r，實得 [{z.聲母}]");
			T(z.介腹 == "'a", $"介腹應為 'a，實得 [{z.介腹}]");
			T(z.尾調 == "t", $"尾調應為 t，實得 [{z.尾調}]");
			T(z.Full == "r'at", $"Full 應為 r'at，實得 [{z.Full}]");
			return null;
		});
		R("義符字形查聲母、聲符字形查韻", async O => {
			// 誠	言成（真實 dkp 行）⇒ 義符 言→ŋ、聲符 成→eŋ ⇒ 聲母 ŋ、介腹 e、尾調 ŋ。
			var z = Svc.Mk上古漢語音節FromDkp("言成");
			T(z.聲母 == "ŋ", $"聲母應為 ŋ（言），實得 [{z.聲母}]");
			T(z.介腹 == "e", $"介腹應為 e（成），實得 [{z.介腹}]");
			T(z.尾調 == "ŋ", $"尾調應為 ŋ（成），實得 [{z.尾調}]");
			T(z.Full == "ŋeŋ", $"Full 應為 ŋeŋ，實得 [{z.Full}]");
			return null;
		});
		R("義符之後照樣切韻", async O => {
			// 挪	手ˁaj（真實 dkp 行）⇒ 義符 手→nʰ、其後 ˁaj 切出介腹 'a、尾調 j。
			var z = Svc.Mk上古漢語音節FromDkp("手ˁaj");
			T(z.聲母 == "nʰ", $"聲母應為 nʰ（手），實得 [{z.聲母}]");
			T(z.介腹 == "'a", $"介腹應為 'a，實得 [{z.介腹}]");
			T(z.尾調 == "j", $"尾調應為 j，實得 [{z.尾調}]");
			return null;
		});
		R("無義符的複聲母取最長前綴", async O => {
			// 礦	kʷrˁaŋʔ ⇒ 最長可行前綴是 kʷ（不是 k，否則 rˁa 切不出介腹）⇒ 聲母 kʷ、介腹 rˁa→r'a、尾調 ŋʔ。
			var z = Svc.Mk上古漢語音節FromDkp("kʷrˁaŋʔ");
			T(z.聲母 == "kʷ", $"聲母應為 kʷ，實得 [{z.聲母}]");
			T(z.介腹 == "r'a", $"介腹應為 r'a，實得 [{z.介腹}]");
			T(z.尾調 == "ŋʔ", $"尾調應為 ŋʔ，實得 [{z.尾調}]");
			return null;
		});
		R("開音節尾調為空", async O => {
			// 吾	ŋˁa ⇒ 尾調空（開音節，DksKeyboard 落 J）。
			var z = Svc.Mk上古漢語音節FromDkp("ŋˁa");
			T(z.聲母 == "ŋ" && z.介腹 == "'a" && z.尾調 == "");
			T(z.Full == "ŋ'a", $"Full 應為 ŋ'a，實得 [{z.Full}]");
			return null;
		});
		R("空拼式返回空音節且不拋異常", async O => {
			// dkp 有純 text 行（無碼），故解析必須容忍空串。
			var z = Svc.Mk上古漢語音節FromDkp("");
			T(z.Full == "", $"空拼式的 Full 應為空串，實得 [{z.Full}]");
			T(z.聲母 == "" && z.介腹 == "" && z.尾調 == "");
			return null;
		});
	}
}
