namespace RimeTools.Shared.Phrase;

using RimeTools.Shared.Dict.Lookup;
using RimeTools.Shared.Freq;

file class DirDoc{
	str Doc =
$"""
#Sum[造詞域
把「詞頻 × 碼表反查 × 造詞策略」組合成簡碼詞條，是 dks_phrase 造詞的公用內核（移植自 cli-tools 的 MkPhrase/PhraseMker）。]
#Descr[
- 詞頻源 {nameof(IWordFreqSource)}：essay.txt 等，按頻降序
- 反查 {nameof(ICharCodeLookup)}：字/詞 → 全部碼
- 策略接口 {nameof(IPhraseMkr)}：一組每字碼 → 詞碼片段；默認 {nameof(PhraseMkr_HeadEtTail)} 取每字首尾
- 造詞器 {nameof(PhraseMaker)}：詞頻 × 反查 × 策略 → 詞條流（text/code/weight）
]
""";
}