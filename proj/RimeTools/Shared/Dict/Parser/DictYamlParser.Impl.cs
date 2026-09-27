namespace RimeTools.Shared.Dict.Parser;

using RimeTools.Shared.Dict.Models;

public partial class DictYamlParser : IDictYamlParser{
	public partial async Task<RimeDictDoc> Parse(str Path, CT Ct){
		// step 1: 讀表頭段（`---` 到 `...`），得到保序鍵值項。
		var items = await ReadHeaderAsync(Path, Ct);

		// step 2: 確定正文列名：Header.Columns 或缺省 text/code/weight。
		var header = new RimeDictHeader(items);
		var colNames = header.Columns ?? DefaultColumns;

		// step 3: 返回表頭 + 惰性正文流（正文流自行重開文件掃到 `...` 後產出）。
		return new RimeDictDoc(header, BodyLinesAsync(Path, colNames, Ct));
	}

	/// 默認三列語義（Rime 無 columns 時的正文列名）。
	private static readonly IReadOnlyList<str> DefaultColumns = ["text", "code", "weight"];

	/// 讀表頭，返回保序鍵值項。
	/// 支援兩種真實文件形狀：
	///   - 標準：`#註釋` … `---` …鍵值… `...`
	///   - 無表頭（如 cangjie 表）：`#註釋` … `...`（`---` 缺失，表頭為空）
	private async Task<IReadOnlyList<HeaderItem>> ReadHeaderAsync(str Path, CT Ct){
		using var reader = new StreamReader(Path, new System.Text.UTF8Encoding(false));
		var items = new List<HeaderItem>();
		str? line;
		var inHeader = false;
		str? curListKey = null;
		var curList = new List<str>();

		// step 1: 逐行掃描；註釋行跳過。
		while((line = await reader.ReadLineAsync(Ct)) is not null){
			var trimmed = line.Trim();
			if(trimmed.StartsWith("#", StringComparison.Ordinal)){
				continue;
			}
			if(!inHeader){
				// step 2: 遇 `...` 直接結束（無表頭文件）；遇 `---` 進入表頭。
				if(trimmed == "..."){
					break;
				}
				if(trimmed == "---"){
					inHeader = true;
				}
				continue;
			}
			if(trimmed == "..."){
				break; // 表頭結束
			}
			if(trimmed.StartsWith("- ", StringComparison.Ordinal)){
				// step 3: 列表項歸入上一鍵的列表。
				curList.Add(trimmed[2..].Trim());
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
				throw new FormatException($"dict.yaml 表頭行無法解析(缺冒號): {line}");
			}
			var key = line[..colonIdx].Trim();
			var val = line[(colonIdx + 1)..].Trim();
			if(val.Length == 0){
				// 值空 → 記為懸空鍵，可能下接 `- ` 列表項。
				curListKey = key;
				curList = new List<str>();
			}
			else{
				items.Add(new HeaderItem(key, val, null));
			}
		}
		// step 6: 收尾最後一個懸空列表鍵。
		if(curListKey is not null){
			items.Add(new HeaderItem(curListKey, null, curList));
		}
		return items;
	}

	/// 正文行流：重開文件掃到第一個 `...` 之後逐行按 Tab 拆列，以 ColNames 命名。
	private async IAsyncEnumerable<IDictLine> BodyLinesAsync(
		str Path, IReadOnlyList<str> ColNames, [System.Runtime.CompilerServices.EnumeratorCancellation] CT Ct){
		using var reader = new StreamReader(Path, new System.Text.UTF8Encoding(false));
		str? line;
		var pastHeader = false;
		while((line = await reader.ReadLineAsync(Ct)) is not null){
			Ct.ThrowIfCancellationRequested();
			var trimmed = line.Trim();
			if(!pastHeader){
				if(trimmed == "..."){
					pastHeader = true;
				}
				continue; // 表頭行/註釋跳過
			}
			if(trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal)){
				continue; // 空行/註釋行
			}
			var cells = trimmed.Split('\t');
			// step 1: 行內註釋截斷——某列含 `#` 時取 `#` 前部分(trim)，其後註釋丟棄。
			//          對齊 ngaq 的 removeSingleLineComments（權重 10000% 無 #，`0% # 舍他音`→`0%`、`# ts`→空）。
			var n = Math.Min(cells.Length, ColNames.Count);
			var clean = new List<str>();
			foreach(var cell in cells){
				var hashIdx = cell.IndexOf('#');
				clean.Add(hashIdx >= 0 ? cell[..hashIdx].TrimEnd() : cell);
			}
			// step 2: 逐格以列名收進字典；格子比列名多 → 忽略尾格。
			var doc = new DictLine();
			for(var i = 0; i < n; i++){
				doc[ColNames[i]] = clean[i];
			}
			yield return doc;
		}
	}
}