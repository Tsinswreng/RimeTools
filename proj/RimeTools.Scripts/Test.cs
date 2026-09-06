namespace RimeTools.Scripts;

/// 以 dotnet run 直接運行 Dks.Test 測試專案（非 AOT 的快速驗證）。
internal static partial class Test{
	/// 執行測試；失敗時以非零退出碼結束。
	internal static partial Task Main(ISCtx Ctx, CT Ct);
}