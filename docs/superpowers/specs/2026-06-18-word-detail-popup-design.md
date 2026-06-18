# 方向一：单词校验详情弹窗 — 设计方案

**日期**: 2026-06-18
**状态**: Approved（已获 Leader 批准）
**对应 PLAN**: `docs/superpowers/plans/2026-06-18-word-detail-popup.md`

## 需求概述

鼠标悬停在结果面板中的不可用（红色）单词上时，弹出悬浮卡片显示详情。

## 关键决策

| 决策 | 选项 | 选择 |
|------|------|------|
| 弹窗形式 | Dialog / Popup / ToolTip | **Popup** — 悬浮卡片式，不打断工作流 |
| 触发方式 | 点击 / 悬停 | **悬停** — 与用户确认一致 |
| 触发范围 | 任意单词 / 仅不可用 | **仅不可用**（红色单词） |
| 频率粒度 | 当前文本 / 历史记录 | **当前文本** — 不需要改 HistoryStore |
| 事件绑定 | Run.MouseEnter / RichTextBox.MouseMove | **RichTextBox.MouseMove** — 因为 Run 非 UIElement |

## 设计要点

- YAGNI：没做"添加到白名单"、没做"完整频率历史趋势"、不对可用词弹窗
- 复用现有 `LevenshteinHelper.FindClosest` 和 `_suggestionCache`
- 弹窗使用 `StaticResource` 中的现有颜色/字体资源，风格一致
- Popup 的 `Placement="Mouse"` 让浮窗跟随鼠标光标

## 架构影响

- Checker 新增一个 ~10 行的辅助方法 `GetWordFrequency`，其他模型层无变化
- Views 层新增 ~50 行弹窗逻辑，其余文件改动极小
- 不影响任何现有功能的数据流
