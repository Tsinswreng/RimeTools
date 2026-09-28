namespace RimeTools.Shared.Dict.Parser;

using System.Runtime.CompilerServices;
using System.Text;
using RimeTools.Shared.Dict.Models;

public partial class DictYamlParser : IDictYamlParser{
	public partial async Task<RimeDictDoc> Parse(TextReader Reader, CT Ct){
		// step 1: 讀表頭段（`---` 到 `...`）；讀完後 Reader 恰好停在正文第一行之前。
		var items = await ReadHeaderAsync(Reader, Ct);

		// step 2: 確定正文列名：Header.Columns 或缺省 text/code/weight（DictColumns.Default）。
		var header = new RimeDictHeader(items);
		var colNames = header.Columns ?? DictColumns.Default;

		// step 3: 返回表頭 + 惰性正文流（續讀同一個 Reader，不重開文件）。
		return new RimeDictDoc(header, BodyLinesAsync(Reader, colNames, Ct));
	}

	/// 讀表頭，返回保序鍵值項。
	/// 支援兩種真實文件形狀：
	///   - 標準：`#註釋` … `---` …鍵值… `...`
	///   - 無表頭（如 cangjie 表）：`#註釋` … `...`（`---` 缺失，表頭為空）
	/// 走到 EOF 仍沒見到 `...` ⇒ 拋 FormatException（Rime 碼表必須有文檔結束標記，否則正文邊界不明）。
	private async Task<IReadOnlyList<HeaderItem>> ReadHeaderAsync(TextReader Reader, CT Ct){
		var items = new List<HeaderItem>();
		var inHeader = false;
		str? curListKey = null;
		var curList = new List<str>();
		var 見文檔尾 = false;

		// step 1: 逐行掃描；註釋行跳過。
		str? line;
		while((line = await Reader.ReadLineAsync(Ct)) is not null){
			Ct.ThrowIfCancellationRequested();
			var trimmed = line.Trim();
			if(trimmed.StartsWith("#", StringComparison.Ordinal)){
				continue;
			}
			if(!inHeader){
				// step 2: 遇 `...` 直接結束（無表頭文件）；遇 `---` 進入表頭。
				if(trimmed == "..."){
					見文檔尾 = true;
					break;
				}
				if(trimmed == "---"){
					inHeader = true;
				}
				continue;
			}
			if(trimmed == "..."){
				見文檔尾 = true;
				break; // 表頭結束
			}
			if(trimmed.StartsWith("- ", StringComparison.Ordinal)){
				// step 3: 列表項歸入上一鍵的列表。
				curList.Add(MkUnquote(trimmed[2..].Trim()));
				continue;
			}
			// step 4: 遇到新鍵時，先落盤上一懸空列表鍵（若存在）。
			if(curListKey is not null){
				items.Add(new HeaderItem(curListKey, null, curList));
				curListKey = null;
				curList = new List<str>();
			}
			// step 5: 解析 `鍵: 值`。
			var colonIdx = line.IndexOf(':');
			if(colonIdx < 0){
				throw new FormatException(
					$"dict.yaml 表頭行無法解析(缺冒號): {line}"
					+ "；若這一行其實是正文，說明表頭少了結束標記 `...`（正文被當成表頭讀了）");
			}
			var key = line[..colonIdx].Trim();
			var rawVal = line[(colonIdx + 1)..].Trim();
			if(rawVal.Length == 0){
				// 值空（`鍵:` 後面甚麼都沒有）→ 懸空鍵，可能下接 `- ` 列表項。
				curListKey = key;
				curList = new List<str>();
			}
			else{
				// 有值即標量。`""` 是「空串標量」，與「沒有值」是兩回事——
				// 故懸空判定必須在去引號之前、按原文判（否則 `version: ""` 會被當成懸空鍵）。
				items.Add(new HeaderItem(key, MkUnquote(rawVal), null));
			}
		}
		if(!見文檔尾){
			throw new FormatException("dict.yaml 缺文檔結束標記 `...`");
		}
		// step 6: 收尾最後一個懸空列表鍵。
		if(curListKey is not null){
			items.Add(new HeaderItem(curListKey, null, curList));
		}
		return items;
	}

	/// 還原 YAML 雙引號標量（與 DictYamlWriter.MkScalar 對稱）。
	/// 寫出器會給空串與含特殊字符的值加雙引號，故讀回時必須去掉引號並還原轉義，
	/// 否則 `version: ""` 讀回來會是兩個引號字符本身（表頭值與寫出前不一致）。
	/// 例：`""` ⇒ 空串；`"a:b"` ⇒ `a:b`；`"he said \"hi\""` ⇒ `he said "hi"`；`dks` ⇒ `dks`（無引號原樣）。
	private static str MkUnquote(str Value){
		var v = Value ?? "";
		if(v.Length < 2 || v[0] != '"' || v[^1] != '"'){
			return v;
		}
		var inner = v[1..^1];
		var sb = new StringBuilder(inner.Length);
		for(var i = 0; i < inner.Length; i++){
			if(inner[i] != '\\' || i + 1 >= inner.Length){
				sb.Append(inner[i]);
				continue;
			}
			i++;
			var esc = inner[i];
			if(esc == 'n'){
				sb.Append('\n');
			}
			else if(esc == 't'){
				sb.Append('\t');
			}
			else{
				// `\"` 與 `\\` 還原成字符本身；其餘未知轉義原樣保留（不猜）。
				sb.Append(esc);
			}
		}
		return sb.ToString();
	}

	/// 正文行流：續讀同一個 Reader（表頭已消費到 `...` 之後），逐行按 Tab 拆列並以列名命名。
	private async IAsyncEnumerable<IDictLine> BodyLinesAsync(
		TextReader Reader, IReadOnlyList<str> ColNames,
		[EnumeratorCancellation] CT Ct){
		str? line;
		while((line = await Reader.ReadLineAsync(Ct)) is not null){
			Ct.ThrowIfCancellationRequested();
			var trimmed = line.Trim();
			if(trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal)){
				continue; // 空行/註釋行
			}
			// step 1: 文檔分隔行不是數據（防某些表尾殘留 `...`）。
			if(trimmed == "..." || trimmed == "---"){
				continue;
			}
			var cells = trimmed.Split('\t');
			// step 2: 逐格以列名收進字典；格子比列名多 → 忽略尾格。
			//         行內註釋截斷——某列含 `#` 時取 `#` 前部分(trim)，其後註釋丟棄。
			//         對齊 ngaq 的 removeSingleLineComments（權重 10000% 無 #，`0% # 舍他音`→`0%`、`# ts`→空）。
			var n = Math.Min(cells.Length, ColNames.Count);
			var doc = new DictLine();
			for(var i = 0; i < n; i++){
				var cell = cells[i];
				var hashIdx = cell.IndexOf('#');
				doc[ColNames[i]] = hashIdx >= 0 ? cell[..hashIdx].TrimEnd() : cell;
			}
			yield return doc;
		}
	}
}
