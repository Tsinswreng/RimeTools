namespace RimeTools.Shared.Dict.Parser;

/// dict.yaml 解析器：把碼表文件解析為表頭 + 正文行流。
/// 這是「存儲可換」抽象的一部分：文件解析是純 IO 層，不感知數據庫。
public interface IDictYamlParser{
	/// 解析一個 dict.yaml 文件，返回表頭與正文行流。
	/// 正文行流是惰性的，解析器不預先載入全部行。
	Task<RimeDictDoc> Parse(str Path, CT Ct);
}