# PLAN: 结果面板排版模式切换

## 目标

在工具栏新增模式切换 ComboBox，支持三种结果展示模式：内嵌模式（现有）、列表模式（逐词成行）、两栏对比模式（可用词/不可用词分栏）。切换时有淡入淡出动画，模式持久化到 `appsettings.json`。

## 验收标准

1. 工具栏新增 ComboBox，可选"内嵌模式""列表模式""两栏对比"
2. 默认"内嵌模式"，与现有行为一致
3. 切换到列表模式 → 每个单词一行，带颜色圆点+状态标签（"可用""不可用""已忽略"）
4. 切换到两栏模式 → 左栏可用词列表（绿色），右栏不可用词列表（红色），去重排序
5. 模式切换有淡出→淡入过渡动画（约 350ms）
6. 模式状态保存到 `appsettings.json`，重启恢复
7. 输入新文本后列表/两栏视图同步刷新

## 涉及文件（共 11 个）

| 文件 | 操作 | 说明 |
|------|------|------|
| `Models/Settings.cs` | 修改 | 新增 `ResultMode` 属性 + 序列化 DTO 字段 |
| `Views/MainWindow.xaml` | 修改 | 新增 ModeCombo + ResultListView + ResultCompareGrid |
| `Views/MainWindow.xaml.cs` | 修改 | 模式切换逻辑、视图构建、过渡动画 |
| `Resources/Locales/zh-CN.json` | 修改 | 新增 3 条翻译 key |
| `Resources/Locales/en-US.json` | 修改 | 同上 |
| `Resources/Locales/ja-JP.json` | 修改 | 同上 |
| `Resources/Locales/ko-KR.json` | 修改 | 同上 |
| `Resources/Locales/de-DE.json` | 修改 | 同上 |
| `Resources/Locales/ru-RU.json` | 修改 | 同上 |
| `Resources/Locales/fr-FR.json` | 修改 | 同上 |

## 切片任务

### Task 1: Settings.cs 新增 ResultMode 属性

**文件**: `d:\Project\Project-C#\CassieWordCheck\Models\Settings.cs`

**操作**: 新增 `ResultMode` 属性（约第 26 行，`WordWrap` 之后），同步更新 `SettingsData` 记录类

**Task 1.1** — 在 `WordWrap` 属性之后新增：

```csharp
    public bool WordWrap { get; set; } = true;
    /// <summary>结果面板排版模式：inline / list / compare 喵~</summary>
    public string ResultMode { get; set; } = "inline";
```

**Task 1.2** — 在 `Load()` 方法的属性赋值区（约第 60 行，`WordWrap` 赋值后）新增：

```csharp
            WordWrap = data.WordWrap;
            ResultMode = data.ResultMode ?? "inline";
```

**Task 1.3** — 在 `Save()` 方法的 `SettingsData` 构建区（约第 84 行，`WordWrap` 赋值后）新增：

```csharp
                WordWrap = WordWrap,
                ResultMode = ResultMode,
```

**Task 1.4** — 在 `SettingsData` 记录类中（约第 107 行，`WordWrap` 后）新增：

```csharp
        public bool WordWrap { get; init; } = true;
        public string? ResultMode { get; init; }
```

**验证**: 代码编译通过

---

### Task 2: 7 个 Locales JSON — 新增模式名称翻译

**7 个文件**（路径同方向一）：
- `d:\Project\Project-C#\CassieWordCheck\Resources\Locales\zh-CN.json`
- `d:\Project\Project-C#\CassieWordCheck\Resources\Locales\en-US.json`
- 其余 5 个同目录

**操作**: 每个 JSON 文件末尾新增 3 条 key：

**zh-CN.json:**
```json
"mode.inline": "内嵌模式",
"mode.list": "列表模式",
"mode.compare": "两栏对比"
```

**en-US.json:**
```json
"mode.inline": "Inline",
"mode.list": "List View",
"mode.compare": "Compare"
```

**ja-JP.json:**
```json
"mode.inline": "インラインモード",
"mode.list": "リストモード",
"mode.compare": "比較モード"
```

**ko-KR.json:**
```json
"mode.inline": "인라인 모드",
"mode.list": "목록 모드",
"mode.compare": "비교 모드"
```

**de-DE.json:**
```json
"mode.inline": "Inline-Modus",
"mode.list": "Listenansicht",
"mode.compare": "Vergleich"
```

