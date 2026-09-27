namespace RimeTools.Tools;

/// 解析後的 pattern 節點樹與匹配器(手寫、無 System.Text.RegularExpressions)。
/// 支持語法面見 SrRule 註釋; 匹配採用回溯, 輸入為單個碼串(長度有限), 性能可接受。

/// pattern 節點基類。
internal abstract class SrsPatNode;

/// 字面段: 需精確匹配。
internal sealed class SrsPatLit(str Text):SrsPatNode{
	public str Text{get;} = Text;
}

/// 錨點: 串首 `^` 或串尾 `$`。
internal sealed class SrsPatAnchor(bool IsStart):SrsPatNode{
	public bool IsStart{get;} = IsStart;
}

/// 單字符通配 `.`。
internal sealed class SrsPatAnyChar:SrsPatNode;

/// 字符集 `[...]`(純列舉)。
internal sealed class SrsPatCharset(str Chars):SrsPatNode{
	public str Chars{get;} = Chars;
}

/// 捕獲組: 組內子節點序列 + 捕獲編號; 組內首節點若是 AnyStr 表示 `(.*?)`。
internal sealed class SrsPatGroup(SrsPatNode[] Body, int GroupIdx):SrsPatNode{
	public SrsPatNode[] Body{get;} = Body;
	public int GroupIdx{get;} = GroupIdx;
}

/// 交替捕獲 `(a|b|c)`(組內含 `|`): 按分支順序嘗試。
internal sealed class SrsPatAlt(SrsPatNode[][] Branches, int GroupIdx):SrsPatNode{
	public SrsPatNode[][] Branches{get;} = Branches;
	public int GroupIdx{get;} = GroupIdx;
}

/// 任意串 `(.*)` / `(.*?)`: 作為捕獲組內唯一節點; NonGreedy 控制先短後長。
internal sealed class SrsPatAnyStr(bool NonGreedy):SrsPatNode{
	public bool NonGreedy{get;} = NonGreedy;
}

/// `+` 量詞(規則中僅 `;+`): 前一單元一次以上。
internal sealed class SrsPatPlus(SrsPatNode Unit):SrsPatNode{
	public SrsPatNode Unit{get;} = Unit;
}

/// pattern 解析器(遞歸下降)。
internal static partial class SrsPat{
	public static SrsPatNode[] Parse(str Pattern){
		var p = new SrsPatParser(Pattern);
		var nodes = p.ParseTop();
		return nodes;
	}

	private sealed class SrsPatParser{
		private readonly str _src;
		private int _pos;
		private int _groupCount;

		public SrsPatParser(str Src){
			_src = Src;
		}

		/// 解析頂層(或捕獲組內)節點序列; stopAtParen 為 true 時遇 ')' 停止。
		private List<SrsPatNode> ParseSeq(bool StopAtParen){
			var list = new List<SrsPatNode>();
			while(_pos < _src.Length){
				var c = _src[_pos];
				if(c == ')'){
					if(StopAtParen){
						return list;
					}
					throw new FormatException($"意外的 ')' 在 {_pos}");
				}
				if(c == '|'){
					// 交替在 ParseGroup 層處理, 此處不應裸奔。
					throw new FormatException($"意外的 '|' 在 {_pos}");
				}
				if(c == '^'){
					list.Add(new SrsPatAnchor(true));
					_pos++;
					continue;
				}
				if(c == '$'){
					list.Add(new SrsPatAnchor(false));
					_pos++;
					continue;
				}
				if(c == '.'){
					if(_pos + 1 < _src.Length && _src[_pos + 1] == '*'){
						var nonGreedy = _pos + 2 < _src.Length && _src[_pos + 2] == '?';
						list.Add(new SrsPatAnyStr(nonGreedy));
						_pos += nonGreedy ? 3 : 2;
						continue;
					}
					list.Add(new SrsPatAnyChar());
					_pos++;
					continue;
				}
				if(c == '['){
					var cs = ParseCharset();
					// 後跟 '+' → 一次以上(僅 `;+` 一處)。
					if(_pos < _src.Length && _src[_pos] == '+'){
						list.Add(new SrsPatPlus(cs));
						_pos++;
					}
					else{
						list.Add(cs);
					}
					continue;
				}
				if(c == '+'){
					// step: 量詞作用於前一節點(規則僅 `;+` 處出現; 前一節點是字面或字符集)。
					if(list.Count == 0){
						throw new FormatException($"'+' 前缺少單位在 {_pos}");
					}
					list[^1] = new SrsPatPlus(list[^1]);
					_pos++;
					continue;
				}
				if(c == '('){
					list.Add(ParseGroup());
					continue;
				}
				if(c == '\\'){
					// 規則實測無轉義; 保守把 \x 視為字面 x。
					_pos++;
					if(_pos < _src.Length){
						AppendLit(list, _src[_pos].ToString());
						_pos++;
					}
					continue;
				}
				// 字面: 累積非特殊字符。
				var sb = new System.Text.StringBuilder();
				while(_pos < _src.Length && "()[]^$.*|+\\".IndexOf(_src[_pos]) < 0){
					sb.Append(_src[_pos]);
					_pos++;
				}
				AppendLit(list, sb.ToString());
			}
			return list;
		}

