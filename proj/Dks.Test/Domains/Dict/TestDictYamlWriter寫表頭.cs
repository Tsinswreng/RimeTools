namespace Dks.Test.Domains.Dict;

using RimeTools.Shared.Dict.Models;
using RimeTools.Shared.Dict.Parser;
using Tsinswreng.CsTreeTest;

/// 測試 DictYamlWriter.Write 的表頭部分（`---`…`...`、空標量、列表、標量引號）。
public partial class TestDictYamlWriter{
	public void Register寫表頭(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestDictYamlWriter),
			[typeof(IDictYamlWriter), typeof(DictYamlWriter)],
			[nameof(DictYamlWriter.Write)],
			"寫表頭"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("空標量寫成空串（Rime 的硬要求）", async O => {
			// 回歸：`version: `（空標量 = YAML null）會讓 DictSettings::LoadDictHeader 報
			// "incomplete dict header"，整張表編譯不過（曾讓部署後一直沿用舊表）。
			var header = new RimeDictHeader([
				new HeaderItem("name", "dks", null),
				new HeaderItem("version", "", null),
			]);
			using var w = TestText.MkWriter();
			await Writer.Write(w, header, Empty(), default);
			var text = w.ToString();
			T(text.Contains("version: \"\"\n"), $"version 空標量應寫成 \"\"，實得:\n{text}");
			T(!text.Contains("version: \n"), "不該寫出 `version: `（那是 YAML null）");
			return null;
		});
		R("null 標量也寫成空串", async O => {
			var header = new RimeDictHeader([new HeaderItem("version", null, null)]);
			using var w = TestText.MkWriter();
			await Writer.Write(w, header, Empty(), default);
			T(w.ToString().Contains("version: \"\"\n"), "null 標量應寫成空串");
			return null;
		});
		R("含特殊字符的標量加雙引號", async O => {
			// `#` 會被當註釋、`:` 會被當映射、首尾空白會被裁掉，故這些值必須引起來。
			var header = new RimeDictHeader([
				new HeaderItem("name", "dk#s", null),
				new HeaderItem("sort", "a:b", null),
				new HeaderItem("foo", " x ", null),
				new HeaderItem("bar", "he said \"hi\"", null),
			]);
			using var w = TestText.MkWriter();
			await Writer.Write(w, header, Empty(), default);
			var text = w.ToString();
			T(text.Contains("name: \"dk#s\"\n"), $"含 # 應加引號，實得:\n{text}");
			T(text.Contains("sort: \"a:b\"\n"), "含 : 應加引號");
			T(text.Contains("foo: \" x \"\n"), "首尾空白應加引號");
			T(text.Contains("bar: \"he said \\\"hi\\\"\"\n"), "內部雙引號應轉義");
			return null;
		});
		R("普通標量不加引號", async O => {
			var header = new RimeDictHeader([new HeaderItem("name", "dks", null)]);
			using var w = TestText.MkWriter();
			await Writer.Write(w, header, Empty(), default);
			T(w.ToString().Contains("name: dks\n"), "普通值應原樣輸出");
			return null;
		});
		R("列表寫成懸空鍵加縮進項", async O => {
			// 對應 dks 的 import_tables（Rime 要求 `鍵:` 換行後 `  - 項`）。
			var header = new RimeDictHeader([
				new HeaderItem("import_tables", null, ["nonKanji", "chineseDict"]),
			]);
			using var w = TestText.MkWriter();
			await Writer.Write(w, header, Empty(), default);
			T(w.ToString().Contains("import_tables:\n  - nonKanji\n  - chineseDict\n"), "列表格式應為懸空鍵 + 縮進項");
			return null;
		});
		R("表頭按 Items 原序、並以 --- 與 ... 包住", async O => {
			var header = new RimeDictHeader([
				new HeaderItem("name", "dks", null),
				new HeaderItem("sort", "by_weight", null),
			]);
			using var w = TestText.MkWriter();
			await Writer.Write(w, header, Empty(), default);
			var lines = w.ToString().Split('\n');
			T(lines[0] == "---", $"首行應為 ---，實得 [{lines[0]}]");
			T(lines[1] == "name: dks" && lines[2] == "sort: by_weight", "表頭應保持原序");
			T(lines[3] == "...", $"第四行應為 ...，實得 [{lines[3]}]");
			return null;
		});
		R("不關閉調用方的 Writer", async O => {
			// Writer 的生命週期歸調用方：寫完之後還能繼續寫（否則端點就沒法複用同一個 writer）。
			var header = new RimeDictHeader([new HeaderItem("name", "dks", null)]);
			using var w = TestText.MkWriter();
			await Writer.Write(w, header, Empty(), default);
			await w.WriteLineAsync("# 尾巴");
			T(w.ToString().EndsWith("# 尾巴\n"), "寫完後應仍可繼續寫入");
			return null;
		});
	}

	/// 空正文流。
	private static async IAsyncEnumerable<IDictLine> Empty(){
		await Task.CompletedTask;
		yield break;
	}
}
