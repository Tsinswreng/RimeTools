namespace RimeTools.Tools;

public static partial class Cartesian{
	public static partial IReadOnlyList<IReadOnlyList<str>> Product(IReadOnlyList<IReadOnlyList<str>> Groups){
		var ans = new List<IReadOnlyList<str>>();
		ProductRec(Groups, 0, new List<str>(), ans);
		return ans;
	}

	/// step: 遞歸枚舉——對 Groups[Idx] 的每個元素, 加入前綴後深入下一組。
	private static void ProductRec(
		IReadOnlyList<IReadOnlyList<str>> Groups, int Idx, List<str> Prefix,
		List<IReadOnlyList<str>> Ans){
		if(Idx >= Groups.Count){
			Ans.Add(Prefix.ToList());
			return;
		}
		var group = Groups[Idx];
		if(group.Count == 0){
			return; // 任一分組為空 → 該分支無組合
		}
		for(var i = 0; i < group.Count; i++){
			Prefix.Add(group[i]);
			ProductRec(Groups, Idx + 1, Prefix, Ans);
			Prefix.RemoveAt(Prefix.Count - 1);
		}
	}
}