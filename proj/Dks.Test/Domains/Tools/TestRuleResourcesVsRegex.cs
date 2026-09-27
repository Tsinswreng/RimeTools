namespace Dks.Test.Domains.Tools;

using System.Text.RegularExpressions;
using Dks.Core;
using RimeTools.Tools;
using Tsinswreng.CsTreeTest;

/// 終極對拍: 用真實規則資源(saffesToOc 490 條 / ocToOc3 143 條 / cangjie 51 條)
/// 對多個代表性碼, 引擎輸出必須等價 .NET Regex(逐條全表替換)。
public partial class TestRuleResourcesVsRegex:ITester{
	public ITestNode RegisterTestsInto(ITestNode? Node){
		Node ??= new TestNode();
		RegisterSaffesToOc(Node);
		RegisterOcToOc3(Node);
		RegisterCangjie(Node);
		return Node;
	}

	/// 返回 null 表示全部一致; 否則返回首個分歧描述。
	private static str? FindDiff(IReadOnlyList<SrRule> Rules, str Input){
		var actual = SrsReplacer.ApplyAll(Input, Rules);
		var expected = Input;
		for(var ri = 0; ri < Rules.Count; ri++){
			expected = Regex.Replace(expected, Rules[ri].Pattern, Rules[ri].Replacement);
		}
		return actual == expected ? null : $"input=[{Input}] 引擎=[{actual}] Regex=[{expected}]";
	}

	/// 檢查一批輸入, 全部一致返回 null, 否則返回首個分歧描述。
	private static void CheckAll(IReadOnlyList<SrRule> Rules, str[] Inputs){
		foreach(var input in Inputs){
			var diff = FindDiff(Rules, input);
			if(diff is not null){
				throw new System.Exception(diff);
			}
		}
	}

	/// 490 條 saffes→OC 音變規則對拍。
	public void RegisterSaffesToOc(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestRuleResourcesVsRegex),
			[typeof(DksRegexRules)],
			[nameof(DksRegexRules.SaffesToOc)],
			"SaffesToOc"
		);
		var R = register.Register;
		R("三拼碼樣本", async O => {
			CheckAll(DksRegexRules.SaffesToOc(), new[]{
				"pmk", "jyd", "doa", "pit", "waj", "bls", "aye", "bbt",
				"首一p首二腹一m腹二尾一k尾二",
				"首一Y首二腹一T腹二尾一Z尾二",
				"首一w首二腹一q腹二尾一s尾二",
			});
			return null;
		});
	}

	/// 143 條 OC→OC3 音變規則對拍。
	public void RegisterOcToOc3(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestRuleResourcesVsRegex),
			[typeof(DksRegexRules)],
			[nameof(DksRegexRules.OcToOc3)],
			"OcToOc3"
		);
		var R = register.Register;
		R("槽串樣本", async O => {
			CheckAll(DksRegexRules.OcToOc3(), new[]{
				"首一p首二腹一m腹二尾一k尾二", "gag", "dzan", "ˤæ", "ʷʰa",
				"首一dz首二腹一ə腹二尾一k尾二",
			});
			return null;
		});
	}

	/// 51 條倉頡字根規則對拍(含 ;+$、^$、. 等語法)。
	public void RegisterCangjie(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestRuleResourcesVsRegex),
			[typeof(DksRegexRules)],
			[nameof(DksRegexRules.Cangjie)],
			"Cangjie"
		);
		var R = register.Register;
		R("字根與碼樣本", async O => {
			CheckAll(DksRegexRules.Cangjie(), new[]{
				"冫", "門目", "走米", "abc", ";;;", ";;", "",
			});
			return null;
		});
	}
}