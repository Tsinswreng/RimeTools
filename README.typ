使用此模板

複製整個 Cs/ 目錄
```sh
dotnet script Init.csx <NewProjectName>
```

Init.csx 會一次完成：

- 把 RimeTools 換成 <NewProjectName>（命名空間與專案名，含目錄/檔案/sln）
- 刪除模板自帶的 .git
- 重新 git init

維護腳本

proj/RimeTools.Scripts 是模板附帶的維護腳本項目：

- dispatcher：`Program.cs` 依首個引數分派到各命令
- 上下文：dispatcher 統一建立 `ISCtx`（`RootDir`、`Tfm`）傳給子命令
- 命令直接跑 `RimeTools.Test` 專案，不依賴專案名

已內建命令

- `Test`：以 `dotnet run` 直接運行 RimeTools.Test（非 AOT 的快速驗證）
- `TestAotWin`：以 win-x64 NativeAOT 發布 RimeTools.Test 後運行發布物

用法（在模板根目錄）：
```sh
dotnet run --project proj/RimeTools.Scripts/RimeTools.Scripts.csproj -- Test
dotnet run --project proj/RimeTools.Scripts/RimeTools.Scripts.csproj -- TestAotWin
```

在 Scripts 專案內新增命令時：

- 新增 `Xxx.cs`（partial class + `partial Task Main(ISCtx Ctx, CT Ct)` 聲明）與 `Xxx.Impl.cs`（實現）
- 在 `Program.cs` 的 switch 與 PrintUsage 中加入分派