**ru-RU.json:**
```json
"mode.inline": "Встроенный",
"mode.list": "Список",
"mode.compare": "Сравнение"
```

**fr-FR.json:**
```json
"mode.inline": "Mode Inline",
"mode.list": "Mode Liste",
"mode.compare": "Comparer"
```

**验证**: 所有 JSON 文件格式有效

---

### Task 3: MainWindow.xaml — 新增切换器 + 列表/两栏容器

**文件**: `d:\Project\Project-C#\CassieWordCheck\Views\MainWindow.xaml`

**操作**: 3 处修改

#### Task 3.1 — 工具栏新增 ModeCombo

在工具栏第 6 列（`StatsButton` 之前）、`AboutButton` 之后插入一个新的 `Grid.Column`：

修改现有 `Grid.ColumnDefinitions` 的末尾（约第 48 行），在倒数第二个 `Width="Auto"` 前新增一列：

```xml
                        <ColumnDefinition Width="Auto" />
                        <ColumnDefinition Width="Auto" />  <!-- 新增：模式切换 -->
                        <ColumnDefinition Width="Auto" />  <!-- 原来是 StatsButton 列 -->
```

在 `AboutButton` 之后（约第 107 行）、`StatsButton` 之前插入：

```xml
                    <ComboBox Grid.Column="6"
                              x:Name="ModeCombo"
                              Style="{StaticResource ModernComboBox}"
                              Width="110"
                              Margin="4,0,8,0"
                              VerticalAlignment="Center"
                              SelectionChanged="OnModeChanged" />
```

注意：后面所有按钮的 `Grid.Column` 值 +1（`StatsButton` col 6→7, `HistoryButton` col 7→8）

**验证**: XAML 无编译错误，工具栏布局正常

#### Task 3.2 — 模板按钮列号修正（6→7, 7→8）

将 StatsButton 的 `Grid.Column="6"` 改为 `Grid.Column="7"`，HistoryButton 的 `Grid.Column="7"` 改为 `Grid.Column="8"`。

#### Task 3.3 — 结果卡片内新增列表模式容器

在 `ResultBox`（RichTextBox）的 `</RichTextBox>` 之后（约第 330 行）插入 `ResultListView`：

```xml
                    <!-- 列表模式（每个单词一行） -->
                    <ListView x:Name="ResultListView"
                              Visibility="Collapsed"
                              Grid.Row="1"
                              Background="Transparent"
                              BorderThickness="0"
                              ScrollViewer.VerticalScrollBarVisibility="Auto"
                              Padding="16,12"
                              FontSize="14"
                              FontFamily="{StaticResource MonoFont}">
                        <ListView.ItemTemplate>
                            <DataTemplate>
                                <Border CornerRadius="6" Padding="8,4" Margin="0,0,0,2"
                                        Background="{StaticResource SurfaceBrush}"
                                        BorderBrush="{StaticResource BorderBrush}"
                                        BorderThickness="1">
                                    <Grid>
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width="Auto" />
                                            <ColumnDefinition Width="*" />
                                            <ColumnDefinition Width="Auto" />
                                        </Grid.ColumnDefinitions>
                                        <Ellipse Width="8" Height="8"
                                                 Fill="{Binding StatusColor}"
                                                 VerticalAlignment="Center" />
                                        <TextBlock Grid.Column="1" Text="{Binding Text}"
                                                   FontSize="14"
                                                   FontFamily="{StaticResource MonoFont}"
                                                   Foreground="{StaticResource TextPrimaryBrush}"
                                                   Margin="10,0,0,0"
                                                   VerticalAlignment="Center" />
                                        <Border Grid.Column="2"
                                                CornerRadius="4" Padding="6,2"
                                                Background="{Binding StatusBg}"
                                                VerticalAlignment="Center">
                                            <TextBlock Text="{Binding StatusLabel}"
                                                       FontSize="11"
                                                       Foreground="{Binding StatusColor}" />
                                        </Border>
                                    </Grid>
                                </Border>
                            </DataTemplate>
                        </ListView.ItemTemplate>
                    </ListView>
```

#### Task 3.4 — 新增两栏对比模式容器

在 `ResultListView` 之后插入：

