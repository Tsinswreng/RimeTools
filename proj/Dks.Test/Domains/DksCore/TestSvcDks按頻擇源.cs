namespace Dks.Test.Domains.DksCore;

using Dks.Core;
using Dks.Core.Svc;
using RimeTools.Shared.Freq;
using Tsinswreng.CsTreeTest;

/// 測試 SvcDks.按頻擇源（Dks4：dkp 最優先；其餘按 essay 字頻排名分流；保證不缺字）。
public partial class TestSvcDks{
	/// 造一個帶內存詞頻源的服務（本步需要 Cfg.WordFreq）。
	private static SvcDks MkSvc(params WordFreq[] 詞頻){
		return new SvcDks(new DksCfg{ WordFreq = new MemWordFreq(詞頻) });
	}

	public void Register按頻擇源(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestSvcDks),
			[typeof(ISvcDks), typeof(SvcDks)],
			[nameof(ISvcDks.按頻擇源)],
			"按頻擇源"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("dkp 覆蓋字不受頻率影響", async O => {
			// 辣 兩側都有且 dkp 覆蓋；即使 辣 是最高頻字，也必須留新側（dkp 說了算）。
			var svc = MkSvc(new WordFreq("辣", 100));
			var 新 = DictText.Mk("dks", "辣\tryt");
			var 舊 = DictText.Mk("dks", "辣\tmpu");
			using var r1 = new StringReader(新);
			using var r2 = new StringReader(舊);
			using var w = TestText.MkWriter();
			await svc.按頻擇源(Ctx, r1, r2, new HashSet<str>{ "辣" }, 5000, w, default);
			var 表 = await DictText.Mk字到碼集(w.ToString());
			T(表["辣"][0] == "ryt", $"dkp 覆蓋字應留新側，實得 [{表["辣"][0]}]");
			return null;
		});
		R("前 N 名用舊側、名次外與 essay 沒有的用新側", async O => {
			// 詞頻序：辣(100)、縮縮(90, 兩字詞不佔名次)、縮(50)、罕(1)；上限 2 ⇒ 高頻字集 = {辣, 縮}。
			//   辣 → dkp 覆蓋 ⇒ 新側（ryt）
			//   縮 → 排名內 ⇒ 舊側（zzz）；若詞條佔了名次，縮 就會掉出名次而留 stk
			//   誠 → 新側有、舊側也有，但不在 essay ⇒ 名次外 ⇒ 新側（wss）
			//   吾 → 只在 essay 外、舊側沒有 ⇒ 只能留新側（wzj）
			//   買、罕 → 新側沒有（布之道缺音）⇒ 用舊側（mpu、jjj）保證不缺字
			var svc = MkSvc(
				new WordFreq("辣", 100),
				new WordFreq("縮縮", 90),
				new WordFreq("縮", 50),
				new WordFreq("罕", 1));
			var 新 = DictText.Mk("dks", "辣\tryt", "吾\twzj", "縮\tstk", "誠\twss");
			var 舊 = DictText.Mk("dks", "辣\tmpu", "縮\tzzz", "誠\tqqq", "買\tnzh", "罕\tjjj");
			using var r1 = new StringReader(新);
			using var r2 = new StringReader(舊);
			using var w = TestText.MkWriter();
			await svc.按頻擇源(Ctx, r1, r2, new HashSet<str>{ "辣" }, 2, w, default);
			var 表 = await DictText.Mk字到碼集(w.ToString());
			T(表["辣"][0] == "ryt", $"dkp 覆蓋 ⇒ 新側，實得 [{表["辣"][0]}]");
			T(表["縮"][0] == "zzz", $"排名內 ⇒ 舊側，實得 [{表["縮"][0]}]");
			T(表["誠"][0] == "wss", $"排名外 ⇒ 新側，實得 [{表["誠"][0]}]");
			T(表["吾"][0] == "wzj", "舊側沒有的字只能留新側");
			T(表["買"][0] == "nzh", $"新側缺音 ⇒ 舊側補，實得 [{表["買"][0]}]");
			T(表["罕"][0] == "jjj", $"新側缺音 ⇒ 舊側補，實得 [{表["罕"][0]}]");
			T(表.Count == 6, $"六個字都要在，實得 {表.Count}");
			return null;
		});
		R("上限 0 時只有 dkp 與缺字兩類走舊側", async O => {
			var svc = MkSvc(new WordFreq("縮", 100));
			var 新 = DictText.Mk("dks", "縮\tstk");
			var 舊 = DictText.Mk("dks", "縮\tzzz", "買\tmpu");
			using var r1 = new StringReader(新);
			using var r2 = new StringReader(舊);
			using var w = TestText.MkWriter();
			await svc.按頻擇源(Ctx, r1, r2, new HashSet<str>(), 0, w, default);
			var 表 = await DictText.Mk字到碼集(w.ToString());
			T(表["縮"][0] == "stk", "上限 0 ⇒ 沒有高頻字 ⇒ 留新側");
			T(表["買"][0] == "mpu", "新側缺音的字仍用舊側補");
			return null;
		});
		R("多音字的全部行一起換源", async O => {
			// 買 新側沒有 ⇒ 舊側兩行都過來；吾 新側兩行、排名內 ⇒ 兩行都換成舊側那兩行。
			var svc = MkSvc(new WordFreq("吾", 10));
			var 新 = DictText.Mk("dks", "吾\twzj", "吾\twzg");
			var 舊 = DictText.Mk("dks", "買\tmpu", "買\tnzh", "吾\tqqq", "吾\tqqj");
			using var r1 = new StringReader(新);
			using var r2 = new StringReader(舊);
			using var w = TestText.MkWriter();
			await svc.按頻擇源(Ctx, r1, r2, new HashSet<str>(), 5000, w, default);
			var 表 = await DictText.Mk字到碼集(w.ToString());
			T(表["吾"].Count == 2, $"吾 是排名內 ⇒ 兩行都換成舊側，實得 {表["吾"].Count}");
			T(表["吾"][0] == "qqq" && 表["吾"][1] == "qqj", "吾 應整組走舊側且保舊序");
			T(表["買"].Count == 2, $"買 的兩個舊讀音都該在，實得 {表["買"].Count}");
			return null;
		});
		R("沒注入詞頻源即拋錯", async O => {
			// 錯誤信息要點明缺哪個策略（不讓 null 傳播成 NullReference）。
			var svc = new SvcDks(new DksCfg());
			using var r1 = new StringReader(DictText.Mk("dks", "辣\tryt"));
			using var r2 = new StringReader(DictText.Mk("dks", "辣\tmpu"));
			using var w = TestText.MkWriter();
			var 拋了 = false;
			try{
				await svc.按頻擇源(Ctx, r1, r2, new HashSet<str>(), 5000, w, default);
			}catch(InvalidOperationException ex){
				拋了 = true;
				T(ex.Message.Contains("WordFreq"), $"異常應點明 WordFreq，實為: {ex.Message}");
			}
			T(拋了, "缺詞頻源時必須拋錯");
			return null;
		});
	}
}
