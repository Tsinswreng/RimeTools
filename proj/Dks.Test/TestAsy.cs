using System.Runtime.CompilerServices;

namespace Dks.Test;

/// 測試用 async 流工具(不引 System.Linq.Async 包, 避免與 BCL 撞名)。
public static class TestAsy{
	/// 把同步序列轉成 IAsyncEnumerable(測試構造輸入用)。
	public static async IAsyncEnumerable<T> ToAsy<T>(IEnumerable<T> Items){
		foreach(var it in Items){
			yield return it;
		}
		await Task.CompletedTask;
	}

	/// 取異步流首個元素; 空流返回 null。
	public static async Task<T?> FirstOrNullAsync<T>(IAsyncEnumerable<T> Src) where T:class{
		await foreach(var it in Src){
			return it;
		}
		return null;
	}
}