```xml
                    <!-- 两栏对比模式 -->
                    <Grid x:Name="ResultCompareGrid"
                          Visibility="Collapsed"
                          Grid.Row="1"
                          Margin="16,12,16,12">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*" />
                            <ColumnDefinition Width="Auto" />
                            <ColumnDefinition Width="*" />
                        </Grid.ColumnDefinitions>

                        <!-- 左栏：可用词 -->
                        <Border Grid.Column="0" CornerRadius="8" Background="#0A2E1A"
                                BorderBrush="{StaticResource BorderBrush}" BorderThickness="1"
                                Padding="12,8">
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="Auto" />
                                    <RowDefinition Height="*" />
                                </Grid.RowDefinitions>
                                <TextBlock Text="可用词" FontSize="12" FontWeight="SemiBold"
                                           Foreground="{StaticResource SuccessBrush}"
                                           Margin="0,0,0,8" />
                                <ListBox x:Name="AvailableList" Grid.Row="1"
                                         Background="Transparent" BorderThickness="0"
                                         ScrollViewer.VerticalScrollBarVisibility="Auto">
                                    <ListBox.ItemTemplate>
                                        <DataTemplate>
                                            <TextBlock Text="{Binding}" FontSize="13"
                                                       FontFamily="{StaticResource MonoFont}"
                                                       Foreground="{StaticResource SuccessBrush}"
                                                       Padding="4,2" />
                                        </DataTemplate>
                                    </ListBox.ItemTemplate>
                                </ListBox>
                            </Grid>
                        </Border>

                        <!-- 分隔线 -->
                        <Border Grid.Column="1" Width="1"
                                Background="{StaticResource BorderBrush}"
                                Margin="12,0" />

                        <!-- 右栏：不可用词 -->
                        <Border Grid.Column="2" CornerRadius="8" Background="#2D1B1B"
                                BorderBrush="{StaticResource BorderBrush}" BorderThickness="1"
                                Padding="12,8">
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="Auto" />
                                    <RowDefinition Height="*" />
                                </Grid.RowDefinitions>
                                <TextBlock Text="不可用词" FontSize="12" FontWeight="SemiBold"
                                           Foreground="{StaticResource ErrorBrush}"
                                           Margin="0,0,0,8" />
                                <ListBox x:Name="UnavailableList" Grid.Row="1"
                                         Background="Transparent" BorderThickness="0"
                                         ScrollViewer.VerticalScrollBarVisibility="Auto">
                                    <ListBox.ItemTemplate>
                                        <DataTemplate>
                                            <TextBlock Text="{Binding}" FontSize="13"
                                                       FontFamily="{StaticResource MonoFont}"
                                                       Foreground="{StaticResource ErrorBrush}"
                                                       Padding="4,2" />
                                        </DataTemplate>
                                    </ListBox.ItemTemplate>
                                </ListBox>
                            </Grid>
                        </Border>
                    </Grid>
```

---

### Task 4: MainWindow.xaml.cs — 模式切换逻辑

**文件**: `d:\Project\Project-C#\CassieWordCheck\Views\MainWindow.xaml.cs`

**操作**: 5 处改动

#### Task 4.1 — 新增字段（约第 29 行，`_snapshotCoverage` 之后）

```csharp
    // 结果面板排版模式（inline / list / compare）
    private string _currentMode = "inline";
```

#### Task 4.2 — 构造函数中初始化 ComboBox（约第 65 行，`_debounceTimer.Tick += ...` 之后）

```csharp
    // 加载结果面板排版模式
    _currentMode = _settings.ResultMode;
    PopulateModeCombo();
```

#### Task 4.3 — 新增 PopulateModeCombo 方法、OnModeChanged 事件、BuildModeViews 方法（在 `FormatSuggestions` 方法之后）

