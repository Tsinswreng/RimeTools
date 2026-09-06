namespace RimeTools.Shared.Dict.Models;

/// dict.yaml 正文的一行：詞/字、編碼、權重。
/// 權重原樣保留字符串，可能是 "10000%"（dks 表頭）或 "374279"（phrase 頻率）等格式，需要數字時由消費方解析。
public sealed record DictLine(
	/// 詞或字（第一列）。
	str Text,
	/// 編碼（第二列）。
	str Code,
	/// 權重（第三列），可缺省。
	str? Weight
);