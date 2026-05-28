---
last_updated: 2026-05-28
updated_by: superpowers-memory:rebuild
triggered_by_plan: null
---

# 决策记录

## 已知问题

### 技术债务

无——未发现显著技术债务。

### 已知 Bug

无——未发现开放 Bug。

### 安全考量

无——纯桌面工具，无网络功能（除 GitHub 更新检查外），不收集用户数据。

---

## ADR-001: 使用 WPF 而非其他 UI 框架
**决策：** 使用 WPF + XAML + code-behind 构建桌面 UI。
**权衡：** 仅限 Windows；不支持跨平台。初始配置比 WinForms 复杂，但样式和动画支持更丰富。
→ [adr/ADR-001-wpf-framework-choice.md](adr/ADR-001-wpf-framework-choice.md)

## ADR-002: 使用 FrozenSet 做词库查询
**决策：** 使用 `FrozenSet<string>`（大小写不敏感）存储已加载的词库。
**权衡：** 初始加载后不可变——添加单词需要重建整个集合。但提供 O(1) 查询性能且内存开销极低。
→ [adr/ADR-002-frozenset-word-lookup.md](adr/ADR-002-frozenset-word-lookup.md)

## ADR-003: 使用 JSON 而非 SQLite 做持久化
**决策：** 使用 `System.Text.Json` 将设置和历史保存为本地 JSON 文件。
**权衡：** 不支持查询；无并发访问安全；每次保存需读写整个文件。但对于小数据量来说比 SQLite 简单得多。
→ [adr/ADR-003-json-persistence.md](adr/ADR-003-json-persistence.md)
