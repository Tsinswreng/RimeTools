namespace RimeTools.Shared.Dict.Parser;

using RimeTools.Shared.Dict.Models;

/// IDictYamlWriter 的默認文件實現：把表頭與正文行流寫成 Rime dict.yaml。
public partial class DictYamlWriter : IDictYamlWriter{
	/// 寫出一個 dict.yaml 文件。目標目錄不存在時自動創建。
	public partial Task<nil> Write(str Path, RimeDictHeader Header, IAsyncEnumerable<DictLine> Body, CT Ct);
}