namespace Dks.Test.Domains.DksCore;

using System.Runtime.CompilerServices;
using RimeTools.Shared.Freq;

/// 測試用的內存詞頻源：按調用方給的順序原樣枚舉（約定傳入時已按頻數降序），不碰文件系統。
/// 例：new MemWordFreq(new WordFreq("辣", 100), new WordFreq("縮", 1))。
public sealed class MemWordFreq:IWordFreqSource{
	private readonly IReadOnlyList<WordFreq> Items;

	public MemWordFreq(params WordFreq[] Items){
		this.Items = Items;
	}

	public async IAsyncEnumerable<WordFreq> Enumerate([EnumeratorCancellation] CT Ct){
		foreach(var it in Items){
			yield return it;
		}
		await Task.CompletedTask;
	}
}
