---
last_updated: 2026-05-28
updated_by: superpowers-memory:rebuild
triggered_by_plan: null
---

# 约定

## 命名规范

**文件：** 所有源文件使用 PascalCase（与类名一致）
**类/方法/属性：** PascalCase（如 `CheckText`、`GetStatistics`）
**私有字段：** `_camelCase`（如 `_wordlist`、`_settings`）
**接口：** `I` 前缀（本项目中未定义自定义接口——直接使用具体类型）
**常量：** PascalCase（如 `MaxRetryCount`）
**枚举：** PascalCase，单数形式（`CheckStatus`）

## 代码风格

**格式化工具：** dotnet-format / Visual Studio 默认配置
**可为空引用类型：** 全局启用（`<Nullable>enable</Nullable>`）
**隐式 using：** 启用（`<ImplicitUsings>enable</ImplicitUsings>`）
**文件范围命名空间：** 整个项目使用
**主构造函数：** 当前未使用（传统的构造函数注入）

## 错误处理

**策略：** 在边界点做 try-catch（文件 I/O、JSON 反序列化）。内部逻辑假设状态有效。
**自定义异常：** 未使用——依赖标准 .NET 异常类型
**资源清理：** 使用 `using` 语句处理 `StreamWriter`、`XLWorkbook` 等
**I/O 失败：** 静默捕获——文件损坏或缺失时回退到默认值

## 架构规则

- Views 可以直接实例化 Models 和 Services（无 DI 容器——轻量级 WPF 模式）
- Models 不得引用 UI 类型（Windows、Controls、Dispatcher）
- Services 不得引用 Views
- 词库加载后不可变（`FrozenSet`），除非通过 `AddFromFile` 显式修改（会重建集合）
- 异步操作：事件处理器使用 `async void`，CPU 密集型任务使用 `Task.Run`

## 测试约定

**测试框架：** 当前未配置（未检测到测试项目）
**Mock 原则：** 不适用

## Git 与工作流

**提交风格：** Conventional Commits（feat/fix/docs 前缀）
**分支：** main（观察到单分支工作流）

## 横切关注点

**国际化：** 所有面向用户的字符串必须通过 `LocalizationService`——禁止硬编码 UI 文本。 → `Resources/Services/LocalizationService.cs`
**动画：** 使用 WPF `DoubleAnimation` + `QuadraticEase`/`BackEase` 实现一致的 UI 动效。 → `Views/MainWindow.xaml.cs` §动画辅助方法
**持久化：** 通过 `System.Text.Json` 以 JSON 格式保存设置和历史；文件损坏时静默回退。 → `Models/Settings.cs`，`Models/HistoryStore.cs`
