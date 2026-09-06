namespace RimeTools.Tools;

file class DirDoc{
	str Doc =
$"""
#Sum[通用工具
與具體方案無關的純函數工具：正則替換、文本處理、碼變換。]
#Descr[
- 正則規則對 {nameof(RegexReplacePair)} 與替換器 {nameof(RegexReplacer)}
- 文本拆分 {nameof(TextUtil)}
- 編碼變換 {nameof(CodeTransformer)}（首尾碼等）
]
""";
}