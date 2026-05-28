---
last_updated: 2026-05-28
updated_by: superpowers-memory:rebuild
triggered_by_plan: null
---

# 功能

## 已实现

### 产品功能

#### CASSIE 词库检查

**功能** —— 用户粘贴 SCP:SL 广播文本后，立即显示哪些单词在 CASSIE TTS 词库中（绿色=可用，红色=不可用）。自动过滤格式标签、阵营名称、北约代号和中文。

**角色 / 入口** —— 最终用户 → `MainWindow` 输入框（文本变化时实时检查，80ms 防抖）

**边界** —— 对已加载的词库文件（默认 `data/cassie-text.txt`）逐词检查。支持通过设置重新加载和自定义词库路径。不支持同时检查多个词库。

**参考** —— 见 `architecture.md` §文本检查流程

#### Levenshtein 拼写建议

**功能** —— 对每个不在 CASSIE 词库中的单词，显示最多 3 个相似词（基于编辑距离）作为建议，帮助用户找到有效替代词。

**角色 / 入口** —— 最终用户 → `MainWindow` 建议面板（当存在不可用单词时自动显示）

**边界** —— 仅从已加载的词库中检索建议。建议结果在会话内缓存（`_suggestionCache`）以提升性能。限制为 Top 3 最接近匹配。

**参考** —— `Resources/Services/LevenshteinHelper.cs`

#### 白名单管理

**功能** —— 用户可以添加自定义豁免词，使其不被标记为不可用；支持导入/导出白名单。

**角色 / 入口** —— 最终用户 → `WhitelistWindow`（从工具栏 ⊞ 按钮打开）

**边界** —— 白名单持久化在 `data/appsettings.json` 中。支持添加、移除、清空和从文件批量导入。大小写不敏感匹配。

**参考** —— `Views/WhitelistWindow.xaml.cs`，`Models/WordList.cs` §白名单

#### 多语言界面

**功能** —— 应用界面可在 7 种语言间切换：简体中文、English、日本語、한국어、Deutsch、Русский、Français。

**角色 / 入口** —— 最终用户 → `SettingsWindow` 语言下拉框

**边界** —— 翻译从 `Resources/Locales/*.json` 在运行时惰性加载。支持不重启即时切换（通过 `LanguageChanged` 事件）。翻译缺失时回退到 key 名。

**参考** —— `Resources/Services/LocalizationService.cs`

### 用户工作流

#### 文本输入与实时检查

**功能** —— 用户输入或粘贴文本后，结果以平滑动画实时更新（80ms 防抖）。支持撤销/重做（Ctrl+Z/Y），最多 50 步历史。

**角色 / 入口** —— 最终用户 → `MainWindow` 输入文本框

**边界** —— `_suppressAnimation` 标志防止回退/删除/清空时的动画闪烁。前 5 次结果有轻微弹跳反馈。空输入显示 0% 覆盖率（而非 Checker 默认的 100%）。

**参考** —— `Views/MainWindow.xaml.cs`

#### 文件导入与批量检查

**功能** —— 用户可以打开 `.txt` 文件（加载到输入框）和导入 `.csv` / `.xlsx` 词表（合并到词库）。支持多文件同时选择。

**角色 / 入口** —— 最终用户 → `MainWindow` 文件打开按钮（🗂）

**边界** —— `.txt` 文件加载内容到输入框；`.csv`/`.xlsx` 文件导入单词到词库。多选时混合处理：TXT 到输入，其余到词库。导入的单词合并到现有 `FrozenSet`（合并后重建冻结集）。

**参考** —— `Models/WordList.cs` §AddFromFile

#### 检查历史

**功能** —— 最近的 50 条检查结果自动保存（3 分钟快照定时器），可浏览、恢复或清空。

**角色 / 入口** —— 最终用户 → `HistoryWindow`（从工具栏时钟按钮打开）

**边界** —— 去重：连续相同的输入文本不会重复存储。历史持久化到 `data/history.json`。点击历史条目可恢复输入文本。

**参考** —— `Models/HistoryStore.cs`，`Views/HistoryWindow.xaml.cs`

#### 结果导出

**功能** —— 检查结果可通过保存文件对话框导出为 `.txt` 文件。

**角色 / 入口** —— 最终用户 → `MainWindow` 导出按钮

**边界** —— 将渲染后的 `FlowDocument` 导出为纯文本。文件名自动按时间戳生成。

**参考** —— `Views/MainWindow.xaml.cs` §OnExportResult

### 平台能力

#### 统计面板

**功能** —— 基于检查历史，展示单词覆盖率和不可用词数量的趋势折线图。

**角色 / 入口** —— 最终用户 → `StatisticsWindow`（从工具栏统计按钮打开）

**边界** —— 折线图使用 WPF `Polyline` 渲染；数据来源于 `HistoryStore`。有历史才有数据。

**参考** —— `Views/StatisticsWindow.xaml.cs`

#### 自动更新

**功能** —— 启动时检查 GitHub Releases 是否有新版本，如有则通知用户。

**角色 / 入口** —— `MainWindow` 构造函数 → `UpdateService` → GitHub API

**边界** —— 非阻塞后台检查。不会自动下载或安装——仅做通知。

**参考** —— `Resources/Services/UpdateService.cs`

### 运维

#### 单文件发布

**功能** —— 应用可通过 `dotnet publish -c Release` 发布为单个 `.exe`，打包 .NET 运行时和所有依赖。

**角色 / 入口** —— 开发者 → `publish.bat` 或 `dotnet publish` 命令

**边界** —— 输出到 `dist/` 目录。Release 为自包含；Debug 为依赖框架。

**参考** —— `CassieWordCheck.csproj` §Release 配置

## 进行中

未检测到进行中的功能。

## 计划中

未检测到计划中的功能。
