---
last_updated: 2026-08-08
updated_by: codex
covers_branch: winui3-msix
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

## ADR-001: 使用 WinUI 3 与 Windows App SDK
**决策：** 使用 WinUI 3 + Windows App SDK 1.8 构建桌面 UI，并以 Fluent 资源和 NavigationView 组织交互。
**权衡：** 目标平台限定为 Windows，需要 Windows SDK 与 MSIX 工具链；换取系统级 Fluent、Mica 和现代窗口能力。
旧版 WPF 决策已被本分支的完整迁移取代。

## ADR-002: 使用 FrozenSet 做词库查询
**决策：** 使用 `FrozenSet<string>`（大小写不敏感）存储已加载的词库。
**权衡：** 初始加载后不可变——添加单词需要重建整个集合。但提供 O(1) 查询性能且内存开销极低。
→ [adr/ADR-002-frozenset-word-lookup.md](adr/ADR-002-frozenset-word-lookup.md)

## ADR-003: 使用 JSON 而非 SQLite 做持久化
**决策：** 使用 `System.Text.Json` 将设置和历史保存为本地 JSON 文件。
**权衡：** 不支持查询；无并发访问安全；每次保存需读写整个文件。但对于小数据量来说比 SQLite 简单得多。
→ [adr/ADR-003-json-persistence.md](adr/ADR-003-json-persistence.md)

## ADR-004: 使用单项目 MSIX
**决策：** 将 `Package.appxmanifest`、包资源和 x64 发布配置放在 WinUI 工程内，使用 Windows SDK Build Tools 生成 MSIX。
**权衡：** 当前默认生成未签名测试包，正式发行必须配置证书；单项目结构避免 WAP 项目依赖并适合命令行和 CI 构建。
