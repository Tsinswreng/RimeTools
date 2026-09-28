namespace RimeTools.Shared.Dict.Parser;

using RimeTools.Shared.Dict.Models;

public partial class DictYamlWriter : IDictYamlWriter{
	public partial async Task<nil> Write(
		TextWriter Writer, RimeDictHeader Header, IAsyncEnumerable<IDictLine> Body, CT Ct){
		// step 1: 寫表頭（`---` 起、`...` 收）。
		await Writer.WriteLineAsync("---");
		foreach(var item in Header.Items){
			if(item.List is not null){
				await Writer.WriteLineAsync($"{item.Key}:");
				foreach(var v in item.List){
					await Writer.WriteLineAsync($"  - {v}");
				}
			}
			else{
				await Writer.WriteLineAsync($"{item.Key}: {MkScalar(item.Scalar)}");
			}
		}
		await Writer.WriteLineAsync("...");

		// step 2: 以 Header.Columns（缺省三列 text/code/weight）為列序，逐行取鍵拼 Tab；尾列缺失不補空。
		var colNames = Header.Columns ?? DictColumns.Default;
		await foreach(var line in Body.WithCancellation(Ct)){
			var cells = new List<str>();
			foreach(var col in colNames){
				// 按列序取值；某列缺鍵 → 停止（其後列必然也缺，Rime 行是左對齊的）。
				if(!line.TryGetValue(col, out var v) || v is null){
					break;
				}
				cells.Add(v.ToString()!);
			}
			await Writer.WriteLineAsync(string.Join("\t", cells));
		}
		await Writer.FlushAsync(Ct);
		return NIL;
	}

	/// 寫一個 YAML 標量的字面。
	/// 空串必須寫成 `""`：Rime 的 DictSettings::LoadDictHeader 要求 name/version 非 null，
	/// 而 `version: `（空標量）在 YAML 裏是 null ⇒ 報 "incomplete dict header" ⇒ 整張表編譯失敗
	/// （實測：dks/dks_v/dkn/dkz/dks_phrase 全部因此編譯不過，部署後一直沿用舊表）。
	/// 另：含 YAML 特殊字符（# : " ' 或首尾空白）者也加雙引號，免得被當成註釋或映射。
	private static str MkScalar(str? Scalar){
		var v = Scalar ?? "";
		if(v.Length == 0){
			return "\"\"";
		}
		if(v.Contains('#') || v.Contains(':') || v.Contains('"') || v.Contains('\'') || v != v.Trim()){
			return "\"" + v.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
		}
		return v;
	}
}
