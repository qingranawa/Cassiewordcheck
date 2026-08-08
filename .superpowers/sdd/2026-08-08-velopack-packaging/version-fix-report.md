# 2.5.1 版本与 Velopack 验证修复报告

## 修改范围

本次仅修改了版本修复任务指定的五个文件，并新增本报告：

- `Directory.Build.props`：将默认 `Version`、`AssemblyVersion` 和 `FileVersion` 统一为 `2.5.1`、`2.5.1.0` 和 `2.5.1.0`。
- `CassieWordCheck.WinUI/CassieWordCheck.WinUI.csproj`：移除重复的 `Version` 和 `FileVersion` 属性，使 `Directory.Build.props` 成为默认版本源。
- `Models/AppInfo.cs`：将界面显示版本更新为 `2.5.1`。
- `scripts/verify-winui-migration.ps1`：保留脚本并重写为 Velopack 验证流程，调用 `scripts/pack-velopack.ps1 -Configuration Release -Version 2.5.1`，验证 `dist/velopack/Releases` 下的 `*-Setup.exe`、`releases.win.json`、`*-full.nupkg` 以及 `dist/publish/win-x64/CassieWordCheck.exe`，并保留 Core 测试和 WPF 残留扫描。
- `Views/AboutWindow.xaml.cs`：在 v2.5.1 更新日志中补充版本统一、Velopack 验证流程调整，以及用户数据迁移到 LocalAppData 并兼容旧 MSIX LocalState 的说明。

未修改 Task 3 的 `scripts/pack-velopack.ps1`、工具清单或 `publish.bat`。

## 验证结果

修改前的版本契约文本检查按预期发现六项旧状态，覆盖旧的 `2.5.0` 版本源、WinUI 项目重复版本属性、旧 AppInfo 版本、MSIX 验证流程和缺少更新日志说明。

Core 测试命令：

```powershell
dotnet test CassieWordCheck.Tests/CassieWordCheck.Tests.csproj --configuration Release --verbosity minimal
```

结果：通过 131，失败 0，跳过 0。输出包含既有的 xUnit2013 警告，来源为 `CassieWordCheck.Tests/WordListBrowserTests.cs`，本次未修改该文件。

文本契约、PowerShell 语法和差异检查均通过，确认版本值、脚本调用、Velopack 产物路径、旧 MSIX/makeappx/AppPackages 契约移除、Core 测试、WPF 扫描及两条 v2.5.1 更新日志说明均存在或符合要求。

程序集元数据检查结果：

- `CassieWordCheck.Core.dll` AssemblyVersion：`2.5.1.0`。
- `CassieWordCheck.dll` AssemblyVersion：`2.5.1.0`。
- `dist/publish/win-x64/CassieWordCheck.exe` FileVersion：`2.5.1.0`。

完整验证命令：

```powershell
pwsh -NoProfile -File scripts/verify-winui-migration.ps1
```

首次在沙箱内执行时，Core 测试通过，但发布阶段因沙箱无法访问 NuGet 源而失败。申请网络放行后使用同一命令重新执行并通过，退出码为 `0`。最终结果为 Core 测试通过 131 项、Velopack 打包成功、WPF 残留扫描通过，并生成以下产物：

- `dist/velopack/Releases/qingranawa.CassieWordCheck-win-Setup.exe`
- `dist/velopack/Releases/releases.win.json`，其中完整包版本为 `2.5.1`
- `dist/velopack/Releases/qingranawa.CassieWordCheck-2.5.1-full.nupkg`
- `dist/publish/win-x64/CassieWordCheck.exe`

## Concerns

Velopack 本次未提供签名参数，工具输出了未签名文件警告；签名配置不在本任务范围内。工作树中其他代理的 Task 2 改动（`Models/AppDataPaths.cs`、`CassieWordCheck.Tests/AppDataPathsTests.cs`、`CassieWordCheck.WinUI/Services/WinUiStoragePathProvider.cs` 和 `CassieWordCheck.Core/Services/StorageMigrationService.cs`）以及用户或其他代理的未跟踪文件均未纳入本次提交，并已保留。
