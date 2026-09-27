namespace RimeTools.Tools;

/// 回溯匹配器: 對節點序列在輸入串上找第一個匹配, 收集捕獲組。
/// 語義等價 JS `/pattern/g` 的單次匹配: 在串中找「最早起點」的成功匹配;
/// 組內 `(.*?)` 先短後長、`(.*)` 先長後短、交替按分支序; 失敗回溯重試。
internal static class SrsMatcher{
	/// 從 StartPos 起找第一個匹配; 找不到返回 null。
	/// 匹配可只占部分串(除非 pattern 帶 $ 錨在末尾)。
	public static SrsMatch? Find(str Input, int StartPos, SrsPatNode[] Nodes){
		for(var s = StartPos; s <= Input.Length; s++){
			var engine = new Engine(Input, Nodes);
			if(engine.TryAt(s)){
				return new SrsMatch{
					Start = s,
					End = engine.EndPos,
					Groups = engine.Groups.ToArray(),
				};
			}
		}
		return null;
	}

	/// 深度優先回溯匹配器(每個匹配起點新建實例, 內部無狀態洩漏)。
	private sealed class Engine{
		private readonly str _input;
		private readonly SrsPatNode[] _nodes;
		private readonly List<str?> _groups = [];
		private bool _ok;
		private int _endPos;

		public Engine(str Input, SrsPatNode[] Nodes){
			_input = Input;
			_nodes = Nodes;
		}

		public int EndPos => _endPos;
		public IReadOnlyList<str?> Groups => _groups;

		/// 從 pos 開始匹配整棵節點樹; 成功則 _ok=true 且 _endPos 為消費終點。
		public bool TryAt(int Pos){
			_ok = false;
			_groups.Clear();
			_groups.Add(null); // group 0 佔位(整體匹配文本, 替換用不到)
			MatchSeq(0, Pos);
			return _ok;
		}

		/// 匹配 _nodes[ni..]; 全部消費成功 → _ok。
		private void MatchSeq(int Ni, int Pos){
			if(_ok){
				return;
			}
			if(Ni >= _nodes.Length){
				_ok = true;
				_endPos = Pos;
				return;
			}
			switch(_nodes[Ni]){
				case SrsPatLit lit:
					// step: 字面逐字符比對。
					if(Pos + lit.Text.Length <= _input.Length
						&& string.CompareOrdinal(_input, Pos, lit.Text, 0, lit.Text.Length) == 0){
						MatchSeq(Ni + 1, Pos + lit.Text.Length);
					}
					return;
				case SrsPatAnchor an:
					// step: 錨點判定(^ 或 $)。
					if((an.IsStart && Pos == 0) || (!an.IsStart && Pos == _input.Length)){
						MatchSeq(Ni + 1, Pos);
					}
					return;
				case SrsPatAnyChar:
					if(Pos < _input.Length){
						MatchSeq(Ni + 1, Pos + 1);
					}
					return;
				case SrsPatCharset cs:
					if(Pos < _input.Length && cs.Chars.IndexOf(_input[Pos]) >= 0){
						MatchSeq(Ni + 1, Pos + 1);
					}
					return;
				case SrsPatPlus plus:
					MatchPlus(plus, Ni, Pos);
					return;
				case SrsPatGroup grp:
					MatchGroup(grp, Ni, Pos);
					return;
				case SrsPatAlt alt:
					MatchAlt(alt, Ni, Pos);
					return;
			}
		}

		/// `+`: 枚舉 1..最大次數(由多到少, 失敗回溯遞減)。
		private void MatchPlus(SrsPatPlus Plus, int Ni, int Pos){
			var max = MaxRepeat(Pos, Plus.Unit);
			for(var n = max; n >= 1; n--){
				MatchSeq(Ni + 1, Pos + n);
				if(_ok){
					return;
				}
			}
		}

