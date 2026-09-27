namespace Dks.Test.Domains.Tools;

using System.Text.RegularExpressions;
using RimeTools.Tools;
using Tsinswreng.CsTreeTest;

/// 對拍測試: 自研 SrsReplacer vs .NET Regex, 同一輸入同一規則集, 輸出必須一致。
/// 規則集取 Dks.Core/Rules 的實測樣本(嵌進測試數據, 不依賴資源加載)。
public partial class TestSrsReplacer:ITester{
	public ITestNode RegisterTestsInto(ITestNode? Node){
		Node ??= new TestNode();
		RegisterSingleRule(Node);
		RegisterAgainstRegex(Node);
		return Node;
	}

	public void RegisterSingleRule(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestSrsReplacer),
			[typeof(SrsReplacer)],
			[nameof(SrsReplacer.ReplaceAll)],
			"SingleRule"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("捕獲三字符拆槽", async O => {
			var r = SrsReplacer.ReplaceAll("pmk", new SrRule("(.)(.)(.)", "首一$1首二腹一$2腹二尾一$3尾二"));
			T(r == "首一p首二腹一m腹二尾一k尾二");
			return null;
		});
		R("非貪婪捕獲", async O => {
			var r = SrsReplacer.ReplaceAll(
				"首一Y首二腹一T腹二尾一abc尾二",
				new SrRule("首一(Y)首二腹一([QTIPHYCN])腹二尾一(.*?)尾二", "首一j首二腹一$2腹二尾一$3尾二"));
			T(r == "首一j首二腹一T腹二尾一abc尾二");
			return null;
		});
		R("交替捕獲", async O => {
			var r = SrsReplacer.ReplaceAll("ˁA", new SrRule("ˁ(æ|A)", "腹一X$1腹二"));
			T(r == "腹一XA腹二", $"交替=[{r}]");
			return null;
		});
		R("純字面全表替換", async O => {
			var r = SrsReplacer.ReplaceAll("gag", new SrRule("g", "ɡ"));
			T(r == "ɡaɡ", $"字面=[{r}]");
			return null;
		});
		R("錨點規則", async O => {
			var r = SrsReplacer.ReplaceAll(";;;", new SrRule(";+$", ""));
			T(r == "", $"錨點=[{r}]");
			return null;
		});
		R("規則0空替換後規則1拆槽", async O => {
			// 復現 vsRegex 的分歧: 先跑 '→空 再跑 (.)(.)(.) 拆槽。
			var s0 = SrsReplacer.ReplaceAll("pmk", new SrRule("'", ""));
			var r = SrsReplacer.ReplaceAll(s0, new SrRule("(.)(.)(.)", "首一$1首二腹一$2腹二尾一$3尾二"));
			T(s0 == "pmk", $"規則0 後=[{s0}]");
			T(r == "首一p首二腹一m腹二尾一k尾二", $"規則0+規則1 後=[{r}]");
			return null;
		});
		R("收斂規則(三個非貪婪組)", async O => {
			// 直接對三槽格式跑收斂規則, 期望還原三字符。
			var r = SrsReplacer.ReplaceAll(
				"首一p首二腹一m腹二尾一k尾二",
				new SrRule("首一(.*?)首二腹一(.*?)腹二尾一(.*?)尾二", "$1$2$3"));
			T(r == "pmk", $"收斂得 [{r}]");
			return null;
		});
		R("拆槽再收斂全鏈", async O => {
			var s = SrsReplacer.ReplaceAll("pmk", new SrRule("(.)(.)(.)", "首一$1首二腹一$2腹二尾一$3尾二"));
			var r = SrsReplacer.ReplaceAll(s, new SrRule("首一(.*?)首二腹一(.*?)腹二尾一(.*?)尾二", "$1$2$3"));
			T(r == "pmk", $"拆槽=[{s}] 收斂=[{r}]");
			return null;
		});
		R("無錨 (.*?)腹一 規則", async O => {
			// .NET: Regex.Replace("pmk腹一X", "(.*?)腹一", "X$1")
			var input = "首一p首二腹一m腹二尾一k尾二";
			var rule = new SrRule("(.*?)腹一", "X$1");
			var actual = SrsReplacer.ReplaceAll(input, rule);
			var expected = Regex.Replace(input, rule.Pattern, rule.Replacement);
			T(actual == expected, $"無錨: 引擎=[{actual}] Regex=[{expected}]");
			return null;
		});
		R("規則3 條件改寫", async O => {
			var input = "首一Y首二腹一T腹二尾一Z尾二";
			var rule = new SrRule("首一(Y)首二腹一([QTIPHYCN])腹二尾一(.*?)尾二", "首一j首二腹一$2腹二尾一$3尾二");
			var actual = SrsReplacer.ReplaceAll(input, rule);
			var expected = Regex.Replace(input, rule.Pattern, rule.Replacement);
			T(actual == expected, $"規則3: 引擎=[{actual}] Regex=[{expected}]");
			return null;
		});
	}

	/// 用一批真實規則 × 碼, 對拍自研引擎與 .NET Regex(開發期校驗, 非運行依賴)。
	public void RegisterAgainstRegex(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestSrsReplacer),
			[typeof(SrsReplacer)],
			[nameof(SrsReplacer.ApplyAll)],
			"VsRegex"
		);
		var R = register.Register;
		var T = Assert.IsTrue;

		// step 1: 樣本規則(取自 saffesToOcRegex / ocToOc3, 覆蓋各語法面)。
		var rules = new List<SrRule>{
			new("'", ""),
			new("(.)(.)(.)", "首一$1首二腹一$2腹二尾一$3尾二"),
			new("首一(Y)首二腹一([QTIPHYCN])腹二尾一(.*?)尾二", "首一j首二腹一$2腹二尾一$3尾二"),
			new("首一(.*?)首二腹一(.*?)腹二尾一(.*?)尾二", "$1$2$3"),
			new("g", "ɡ"),
			new("ˤ", "ˁ"),
			new("rr", "r"),
			new("dz", "從"),
			new("從", "dz"),
			new("ˁ(æ|A)", "腹一.腹二"),
			new("r(æ|A|ˡa|ʲa)", "腹一H腹二"),
			new("(.*?)腹一", "X$1"),
		};

		// step 2: 輸入樣本(覆蓋: 短碼、三槽碼、多字詞、帶撇號)。
		var inputs = new[]{
			"pmk", "jyd", "doa", "pit", "waj", "bls", "'A'Q",
			"首一Y首二腹一T腹二尾一Z尾二",
			"首一w首二腹一q腹二尾一s尾二",
			"gag", "rrr", "dzan", "ˤæ",
			"首一p首二腹一m腹二尾一k尾二",
		};

		foreach(var input in inputs){
			var actual = SrsReplacer.ApplyAll(input, rules);
			var expected = input;
			for(var ri = 0; ri < rules.Count; ri++){
				expected = Regex.Replace(expected, rules[ri].Pattern, rules[ri].Replacement);
			}
			T(actual == expected, $"input={input}");
		}
	}
}