namespace Dks.Core;

using RimeTools.Shared.Dict.Parser;
using RimeTools.Shared.Freq;

public sealed partial class DksCfg{
	public partial DksCfg(){
		// step 1: 默認路徑集中於此（對應 Dks.sh 的默認 rime_dir / ngaq 原表目錄），
		//         入口（Scripts.Dks）可依參數覆寫 UserDataDir/SrcTableDir。
		UserDataDir = DefaultUserDataDir;
		SrcTableDir = DefaultSrcTableDir;

		// step 2: 默認用公用內存實現；WordFreq 指向 UserDataDir/essay.txt。
		Parser = new DictYamlParser();
		Writer = new DictYamlWriter();
		WordFreq = new EssayWordFreqSource(Path.Combine(UserDataDir, "essay.txt"));
	}
}