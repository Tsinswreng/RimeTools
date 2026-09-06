using Microsoft.Extensions.DependencyInjection;
using Tsinswreng.CsTreeTest;

namespace Dks.Test;

internal class Program{
	public static IServiceCollection SvcColct = new ServiceCollection();
	public static IServiceProvider SvcProvdr = null!;
	public static async Task Main(string[] args){
		// 註冊被測對象與測試依賴（無 DI 需求時留空）。
		//SvcColct.AddSingleton<...>();

		var mgr = DksTestMgr.Inst;
		SvcProvdr = mgr.InitSvc(SvcColct, sc => sc.BuildServiceProvider());

		ITestExecutor executor = new TreeTestExecutor();
		await executor.RunEtPrint(mgr.TestNode);
	}
}