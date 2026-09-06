namespace RimeTools.Shared.Dict;

using RimeTools.Tools;

file class DirDoc{
	str Doc =
$"""
#Sum[Rime 詞典(碼表)領域
Rime 輸入法的 dict.yaml 是整個方案的數據載體：解析、寫出、查詢。]
#Descr[
- 模型{nameof(Models.DictLine)} {nameof(Models.RimeDictHeader)} {nameof(Models.RimeDictDoc)}
- 解析/寫出 {nameof(Parser.IDictYamlParser)} {nameof(Parser.IDictYamlWriter)}（文件實現 {nameof(Parser.DictYamlParser)} {nameof(Parser.DictYamlWriter)}）
- 字/詞 → 碼查詢 {nameof(Lookup.ICharCodeLookup)}（內存實現 {nameof(Lookup.MemoryCharCodeLookup)}）
- 行權重是字符串原樣保留("10000%"/"374279")，需要數字時由消費方解析
]
""";
}