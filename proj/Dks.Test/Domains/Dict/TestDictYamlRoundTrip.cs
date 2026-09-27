namespace Dks.Test.Domains.Dict;

using RimeTools.Shared.Dict.Models;
using RimeTools.Shared.Dict.Parser;
using Tsinswreng.CsTreeTest;

/// 測試 DictYamlParser/DictYamlWriter 的寫出→再讀入 round-trip(真實 dks_phrase 格式)。
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
			var tmpPath = System.IO.Path.GetTempFileName();
			try{
				// step 1: 構造表頭(columns: text/code/weight)與兩行正文。
				var header = new RimeTools.Shared.Dict.Models.RimeDictHeader([
					new RimeTools.Shared.Dict.Models.HeaderItem("name", "dks_phrase", null),
					new RimeTools.Shared.Dict.Models.HeaderItem("sort", "by_weight", null),
					new RimeTools.Shared.Dict.Models.HeaderItem("columns", null, ["text", "code", "weight"]),
					new RimeTools.Shared.Dict.Models.HeaderItem("use_preset_vocabulary", "false", null),
				]);
				var body = new List<RimeTools.Shared.Dict.Models.DictLine>{
					new(){ ["text"]="一個", ["code"]="qkkn", ["weight"]="374279" },
					new(){ ["text"]="保密", ["code"]="bmsx", ["weight"]="100" },
				};
				await Writer.Write(tmpPath, header, TestAsy.ToAsy(body), default);

				// step 2: 讀回並比對表頭與正文。
				var doc = await Parser.Parse(tmpPath, default);
				T(doc.Header.Name == "dks_phrase");
				T(doc.Header.Columns!.Count == 3 && doc.Header.Columns[0] == "text");
				var lines = new List<RimeTools.Shared.Dict.Models.IDictLine>();
				await foreach(var l in doc.Body){
					lines.Add(l);
				}
				T(lines.Count == 2);
				T(lines[0].text == "一個" && lines[0].code == "qkkn" && lines[0].weight == "374279");
				T(lines[1].text == "保密" && lines[1].code == "bmsx" && lines[1].weight == "100");
			}
			finally{
				System.IO.File.Delete(tmpPath);
			}
			return null;
		});
		R("文件頂註釋跳過", async O => {
			var tmpPath = System.IO.Path.GetTempFileName();
			try{
				await System.IO.File.WriteAllTextAsync(tmpPath,
					"#20260818214532\n# Rime dictionary\n---\nname: dks\n...\n辣\tryt\t10000%\n");
				var doc = await Parser.Parse(tmpPath, default);
				T(doc.Header.Name == "dks");
				var line = await TestAsy.FirstOrNullAsync(doc.Body);
				T(line is not null && line.text == "辣" && line.code == "ryt");
			}
			finally{
				System.IO.File.Delete(tmpPath);
			}
			return null;
		});
	}
}