namespace RimeTools.Shared.Dict.Parser;

using RimeTools.Shared.Dict.Models;

/// IDictYamlParser 的默認文件實現：解析 Rime dict.yaml（表頭 `---`/`...` 段 + 表格正文）。
/// 正文以惰性流返回，不預先載入全部行。
public partial class DictYamlParser : IDictYamlParser{
	/// 解析一個 dict.yaml 文件。文件不存在或格式錯誤時拋出異常。
	public partial Task<RimeDictDoc> Parse(str Path, CT Ct);
}