namespace RimeTools.Scripts;

/// Dks 命令（新）：讀事實源的四張 csx 表，按整字優先級合併，再生成 dks／dks_v／dkn／dks_phrase。
///
/// 合併優先級（同字整組取最高那層）：
///   TswgOcModed（魔改） > TswgOc（正音） > PaasTvLuuq（布之道） > ZhongGuDaoTui（中古倒推）。
/// 魔改表內未改的讀音已在表裏標「未改」；本命令不看 Tags，只取字／音／權重。
///
/// 參數：`-i <真相源目錄>`、`-o <產物目錄>`；省略時用 DksFactPaths 的默認值。
/// 例：dotnet run --project proj/RimeTools.Scripts -- Dks -i D:/Facts -o D:/out
internal static partial class Dks{
	/// Args 是去除命令名後的剩餘參數（只有 `-i`／`-o` 兩種）。
	internal static partial Task Main(ISCtx Ctx, str[] Args, CT Ct);
}
