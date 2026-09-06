namespace RimeTools.Scripts;

/// 以 win-x64 NativeAOT 發布 Dks.Test 後運行發布物（驗證 AOT 兼容性）。
internal static partial class TestAotWin{
	/// 執行 AOT 發布並運行；失敗時以非零退出碼結束。
	internal static partial Task Main(ISCtx Ctx, CT Ct);
}