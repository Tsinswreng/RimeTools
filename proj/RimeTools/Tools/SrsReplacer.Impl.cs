namespace RimeTools.Tools;

using System.Text;

public static partial class SrsReplacer{
	public static partial str ApplyAll(str Input, IReadOnlyList<SrRule> Rules){
		var result = Input;
		for(var i = 0; i < Rules.Count; i++){
			result = ReplaceAll(result, Rules[i]);
		}
		return result;
	}

	public static partial str ReplaceAll(str Input, SrRule Rule){
		var pat = SrsPat.Parse(Rule.Pattern);
		var sb = new StringBuilder();
		var pos = 0;
		// step 1: 從 pos 起找下一個匹配(非重疊), 等價 JS replace 帶 g。
		while(pos <= Input.Length){
			var m = SrsMatcher.Find(Input, pos, pat);
			if(m is null){
				break;
			}
			// step 2: 拼接匹配前段 + 替換展開。
			sb.Append(Input, pos, m.Start - pos);
			sb.Append(ExpandReplacement(Rule.Replacement, m));
			pos = m.End;
			if(pos == m.Start){
				pos++; // 零寬匹配保護
			}
		}
		// step 3: 補尾段。
		if(pos <= Input.Length){
			sb.Append(Input, pos, Input.Length - pos);
		}
		return sb.ToString();
	}

	/// 展開 $N / $$。N 越界當空。
	private static str ExpandReplacement(str Replacement, SrsMatch M){
		var sb = new StringBuilder();
		for(var i = 0; i < Replacement.Length; i++){
			var c = Replacement[i];
			if(c == '$' && i + 1 < Replacement.Length){
				var n = Replacement[i + 1];
				if(n == '$'){
					sb.Append('$');
					i++;
					continue;
				}
				if(n is >= '1' and <= '9'){
					var gi = n - '0';
					// Groups[0] 是佔位(null), 組 N 的捕獲在 Groups[N]。
					sb.Append(gi < M.Groups.Count ? M.Groups[gi] ?? "" : "");
					i++;
					continue;
				}
			}
			sb.Append(c);
		}
		return sb.ToString();
	}
}

/// 一次匹配結果: 起止 + 捕獲組文本(0 基, 0=全匹配)。
internal sealed class SrsMatch{
	public required int Start{get;init;}
	public required int End{get;init;}
	public required IReadOnlyList<str?> Groups{get;init;}
}