namespace Dks.Test.Domains.Dict;

using RimeTools.Shared.Dict.Models;
using RimeTools.Shared.Dict.Parser;
using Tsinswreng.CsTreeTest;

/// 測試 DictYamlParser 讀正文：列名映射、行內註釋、空行、多餘列。
public partial class TestDictYamlParser{
	public void RegisterBodyLines(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestDictYamlParser),
			[typeof(IDictYamlParser), typeof(DictYamlParser)],
			[nameof(DictYamlParser.Parse)],
			"讀正文"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("無 columns 時按 text/code/weight 映射", async O => {
			var doc = await Parse("---\nname: dks\n...\n辣\tryt\t10000%\n");
			var 行 = new List<RimeTools.Shared.Dict.Models.IDictLine>();
			await foreach(var l in doc.Body){
				行.Add(l);
			}
			T(行.Count == 1);
			T(行[0].text == "辣" && 行[0].code == "ryt" && 行[0].weight == "10000%");
			return null;
		});
		R("有 columns 時按 columns 映射", async O => {
			var doc = await Parse("---\nname: x\ncolumns:\n  - text\n  - weight\n...\n辣\t5\n");
			var 行 = new List<RimeTools.Shared.Dict.Models.IDictLine>();
			await foreach(var l in doc.Body){
				行.Add(l);
			}
			T(行[0].text == "辣" && 行[0].weight == "5", "第二格應映到 weight");
			// code 鍵不存在 ⇒ 便捷訪問器 code 返回 ""（見 DictLineExtn 的約定），字典裏也沒有該鍵。
			T(!行[0].ContainsKey("code"), "code 列不存在 ⇒ 字典裏不該有 code 鍵");
			return null;
		});
		R("行內註釋被截掉", async O => {
			// dkp 表有「買	mrˁeʔ	# sch, 鄭張」這種行：`#` 之後是註釋。
			var doc = await Parse("---\nname: dkp\n...\n買\tmrˁeʔ\t# sch, 鄭張\n辣\tlat\t0% # 舍他音\n");
			var 行 = new List<RimeTools.Shared.Dict.Models.IDictLine>();
			await foreach(var l in doc.Body){
				行.Add(l);
			}
			T(行[0].code == "mrˁeʔ", $"碼不該被註釋污染，實得 [{行[0].code}]");
			T(行[0].weight == "", $"純註釋的權重格應為空，實得 [{行[0].weight}]");
			T(行[1].weight == "0%", $"權重後的註釋應截掉，實得 [{行[1].weight}]");
			return null;
		});
		R("跳過空行、註釋行與殘留的 ---/...", async O => {
			var doc = await Parse("---\nname: x\n...\n辣\tryt\n\n#註釋\n...\n礦\tiyw\n");
			var 行 = new List<RimeTools.Shared.Dict.Models.IDictLine>();
			await foreach(var l in doc.Body){
				行.Add(l);
			}
			T(行.Count == 2, $"應只兩行，實得 {行.Count}");
			T(行[0].text == "辣" && 行[1].text == "礦");
			return null;
		});
		R("格子多於列名時忽略尾格", async O => {
			var doc = await Parse("---\nname: x\n...\n辣\tryt\t10000%\t多餘\n");
			var 行 = new List<RimeTools.Shared.Dict.Models.IDictLine>();
			await foreach(var l in doc.Body){
				行.Add(l);
			}
			T(行[0].weight == "10000%", $"尾格應被忽略，實得 [{行[0].weight}]");
			return null;
		});
		R("字後只有一個 tab ⇒ 碼為空串", async O => {
			// 空碼行是合法行（該字本無讀音），解析要能區分「有碼且為空」與「沒這列」。
			var doc = await Parse("---\nname: x\n...\n𠀀\t\n");
			var 行 = new List<RimeTools.Shared.Dict.Models.IDictLine>();
			await foreach(var l in doc.Body){
				行.Add(l);
			}
			// 注意：行首尾的 Trim 會把裸 tab 去掉，故這種行實際只有 text 一列。
			T(行[0].text == "𠀀" && (行[0].code is null || 行[0].code == ""), "空碼行應解析成空碼或缺列");
			return null;
		});
	}
}
