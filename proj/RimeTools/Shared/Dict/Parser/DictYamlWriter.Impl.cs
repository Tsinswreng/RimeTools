namespace RimeTools.Shared.Dict.Parser;

using RimeTools.Shared.Dict.Models;

public partial class DictYamlWriter : IDictYamlWriter{
	public partial async Task<nil> Write(
		str Path, RimeDictHeader Header, IAsyncEnumerable<IDictLine> Body, CT Ct){
		// step 1: 確保目錄存在。
		var dir = System.IO.Path.GetDirectoryName(Path);
		if(!string.IsNullOrEmpty(dir)){
			System.IO.Directory.CreateDirectory(dir);
		}

		// step 2: 寫表頭（`---` 起、`...` 收）。
		using var writer = new StreamWriter(Path, false, new System.Text.UTF8Encoding(false));
		writer.WriteLine("---");
		foreach(var item in Header.Items){
			if(item.List is not null){
				writer.WriteLine($"{item.Key}:");
				foreach(var v in item.List){
					writer.WriteLine($"  - {v}");
				}
			}
			else{
				writer.WriteLine($"{item.Key}: {item.Scalar}");
			}
		}
		writer.WriteLine("...");

		// step 3: 以 Header.Columns（缺省三列）為列序，逐行取鍵拼 Tab；尾列缺失不補空。
		var colNames = Header.Columns ?? DefaultColumns;
		await foreach(var line in Body.WithCancellation(Ct)){
			var cells = new List<str>();
			foreach(var col in colNames){
				// 按列序取值；某列缺鍵 → 停止（其後列必然也缺，Rime 行是左對齊的）。
				if(!line.TryGetValue(col, out var v) || v is null){
					break;
				}
				cells.Add(v.ToString()!);
			}
			writer.WriteLine(string.Join("\t", cells));
		}
		await writer.FlushAsync(Ct);
		return NIL;
	}

	/// 默認三列語義。
	private static readonly IReadOnlyList<str> DefaultColumns = ["text", "code", "weight"];
}