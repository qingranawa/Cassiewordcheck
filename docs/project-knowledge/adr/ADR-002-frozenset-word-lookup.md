---
adr: 002
title: 使用 FrozenSet 做词库查询
date: 2026-05-28
status: Accepted
---

# ADR-002: 使用 FrozenSet 做词库查询

## 背景

词库文件（`data/cassie-text.txt`）包含数千条记录。每次按键都会触发全文本重新检查，每次检查需要数百次 O(1) 查询。词库在启动时加载一次，极少修改。

## 决策

使用 `System.Collections.Frozen.FrozenSet<string>` + `StringComparer.OrdinalIgnoreCase`。理由：
- 构建后 O(1) 查询性能
- 最小内存开销（优化的哈希布局）
- 线程安全的只读访问（构建后不可变）

## 被拒绝的替代方案

- **HashSet\<string\>**：可变的——适合动态添加，但为读密集型工作负载带来额外开销。未针对 JIT 编译的字典查询做优化。
- **SortedList / BinarySearch**：O(log n) 查询——比每次按键的检查循环慢。维护排序顺序的内存开销更高。
- **Trie / 前缀树**：大材小用——本项目匹配完整单词而非前缀。内存开销比 FrozenSet 更高。

## 后果

- 词库修改（白名单添加、文件导入）需要重建 FrozenSet——可接受，因为这些操作不频繁且批量处理
- 通过比较器内置大小写不敏感匹配
- 启动开销：典型的 1 万-5 万条词库约 50-100ms
