# 三处 Bug 修复设计

## Bug 1：工具栏按钮重叠

**现象：** ⚙ 设置 / ⓘ 关于 按钮「飘」在「词库管理」按钮上方，三者堆叠重叠。

**根因：** `Views/MainWindow.xaml` 的 Grid.ColumnDefinitions 只定义到列 6（共 7 列），但按钮布局用了列 7（设置）和列 8（关于），WPF Grid 将二者都渲染到最后一列（列 6），与「词库管理」按钮位置冲突。

**修复：** 在 ColumnDefinitions 末尾追加两列 `<ColumnDefinition Width="Auto" />`。

## Bug 2：Logo 加载失败

**现象：** 工具栏图标和关于窗口图标均不显示。文件 `data/AAA.JPG`（1MB）和 `data/AAA.ico` 存在且已配 `<CopyToOutputDirectory>`。

**根因：** 当前使用 `BitmapImage(UriSource)` 从绝对路径加载 JPG，在某些环境下（如调试模式、路径含中文字符）会静默失败。所有异常被 `catch { }` 吞噬，无日志。

**修复：** 改用 `FileStream` + `BitmapImage(StreamSource)` 方式加载，提高兼容性。

## Bug 3：不可用词建议面板空框

**现象：** 建议面板弹出但三个分类区（通配/拼写/拆分）均无内容，显示空面板。

**根因：** 时序问题——`MainWindow` 构造函数中：
1. `LoadWordListAsync()` 异步加载词库（不阻塞）
2. 紧接着 `_suggestionEngine = new SuggestionEngine(_wordlist.Words)` 使用当前空词库

词库加载完成后 `_suggestionEngine` 持有的仍是空的 `FrozenSet`，导致 `LevenshteinSearch` 和 `CompoundSplit` 遍历空集合返回零结果。

**修复：** 在 `LoadWordListAsync` 的完成回调中重新创建 `_suggestionEngine`。

## 文件变更清单

| 文件 | 修改内容 |
|------|---------|
| `Views/MainWindow.xaml` | ColumnDefinitions 追加两列 |
| `Views/MainWindow.xaml.cs` | LoadToolbarIcon 改用 Stream；LoadWordListAsync 回调重建 SuggestionEngine |
| `Views/AboutWindow.xaml.cs` | LoadAppIcon 改用 Stream |
| `Views/AboutWindow.xaml.cs` | ChangelogText 新增 v2.3.3 记录 |
