namespace RimeTools.Shared.Dict.Lookup;

using RimeTools.Shared.Dict.Models;

public partial class MemoryCharCodeLookup : ICharCodeLookup{
	public static partial async Task<MemoryCharCodeLookup> FromDocAsync(RimeDictDoc Doc, CT Ct){
		var lookup = new MemoryCharCodeLookup();
		// step 1: 消費一遍 Body 流，按 text 聚合全部 code。
		await foreach(var line in Doc.Body.WithCancellation(Ct)){
			var text = line.TryGetValue(DictColumns.Text, out var t) ? t as str : null;
			var code = line.TryGetValue(DictColumns.Code, out var c) ? c as str : null;
			if(string.IsNullOrEmpty(text) || string.IsNullOrEmpty(code)){
				continue;
			}
			if(!lookup._Codes.TryGetValue(text!, out var list)){
				list = new List<str>();
				lookup._Codes[text!] = list;
			}
			list.Add(code!);
		}
		return lookup;
	}

	public partial IReadOnlyList<str> GetCodes(str Text){
		if(_Codes.TryGetValue(Text, out var list)){
			return list;
		}
		return [];
	}
}