		private static void AppendLit(List<SrsPatNode> List, str Text){
			if(Text.Length == 0){
				return;
			}
			// 與前一字面節點合併, 減少節點數。
			if(List.Count > 0 && List[^1] is SrsPatLit last){
				List[^1] = new SrsPatLit(last.Text + Text);
			}
			else{
				List.Add(new SrsPatLit(Text));
			}
		}

		private SrsPatCharset ParseCharset(){
			_pos++; // 跳 '['
			var sb = new System.Text.StringBuilder();
			while(_pos < _src.Length && _src[_pos] != ']'){
				sb.Append(_src[_pos]);
				_pos++;
			}
			if(_pos >= _src.Length){
				throw new FormatException($"未閉合字符集: {_src}");
			}
			_pos++; // 跳 ']'
			return new SrsPatCharset(sb.ToString());
		}

		private SrsPatNode ParseGroup(){
			_pos++; // 跳 '('
			_groupCount++;
			var idx = _groupCount;

			// step 1: 解析組內序列; 遇 '|' 表示交替。
			var branches = new List<SrsPatNode[]>();
			var cur = new List<SrsPatNode>();
			while(true){
				if(_pos >= _src.Length){
					throw new FormatException($"未閉合捕獲組: {_src}");
				}
				if(_src[_pos] == ')'){
					_pos++;
					break;
				}
				if(_src[_pos] == '|'){
					branches.Add(cur.ToArray());
					cur = new List<SrsPatNode>();
					_pos++;
					continue;
				}
				var sub = ParseOneInGroup();
				cur.AddRange(sub);
			}
			branches.Add(cur.ToArray());

			if(branches.Count > 1){
				return new SrsPatAlt(branches.ToArray(), idx);
			}
			var body = branches[0];
			return new SrsPatGroup(body, idx);
		}

		/// 組內解析一個「單位」(字面段/字符集/任意串/字面單元+?)。返回節點序列(可能多個字面節點合併)。
		private SrsPatNode[] ParseOneInGroup(){
			var c = _src[_pos];
			if(c == '^'){
				_pos++;
				return [new SrsPatAnchor(true)];
			}
			if(c == '$'){
				_pos++;
				return [new SrsPatAnchor(false)];
			}
			if(c == '.'){
				if(_pos + 1 < _src.Length && _src[_pos + 1] == '*'){
					var nonGreedy = _pos + 2 < _src.Length && _src[_pos + 2] == '?';
					_pos += nonGreedy ? 3 : 2;
					return [new SrsPatAnyStr(nonGreedy)];
				}
				_pos++;
				return [new SrsPatAnyChar()];
			}
			if(c == '['){
				var cs = ParseCharset();
				return [cs];
			}
			// 字面: 累積直到組結束/交替/特殊。
			var sb = new System.Text.StringBuilder();
			while(_pos < _src.Length && "()[]^$.*|+\\".IndexOf(_src[_pos]) < 0){
				sb.Append(_src[_pos]);
				_pos++;
			}
			return [new SrsPatLit(sb.ToString())];
		}

		public SrsPatNode[] ParseTop(){
			return ParseSeq(false).ToArray();
		}
	}
}