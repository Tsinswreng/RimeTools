using Dks.Test.Domains.Dict;
using Dks.Test.Domains.Freq;
using Dks.Test.Domains.Phrase;
using Dks.Test.Domains.Tools;
using Tsinswreng.CsTreeTest;

namespace Dks.Test;

/// Dks.Core 測試的管理器：把各測試域註冊進測試樹。
public class DksTestMgr:DiEtTestMgr{
	public static DksTestMgr Inst = new();
	public override ITestNode RegisterTestsInto(ITestNode? Node){
		Node = this.TestNode;
		// 公用件測試域。
		this.RegisterTester<TestCodeTransformer>();
		this.RegisterTester<TestTextUtil>();
		this.RegisterTester<TestCartesian>();
		this.RegisterTester<TestSrsReplacer>();
		this.RegisterTester<TestRuleResourcesVsRegex>();
		// Dict 領域測試（文檔模型 + 解析 round-trip + 反查索引）。
		this.RegisterTester<TestIDictLine>();
		this.RegisterTester<TestRimeDictHeader>();
		this.RegisterTester<TestDictYamlRoundTrip>();
		this.RegisterTester<TestMemoryCharCodeLookup>();
		// Freq 領域測試。
		this.RegisterTester<TestEssayWordFreqSource>();
		// Phrase 領域測試（造詞策略 + 造詞主流程）。
		this.RegisterTester<TestPhraseMkrHeadEtTail>();
		this.RegisterTester<TestPhraseMaker>();
		return Node;
	}
}