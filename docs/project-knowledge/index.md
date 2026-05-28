---
last_updated: 2026-05-28
updated_by: superpowers-memory:rebuild
covers_branch: main@fdeceab
---

# 项目知识索引

- [architecture.md](architecture.md) — WPF 单部署应用；文本检查流水线；面向服务的内部分层
  关键点：3 个关键设计决策（WPF、FrozenSet、JSON），检查与设置流程的 Mermaid 时序图

- [tech-stack.md](tech-stack.md) — .NET 8 + WPF；CommunityToolkit.Mvvm + ClosedXML
  关键点：自包含发布，仅限 Windows，JSON 持久化

- [features.md](features.md) — 10 项已实现功能，分 4 个组
  关键点：实时词库检查、Levenshtein 拼写建议、白名单、多语言（7 种语言）、历史记录、统计

- [conventions.md](conventions.md) — PascalCase 命名、启用可为空引用、静默错误恢复
  关键点：无 DI 容器、Models 不得引用 UI、FrozenSet 加载后不可变

- [decisions.md](decisions.md) — 3 个 ADR
  关键点：选 WPF 因为 FlowDocument + 动画；FrozenSet 实现 O(1) 查询；小数据量选 JSON 而非 SQLite

- [glossary.md](glossary.md) — 4 个领域术语：CASSIE、CheckStatus、FrozenSet、Mica
  关键点：CASSIE = SCP:SL TTS 系统；CheckStatus 枚举驱动 UI 着色
