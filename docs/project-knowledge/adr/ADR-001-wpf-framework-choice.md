---
adr: 001
title: 使用 WPF 而非其他 UI 框架
date: 2026-05-28
status: Accepted
---

# ADR-001: 使用 WPF 而非其他 UI 框架

## 背景

项目需要一个 Windows 桌面 UI 框架，用于面向 SCP:SL 内容创作者的词库检查工具。需求包括：富文本渲染（FlowDocument 支持着色/高亮检查结果）、流畅动画、暗色主题（Mica 背景）和多窗口支持。

## 决策

使用 WPF（.NET 8-windows）+ XAML + code-behind。理由：
- 原生 `FlowDocument` 支持逐词着色的富文本展示
- `DoubleAnimation` / 缓动函数支持平滑 UI 过渡
- 通过 DWM API 直接进行 Win32 互操作，实现暗色标题栏 + Mica
- 多窗口 `ShowDialog` 模式是 .NET 开发者熟悉的模式

## 被拒绝的替代方案

- **WinForms**：缺少与 `FlowDocument` 媲美的内置富文本渲染。动画需要手动定时器实现。无原生 Mica 支持，需要大量 Win32 互操作。
- **Avalonia / MAUI**：跨平台方案会增加不必要的复杂度（目标平台仅为 Windows）。MAUI 在 .NET 8 上仍在成熟中；Avalonia 的 XAML 与 WPF 惯例有差异。

## 后果

- 仅限 Windows 部署（可接受——目标用户是 Windows 上的 SCP:SL 服务器管理员）
- 二进制体积比 WinForms 方案更大
- 新人学习曲线比 WinForms 陡峭
