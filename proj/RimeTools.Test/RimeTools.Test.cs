using RimeTools.Test.Domains.Calculator;
using Microsoft.Extensions.DependencyInjection;
using Tsinswreng.CsTreeTest;

namespace RimeTools.Test;

internal class Program{
	public static IServiceCollection SvcColct = new ServiceCollection();
	public static IServiceProvider SvcProvdr = null!;
	public static async Task Main(string[] args){
		//SvcColct.SetupMyDi();
		SvcColct.AddSingleton<ICalculator, Calculator>();

		var mgr = RimeToolsTestMgr.Inst;
		SvcProvdr = mgr.InitSvc(SvcColct, sc => sc.BuildServiceProvider());

		ITestExecutor executor = new TreeTestExecutor();
		await executor.RunEtPrint(mgr.TestNode);
	}
}
