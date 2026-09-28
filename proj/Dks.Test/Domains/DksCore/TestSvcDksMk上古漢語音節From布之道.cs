namespace Dks.Test.Domains.DksCore;

using Dks.Core.Svc;
using Tsinswreng.CsTreeTest;

/// 測試 SvcDks.Mk上古漢語音節From布之道（布之道 IPA 段 → 上古漢語音節）。
/// 用例取真實 OC_msoegDK.dict.yaml 的 IPA 段，期望值與現有 dks 表對得上。
public partial class TestSvcDks{
	public void RegisterMk上古漢語音節From布之道(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestSvcDks),
			[typeof(ISvcDks), typeof(SvcDks)],
			[nameof(ISvcDks.Mk上古漢語音節From布之道)],
			"Mk音節From布之道"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("圓脣化寫成上標 ʷ", async O => {
			// 礦: kwrˤaŋʔ ⇒ kʷr'aŋʔ（w 緊跟聲母輔音）⇒ 聲母 kʷ、介腹 r'a、尾調 ŋʔ。
			var z = Svc.Mk上古漢語音節From布之道("kwrˤaŋʔ");
			T(z.聲母 == "kʷ", $"聲母應為 kʷ，實得 [{z.聲母}]");
			T(z.介腹 == "r'a", $"介腹應為 r'a，實得 [{z.介腹}]");
			T(z.尾調 == "ŋʔ", $"尾調應為 ŋʔ，實得 [{z.尾調}]");
			return null;
		});
		R("去聲 h 尾改成 s", async O => {
			// 炮: pʰrˤuh ⇒ pʰr'us ⇒ 聲母 pʰ、介腹 r'u、尾調 s。
			var z = Svc.Mk上古漢語音節From布之道("pʰrˤuh");
			T(z.聲母 == "pʰ" && z.介腹 == "r'u" && z.尾調 == "s");
			T(z.Full == "pʰr'us", $"Full 應為 pʰr'us，實得 [{z.Full}]");
			return null;
		});
		R("元音韻尾 i/u 改成半元音 j/w", async O => {
			// dˤiuk ⇒ dˤiwk ⇒ 聲母 d、介腹 'i、尾調 wk（三拼 dc,）。
			var z = Svc.Mk上古漢語音節From布之道("dˤiuk");
			T(z.聲母 == "d" && z.介腹 == "'i" && z.尾調 == "wk");
			T(z.Full == "d'iwk", $"Full 應為 d'iwk，實得 [{z.Full}]");
			return null;
		});
		R("圓括號連內容刪掉", async O => {
			// 枹: b(r)u ⇒ bu ⇒ 聲母 b、介腹 u、尾調空（沿用 dkp 同一套切韻）。
			var z = Svc.Mk上古漢語音節From布之道("b(r)u");
			T(z.聲母 == "b" && z.介腹 == "u" && z.尾調 == "");
			T(z.Full == "bu", $"Full 應為 bu，實得 [{z.Full}]");
			return null;
		});
		R("清化符改成後置 ʰ", async O => {
			// m̥(r)uk ⇒ mʰuk ⇒ 聲母 mʰ、介腹 u、尾調 k（清化信息保留，不歸併成別的音）。
			var z = Svc.Mk上古漢語音節From布之道("m̥(r)uk");
			T(z.聲母 == "mʰ", $"聲母應為 mʰ，實得 [{z.聲母}]");
			T(z.介腹 == "u" && z.尾調 == "k");
			T(z.Full == "mʰuk", $"Full 應為 mʰuk，實得 [{z.Full}]");
			return null;
		});
		R("ɫ 改成 j", async O => {
			// ɫa ⇒ ja ⇒ 聲母 j、介腹 a（G 鍵 R 欄的 j 與 r 由介腹區分）。
			var z = Svc.Mk上古漢語音節From布之道("ɫa");
			T(z.聲母 == "j" && z.介腹 == "a" && z.尾調 == "");
			return null;
		});
		R("中括號只刪括號保內容", async O => {
			// l[a]u ⇒ lau ⇒ 聲母 l、介腹 a、尾調 w。
			var z = Svc.Mk上古漢語音節From布之道("l[a]u");
			T(z.聲母 == "l" && z.介腹 == "a" && z.尾調 == "w");
			T(z.Full == "law", $"Full 應為 law，實得 [{z.Full}]");
			return null;
		});
		R("本步不做歸併：hl/ʍ/ɬ/ml 原樣保留", async O => {
			// A 步（記法適配）無損，歸鍵是 B 步（ToDks）的事：故這些音值必須原樣出現在音節裏。
			T(Svc.Mk上古漢語音節From布之道("hlak").聲母 == "hl", "hl 應原樣保留");
			T(Svc.Mk上古漢語音節From布之道("ʍa").聲母 == "ʍ", "ʍ 應原樣保留");
			T(Svc.Mk上古漢語音節From布之道("ɬa").聲母 == "ɬ", "ɬ 應原樣保留");
			T(Svc.Mk上古漢語音節From布之道("mlit").聲母 == "ml", "ml 應原樣保留");
			return null;
		});
	}
}
