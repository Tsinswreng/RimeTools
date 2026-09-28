namespace Dks.Test.Domains.DksCore;

using Dks.Core;
using Dks.Core.Svc;
using Tsinswreng.CsCtx;
using Tsinswreng.CsTreeTest;

/// 測試 SvcDks（Dks 流水線服務，實現 ISvcDks）。
/// 本檔只組裝；每個 `TestSvcDks*.cs` 主要測一個成員，用例名要能看出所測行爲。
/// 服務本身無狀態（可換策略都在 DksCfg），故需要詞頻源的用例各自 new 一個 Svc。
public partial class TestSvcDks:ITester{
	/// 純函數／不需要詞頻的用例共用的服務實例（解析/寫出用默認實現）。
	SvcDks Svc = new(new DksCfg());

	/// 步驟方法的第一個參數（函數上下文袋）；本服務實現不讀它，但按接口簽名照傳。
	IFnCtx Ctx = new FnCtx();

	public ITestNode RegisterTestsInto(ITestNode? Node){
		Node ??= new TestNode();
		RegisterMk上古漢語音節FromDkp(Node);
		RegisterMk上古漢語音節From布之道(Node);
		RegisterToDks(Node);
		RegisterDkzToDks(Node);
		Register布之道ToDkz(Node);
		RegisterSaffesToDkz(Node);
		RegisterUpdateDkzFile(Node);
		RegisterUpdateDks(Node);
		RegisterAttachCangjie(Node);
		RegisterToDkn(Node);
		Register回退缺音(Node);
		Register按頻擇源(Node);
		RegisterMkDksPhrase(Node);
		return Node;
	}
}
