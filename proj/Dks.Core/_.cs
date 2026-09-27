namespace Dks.Core;

using RimeTools.Shared.Dict.Parser;
using RimeTools.Shared.Freq;

file class DirDoc{
	str Doc =
$"""
#Sum[Dks 輸入法方案
把 ngaq(Dks.ts) + cli-tools RimeTools + Dks.sh 的整條流水線用純 C# 重寫，全部內存運算，AOT 可發布。]
#Descr[
- 入口：{nameof(Svc.ISvcDks)}（七步）——Svc 方法首參一律 IFnCtx，配置 DksCfg 由實現構造注入
- 配置{nameof(DksCfg)}（路徑 + 可換後端 Parser/Writer/WordFreq）
- 規則表{nameof(DksRegexRules)}（移植自 TS 源碼與 聲符/義符 txt，內嵌資源）
- 七步：SaffesToDkz / UpdateDkzFile / UpdateDks / AttachCangjie / CopyDkpDkz / ToDkn / MkDksPhrase
- 造詞內核在公用 RimeTools.Shared.Phrase.PhraseMaker（詞頻 × 反查 × 策略）
]
""";
}