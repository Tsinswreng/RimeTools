using Dks.Test.Domains.Dict;
using Dks.Test.Domains.DksCore;
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
		// Dict 領域測試（文檔模型 + 解析/寫出 + 反查索引）。
		this.RegisterTester<TestIDictLine>();
		this.RegisterTester<TestRimeDictHeader>();
		this.RegisterTester<TestDictYamlRoundTrip>();
		this.RegisterTester<TestDictYamlParser>();
		this.RegisterTester<TestDictYamlWriter>();
		this.RegisterTester<TestMemoryCharCodeLookup>();
		// Freq 領域測試。
		this.RegisterTester<TestEssayWordFreqSource>();
		// Phrase 領域測試（造詞策略 + 造詞主流程）。
		this.RegisterTester<TestPhraseMkrHeadEtTail>();
		this.RegisterTester<TestPhraseMaker>();
		// Dks 領域測試（三層鍵位表 + 兩支拼式解析器 + Dks 流水線服務各步）。
		this.RegisterTester<TestDksKeyboard>();
		this.RegisterTester<TestDkp拼式Parser>();
		this.RegisterTester<Test布之道拼式Parser>();
		this.RegisterTester<TestSvcDks>();
		return Node;
	}
}