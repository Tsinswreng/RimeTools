namespace RimeTools.Shared.Dict.Parser;

/// dict.yaml 寫出器：把表頭與正文行流寫成 Rime 碼表文件。
public interface IDictYamlWriter{
	/// 寫出一個 dict.yaml 文件。Header 寫為表頭，Body 逐條寫為正文行。
	Task<nil> Write(str Path, RimeDictHeader Header, IAsyncEnumerable<DictLine> Body, CT Ct);
}