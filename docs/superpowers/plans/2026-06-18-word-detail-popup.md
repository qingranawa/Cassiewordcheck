# PLAN: 单词校验详情弹窗（Word Detail Popup）

## 目标

当鼠标悬停在结果面板中的不可用（红色）单词上时，弹出一个悬浮卡片，显示该单词的：出现频率、拼写建议列表。

## 验收标准

1. 鼠标移到红色（不可用）单词上 → 悬浮卡片弹出单词详情
2. 弹窗显示：单词原文、在当前文本中出现次数、Top 3 拼写建议
3. 鼠标移出单词/移出卡片范围 → 弹窗消失，不残留
4. 弹窗不遮挡输入框的正常使用
5. 清空输入/切换文本后弹窗状态同步更新
6. 切换语言后弹窗内容使用新语言

## 涉及文件（共 10 个）

| 文件 | 操作 | 说明 |
|------|------|------|
| `Models/Checker.cs` | 修改 | 新增 `GetWordFrequency` 方法 |
| `Resources/Services/DocumentBuilder.cs` | 修改 | 给不可用 Run 附加 Tag 和鼠标事件 |
| `Views/MainWindow.xaml` | 修改 | 新增 Popup 控件定义 |
| `Views/MainWindow.xaml.cs` | 修改 | 弹窗显示/隐藏逻辑、词频缓存 |
| `Resources/Locales/zh-CN.json` | 修改 | 新增 2 条翻译 key |
| `Resources/Locales/en-US.json` | 修改 | 同上 |
| `Resources/Locales/ja-JP.json` | 修改 | 同上 |
| `Resources/Locales/ko-KR.json` | 修改 | 同上 |
| `Resources/Locales/de-DE.json` | 修改 | 同上 |
| `Resources/Locales/ru-RU.json` | 修改 | 同上 |
| `Resources/Locales/fr-FR.json` | 修改 | 同上 |

## 切片任务

### Task 1: Checker 新增词频统计 + Locales 新增翻译 key

**任务描述**：在 `Checker` 中新增 `GetWordFrequency` 方法，统计当前检查结果中每个不可用词的出现次数。同时在 7 个语言文件中新增弹窗相关翻译 key。

#### Task 1.1: Checker.cs — 新增 GetWordFrequency

**文件**: `d:\Project\Project-C#\CassieWordCheck\Models\Checker.cs`

**操作**: 在 `GetStatistics` 方法后新增一个方法

**代码**: 在 `GetStatistics` 方法（约第 197 行）之后、正则区域之前插入：

```csharp
/// <summary>统计当前检查结果中每个不可用词的出现频率喵~</summary>
public Dictionary<string, int> GetWordFrequency(List<CheckResult> results)
{
    var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    foreach (var r in results.Where(r => r.Status == CheckStatus.Unavailable))
    {
        freq[r.Text] = freq.GetValueOrDefault(r.Text, 0) + 1;
    }
    return freq;
}
```

**验证**: 代码编译通过（无语法错误）

---

#### Task 1.2: 7 个 Locales JSON — 新增翻译 key

**文件列表** (7 个文件):
- `d:\Project\Project-C#\CassieWordCheck\Resources\Locales\zh-CN.json`
- `d:\Project\Project-C#\CassieWordCheck\Resources\Locales\en-US.json`
- `d:\Project\Project-C#\CassieWordCheck\Resources\Locales\ja-JP.json`
- `d:\Project\Project-C#\CassieWordCheck\Resources\Locales\ko-KR.json`
- `d:\Project\Project-C#\CassieWordCheck\Resources\Locales\de-DE.json`
- `d:\Project\Project-C#\CassieWordCheck\Resources\Locales\ru-RU.json`
- `d:\Project\Project-C#\CassieWordCheck\Resources\Locales\fr-FR.json`

**操作**: 在 JSON 末尾（`import.done` 之前或 `update.*` 之前等合适位置）新增以下 key：

对 `zh-CN.json`：
```json
"detail.frequency": "出现 {0} 次",
"detail.suggestions": "拼写建议"
```

对 `en-US.json`：
```json
"detail.frequency": "Appears {0} time(s)",
"detail.suggestions": "Suggestions"
```

对 `ja-JP.json`：
```json
"detail.frequency": "出現 {0} 回",
"detail.suggestions": "スペル提案"
```

对 `ko-KR.json`：
```json
"detail.frequency": "출연 {0}회",
"detail.suggestions": "철자 제안"
```

对 `de-DE.json`：
```json
"detail.frequency": "Erscheint {0} Mal",
"detail.suggestions": "Vorschlage"
```

对 `ru-RU.json`：
```json
"detail.frequency": "Появляется {0} раз(а)",
"detail.suggestions": "Предложения"
```

对 `fr-FR.json`：
```json
"detail.frequency": "Apparait {0} fois",
"detail.suggestions": "Suggestions"
```

**验证**: 所有 JSON 文件格式有效（无 trailing comma，花括号匹配）

---

### Task 2: DocumentBuilder 改动 — 给不可用 Run 挂 Tag 和鼠标事件

**文件**: `d:\Project\Project-C#\CassieWordCheck\Resources\Services\DocumentBuilder.cs`

