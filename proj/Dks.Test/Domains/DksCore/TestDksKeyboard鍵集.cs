namespace Dks.Test.Domains.DksCore;

using Dks.Core;
using Tsinswreng.CsTreeTest;

/// 測試三張鍵位表合起來的**鍵集**：dks 碼只允許出現這批鍵。
/// 這條不變式是產出驗證（SvcDks 的 MkDks鍵集 / MkAssert三鍵）的根據，故單獨釘住。
public partial class TestDksKeyboard{
	/// dks 方案的全部鍵位（29 個）：鍵位圖的 q..p / a..l / ; / z..m / , / .。
	private const str 全部鍵 = "qwertyuiopasdfghjkl;zxcvbnm,.";

	public void Register鍵集(ITestNode Node){
		var register = Node.MkTestFnRegister(
			typeof(TestDksKeyboard),
			[typeof(DksKeyboard)],
			[nameof(DksKeyboard.首鍵Of聲母), nameof(DksKeyboard.次鍵Of介腹), nameof(DksKeyboard.末鍵Of尾調)],
			"鍵集"
		);
		var R = register.Register;
		var T = Assert.IsTrue;
		R("三表的值恰為 29 個 dks 鍵", async O => {
			// step 1: 收集三張表（含大小寫）出現過的全部鍵，統一轉小寫。
			var 集 = new HashSet<char>();
			foreach(var k in DksKeyboard.首鍵Of聲母.Values){
				集.Add(char.ToLowerInvariant(k));
			}
			foreach(var k in DksKeyboard.次鍵Of介腹.Values){
				集.Add(char.ToLowerInvariant(k));
			}
			foreach(var k in DksKeyboard.末鍵Of尾調.Values){
				集.Add(char.ToLowerInvariant(k));
			}
			// step 2: 逐個鍵必須在 29 鍵裏（沒有多餘、沒有用錯的符號）。
			foreach(var ch in 集){
				T(全部鍵.Contains(ch), $"鍵位表出現非 dks 的鍵 [{ch}]");
			}
			// step 3: 三表合起來要覆蓋全部 29 鍵（缺鍵會讓某些音節查不到碼）。
			foreach(var ch in 全部鍵){
				T(集.Contains(ch), $"鍵 [{ch}] 不曾被任何一張表用到");
			}
			T(集.Count == 全部鍵.Length, $"鍵集大小應為 {全部鍵.Length}，實為 {集.Count}");
			return null;
		});
		R("表內的值都是單一字符", async O => {
			// 鍵位表的 value 必須是單個字符（三拼碼一位一鍵），否則產出驗證的長度判定就沒意義。
			var 壞 = new List<str>();
			foreach(var kv in DksKeyboard.首鍵Of聲母){
				if(kv.Value.ToString().Length != 1){
					壞.Add($"首鍵:{kv.Key}={kv.Value}");
				}
			}
			foreach(var kv in DksKeyboard.次鍵Of介腹){
				if(kv.Value.ToString().Length != 1){
					壞.Add($"次鍵:{kv.Key}={kv.Value}");
				}
			}
			foreach(var kv in DksKeyboard.末鍵Of尾調){
				if(kv.Value.ToString().Length != 1){
					壞.Add($"末鍵:{kv.Key}={kv.Value}");
				}
			}
			T(壞.Count == 0, "鍵不是單字符: " + string.Join(", ", 壞));
			return null;
		});
	}
}
