namespace RimeTools.Shared.Dict.Models;

public sealed partial class RimeDictHeader{
	public partial RimeDictHeader(IReadOnlyList<HeaderItem> Items){
		_Items = Items;
	}

	public partial IReadOnlyList<HeaderItem> Items => _Items;

	public partial str? GetScalar(str Key){
		// step 1: 找首個同鍵且為標量型的項。
		for(var i = 0; i < _Items.Count; i++){
			var it = _Items[i];
			if(it.Key == Key && it.Scalar is not null){
				return it.Scalar;
			}
		}
		return null;
	}

	public partial IReadOnlyList<str>? GetList(str Key){
		// step 1: 找首個同鍵且為列表型的項。
		for(var i = 0; i < _Items.Count; i++){
			var it = _Items[i];
			if(it.Key == Key && it.List is not null){
				return it.List;
			}
		}
		return null;
	}

	public partial str Name => GetScalar("name") ?? "";
	public partial str Version => GetScalar("version") ?? "";
	public partial str? Sort => GetScalar("sort");
	public partial IReadOnlyList<str>? Columns => GetList("columns");
	public partial bool? UsePresetVocabulary => GetScalar("use_preset_vocabulary") is null ? null : bool.Parse(GetScalar("use_preset_vocabulary")!);
	public partial IReadOnlyList<str>? ImportTables => GetList("import_tables");
}