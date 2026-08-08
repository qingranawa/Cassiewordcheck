---
last_updated: 2026-08-08
updated_by: codex
covers_branch: winui3-msix
---

# 技术栈

## 语言与框架

| 技术 | 角色 | 版本 | 备注 |
|------|------|------|------|
| C# | 应用语言 | 12 / .NET 8 | 启用可为空引用类型和隐式 using |
| WinUI 3 | 桌面 UI | Windows App SDK 1.8.260508005 | Fluent XAML、NavigationView、Mica |
| MSIX | 安装与分发 | Windows SDK Build Tools | 单项目 x64 包，默认生成未签名测试包 |
| xUnit | 测试 | 2.9.2 | Core 领域和迁移回归测试 |

## 关键依赖

| 包 | 用途 |
|----|------|
| CommunityToolkit.Mvvm 8.4.0 | WinUI 状态和 MVVM 辅助 |
| ClosedXML 0.102.3 | Excel `.xlsx` 词库导入 |
| System.Text.Json | 设置、历史和多语言 JSON |

## 工程结构

`CassieWordCheck.Core` 是无 UI 类库；`CassieWordCheck.WinUI` 是唯一桌面可执行项目，同时承载 MSIX 清单和资源；`CassieWordCheck.Tests` 只引用 Core，避免测试依赖桌面 UI。

## 构建与发布

开发构建：

```powershell
dotnet build CassieWordCheck.sln --configuration Debug --no-restore
```

Release MSIX：

```powershell
dotnet build CassieWordCheck.WinUI/CassieWordCheck.WinUI.csproj --configuration Release `
  -p:Platform=x64 `
  -p:PublishProfile=Properties/PublishProfiles/win10-x64.pubxml `
  -p:GenerateAppxPackageOnBuild=true `
  -p:AppxPackageSigningEnabled=false
```

输出位于 `CassieWordCheck.WinUI/bin/Release/AppPackages/`。正式发布需要在 CI 中提供受信任的包签名证书，开发包可以使用未签名 MSIX 做结构验证。

## 平台要求

开发环境需要 Windows 10/11、Windows SDK 19041 或更高版本和 .NET 8 SDK。应用最低目标版本为 Windows 10 build 19041；MSIX 安装后由 Windows App SDK 包依赖处理运行时组件。
