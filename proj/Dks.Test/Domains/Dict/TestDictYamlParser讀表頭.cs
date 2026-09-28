namespace Dks.Test.Domains.Dict;

using RimeTools.Shared.Dict.Parser;
using Tsinswreng.CsTreeTest;

/// 測試 DictYamlParser 讀表頭：標量、列表、註釋、無表頭文件、缺標記報錯。
public partial class TestDictYamlParser{
	public void RegisterReadHeader(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestDictYamlParser),
			[typeof(IDictYamlParser), typeof(DictYamlParser)],
			[nameof(DictYamlParser.Parse)],
			"讀表頭"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("讀標量與列表鍵", async O => {
			var doc = await Parse("""
				#20260818214532
				---
				name: dks
				version: ""
				sort: by_weight
				import_tables:
				  - nonKanji
				  - chineseDict
				...
				辣	ryt	10000%
				""");
			T(doc.Header.Name == "dks", $"name 應為 dks，實得 [{doc.Header.Name}]");
			T(doc.Header.Items[1].Key == "version", $"第二項應為 version，實得 [{doc.Header.Items[1].Key}]");
			T(doc.Header.Items[1].Scalar == "", $"`version: \"\"` 應讀成空串，實得 [{doc.Header.Items[1].Scalar}]");
			T(doc.Header.Items[3].List is not null && doc.Header.Items[3].List!.Count == 2, "import_tables 應為兩項");
			T(doc.Header.Items[3].List![0] == "nonKanji" && doc.Header.Items[3].List![1] == "chineseDict");
			return null;
		});
		R("無 --- 的文件（只有 ...）表頭為空", async O => {
			// 倉頡表就是這種形狀：沒有 `---`，正文之前只有註釋與 `...`。
			var doc = await Parse("#註釋\n...\n辣\tab\n");
			T(doc.Header.Items.Count == 0, $"表頭應為空，實得 {doc.Header.Items.Count} 項");
			return null;
		});
		R("缺文檔結束標記 `...` 即報錯", async O => {
			// 表頭讀到 EOF 都沒見到 `...` ⇒ 分不清表頭與正文的邊界，故必須報錯而不是猜。
			var 拋了 = false;
			try{
				await Parse("---\nname: dks\n");
			}catch(FormatException ex){
				拋了 = true;
				T(ex.Message.Contains("..."), $"異常應點明缺 `...`，實得: {ex.Message}");
			}
			T(拋了, "缺 `...` 時必須拋 FormatException");
			return null;
		});
		R("忘了 `...` 就寫正文 ⇒ 當場報錯而非吞掉正文", async O => {
			// 正文行會被當表頭行讀（缺冒號）⇒ 報錯；錯誤信息裏要提到「少了 `...`」這條線索。
			var 拋了 = false;
			try{
				await Parse("---\nname: dks\n辣\tryt\n");
			}catch(FormatException ex){
				拋了 = true;
				T(ex.Message.Contains("..."), $"異常應提到缺 `...`，實得: {ex.Message}");
			}
			T(拋了, "正文被當表頭讀時必須拋 FormatException");
			return null;
		});
		R("表頭行缺冒號即報錯", async O => {
			var 拋了 = false;
			try{
				await Parse("---\nname dks\n...\n");
			}catch(FormatException ex){
				拋了 = true;
				T(ex.Message.Contains("冒號"), $"異常應點明缺冒號，實得: {ex.Message}");
			}
			T(拋了, "表頭行缺冒號時必須拋 FormatException");
			return null;
		});
	}
}
