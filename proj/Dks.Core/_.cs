namespace Dks.Core;

file class DirDoc{
	str Doc =
$"""
#Sum[Dks 輸入法方案
把 ngaq(Dks.ts) + cli-tools RimeTools + Dks.sh 的整條流水線用純 C# 重寫，全部內存運算，AOT 可發布。]
#Descr[
- 配置{nameof(DksCfg)}（路徑 + 可換存儲後端）
- 規則表{nameof(DksRegexRules)}（移植自 TS 源碼與 聲符/義符 txt）
- 七步{nameof(DksSteps)}：SaffesToDkz / UpdateDkzFile / UpdateDks / AttachCangjie / CopyDkpDkz / ToDkn / MkDksPhrase
- 入口{nameof(DksPipeline.Run)}：依序執行全部步驟，等效 Dks.sh
]
""";
}