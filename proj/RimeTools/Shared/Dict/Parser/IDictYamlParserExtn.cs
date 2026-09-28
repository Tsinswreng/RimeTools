namespace RimeTools.Shared.Dict.Parser;

using System.Runtime.CompilerServices;
using RimeTools.Shared.Dict.Models;

/// `IDictYamlParser` 的路徑版簡便寫法（端點與測試用）。
/// 成員只認 `TextReader`（流式、無文件系統概念）；開檔與關檔是這裏額外補的一層，
/// 屬「寫法上的簡化」而非能力上的擴展，故按規範放在擴展類而非接口成員。
/// 例：`dotnet` 端點要讀 dks.dict.yaml 時用它，之後不必自己管 StreamReader 生命週期——
/// 正文流消費完（或提前中止）時本擴展自動關檔。
public static class IDictYamlParserExtn{
	/// 開檔解析；返回文檔的 Body 消費完畢（或提前中止）時自動關閉底層 Reader。
	/// <param name="z">解析器實現。</param>
	/// <param name="Path">dict.yaml 文件路徑。</param>
	/// <param name="Ct">取消令牌。</param>
	public static async Task<RimeDictDoc> Parse(this IDictYamlParser z, str Path, CT Ct){
		var reader = new StreamReader(Path, new System.Text.UTF8Encoding(false));
		try{
			var doc = await z.Parse(reader, Ct);
			// step 1: 表頭已讀完；正文流包一層「消費完即關檔」，調用方只管路徑。
			return new RimeDictDoc(doc.Header, MkAutoClose(doc.Body, reader, Ct));
		}
		catch{
			// step 2: 表頭階段就失敗（文件不存在/格式錯）→ 立即關檔，不留句柄。
			reader.Dispose();
			throw;
		}
	}

	/// 把正文流包一層：正常消費完、或調用方提前 break/Dispose，都在 finally 關檔。
	private static async IAsyncEnumerable<IDictLine> MkAutoClose(
		IAsyncEnumerable<IDictLine> Body, StreamReader Reader,
		[EnumeratorCancellation] CT Ct){
		try{
			await foreach(var line in Body.WithCancellation(Ct)){
				yield return line;
			}
		}
		finally{
			Reader.Dispose();
		}
	}
}