**操作**: 
1. 在 `BuildResultDocument` 方法中，对不可用 `Run` 附加 `Tag` 存储 `CheckResult`，并注册鼠标事件

**注意**: `Run` 继承自 `Inline`，不是 `UIElement`，不能直接挂 `MouseEnter`/`MouseLeave`。需要在 `Paragraph` 或 `RichTextBox` 上用 `AddHandler` 注册路由事件。此处给 Run 附加 Tag 供弹窗使用，事件在 Task 4 中通过 `Run.MouseEnter` 路由事件实现。

**代码**:

在 `var run = new Run(r.Text)` 之后（约第 46 行）、设置 `Foreground` 之前，保持现有逻辑不变。

修改不可用词区块（约第 57-62 行）：

```csharp
// 不可用词加下划线 + 悬停提示 + 存储原始检查结果喵~
if (r.Status == CheckStatus.Unavailable)
{
    run.TextDecorations = TextDecorations.Underline;
    run.ToolTip = null; // 移除默认 ToolTip，改用 Popup
    run.Tag = r; // 存储 CheckResult 供 Popup 使用
}
```

并添加 using：
```csharp
// 文件顶部已有 using CassieWordCheck.Models; ，确认存在即可
```

**验证**: 代码编译通过（Run.Tag 是 Inline 的基类 DependencyObject 的属性，可用）

---

### Task 3: MainWindow.xaml — 新增 Popup 控件

**文件**: `d:\Project\Project-C#\CassieWordCheck\Views\MainWindow.xaml`

**操作**: 在 `ResultCard` 的 Grid 内，`RichTextBox`（ResultBox）之后新增一个 Popup

**代码**: 在 `</RichTextBox>` 之后、`</Grid>`（ResultCard 的 Grid 结束）之前插入：

```xml
                    <!-- 单词详情浮窗（鼠标悬停不可用词时弹出） -->
                    <Popup x:Name="WordDetailPopup"
                           Placement="Mouse"
                           AllowsTransparency="True"
                           PopupAnimation="Fade"
                           StaysOpen="False"
                           IsOpen="False">
                        <Border CornerRadius="12"
                                Background="{StaticResource SurfaceBrush}"
                                BorderBrush="{StaticResource BorderBrush}"
                                BorderThickness="1"
                                Padding="16,12"
                                MaxWidth="320">
                            <Border.Effect>
                                <DropShadowEffect BlurRadius="14" Opacity="0.4" ShadowDepth="4" Direction="270" Color="#000000" />
                            </Border.Effect>
                            <StackPanel>
                                <!-- 单词标题 -->
                                <TextBlock x:Name="PopupWordText"
                                           FontSize="15" FontWeight="SemiBold"
                                           Foreground="{StaticResource ErrorBrush}"
                                           FontFamily="{StaticResource MonoFont}" />

                                <!-- 频率统计 -->
                                <Grid Margin="0,8,0,0">
                                    <Border CornerRadius="6" Background="#2D1B1B" Padding="10,6">
                                        <TextBlock x:Name="PopupFreqText"
                                                   FontSize="11"
                                                   Foreground="{StaticResource ErrorBrush}"
                                                   FontFamily="{StaticResource MonoFont}" />
                                    </Border>
                                </Grid>

                                <!-- 分隔线 -->
                                <Rectangle Height="1" Fill="{StaticResource BorderBrush}"
                                           Margin="0,8,0,8" />

                                <!-- 拼写建议标题 -->
                                <TextBlock x:Name="PopupSuggestionTitle"
                                           FontSize="11"
                                           Foreground="{StaticResource TextMutedBrush}" />

                                <!-- 建议列表 -->
                                <ItemsControl x:Name="PopupSuggestionList"
                                              Margin="0,6,0,0">
                                    <ItemsControl.ItemsPanel>
                                        <ItemsPanelTemplate>
                                            <StackPanel />
                                        </ItemsPanelTemplate>
                                    </ItemsControl.ItemsPanel>
                                    <ItemsControl.ItemTemplate>
                                        <DataTemplate>
                                            <Border Background="#1A1A2E"
                                                    CornerRadius="6"
                                                    Padding="8,4" Margin="0,0,0,4">
                                                <TextBlock Text="{Binding}"
                                                           FontSize="12"
                                                           Foreground="{StaticResource AccentLightBrush}"
                                                           FontFamily="{StaticResource MonoFont}" />
                                            </Border>
                                        </DataTemplate>
                                    </ItemsControl.ItemTemplate>
                                </ItemsControl>
                            </StackPanel>
                        </Border>
                    </Popup>
```

**验证**: XAML 无编译错误

---

### Task 4: MainWindow.xaml.cs — 弹窗显示/隐藏逻辑

**文件**: `d:\Project\Project-C#\CassieWordCheck\Views\MainWindow.xaml.cs`

**操作**:
1. 新增 `_currentWordFreq` 字段
2. 在 `UpdateResult` 中计算词频缓存
3. 给 `ResultBox` 挂载 `MouseMove` 事件
4. 新增 `ShowWordDetailPopup` 和 `HideWordDetailPopup` 方法
5. 在 `UpdateUILanguage` 中更新弹窗文本

