namespace Dks.Core;

using System.Text;

public static partial class 布之道拼式Parser{
	/// 元音集（判「前一個字符是元音」用）：含布之道會用的 ǝ，故一併列出。
	private const str 元音集 = "aeiouəǝæA";

	public static partial Tswg上古漢語音節 Parse(str 布之道Spelling){
		// 適配後就是本方案的「純音」串（無義符字形），故直接交給 Dkp拼式Parser 的純音切分那條路。
		return Dkp拼式Parser.Parse(Adapt(布之道Spelling));
	}

	public static partial str Adapt(str 拼式){
		if(string.IsNullOrEmpty(拼式)){
			return "";
		}
		var 原 = 拼式.Trim();

		// step 1+2: 一遍掃描——() 連內容整段丟掉；[] 只丟括號、內容保留。
		var buf = new StringBuilder(原.Length);
		var 圓括號內 = 0;
		foreach(var ch in 原){
			if(ch == '('){
				圓括號內++;
				continue;
			}
			if(ch == ')'){
				if(圓括號內 > 0){
					圓括號內--;
				}
				continue;
			}
			if(ch == '[' || ch == ']'){
				continue;
			}
			if(圓括號內 > 0){
				continue;
			}
			buf.Append(ch);
		}
		var s = buf.ToString();

		// step 3: 末尾 h 是 msoeg 的去聲標記，本方案用 s 韻尾。
		if(s.EndsWith("h")){
			s = s[..^1] + "s";
		}

		// step 4: 清化符是組合字符（跟在被清化的音之後），換成本方案的後置 ʰ。
		//         組合下環 U+0325 與組合上環 U+030A 都是 msoeg 的清化寫法。
		//         這只是記法改寫，清化信息原樣保留：w̥ 只寫成 wʰ，不歸併成 hʷ。
		s = s.Replace("̥", "ʰ");
		s = s.Replace("̊", "ʰ");

		// step 5: ɫ → j；ɬ 原樣保留（轉三拼時走 s 鍵）。
		s = s.Replace("ɫ", "j");

		// step 6: ǝ → ə（同一個音的另一種寫法）。
		s = s.Replace("ǝ", "ə");

		// step 7: 元音後的 i/u 改寫成半元音 j/w（本方案的韻尾寫法）。
		s = Mk韻尾半元音(s);

		// step 8: 聲母段緊跟輔音的 w 是圓脣化，本方案寫上標 ʷ（kwrˤaŋʔ → kʷrˤaŋʔ = 礦）。
		//         msoeg 的 hl（清化流音）、ʍ／w̥（清化圓脣半元音）一律原樣保留：
		//         本步只做記法轉換，不做歸併；它們落哪個鍵是「我的擬音→Dks」那一步的事。
		return Mk圓脣上標(s);
	}

	/// 把「聲母段」裏緊跟在輔音後的 w 改成上標 ʷ；遇到第一個元音之後就停手（韻尾的 w 不動）。
	/// 例：kwrˤaŋʔ → kʷrˤaŋʔ；ˁuw → ˁuw（韻尾 w 保留）；w（單獨作聲母）→ w。
	private static partial str Mk圓脣上標(str 音){
		var buf = new StringBuilder(音.Length);
		var 已入韻 = false;
		for(var i = 0; i < 音.Length; i++){
			var ch = 音[i];
			if(元音集.Contains(ch)){
				已入韻 = true;
			}
			if(!已入韻 && ch == 'w' && i > 0){
				buf.Append('ʷ');
				continue;
			}
			buf.Append(ch);
		}
		return buf.ToString();
	}

	/// 把「緊跟在元音之後」的 i/u 改寫成 j/w：tˤui→tˤuj、dˤiuk→dˤiwk、ɡiu→ɡiw。
	/// 只認前一個字符是元音的情況，故主元音 i/u（前面是輔音或音節首）不會被動到。
	private static partial str Mk韻尾半元音(str 音){
		var buf = new StringBuilder(音.Length);
		for(var i = 0; i < 音.Length; i++){
			var ch = 音[i];
			var 前是元音 = i > 0 && 元音集.Contains(音[i - 1]);
			if(前是元音 && ch == 'i'){
				buf.Append('j');
				continue;
			}
			if(前是元音 && ch == 'u'){
				buf.Append('w');
				continue;
			}
			buf.Append(ch);
		}
		return buf.ToString();
	}
}
