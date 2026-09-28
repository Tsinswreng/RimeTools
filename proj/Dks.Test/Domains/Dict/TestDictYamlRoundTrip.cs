namespace Dks.Test.Domains.Dict;

using RimeTools.Shared.Dict.Models;
using RimeTools.Shared.Dict.Parser;
using Tsinswreng.CsTreeTest;

/// 測試 DictYamlParser/DictYamlWriter 的寫出→再讀入 round-trip(真實 dks_phrase 格式)。
/// 走的是純文本接口(TextReader/TextWriter)——庫裏不認路徑；臨時文件一律落在工作區內。
public partial class TestDictYamlRoundTrip:ITester{
	IDictYamlParser Parser = new DictYamlParser();
	IDictYamlWriter Writer = new DictYamlWriter();

	public ITestNode RegisterTestsInto(ITestNode? Node){
		Node ??= new TestNode();
		RegisterRoundTrip(Node);
		return Node;
	}

	public void RegisterRoundTrip(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestDictYamlRoundTrip),
			[typeof(DictYamlParser), typeof(DictYamlWriter)],
			[nameof(DictYamlParser.Parse), nameof(DictYamlWriter.Write)],
			"RoundTrip"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("dks_phrase 寫出後再讀回一致", async O => {
			// step 1: 構造表頭(columns: text/code/weight)與兩行正文。
			var header = new RimeDictHeader([
				new HeaderItem("name", "dks_phrase", null),
				new HeaderItem("sort", "by_weight", null),
				new HeaderItem("columns", null, ["text", "code", "weight"]),
				new HeaderItem("use_preset_vocabulary", "false", null),
			]);
			var body = new List<DictLine>{
				new(){ ["text"]="一個", ["code"]="qkkn", ["weight"]="374279" },
				new(){ ["text"]="保密", ["code"]="bmsx", ["weight"]="100" },
			};
			// step 2: 寫進內存(不碰文件系統)。
			using var writer = TestText.MkWriter();
			await Writer.Write(writer, header, TestAsy.ToAsy(body), default);

			// step 3: 從同一份文本讀回並比對表頭與正文。
			using var reader = new StringReader(writer.ToString());
			var doc = await Parser.Parse(reader, default);
			T(doc.Header.Name == "dks_phrase");
			T(doc.Header.Columns!.Count == 3 && doc.Header.Columns[0] == "text");
			var lines = new List<IDictLine>();
			await foreach(var l in doc.Body){
				lines.Add(l);
			}
			T(lines.Count == 2);
			T(lines[0].text == "一個" && lines[0].code == "qkkn" && lines[0].weight == "374279");
			T(lines[1].text == "保密" && lines[1].code == "bmsx" && lines[1].weight == "100");
			return null;
		});
		R("路徑版擴展方法寫出後再讀回一致", async O => {
			// 端點用的便捷寫法（自動開檔/關檔）也要能 round-trip；文件落在工作區內的 _tmp。
			var path = TestTmp.MkPath("roundTrip", "dks.dict.yaml");
			var header = new RimeDictHeader([
				new HeaderItem("name", "dks", null),
				new HeaderItem("version", "", null),
			]);
			var body = new List<DictLine>{
				new(){ ["text"]="辣", ["code"]="ryt" },
			};
			try{
				await Writer.Write(path, header, TestAsy.ToAsy(body), default);
				var doc = await Parser.Parse(path, default);
				T(doc.Header.Name == "dks", "表頭 name 應讀回 dks");
				var line = await TestAsy.FirstOrNullAsync(doc.Body);
				T(line is not null && line.text == "辣" && line.code == "ryt", "正文應讀回 辣/ryt");
			}finally{
				System.IO.File.Delete(path);
			}
			return null;
		});
		R("文件頂註釋跳過", async O => {
			var path = TestTmp.MkPath("roundTrip", "withComment.dict.yaml");
			try{
				await System.IO.File.WriteAllTextAsync(path,
					"#20260818214532\n# Rime dictionary\n---\nname: dks\n...\n辣\tryt\t10000%\n");
				var doc = await Parser.Parse(path, default);
				T(doc.Header.Name == "dks");
				var line = await TestAsy.FirstOrNullAsync(doc.Body);
				T(line is not null && line.text == "辣" && line.code == "ryt");
			}finally{
				System.IO.File.Delete(path);
			}
			return null;
		});
	}
}
