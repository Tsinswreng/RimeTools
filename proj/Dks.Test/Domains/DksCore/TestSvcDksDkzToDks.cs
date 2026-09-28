namespace Dks.Test.Domains.DksCore;

using Dks.Core.Svc;
using Tsinswreng.CsTreeTest;

/// 測試 SvcDks.DkzToDks（dkz → dks：解音節、查三層鍵位表、產出驗證）。
public partial class TestSvcDks{
	public void RegisterDkzToDks(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestSvcDks),
			[typeof(ISvcDks), typeof(SvcDks)],
			[nameof(ISvcDks.DkzToDks)],
			"DkzToDks"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("查表轉三拼並寫出 dks 表頭", async O => {
			// step 1: 造 dkz（真實 dkp 來源的兩行：辣從 dkp、礦從布之道）。
			var dkz = DictText.Mk("dkz", "辣\tr'at\t10000%", "礦\tkʷr'aŋʔ");
			using var reader = new StringReader(dkz);
			using var writer = TestText.MkWriter();
			// step 2: 跑步驟。
			await Svc.DkzToDks(Ctx, reader, writer, default);
			// step 3: 表頭與正文都要對（表頭決定 Rime 端能不能編譯）。
			var text = writer.ToString();
			T(text.Contains("name: dks\n"), "dks 表頭 name 應為 dks");
			T(text.Contains("version: \"\"\n"), "空 version 必須寫成空串（否則 Rime 判 incomplete dict header）");
			T(text.Contains("- nonKanji\n") && text.Contains("- chineseDict\n"), "dks 必須帶 import_tables");
			var 表 = await DictText.Mk字到碼集(text);
			T(表["辣"][0] == "ryt", $"辣 應為 ryt，實得 {表["辣"][0]}");
			T(表["礦"][0] == "iyw", $"礦 應為 iyw，實得 {表["礦"][0]}");
			T(表["辣"].Count == 1 && 表["礦"].Count == 1);
			return null;
		});
		R("一行多音各得各碼", async O => {
			// 同一字的兩個讀音各查各的鍵（枹 pr'u 與 bu 來自布之道原表的兩條）。
			var dkz = DictText.Mk("dkz", "枹\tpr'u", "枹\tbu");
			using var reader = new StringReader(dkz);
			using var writer = TestText.MkWriter();
			await Svc.DkzToDks(Ctx, reader, writer, default);
			var 表 = await DictText.Mk字到碼集(writer.ToString());
			T(表["枹"].Count == 2, "同字兩行應保留兩行");
			T(表["枹"][0] == "puj", $"枹 pr'u 應為 puj，實得 {表["枹"][0]}");
			T(表["枹"][1] == "bgj", $"枹 bu 應為 bgj，實得 {表["枹"][1]}");
			return null;
		});
		R("權重原樣帶過", async O => {
			var dkz = DictText.Mk("dkz", "辣\tr'at\t10000%", "礦\tkʷr'aŋʔ\t42");
			using var reader = new StringReader(dkz);
			using var writer = TestText.MkWriter();
			await Svc.DkzToDks(Ctx, reader, writer, default);
			var 行 = await DictText.Mk行(writer.ToString());
			T(行[0].權重 == "10000%", $"權重應原樣帶過，實得 [{行[0].權重}]");
			T(行[1].權重 == "42", $"權重應原樣帶過，實得 [{行[1].權重}]");
			return null;
		});
		R("查不到鍵即拋錯並列出問題行", async O => {
			// 產出驗證: 碼必須恰為三鍵。查不到鍵時不寫出半成品，並在異常裏列出該字與全拼。
			var dkz = DictText.Mk("dkz", "辣\tr'at", "𠀀\tzzz");
			using var reader = new StringReader(dkz);
			using var writer = TestText.MkWriter();
			var 拋了 = false;
			try{
				await Svc.DkzToDks(Ctx, reader, writer, default);
			}catch(InvalidOperationException ex){
				拋了 = true;
				T(ex.Message.Contains("產出驗證"), "異常信息應點明產出驗證");
				T(ex.Message.Contains("𠀀"), "異常信息應列出有問題的字");
				T(ex.Message.Contains("zzz"), "異常信息應帶上該行的全拼");
			}
			T(拋了, "查不到鍵時必須拋錯");
			T(writer.ToString().Length == 0, "驗證不過時不該寫出任何內容");
			return null;
		});
	}
}