```csharp
    // ── 结果面板排版模式切换 ──────────────────────────
    private void PopulateModeCombo()
    {
        ModeCombo.Items.Clear();
        // 使用匿名类型 + SelectedValuePath 实现 ComboBox 值绑定
        var modes = new[]
        {
            new { Text = _localization["mode.inline"], Value = "inline" },
            new { Text = _localization["mode.list"], Value = "list" },
            new { Text = _localization["mode.compare"], Value = "compare" },
        };
        foreach (var m in modes)
            ModeCombo.Items.Add(m);
        ModeCombo.SelectedValuePath = "Value";
        ModeCombo.DisplayMemberPath = "Text";
        ModeCombo.SelectedValue = _currentMode;
    }

    private void OnModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ModeCombo.SelectedValue is string mode && mode != _currentMode)
        {
            _currentMode = mode;
            _settings.ResultMode = mode;
            _settings.Save();
            SwitchModeView(mode);
        }
    }

    private void SwitchModeView(string mode)
    {
        // 淡出当前可见的视图
        var activeViews = new FrameworkElement[] { ResultBox, ResultListView, ResultCompareGrid };
        foreach (var v in activeViews)
        {
            if (v.Visibility == Visibility.Visible)
                Animate(v, UIElement.OpacityProperty, 1, 0, 150, null);
        }

        // 160ms 后切换可见性并淡入
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(160) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            ResultBox.Visibility = mode == "inline" ? Visibility.Visible : Visibility.Collapsed;
            ResultListView.Visibility = mode == "list" ? Visibility.Visible : Visibility.Collapsed;
            ResultCompareGrid.Visibility = mode == "compare" ? Visibility.Visible : Visibility.Collapsed;

            foreach (var v in activeViews)
            {
                if (v.Visibility == Visibility.Visible)
                {
                    v.Opacity = 0;
                    Animate(v, UIElement.OpacityProperty, 0, 1, 200, null);
                }
            }
        };
        timer.Start();
    }

    private void BuildModeViews(List<CheckResult> results)
    {
        // 列表模式数据
        var colorSuccess = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
        var colorError = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
        var colorIgnored = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));
        var bgSuccess = new SolidColorBrush(Color.FromRgb(0x0A, 0x2E, 0x1A));
        var bgError = new SolidColorBrush(Color.FromRgb(0x2D, 0x1B, 0x1B));
        var bgIgnored = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x24));

        ResultListView.ItemsSource = results.Select(r => new
        {
            Text = r.Text,
            StatusColor = r.Status switch
            {
                CheckStatus.Available => colorSuccess,
                CheckStatus.Unavailable => colorError,
                _ => colorIgnored,
            },
            StatusLabel = r.Status switch
            {
                CheckStatus.Available => "可用",
                CheckStatus.Unavailable => "不可用",
                CheckStatus.Ignored => "已忽略",
                _ => "",
            },
            StatusBg = r.Status switch
            {
                CheckStatus.Available => bgSuccess,
                CheckStatus.Unavailable => bgError,
                _ => bgIgnored,
            },
        }).ToList();

        // 两栏模式数据
        var available = results
            .Where(r => r.Status == CheckStatus.Available)
            .Select(r => r.Text)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(w => w)
            .ToList();
        var unavailable = results
            .Where(r => r.Status == CheckStatus.Unavailable)
            .Select(r => r.Text)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(w => w)
            .ToList();

        AvailableList.ItemsSource = available;
        UnavailableList.ItemsSource = unavailable;
    }
```

#### Task 4.4 — 在 UpdateResult 中调用 BuildModeViews（约第 186 行，`ResultBox.Document = ...` 之后）

```csharp
        ResultBox.Document = DocumentBuilder.BuildResultDocument(results, ResultBox.ActualWidth, _settings.FontSize);
        BuildModeViews(results);  // 同步更新列表和两栏视图
```

#### Task 4.5 — 在 UpdateUILanguage 末尾（约第 512 行 `UpdateResult()` 之前）刷新 ComboBox

```csharp
        // 刷新模式 ComboBox 语言
        PopulateModeCombo();
```

**验证**: 代码编译通过，运行后：
- 工具栏有模式切换下拉，3 个选项
- 默认内嵌模式，行为与之前完全一致
- 切换到列表模式 → 逐词成行显示
- 切换到两栏模式 → 左右分栏
- 模式切换有淡入淡出动画
- 重启应用后模式恢复

---

## 执行顺序

```
Task 1 (Settings.cs) ─┐
                       ├─→ Task 3 (XAML) ─→ Task 4 (xaml.cs)
Task 2 (Locales) ─────┘
```

Task 1 和 Task 2 可并行，之后按 Task 3 → Task 4 顺序。

## 回滚方案

```bash
git checkout -- Models/Settings.cs Views/MainWindow.xaml Views/MainWindow.xaml.cs
# 手动从 7 个 Locales JSON 中移除 3 条 mode.* key
```
