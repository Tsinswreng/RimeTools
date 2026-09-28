namespace Dks.Test.Domains.Dict;

using RimeTools.Shared.Dict.Models;
using RimeTools.Shared.Dict.Parser;
using Tsinswreng.CsTreeTest;

/// 測試 DictYamlWriter.Write 的正文部分（列序、缺列、空碼、權重）。
public partial class TestDictYamlWriter{
	public void Register寫正文(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestDictYamlWriter),
			[typeof(IDictYamlWriter), typeof(DictYamlWriter)],
			[nameof(DictYamlWriter.Write)],
			"寫正文"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("按 Header.Columns 的列序取值", async O => {
			// 列序由表頭決定，不是字典的枚舉順序（自定義列名時尤其重要）。
			var header = new RimeDictHeader([
				new HeaderItem("name", "x", null),
				new HeaderItem("columns", null, ["text", "weight", "code"]),
			]);
			var body = new List<IDictLine>{
				new DictLine{ ["text"] = "辣", ["code"] = "ryt", ["weight"] = "5" },
			};
			using var w = TestText.MkWriter();
			await Writer.Write(w, header, TestAsy.ToAsy(body), default);
			T(w.ToString().EndsWith("辣\t5\tryt\n"), $"應按 columns 序寫出，實得:\n{w}");
			return null;
		});
		R("尾列缺失不補空 tab", async O => {
			// 兩格的詞條行（無 weight）照樣只有兩格，不多餘一個 tab。
			var header = new RimeDictHeader([new HeaderItem("name", "x", null)]);
			var body = new List<IDictLine>{
				new DictLine{ ["text"] = "一", ["code"] = "qdkm;" },
			};
			using var w = TestText.MkWriter();
			await Writer.Write(w, header, TestAsy.ToAsy(body), default);
			T(w.ToString().EndsWith("一\tqdkm;\n"), $"不該補空 tab，實得:\n{w}");
			return null;
		});
		R("空碼寫成空的一格", async O => {
			// 空碼是合法行（該字本無讀音），故 tab 要留住、碼格為空。
			var header = new RimeDictHeader([new HeaderItem("name", "x", null)]);
			var body = new List<IDictLine>{
				new DictLine{ ["text"] = "𠀀", ["code"] = "" },
			};
			using var w = TestText.MkWriter();
			await Writer.Write(w, header, TestAsy.ToAsy(body), default);
			T(w.ToString().EndsWith("𠀀\t\n"), $"空碼應寫成空的一格，實得:\n{w}");
			return null;
		});
		R("中間列缺失即停（Rime 行是左對齊的）", async O => {
			var header = new RimeDictHeader([new HeaderItem("name", "x", null)]);
			var body = new List<IDictLine>{
				new DictLine{ ["text"] = "辣", ["weight"] = "5" },
			};
			using var w = TestText.MkWriter();
			await Writer.Write(w, header, TestAsy.ToAsy(body), default);
			T(w.ToString().EndsWith("辣\n"), $"缺 code 時只寫到缺的那列，實得:\n{w}");
			return null;
		});
		R("權重原樣輸出", async O => {
			var header = new RimeDictHeader([new HeaderItem("name", "x", null)]);
			var body = new List<IDictLine>{
				new DictLine{ ["text"] = "辣", ["code"] = "ryt", ["weight"] = "10000%" },
			};
			using var w = TestText.MkWriter();
			await Writer.Write(w, header, TestAsy.ToAsy(body), default);
			T(w.ToString().EndsWith("辣\tryt\t10000%\n"), $"權重應原樣，實得:\n{w}");
			return null;
		});
		R("空正文也能寫出完整文檔", async O => {
			var header = new RimeDictHeader([new HeaderItem("name", "x", null)]);
			using var w = TestText.MkWriter();
			await Writer.Write(w, header, TestAsy.ToAsy(new List<IDictLine>()), default);
			T(w.ToString() == "---\nname: x\n...\n", $"應只有表頭，實得:\n{w}");
			return null;
		});
	}
}
