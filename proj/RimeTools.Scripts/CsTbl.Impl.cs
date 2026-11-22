namespace RimeTools.Scripts;

using System.Text;
using Tsinswreng.CsCtx;

// 注意：本項目裏有個類叫 Dks（命令），它會遮蔽命名空間 Dks，
// 故引用 Dks.Core 的類型一律走 global:: 別名（Dks.cs 的註釋裏記過這個坑）。
using CsTbl來源集 = global::Dks.Core.Tables.CsTbl.CsTbl來源集;
using CsTbl產出報告 = global::Dks.Core.Tables.CsTbl.CsTbl產出報告;
using SvcCsTbl = global::Dks.Core.Tables.CsTbl.SvcCsTbl;

/// CsTbl 命令的實現。
/// 分工：本類只做「開檔／寫檔／改名／彙報」；抽取、校驗、文本組裝全在 Dks.Core 的 SvcCsTbl（庫不認路徑）。
internal static partial class CsTbl{
	/// 本命令刻意不用 `Ctx`：事實源位置由 CsTblFacts 集中決定（不隨倉庫根走），
	/// 產物位置由命令列參數決定；留著參數只是為了與 dispatcher 的其他命令同形。
	internal static async partial Task Main(ISCtx Ctx, str[] Args, CT Ct){
		// step 1: 參數 → 路徑集合。目標文件夾必填（使用者要求「輸出時指定文件夾」）。
		var P = CsTblPaths.FromArgs(Args);
		if(P.OutDir.Length == 0){
			throw new ArgumentException("CsTbl 需要指定目標文件夾。用法：CsTbl <目標文件夾> [表名]");
		}

		// step 2: 開九張來源表。這是本命令唯一碰文件系統的輸入側；
		//         編碼明確指定 UTF-8（來源檔都是 UTF-8，部分是無 BOM）。
		using var dkp = 開讀(P.Dkp);
		using var zyenpheng = 開讀(P.Zyenpheng);
		using var 形聲考 = 開讀(P.形聲考字表);
		using var 白沙 = 開讀(P.白沙官方Tsv);
		using var msoeg = 開讀(P.Msoeg);
		using var schuessler = 開讀(P.Schuessler);
		using var zhengzhang = 開讀(P.Zhengzhang);
		using var staoc = 開讀(P.StaOc);
		using var 廣韻 = 開讀(P.廣韻);
		var 來源 = new CsTbl來源集{
			Dkp = dkp,
			Zyenpheng = zyenpheng,
			廣韻形聲考字表 = 形聲考,
			白沙官方Tsv = 白沙,
			MsoegOc = msoeg,
			SchuesslerOc = schuessler,
			ZhengzhangOc = zhengzhang,
			StarostinOc = staoc,
			廣韻字表 = 廣韻,
		};

		// step 3: 產出三份文本到 `.tmp`、三份都寫成功才一齊改名。
		//         為甚麼要繞這一步：SvcCsTbl 雖然在寫任何字之前就校驗，但一旦開始寫，中途失敗就會留下半成品；
		//         寫 .tmp 再 File.Move 可以保證正式檔要嘛是舊的完整版、要嘛是新的完整版（與 DksPipeline 同款做法）。
		Directory.CreateDirectory(P.OutDir);
		var programTmp = P.OutProgram + ".tmp";
		var dslTmp = P.OutDsl + ".tmp";
		var projTmp = P.OutCsproj + ".tmp";
		CsTbl產出報告 報告;
		try{
			using(var programW = Mk寫者(programTmp))
			using(var dslW = Mk寫者(dslTmp))
			using(var projW = Mk寫者(projTmp)){
				using IFnCtx fnCtx = new FnCtx();
				var svc = new SvcCsTbl();
				報告 = await svc.產出(fnCtx, 來源, P.表名, programW, dslW, projW, Ct);
			}
			File.Move(programTmp, P.OutProgram, overwrite: true);
			File.Move(dslTmp, P.OutDsl, overwrite: true);
			File.Move(projTmp, P.OutCsproj, overwrite: true);
		} catch {
			// 校驗不通過或中途失敗：清掉 .tmp，正式檔保持原樣，異常照原樣往上拋（訊息裏已列出全部問題）。
			試刪(programTmp);
			試刪(dslTmp);
			試刪(projTmp);
			throw;
		}

		// step 4: 彙報。對帳數字與備註都要看得見（使用者要求「不丟信息」，也要求知道跳過了甚麼）。
		Console.WriteLine($"綜合字表源：表名={報告.表名}　字數={報告.字數}　調用數={報告.調用數}");
		foreach(var 帳 in 報告.來源統計){
			Console.WriteLine($"  {帳.來源}：資料行 {帳.資料行}、產出 {帳.產出調用}、跳過 {帳.跳過}");
		}
		foreach(var 註 in 報告.備註){
			Console.WriteLine($"  備註：{註}");
		}
		Console.WriteLine($"已產出：{P.OutProgram}");
		Console.WriteLine($"已產出：{P.OutDsl}");
		Console.WriteLine($"已產出：{P.OutCsproj}");
		Console.WriteLine($"下一步：cd \"{P.OutDir}\" 然後 dotnet run —— 會導出 {P.OutJson}");
	}

	/// 以 UTF-8 開一張來源表。部分來源檔沒有 BOM，故明確指定編碼，不靠運行環境的默認值。
	private static StreamReader 開讀(str 路徑){
		return new StreamReader(路徑, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
	}

	/// 產出用的寫者：UTF-8 無 BOM、換行固定 `\n`。
	/// 產物要進版本庫、要能跨平臺 diff，不能跟著運行環境變成 CRLF。
	private static StreamWriter Mk寫者(str 路徑){
		return new StreamWriter(路徑, append: false, new UTF8Encoding(false)){ NewLine = "\n" };
	}

	/// 盡力刪掉暫存檔。刪不掉就算了——它不該掩蓋「為甚麼產出失敗」這個真正的異常。
	private static void 試刪(str 路徑){
		try{
			if(File.Exists(路徑)){
				File.Delete(路徑);
			}
		} catch {
			// 故意吞掉：清理失敗不是本次失敗的主因，原始異常才是要緊的。
		}
	}
}
