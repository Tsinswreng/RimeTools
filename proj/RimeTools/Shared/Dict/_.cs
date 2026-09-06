namespace RimeTools.Shared.Dict;

file class DirDoc{
	str Doc =
$"""
#Sum[Rime 詞典(碼表)領域
Rime 輸入法的 dict.yaml 是整個方案的數據載體：解析、寫出、查詢。]
#Descr[
- dict.yaml 是「文檔型」數據，不是固定 schema 的關係型：表頭鍵不固定（HeaderItem 保序保留，值可為標量或列表），正文行列數不固定（Tab 分隔，語義由 columns 定義）
- 模型{nameof(Models.HeaderItem)} {nameof(Models.IDictLine)} {nameof(Models.DictLine)} {nameof(Models.RimeDictHeader)} {nameof(Models.RimeDictDoc)}（便捷器{nameof(Models.DictLineExtn)}）
- 正文行 = 文檔對象(IDictLine:列名→值字典,jsonb 式);表頭 = 保序鍵值(RimeDictHeader)
- 解析/寫出 {nameof(Parser.IDictYamlParser)} {nameof(Parser.IDictYamlWriter)}（文件實現 {nameof(Parser.DictYamlParser)} {nameof(Parser.DictYamlWriter)}）
- 字/詞 → 碼查詢 {nameof(Lookup.ICharCodeLookup)}（內存實現 {nameof(Lookup.MemoryCharCodeLookup)}）
- 行權重是字符串原樣保留("10000%"/"374279")，需要數字時由消費方解析
- 例：dks.dict.yaml 表頭含 name/version/sort/use_preset_vocabulary/import_tables；正文「辣	ryt	10000%」三列
]
""";
}