		private int MaxRepeat(int Pos, SrsPatNode Unit){
			// 規則中 `+` 僅作用於單字符(字符集或單字面, 如 `;+`); 返回可重複的最大次數。
			if(Unit is SrsPatCharset cs){
				var n = 0;
				while(Pos + n < _input.Length && cs.Chars.IndexOf(_input[Pos + n]) >= 0){
					n++;
				}
				return n;
			}
			if(Unit is SrsPatLit { Text.Length: 1 } lit){
				var ch = lit.Text[0];
				var n = 0;
				while(Pos + n < _input.Length && _input[Pos + n] == ch){
					n++;
				}
				return n;
			}
			return 0;
		}

		/// 捕獲組: 枚舉組的候選終點, 每種都記錄捕獲後繼續後續節點。
		private void MatchGroup(SrsPatGroup Grp, int Ni, int Pos){
			// step 1: 若組體是單一任意串 → 枚舉長度(非貪婪由短到長, 貪婪由長到短)。
			if(Grp.Body.Length == 1 && Grp.Body[0] is SrsPatAnyStr any){
				if(any.NonGreedy){
					for(var len = 0; len <= _input.Length - Pos; len++){
						TryGroupEnd(Grp.GroupIdx, Pos, Pos + len, Ni);
						if(_ok){
							return;
						}
					}
				}
				else{
					for(var len = _input.Length - Pos; len >= 0; len--){
						TryGroupEnd(Grp.GroupIdx, Pos, Pos + len, Ni);
						if(_ok){
							return;
						}
					}
				}
				return;
			}

			// step 2: 組體為字面/單字符/字符集序列 → 確定性消費, 單一終點。
			var end = ConsumeSeq(Grp.Body, Pos);
			if(end >= 0){
				TryGroupEnd(Grp.GroupIdx, Pos, end, Ni);
			}
		}

		/// 組結束於 GroupEnd: push 捕獲 → 匹配後續 → 失敗則 pop(回溯)。
		private void TryGroupEnd(int GroupIdx, int GroupStart, int GroupEnd, int Ni){
			var text = _input.Substring(GroupStart, GroupEnd - GroupStart);
			// 保證 _groups 有 GroupIdx 位。
			while(_groups.Count <= GroupIdx){
				_groups.Add(null);
			}
			var prev = _groups[GroupIdx];
			_groups[GroupIdx] = text;
			MatchSeq(Ni + 1, GroupEnd);
			if(!_ok){
				_groups[GroupIdx] = prev; // 回溯還原
			}
		}

		/// 線性消費若干「組內節點」(lit/anychar/charset), 返回終點; 失敗返回 -1。
		private int ConsumeSeq(SrsPatNode[] Body, int Pos){
			var p = Pos;
			foreach(var bn in Body){
				switch(bn){
					case SrsPatLit lit:
						if(p + lit.Text.Length > _input.Length
							|| string.CompareOrdinal(_input, p, lit.Text, 0, lit.Text.Length) != 0){
							return -1;
						}
						p += lit.Text.Length;
						break;
					case SrsPatAnyChar:
						if(p >= _input.Length){
							return -1;
						}
						p++;
						break;
					case SrsPatCharset cs:
						if(p >= _input.Length || cs.Chars.IndexOf(_input[p]) < 0){
							return -1;
						}
						p++;
						break;
					default:
						return -1;
				}
			}
			return p;
		}

		/// 交替捕獲 `(a|b|c)`: 按分支序嘗試各分支; 命中的分支文本即該組捕獲。
		/// 分支內不允許再嵌套捕獲/任意串(規則實測如此)。
		private void MatchAlt(SrsPatAlt Alt, int Ni, int Pos){
			foreach(var branch in Alt.Branches){
				var end = ConsumeSeq(branch, Pos);
				if(end >= 0){
					TryGroupEnd(Alt.GroupIdx, Pos, end, Ni);
					if(_ok){
						return;
					}
				}
			}
		}
	}
}