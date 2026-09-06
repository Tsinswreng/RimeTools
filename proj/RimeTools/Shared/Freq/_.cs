namespace RimeTools.Shared.Freq;

file class DirDoc{
	str Doc =
$"""
#Sum[詞頻領域
詞頻是造詞的權重來源，以「詞 → 頻率」條目流提供。]
#Descr[
- 模型{nameof(WordFreq)}
- 源接口{nameof(IWordFreqSource)}（當前文件實現 {nameof(EssayWordFreqSource)} 讀 essay.txt，未來可換 CsSql 數據庫實現）
- 條目按頻率降序枚舉，流只能消費一遍
]
""";
}