**代码**:

**Step 4.1** — 新增字段（约第 28 行，`_snapshotCoverage` 之后）：

```csharp
    private double _snapshotCoverage;
    // 当前文本每个不可用词的出现频率（由 UpdateResult 计算）
    private Dictionary<string, int> _currentWordFreq = new(StringComparer.OrdinalIgnoreCase);
```

**Step 4.2** — 在 `UpdateResult` 中计算词频（约第 183 行，`var results = _checker.CheckText(text);` 之后）：

```csharp
    var results = _checker.CheckText(text);
    _currentWordFreq = _checker.GetWordFrequency(results);
```

**Step 4.3** — 给 ResultBox 注册路由事件 `Run.MouseEnter` 和 `Run.MouseLeave`（约第 65 行，`_debounceTimer.Tick += OnDebounceTick;` 之后）：

```csharp
    // 不可用 Run 的 MouseEnter/MouseLeave 路由事件 —— 触发单词详情弹窗
    ResultBox.AddHandler(Run.MouseEnterEvent, new MouseEventHandler(OnRunMouseEnter));
    ResultBox.AddHandler(Run.MouseLeaveEvent, new MouseEventHandler(OnRunMouseLeave));
```

注意：`Run.MouseEnterEvent` / `Run.MouseLeaveEvent` 是路由事件，注册在 RichTextBox 上可捕获所有子 Inline 的鼠标事件。

**Step 4.4** — 新增新字段 + 4 个方法（新字段在字段区域，方法在 `FormatSuggestions` 方法之后，约第 377 行）：

新增字段（与 Step 4.1 的 `_currentWordFreq` 放在一起）：

```csharp
    private double _snapshotCoverage;
    // 当前文本每个不可用词的出现频率（由 UpdateResult 计算）
    private Dictionary<string, int> _currentWordFreq = new(StringComparer.OrdinalIgnoreCase);
    // 弹窗防抖计时器（鼠标快速扫过单词时不反复闪烁）
    private readonly DispatcherTimer _popupDebounceTimer = new();
```

在构造函数中初始化（与 Step 4.3 放一起）：

```csharp
    // 弹窗防抖：鼠标离开单词后延迟 100ms 关闭
    _popupDebounceTimer.Interval = TimeSpan.FromMilliseconds(100);
    _popupDebounceTimer.Tick += (_, _) =>
    {
        _popupDebounceTimer.Stop();
        WordDetailPopup.IsOpen = false;
    };
```

新增 4 个方法（在 `FormatSuggestions` 方法之后）：

```csharp
    // ── 单词详情弹窗 ──────────────────────────────────
    private void OnRunMouseEnter(object sender, MouseEventArgs e)
    {
        // 停止防抖（鼠标又回来了）
        _popupDebounceTimer.Stop();

        if (e.OriginalSource is Run run && run.Tag is CheckResult cr && cr.Status == CheckStatus.Unavailable)
        {
            ShowWordDetailPopup(cr.Text);
        }
    }

    private void OnRunMouseLeave(object sender, MouseEventArgs e)
    {
        // 启动 100ms 防抖，避免单词间快速移动时闪烁
        _popupDebounceTimer.Start();
    }

    private void ShowWordDetailPopup(string word)
    {
        PopupWordText.Text = word;

        // 频率统计
        var freq = _currentWordFreq.GetValueOrDefault(word, 0);
        PopupFreqText.Text = string.Format(_localization["detail.frequency"], freq);

        // 拼写建议
        PopupSuggestionTitle.Text = _localization["detail.suggestions"];
        var suggestions = LevenshteinHelper.FindClosest(word, _wordlist.Words, 3);
        PopupSuggestionList.ItemsSource = suggestions.Select(s => s.word).ToList();

        WordDetailPopup.IsOpen = true;
    }

    private void HideWordDetailPopup()
    {
        WordDetailPopup.IsOpen = false;
    }
```

**Step 4.5** — 在 `UpdateUILanguage` 中更新弹窗文本（该方法中已有大量 UI 更新代码，保持不变，因为弹窗是动态构建的，`ShowWordDetailPopup` 每次都会用当前语言重新读取 `_localization`）

**验证**: 代码编译通过，运行后：
- 输入一段包含不可用词的文本
- 鼠标悬停在红色单词上 → 弹出浮窗
- 鼠标移出 → 浮窗消失
- 切换语言 → 浮窗使用新语言

---

## 执行顺序

```
Task 1.1 (Checker.cs) ─┐
                        ├─→ Task 2 (DocumentBuilder.cs) ─→ Task 3 (XAML) ─→ Task 4 (xaml.cs)
Task 1.2 (Locales) ────┘
```

Task 1.1 和 Task 1.2 可并行执行，之后按 2 → 3 → 4 顺序执行。

## 回滚方案

如需回滚：
1. `git checkout -- Models/Checker.cs Resources/Services/DocumentBuilder.cs Views/MainWindow.xaml Views/MainWindow.xaml.cs`
2. 手动恢复 7 个 Locales JSON（移除新增的 2 个 key）
