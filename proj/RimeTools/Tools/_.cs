namespace RimeTools.Tools;

file class DirDoc{
	str Doc =
$"""
#Sum[通用工具
與具體方案無關的純函數工具：文本處理、碼變換、笛卡爾積。]
#Descr[
- 文本拆分 {nameof(TextUtil)}
- 編碼變換 {nameof(CodeTransformer)}（首尾碼等）
- 笛卡爾積 {nameof(Cartesian)}（造詞窮舉發音組合用）
- 註：規則替換不走正則引擎，改為手寫轉換器（見 Dks.Core 規則分析）
]
""";
}