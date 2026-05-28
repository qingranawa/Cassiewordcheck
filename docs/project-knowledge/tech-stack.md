---
last_updated: 2026-05-28
updated_by: superpowers-memory:rebuild
triggered_by_plan: null
---

# 技术栈

## 语言与框架

| 技术 | 角色 | 版本 | 备注 |
|------|------|------|------|
| C# | 应用语言 | 12 (.NET 8) | 启用可为空引用类型，启用隐式 using |
| .NET | 运行时框架 | 8.0-windows | WinExe 输出，支持自包含发布 |
| WPF | 桌面 UI 框架 | net8.0-windows | 使用 XAML + code-behind 模式 |

## 运行时

**运行环境：** Windows 10 1809+ / Windows 11
**运行时：** .NET 8（自包含发布打包运行时；依赖框架安装需要 .NET 8 运行时）
**单实例：** 通过 app.manifest + mutex 强制

## 关键依赖

| 包 | 用途 | 选择理由 |
|----|------|----------|
| CommunityToolkit.Mvvm 8.4.0 | MVVM 辅助（ObservableObject, RelayCommand） | 轻量级，微软官方维护，与 .NET 8 兼容 |
| ClosedXML 0.102.3 | Excel (.xlsx) 词库导入 | 纯 .NET 实现，无需 Excel 互操作，可处理大文件 |

## 构建与开发工具

| 工具 | 用途 |
|------|------|
| dotnet CLI | 构建、还原、发布 |
| GitHub Actions | CI/CD —— 自动构建 + 发布 |
| publish.bat | 一键发布脚本 |

## 配置

**运行时：** `data/appsettings.json` —— 自动创建的 JSON，保存用户偏好（过滤器、语言、主题、字体、白名单路径）
**构建：** `CassieWordCheck.csproj` —— Release 单文件发布，Debug 可移植调试

## 平台要求

**开发环境：** Windows 10+；Visual Studio 2022+ 或 JetBrains Rider；.NET 8 SDK
**生产环境：** Windows 10 1809+（自包含）或 .NET 8 运行时（依赖框架）
