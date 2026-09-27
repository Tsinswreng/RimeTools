namespace Dks.Core;

public static partial class Dkp拼式Parser{
	public static partial Tswg上古漢語音節 Parse(str 拼式){
		var 音節 = new Tswg上古漢語音節();
		var 拼 = (拼式 ?? "").Trim();
		if(拼.Length == 0){
			return 音節;
		}
		var 義符表 = DksRegexRules.YiFu();
		var 聲符表 = DksRegexRules.ShengFu();
		var 聲母 = "";
		var 韻 = 拼;

		// step 1: 首字是義符字形 → 查得聲母，去掉首字後其餘整段就是韻。
		if(義符表.TryGetValue(拼[0].ToString(), out var 義符音)){
			聲母 = Normalize(義符音);
			韻 = 拼[1..];
		}

		// step 2: 韻段首字是聲符字形 → 用聲符表換成音（介腹+尾調），其後字符原樣接上。
		if(韻.Length > 0 && 聲符表.TryGetValue(韻[0].ToString(), out var 聲符音)){
			韻 = Normalize(聲符音) + 韻[1..];
		}

		// step 3: 仍無聲母（拼式整串就是音的場合）→ 取「最短可行」的前綴當聲母。
		//   短的優先令介音 r 歸介腹：rˁat ⇒ 聲母空、介腹 rˁa（三拼 ryt，與 dks 表一致）；
		//   ŋˁak 因介腹 ŋˁa 不在表內，故退到聲母 ŋ、介腹 ˁa（三拼 wzk，亦與 dks 表一致）。
		if(聲母.Length == 0){
			for(var i = 0; i <= 韻.Length; i++){
				var 前 = Normalize(韻[..i]);
				if(!DksKeyboard.首鍵Of聲母.ContainsKey(前)){
					continue;
				}
				var (介腹Try, 尾調Try) = 切韻(韻[i..]);
				if(DksKeyboard.次鍵Of介腹.ContainsKey(介腹Try) && DksKeyboard.末鍵Of尾調.ContainsKey(尾調Try)){
					聲母 = 前;
					韻 = 韻[i..];
					break;
				}
			}
		}

		// step 4: 切韻 → 填三段；Full 為三段相接（音節模型的正規拼式）。
		var (介腹, 尾調) = 切韻(韻);
		音節.聲母 = 聲母;
		音節.介腹 = 介腹;
		音節.尾調 = 尾調;
		音節.Full = 聲母 + 介腹 + 尾調;
		return 音節;
	}

	private static partial (str 介腹, str 尾調) 切韻(str 韻){
		var 原 = 韻;
		var 界 = -1;
		for(var i = 0; i < 原.Length; i++){
			if(元音集.Contains(原[i])){
				界 = i;
			}
		}
		if(界 < 0){
			// 沒有元音（如只剩一串輔音）：整段當介腹，尾調留空。
			return (Normalize(原), "");
		}
		return (Normalize(原[..(界 + 1)]), Normalize(原[(界 + 1)..]));
	}

	private static partial str Normalize(str 音){
		if(string.IsNullOrEmpty(音)){
			return "";
		}
		var s = 音.Trim();
		// 非三等標記：源表用 ˤ/ˁ，音節模型統一用 '。
		s = s.Replace("ˤ", "'");
		s = s.Replace("ˁ", "'");
		// 圓脣與送氣並存時，先送氣後圓脣。
		s = s.Replace("ʷʰ", "ʰʷ");
		// 清化一律用後置 ʰ，不用清化符。
		s = s.Replace("n̥", "nʰ");
		s = s.Replace("m̥", "mʰ");
		s = s.Replace("l̥", "lʰ");
		s = s.Replace("ŋ̊", "ŋʰ");
		// 濁塞音一律寫 ɡ。
		s = s.Replace("g", "ɡ");
		return s;
	}
}
