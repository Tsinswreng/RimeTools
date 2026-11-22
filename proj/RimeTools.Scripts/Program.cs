namespace RimeTools.Scripts;

using System.Runtime.CompilerServices;
using Tsinswreng.CsSh;
using static Tsinswreng.CsSh.ShGlobal;

/// 子命令共用的執行上下文。
/// dispatcher 統一建立並填入項目固定事實，子命令直接取用，不各自解析路徑。
public interface ISCtx{
	/// 倉庫根目錄，各命令以此定位 proj/ 下專案。
	public Pth RootDir{get;set;}
	/// 目標框架（TFM），與 proj/Directory.Build.props 的 TargetFramework 同步；
	/// 決定構建/發布輸出目錄名（如 net10.0），升級 TFM 時此處需同步更新。
	public str Tfm{get;set;}
}

public class SCtx:ISCtx{
	public Pth RootDir{get;set;}
	/// 默認空串，dispatcher 建立時必定覆蓋，故不允許為 null。
	public str Tfm{get;set;} = "";
}

/// 命令列入口。第一個引數選擇具體命令；命令本身負責完整的一次性流程。
internal static partial class Program{
	/// 將命令列入口分派至具名命令。
	internal static async Task Main(str[] Args){
		var Ct = default(CT);
		// Program.cs 位於 <根>/proj/RimeTools.Scripts；上推兩級得到倉庫根。
		var Root = FullPath(DirName(OwnPath())/"../..");

		// step 1: 建立統一的子命令上下文，所有子命令都從中取得 RootDir、Tfm。
		ISCtx Ctx = new SCtx {
			RootDir = Root,
			// 與 proj/Directory.Build.props 的 TargetFramework 同步；升級 TFM 時兩處一起改。
			Tfm = "net10.0",
		};

		if(Args.Length == 0){
			PrintUsage();
			return;
		}

		// step 2: 依首個引數分派；新命令在此與 PrintUsage 各加一行。
		// 各命令一覽（詳細職責見各自的 Decl 檔註釋）：
		switch(Args[0]){
			// Test：跑 proj/Dks.Test 的測試（不發布），即 dotnet run → Dks.Test 的用例。
			case nameof(Test):
				await Test.Main(Ctx, Ct);
				break;

			// TestAotWin：先 win-x64 NativeAOT 發布，再跑同一批測試（驗證 AOT 下行為一致）。
			case nameof(TestAotWin):
				await TestAotWin.Main(Ctx, Ct);
				break;

			// Dks（事實源合併）：讀四張 csx 表（TswgOcModed > TswgOc > PaasTvLuuq > ZhongGuDaoTui），
			//   按整字優先級合併 → 上古三拼 dks → dks_v（接倉頡輔助碼）／dkn（首尾雙碼）／dks_phrase（造詞）。
			//   參數：-i 真相源目錄、-o 產物目錄；省略則用 DksFactPaths 默認值。
			case nameof(Dks):
				await Dks.Main(Ctx, Args[1..], Ct);
				break;

			// Dks2（布之道擬音）：只換第一段——源頭改 OC_msoegDK.dict.yaml（布之道《廣韻形聲考》），
			//   把布之道 IPA 適配成本方案音節，其餘（dkp 覆蓋、查表轉三拼、後段三件）不變。
			//   轉換全走查表（DksKeyboard 三層鍵位表），無正則。可選參數：UserDataDir、SrcTableDir、布之道DictPath。
			case nameof(Dks2):
				await Dks2.Main(Ctx, Args[1..], Ct);
				break;

			// Dks3（布之道 + 缺音回退）：在 Dks2 之上，凡「布之道缺音」的字（Dks2 那份裏沒有、
			//   或全部碼為空）改用舊 Dks（中古倒推）的讀音補上。可選參數同 Dks2。
			case nameof(Dks3):
				await Dks3.Main(Ctx, Args[1..], Ct);
				break;

			// Dks4（按漢字頻率擇源）：dkp 永遠最優先；其餘的字按 essay.txt 的漢字頻率排名分流——
			//   前 5000 名用中古倒推（舊 Dks）的碼，5000 名以外（含 essay.txt 裏沒有的）用布之道（Dks2）的碼。
			//   目的：高頻字保留舊讀音、不破壞肌肉記憶；罕見字改採新擬音。可選參數同 Dks2。
			case nameof(Dks4):
				await Dks4.Main(Ctx, Args[1..], Ct);
				break;

			// AuditDks（審查．只讀比對）：分別用舊 Dks 流程與 Dks2 流程各建一份 dks.dict.yaml，
			//   逐字比對碼集，按 essay.txt 漢字頻率（十萬分之）降序列出**有變化**的字；
			//   dkp 覆蓋到的字不列（兩邊都由 dkp 決定，比了沒意義）。
			//   產物與報告寫在工作區：<倉庫根>/_AuditDks/{舊,新}/{src,user} 與 _AuditDks/差異.txt。
			//   可選參數：輸出根（默認 <倉庫根>/_AuditDks）。此命令不改動正式產物。
			case nameof(AuditDks):
				await AuditDks.Main(Ctx, Args[1..], Ct);
				break;

			// CsTbl（綜合字表源）：讀九張來源表（dkp／zyenpheng／廣韻形聲考主字表／白沙官方 TSV／
			//   msoeg／許思萊／鄭張／斯塔羅斯金／廣韻字表），抽出「字 → 各家讀音與欄位」，
			//   在指定文件夾裏寫出三份文本：Program.cs（只放數據）／Dsl.cs（只放結構：定義與運行時）／
			//   <表名>.csproj（零依賴工程）；使用者 cd 進去 dotnet run 即導出 <表名>.json。
			//   參數：CsTbl <目標文件夾> [表名=（默認 Dk）]。
			case nameof(CsTbl):
				await CsTbl.Main(Ctx, Args[1..], Ct);
				break;

			default:
				throw new ArgumentException($"Unknown script: {Args[0]}.", nameof(Args));
		}
	}

	/// 列出可由 dotnet run -- <entry> 呼叫的命令名稱（各自職責見 Main 的分派註釋）。
	private static void PrintUsage(){
		Console.Error.WriteLine("Usage: dotnet run --project proj/RimeTools.Scripts -- <entry>");
		Console.Error.WriteLine("Entries: Test, TestAotWin, Dks, Dks2, Dks3, Dks4, AuditDks, CsTbl");
		Console.Error.WriteLine("  Test/TestAotWin : 跑測試 / 先 AOT 發布再跑測試");
		Console.Error.WriteLine("  Dks             : 事實源四層合併（魔改>正音>布之道>中古倒推）→ dks/dks_v/dkn/dks_phrase；-i/-o 可選");
		Console.Error.WriteLine("  Dks2            : 布之道擬音 → dks/…（換掉第一段）");
		Console.Error.WriteLine("  Dks3            : Dks2 + 布之道缺音者回退舊讀音");
		Console.Error.WriteLine("  Dks4            : dkp 最優先；其餘按漢字頻率前 5000 用中古倒推、其餘用布之道");
		Console.Error.WriteLine("  AuditDks        : 舊 Dks 與 Dks2 的 dks 對比（只讀，報告在 _AuditDks/差異.txt）");
		Console.Error.WriteLine("  CsTbl           : 九張來源表 → 綜合字表源（Program.cs ＋ Dsl.cs ＋ <表名>.csproj；cd 進去 dotnet run 出 JSON）");
	}

	/// 讓編譯器提供命令源文件路徑，故命令不依賴啟動時的當前目錄。
	private static str OwnPath([CallerFilePath] str CallerPath = ""){
		return CallerPath;
	}
}