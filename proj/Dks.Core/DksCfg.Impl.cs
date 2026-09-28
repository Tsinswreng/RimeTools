namespace Dks.Core;

using RimeTools.Shared.Dict.Parser;

public sealed partial class DksCfg{
	public partial DksCfg(){
		// step 1: 默認策略：純文本解析/寫出（只認 reader/writer，不碰文件系統）。
		Parser = new DictYamlParser();
		Writer = new DictYamlWriter();
		// step 2: 詞頻源不在此給默認值——庫不知道路徑，由端點按自己的目錄約定注入
		//         （見 DksCfg.WordFreq 的文檔：只有造詞與按頻擇源兩步會用到它）。
	}
}
