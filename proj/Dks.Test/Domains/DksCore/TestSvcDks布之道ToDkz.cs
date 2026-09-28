namespace Dks.Test.Domains.DksCore;

using Dks.Core.Svc;
using Tsinswreng.CsTreeTest;

/// 測試 SvcDks.布之道ToDkz（布之道原表 → dkz 中介表：取 `*` 後的 IPA 段、適配、寫 Full）。
public partial class TestSvcDks{
	public void Register布之道ToDkz(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestSvcDks),
			[typeof(ISvcDks), typeof(SvcDks)],
			[nameof(ISvcDks.布之道ToDkz)],
			"布之道ToDkz"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("只取 * 之後的 IPA 段並寫成本方案拼式", async O => {
			// 真實 OC_msoegDK 行：包 prXu*prˤu ⇒ 碼欄取 prˤu ⇒ 適配後 pr'u。
			var 源 = DictText.Mk("OC_msoegDK",
				"包\tprXu*prˤu\t10000%",
				"枹\tb(r)u*b(r)u",
				"炮\tphrXuS*pʰrˤuh");
			using var reader = new StringReader(源);
			using var writer = TestText.MkWriter();
			await Svc.布之道ToDkz(Ctx, reader, writer, default);
			var 表 = await DictText.Mk字到碼集(writer.ToString());
			T(表["包"][0] == "pr'u", $"包 應為 pr'u，實得 [{表["包"][0]}]");
			T(表["枹"][0] == "bu", $"枹 應為 bu，實得 [{表["枹"][0]}]");
			T(表["炮"][0] == "pʰr'us", $"炮 應為 pʰr'us，實得 [{表["炮"][0]}]");
			return null;
		});
		R("表頭為 dkz、權重原樣帶過", async O => {
			var 源 = DictText.Mk("OC_msoegDK", "包\tprXu*prˤu\t10000%");
			using var reader = new StringReader(源);
			using var writer = TestText.MkWriter();
			await Svc.布之道ToDkz(Ctx, reader, writer, default);
			var text = writer.ToString();
			T(text.Contains("name: dkz\n"), "dkz 中介表的 name 應為 dkz");
			T(text.Contains("version: \"\"\n"), "空 version 必須寫成空串");
			var 行 = await DictText.Mk行(text);
			T(行[0].權重 == "10000%", $"權重應原樣帶過，實得 [{行[0].權重}]");
			return null;
		});
		R("無 * 的碼欄整串當 IPA", async O => {
			// 有些行沒有 ASCII 段（如 半 pˤans），此時整串就是 IPA。
			var 源 = DictText.Mk("OC_msoegDK", "半\tpˤans");
			using var reader = new StringReader(源);
			using var writer = TestText.MkWriter();
			await Svc.布之道ToDkz(Ctx, reader, writer, default);
			var 表 = await DictText.Mk字到碼集(writer.ToString());
			T(表["半"][0] == "p'ans", $"半 應為 p'ans，實得 [{表["半"][0]}]");
			return null;
		});
		R("只收單字行、空碼行丟掉", async O => {
			// 詞條行（兩字以上）與空碼行是髒數據，不進 dkz（否則後面查表會得到空碼行）。
			var 源 = DictText.Mk("OC_msoegDK",
				"半半\tpˤans",
				"𠀀\t",
				"半\tpˤans");
			using var reader = new StringReader(源);
			using var writer = TestText.MkWriter();
			await Svc.布之道ToDkz(Ctx, reader, writer, default);
			var 行 = await DictText.Mk行(writer.ToString());
			T(行.Count == 1, $"應只留單字有碼的一行，實得 {行.Count} 行");
			T(行[0].字 == "半");
			return null;
		});
	}
}
