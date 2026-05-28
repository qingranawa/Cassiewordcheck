---
last_updated: 2026-05-28
updated_by: superpowers-memory:rebuild
triggered_by_plan: null
---

# 术语表

**CASSIE** —— SCP:SL 的文本转语音系统，用于播放游戏内广播。 → `data/cassie-text.txt`
**CheckStatus** —— 枚举，标识单词检查结果状态：Available（词库中）、Unavailable（未找到）、Ignored（被过滤）、Separator（分隔符/标点）。 → `Models/CheckResult.cs`
**FrozenSet** —— .NET 不可变哈希集合，初始加载后用于 O(1) 词库查询。 → `Models/WordList.cs`
**Mica** —— Windows 11 丙烯酸背景材质，用于暗色主题效果。 → `Resources/Services/WindowHelper.cs`
