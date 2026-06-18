# 词库浏览器 — 执行计划

## 架构概览

新增一个 WPF 模态窗口 `WordListBrowserWindow`，从 `WordList.Words`（FrozenSet）读取数据，提供搜索/排序/统计功能。主窗口工具栏新增按钮打开该窗口。

### 涉及文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `Models/WordList.cs` | 修改 | 新增两个统计方法 |
| `Views/WordListBrowserWindow.xaml` | 新建 | 词库浏览器窗口布局 |
| `Views/WordListBrowserWindow.xaml.cs` | 新建 | 窗口逻辑 |
| `Views/MainWindow.xaml` | 修改 | 工具栏新增按钮 |
| `Views/MainWindow.xaml.cs` | 修改 | 按钮点击事件 |
| `Resources/Locales/zh-CN.json` | 修改 | 中文翻译 key |
| `Resources/Locales/en-US.json` | 修改 | 英文翻译 key |
| `Resources/Locales/ja-JP.json` | 修改 | 日文翻译 key |
| `Resources/Locales/ko-KR.json` | 修改 | 韩文翻译 key |
| `Resources/Locales/de-DE.json` | 修改 | 德文翻译 key |
| `Resources/Locales/ru-RU.json` | 修改 | 俄文翻译 key |
| `Resources/Locales/fr-FR.json` | 修改 | 法文翻译 key |
| `Views/AboutWindow.xaml.cs` | 修改 | 更新日志 |
| `CassieWordCheck.Tests/WordListBrowserTests.cs` | 新建 | 统计方法单元测试 |

---

## 切片 1: WordList 统计方法 + 单元测试

给 `WordList` 新增两个公开方法供浏览器窗口调用，并为其编写单元测试。

### Task 1.1: 新增词长分布统计方法

**文件**: `d:\Project\Project-C#\CassieWordCheck\Models\WordList.cs`

在类末尾（`AddPartsDirect` 方法之后）插入两个方法：

```csharp

    /// <summary>获取词长分布：词长 -> 单词数量</summary>
    public Dictionary<int, int> GetWordLengthDistribution()
    {
        var dist = new Dictionary<int, int>();
        foreach (var word in _words)
        {
            var len = word.Length;
            dist[len] = dist.GetValueOrDefault(len) + 1;
        }
        return dist;
    }

    /// <summary>获取首字母分布：首字母 -> 单词数量（不区分大小写，统一小写）</summary>
    public Dictionary<char, int> GetFirstLetterDistribution()
    {
        var dist = new Dictionary<char, int>();
        foreach (var word in _words)
        {
            if (word.Length == 0) continue;
            var ch = char.ToLowerInvariant(word[0]);
            // 只统计字母 a-z
            if (ch is >= 'a' and <= 'z')
                dist[ch] = dist.GetValueOrDefault(ch) + 1;
        }
        return dist;
    }
```

**验证**: `dotnet test d:\Project\Project-C#\CassieWordCheck\CassieWordCheck.Tests\CassieWordCheck.Tests.csproj --filter "FullyQualifiedName~WordListBrowserTests"`

### Task 1.2: 编写 WordList 统计方法单元测试

**文件**: `d:\Project\Project-C#\CassieWordCheck\CassieWordCheck.Tests\WordListBrowserTests.cs`

```csharp
using CassieWordCheck.Models;

namespace CassieWordCheck.Tests;

public class WordListBrowserTests
{
    [Fact]
    public void GetWordLengthDistribution_基本分布_返回正确计数()
    {
        // 给定
        var wl = CheckerTests.CreateWordList("cat", "dog", "elephant", "bird");

        // 当
        var dist = wl.GetWordLengthDistribution();

        // 则
        Assert.Equal(3, dist[3]); // cat, dog, bird 长度=3
        Assert.Equal(1, dist[8]); // elephant 长度=8
    }

    [Fact]
    public void GetWordLengthDistribution_空词库_返回空字典()
    {
        // 给定
        var wl = new WordList();

        // 当
        var dist = wl.GetWordLengthDistribution();

        // 则
        Assert.Empty(dist);
    }

    [Fact]
    public void GetFirstLetterDistribution_基本分布_返回正确计数()
    {
        // 给定
        var wl = CheckerTests.CreateWordList("apple", "ant", "bear", "cat", "dog");

        // 当
        var dist = wl.GetFirstLetterDistribution();

        // 则
        Assert.Equal(2, dist['a']); // apple, ant
        Assert.Equal(1, dist['b']); // bear
        Assert.Equal(1, dist['c']); // cat
        Assert.Equal(1, dist['d']); // dog
        Assert.Equal(5, dist.Values.Sum());
    }

    [Fact]
    public void GetFirstLetterDistribution_非字母开头_被忽略()
    {
        // 给定
        var wl = CheckerTests.CreateWordList("hello", "123abc", "_test");

        // 当
        var dist = wl.GetFirstLetterDistribution();

        // 则
        Assert.Equal(1, dist['h']); // 只有 hello
        Assert.Equal(1, dist.Count);
    }

    [Fact]
    public void GetFirstLetterDistribution_空词库_返回空字典()
    {
        // 给定
        var wl = new WordList();

        // 当
        var dist = wl.GetFirstLetterDistribution();

        // 则
        Assert.Empty(dist);
    }
}
```

说明：`CheckerTests.CreateWordList` 已在测试项目中存在且为 `internal`，由于同名 namespace 且在同一个测试项目内，可直接调用。如果它是 `private`，需要改为 `internal` 或 `public`。经检查实际代码中它是 `private static`，所以这里需要改为 `internal static`，或者在本文件中重新实现一个 helper。

**结论**：把 `CheckerTests.CreateWordList` 改为 `internal static`，或者在 `WordListBrowserTests` 中自己写一个相同的 helper。考虑到"外科手术式改动"原则，不在已有测试文件中做非必要改动，改为在 `WordListBrowserTests` 文件中写一个自己的 `CreateWordList` helper：

```csharp
    private static WordList CreateWordList(params string[] words)
    {
        var path = Path.GetTempFileName();
        File.WriteAllLines(path, words);
        var wl = new WordList();
        wl.LoadFromFile(path);
        return wl;
    }
```

（需添加 `using System.IO;`）

**验证**: `dotnet test d:\Project\Project-C#\CassieWordCheck\CassieWordCheck.Tests\CassieWordCheck.Tests.csproj --filter "FullyQualifiedName~WordListBrowserTests"`

---

## 切片 2: 词库浏览器窗口

新建 WPF 窗口，包含搜索框、排序切换、单词列表和统计面板。

### Task 2.1: 创建 XAML 布局

**文件**: `d:\Project\Project-C#\CassieWordCheck\Views\WordListBrowserWindow.xaml`

参照 `WhitelistWindow.xaml` 的风格（卡片 + 暗色主题），布局如下：

- 顶部：标题 + 搜索框 + 排序 ComboBox
- 中部左：单词列表（ListView）
- 中部右：统计面板（词长分布 + 首字母分布，用 Rectangle/Border 渲染柱状图）
- 底部：状态栏（总词数 / 匹配词数）

```xml
<Window x:Class="CassieWordCheck.Views.WordListBrowserWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="词库浏览器"
        Width="820" Height="580"
        MinWidth="640" MinHeight="420"
        ResizeMode="CanResizeWithGrip"
        WindowStartupLocation="CenterOwner"
        Background="{StaticResource BgBrush}"
        Foreground="{StaticResource TextPrimaryBrush}"
        Loaded="OnWindowLoaded">

    <Window.RenderTransform>
        <ScaleTransform ScaleX="1" ScaleY="1" />
    </Window.RenderTransform>

    <Border Margin="1" CornerRadius="8"
            Background="{StaticResource SurfaceBrush}"
            BorderBrush="{StaticResource BorderBrush}"
            BorderThickness="1">
        <Grid Margin="16,16,16,16">
            <Grid.RowDefinitions>
                <!-- 搜索/排序栏 -->
                <RowDefinition Height="Auto" />
                <!-- 主区域：单词列表 + 统计面板 -->
                <RowDefinition Height="*" />
                <!-- 状态栏 -->
                <RowDefinition Height="Auto" />
            </Grid.RowDefinitions>

            <!-- ═══ 顶部：搜索 + 排序 ═══ -->
            <Border Grid.Row="0"
                    CornerRadius="8"
                    Background="{StaticResource BgBrush}"
                    BorderBrush="{StaticResource BorderBrush}"
                    BorderThickness="1"
                    Padding="12,8"
                    Margin="0,0,0,12">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto" />
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="Auto" />
                        <ColumnDefinition Width="Auto" />
                        <ColumnDefinition Width="Auto" />
                    </Grid.ColumnDefinitions>

                    <TextBlock Grid.Column="0"
                               x:Name="SearchIcon"
                               Text="🔍"
                               FontSize="14"
                               VerticalAlignment="Center"
                               Margin="0,0,8,0" />

                    <TextBox Grid.Column="1"
                             x:Name="SearchBox"
                             Style="{StaticResource ModernTextBox}"
                             FontSize="13"
                             TextChanged="OnSearchTextChanged"
                             VerticalAlignment="Center" />

                    <TextBlock Grid.Column="2"
                               Text="排序"
                               Foreground="{StaticResource TextSecondaryBrush}"
                               FontSize="12"
                               VerticalAlignment="Center"
                               Margin="12,0,6,0" />

                    <ComboBox Grid.Column="3"
                              x:Name="SortCombo"
                              Style="{StaticResource ModernComboBox}"
                              Width="140"
                              VerticalAlignment="Center"
                              SelectionChanged="OnSortChanged">
                        <ComboBoxItem IsSelected="True" Content="字母升序 (A→Z)" Tag="alpha-asc" />
                        <ComboBoxItem Content="字母降序 (Z→A)" Tag="alpha-desc" />
                        <ComboBoxItem Content="词长升序 (短→长)" Tag="length-asc" />
                        <ComboBoxItem Content="词长降序 (长→短)" Tag="length-desc" />
                    </ComboBox>

                    <Button Grid.Column="4"
                            x:Name="RefreshButton"
                            Style="{StaticResource ModernButton}"
                            Content="⟳"
                            Padding="10,6"
                            Width="34"
                            FontSize="16"
                            Margin="8,0,0,0"
                            ToolTip="刷新"
                            VerticalAlignment="Center"
                            Click="OnRefresh" />
                </Grid>
            </Border>

            <!-- ═══ 主区域：单词列表 + 统计面板 ═══ -->
            <Grid Grid.Row="1">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="12" />
                    <ColumnDefinition Width="280" MinWidth="220" />
                </Grid.ColumnDefinitions>

                <!-- ─── 左：单词列表 ─── -->
                <Border Grid.Column="0"
                        CornerRadius="8"
                        Background="{StaticResource BgBrush}"
                        BorderBrush="{StaticResource BorderBrush}"
                        BorderThickness="1"
                        Padding="0">
                    <Grid>
                        <ListView x:Name="WordListView"
                                  Background="Transparent"
                                  BorderThickness="0"
                                  ScrollViewer.VerticalScrollBarVisibility="Auto"
                                  VirtualizingPanel.ScrollUnit="Item"
                                  Padding="4">
                            <ListView.ItemTemplate>
                                <DataTemplate>
                                    <Border CornerRadius="6" Padding="8,4" Margin="0,0,0,2"
                                            Background="{StaticResource SurfaceBrush}"
                                            BorderBrush="{StaticResource BorderBrush}"
                                            BorderThickness="1">
                                        <Grid>
                                            <Grid.ColumnDefinitions>
                                                <ColumnDefinition Width="*" />
                                                <ColumnDefinition Width="Auto" />
                                            </Grid.ColumnDefinitions>
                                            <TextBlock Grid.Column="0"
                                                       Text="{Binding Text}"
                                                       FontSize="13"
                                                       FontFamily="{StaticResource MonoFont}"
                                                       VerticalAlignment="Center" />
                                            <TextBlock Grid.Column="1"
                                                       Text="{Binding Length, StringFormat='{0}字'}"
                                                       Foreground="{StaticResource TextMutedBrush}"
                                                       FontSize="11"
                                                       FontFamily="{StaticResource MonoFont}"
                                                       VerticalAlignment="Center"
                                                       Margin="8,0,0,0" />
                                        </Grid>
                                    </Border>
                                </DataTemplate>
                            </ListView.ItemTemplate>
                        </ListView>

                        <TextBlock x:Name="EmptyListLabel"
                                   Text="词库为空"
                                   Foreground="{StaticResource TextMutedBrush}"
                                   FontSize="14"
                                   HorizontalAlignment="Center"
                                   VerticalAlignment="Center"
                                   Visibility="Collapsed" />
                    </Grid>
                </Border>

                <!-- ─── 右：统计面板 ─── -->
                <Border Grid.Column="2"
                        CornerRadius="8"
                        Background="{StaticResource BgBrush}"
                        BorderBrush="{StaticResource BorderBrush}"
                        BorderThickness="1"
                        Padding="12,12">
                    <Grid>
                        <Grid.RowDefinitions>
                            <!-- 词长分布 -->
                            <RowDefinition Height="Auto" />
                            <RowDefinition Height="*" />
                            <!-- 分隔 -->
                            <RowDefinition Height="Auto" />
                            <!-- 首字母分布 -->
                            <RowDefinition Height="Auto" />
                            <RowDefinition Height="*" />
                        </Grid.RowDefinitions>

                        <!-- 词长分布标题 -->
                        <TextBlock Grid.Row="0"
                                   x:Name="LengthDistTitle"
                                   Text="词长分布"
                                   Style="{StaticResource SectionLabel}"
                                   Margin="0,0,0,6" />

                        <!-- 词长分布柱状图容器 -->
                        <ScrollViewer Grid.Row="1"
                                      VerticalScrollBarVisibility="Auto"
                                      HorizontalScrollBarVisibility="Disabled"
                                      Margin="0,0,0,8">
                            <StackPanel x:Name="LengthDistPanel" />
                        </ScrollViewer>

                        <!-- 分隔线 -->
                        <Border Grid.Row="2"
                                Height="1"
                                Background="{StaticResource BorderBrush}"
                                Margin="0,4,0,8" />

                        <!-- 首字母分布标题 -->
                        <TextBlock Grid.Row="3"
                                   x:Name="FirstLetterDistTitle"
                                   Text="首字母分布"
                                   Style="{StaticResource SectionLabel}"
                                   Margin="0,0,0,6" />

                        <!-- 首字母分布柱状图容器 -->
                        <ScrollViewer Grid.Row="4"
                                      VerticalScrollBarVisibility="Auto"
                                      HorizontalScrollBarVisibility="Disabled">
                            <StackPanel x:Name="FirstLetterDistPanel" />
                        </ScrollViewer>
                    </Grid>
                </Border>
            </Grid>

            <!-- ═══ 底部：状态栏 ═══ -->
            <Border Grid.Row="2"
                    Margin="0,12,0,0"
                    CornerRadius="8"
                    Background="{StaticResource BgBrush}"
                    BorderBrush="{StaticResource BorderBrush}"
                    BorderThickness="1"
                    Padding="16,8">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto" />
                        <ColumnDefinition Width="Auto" />
                        <ColumnDefinition Width="*" />
                    </Grid.ColumnDefinitions>

                    <Border Grid.Column="0"
                            CornerRadius="6"
                            Background="#1A1A2E"
                            Padding="12,6">
                        <TextBlock x:Name="TotalWordsLabel"
                                   Text="总词数：0"
                                   Foreground="{StaticResource AccentLightBrush}"
                                   FontSize="11"
                                   FontFamily="{StaticResource MonoFont}" />
                    </Border>

                    <Border Grid.Column="1"
                            CornerRadius="6"
                            Background="#0A2E1A"
                            Padding="12,6"
                            Margin="8,0,0,0">
                        <TextBlock x:Name="FilteredWordsLabel"
                                   Text="匹配：0"
                                   Foreground="{StaticResource SuccessBrush}"
                                   FontSize="11"
                                   FontFamily="{StaticResource MonoFont}" />
                    </Border>
                </Grid>
            </Border>
        </Grid>
    </Border>
</Window>
```

### Task 2.2: 创建代码后置

**文件**: `d:\Project\Project-C#\CassieWordCheck\Views\WordListBrowserWindow.xaml.cs`

```csharp
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CassieWordCheck.Models;
using CassieWordCheck.Services;

namespace CassieWordCheck.Views;

public partial class WordListBrowserWindow : Window
{
    private readonly WordList _wordlist;
    private readonly LocalizationService _localization;
    private List<string> _allWords = [];
    private string _currentSort = "alpha-asc";
    private string _searchText = "";

    // 统计面板颜色
    private static readonly Brush BarBrush = new SolidColorBrush(Color.FromRgb(0x6C, 0x5C, 0xE7));
    private static readonly Brush BarBgBrush = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x24));
    private static readonly Brush LabelBrush = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));
    private static readonly Brush ValueBrush = new SolidColorBrush(Color.FromRgb(0x8B, 0x7C, 0xF0));

    public WordListBrowserWindow(WordList wordlist, LocalizationService localization)
    {
        InitializeComponent();
        _wordlist = wordlist;
        _localization = localization;
        this.EnableDarkTitleBar();
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        // 加载所有单词
        _allWords = [.. _wordlist.Words];
        ApplyFilterAndSort();
        RenderStatistics();

        // 窗口入场动画
        var sb = new System.Windows.Media.Animation.Storyboard();
        var scaleX = new System.Windows.Media.Animation.DoubleAnimation(0.95, 1, new Duration(TimeSpan.FromMilliseconds(350)));
        scaleX.EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut };
        System.Windows.Media.Animation.Storyboard.SetTarget(scaleX, this);
        System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleX, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));

        var scaleY = new System.Windows.Media.Animation.DoubleAnimation(0.95, 1, new Duration(TimeSpan.FromMilliseconds(350)));
        scaleY.EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut };
        System.Windows.Media.Animation.Storyboard.SetTarget(scaleY, this);
        System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleY, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));

        var fade = new System.Windows.Media.Animation.DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(300)));
        System.Windows.Media.Animation.Storyboard.SetTarget(fade, this);
        System.Windows.Media.Animation.Storyboard.SetTargetProperty(fade, new PropertyPath(OpacityProperty));

        sb.Children.Add(scaleX);
        sb.Children.Add(scaleY);
        sb.Children.Add(fade);
        sb.Begin(this);
    }

    // ── 搜索 ──────────────────────────────────────────────
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = SearchBox.Text.Trim();
        ApplyFilterAndSort();
    }

    // ── 排序 ──────────────────────────────────────────────
    private void OnSortChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SortCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            _currentSort = tag;
            ApplyFilterAndSort();
        }
    }

    // ── 刷新 ──────────────────────────────────────────────
    private void OnRefresh(object sender, RoutedEventArgs e)
    {
        _allWords = [.. _wordlist.Words];
        ApplyFilterAndSort();
        RenderStatistics();
    }

    // ── 核心：过滤 + 排序 ─────────────────────────────────
    private void ApplyFilterAndSort()
    {
        // 过滤
        var filtered = string.IsNullOrEmpty(_searchText)
            ? _allWords
            : _allWords.Where(w => w.Contains(_searchText, StringComparison.OrdinalIgnoreCase)).ToList();

        // 排序
        var sorted = _currentSort switch
        {
            "alpha-desc" => filtered.OrderByDescending(w => w).ToList(),
            "length-asc" => filtered.OrderBy(w => w.Length).ThenBy(w => w).ToList(),
            "length-desc" => filtered.OrderByDescending(w => w.Length).ThenBy(w => w).ToList(),
            _ => filtered.OrderBy(w => w).ToList(), // alpha-asc 默认
        };

        // 绑定到 ListView
        WordListView.ItemsSource = sorted.Select(w => new { Text = w, Length = w.Length }).ToList();

        // 空数据提示
        EmptyListLabel.Visibility = sorted.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        // 状态栏
        TotalWordsLabel.Text = $"总词数：{_allWords.Count}";
        FilteredWordsLabel.Text = $"匹配：{sorted.Count}";
    }

    // ── 统计面板渲染 ─────────────────────────────────────
    private void RenderStatistics()
    {
        RenderLengthDistribution();
        RenderFirstLetterDistribution();
    }

    private void RenderLengthDistribution()
    {
        LengthDistPanel.Children.Clear();
        var dist = _wordlist.GetWordLengthDistribution();
        if (dist.Count == 0)
        {
            LengthDistPanel.Children.Add(new TextBlock
            {
                Text = "无数据",
                Foreground = LabelBrush,
                FontSize = 11,
            });
            return;
        }

        var maxCount = dist.Values.Max();
        var maxBarWidth = 220.0;

        foreach (var kv in dist.OrderBy(k => k.Key))
        {
            var barWidth = Math.Max(4, maxBarWidth * kv.Value / maxCount);
            var row = new Grid { Margin = new Thickness(0, 0, 0, 3) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // 标签：词长
            row.Children.Add(new TextBlock
            {
                Text = kv.Key.ToString(),
                Foreground = LabelBrush,
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI, Consolas"),
                VerticalAlignment = VerticalAlignment.Center,
            });

            // 柱状图背景 + 前景
            var barBorder = new Border
            {
                CornerRadius = new CornerRadius(4),
                Background = BarBgBrush,
                Height = 14,
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            var barFill = new Border
            {
                CornerRadius = new CornerRadius(4),
                Background = BarBrush,
                Width = barWidth,
                Height = 14,
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            barBorder.Child = barFill;

            // 让 barBorder 占满星列
            Grid.SetColumn(barBorder, 1);
            row.Children.Add(barBorder);

            // 数值标签
            row.Children.Add(new TextBlock
            {
                Text = kv.Value.ToString(),
                Foreground = ValueBrush,
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI, Consolas"),
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            });
            Grid.SetColumn(row.Children[^1], 2);

            LengthDistPanel.Children.Add(row);
        }
    }

    private void RenderFirstLetterDistribution()
    {
        FirstLetterDistPanel.Children.Clear();
        var dist = _wordlist.GetFirstLetterDistribution();
        if (dist.Count == 0)
        {
            FirstLetterDistPanel.Children.Add(new TextBlock
            {
                Text = "无数据",
                Foreground = LabelBrush,
                FontSize = 11,
            });
            return;
        }

        var maxCount = dist.Values.Max();
        var maxBarWidth = 220.0;

        foreach (var kv in dist.OrderBy(k => k.Key))
        {
            var barWidth = Math.Max(4, maxBarWidth * kv.Value / maxCount);
            var row = new Grid { Margin = new Thickness(0, 0, 0, 2) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // 标签：首字母
            row.Children.Add(new TextBlock
            {
                Text = kv.Key.ToString().ToUpperInvariant(),
                Foreground = LabelBrush,
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI, Consolas"),
                VerticalAlignment = VerticalAlignment.Center,
            });

            // 柱状图
            var barBorder = new Border
            {
                CornerRadius = new CornerRadius(4),
                Background = BarBgBrush,
                Height = 12,
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            var barFill = new Border
            {
                CornerRadius = new CornerRadius(4),
                Background = BarBrush,
                Width = barWidth,
                Height = 12,
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            barBorder.Child = barFill;

            Grid.SetColumn(barBorder, 1);
            row.Children.Add(barBorder);

            // 数值
            row.Children.Add(new TextBlock
            {
                Text = kv.Value.ToString(),
                Foreground = ValueBrush,
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI, Consolas"),
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            });
            Grid.SetColumn(row.Children[^1], 2);

            FirstLetterDistPanel.Children.Add(row);
        }
    }
}
```

**验证**: 
```
dotnet build d:\Project\Project-C#\CassieWordCheck\CassieWordCheck.csproj
```

---

## 切片 3: 集成到主窗口 + 本地化 + 更新日志

### Task 3.1: 主窗口工具栏新增按钮

**文件**: `d:\Project\Project-C#\CassieWordCheck\Views\MainWindow.xaml`

在工具栏卡片（`ToolbarCard`）内，`HistoryButton`（Grid.Column=8）后面插入一个新按钮。需要把 Grid.ColumnDefinitions 增加到 10 列（原来是 9 列）。修改 `Grid.ColumnDefinitions` 在 `ToolbarCard` 内的那个 Grid（第 39-49 行），增加一列。

编辑：在第 48 行 `ColumnDefinition Width="Auto" />` 之后插入：
```xml
                        <ColumnDefinition Width="Auto" />
```

然后在第 138 行（`HistoryButton` 的结束标签 `/>` 之后）插入新的按钮：

```xml
                    <Button Grid.Column="9"
                            x:Name="WordListBrowserButton"
                            Style="{StaticResource ModernButton}"
                            Content="📖"
                            Padding="8,6"
                            Width="32"
                            FontSize="15"
                            Margin="4,0,0,0"
                            VerticalAlignment="Center"
                            ToolTip="词库浏览器"
                            Click="OnOpenWordListBrowser" />
```

### Task 3.2: 主窗口代码后置新增事件

**文件**: `d:\Project\Project-C#\CassieWordCheck\Views\MainWindow.xaml.cs`

在 `OnToggleHistory` 方法之后插入：

```csharp
    // ── 词库浏览器 ─────────────────────────────────────────
    private void OnOpenWordListBrowser(object sender, RoutedEventArgs e)
    {
        var dialog = new WordListBrowserWindow(_wordlist, _localization)
        {
            Owner = this,
        };
        dialog.ShowDialog();
    }
```

同时在 `UpdateUILanguage` 方法中给新按钮添加 ToolTip 本地化支持：

在 `UpdateUILanguage()` 方法中（约第 668 行附近），在 `FileOpenButton.ToolTip` 那一行之后添加：
```csharp
        WordListBrowserButton.ToolTip = "📖 " + (_localization["wordlist_browser.tooltip"]);
```

需要先在 `UpdateUILanguage` 中找到合适位置。注意到现有代码在第 668 行设置了 `FileOpenButton.ToolTip`，在这一行后面加。

### Task 3.3: 新增本地化 key

**文件**: `d:\Project\Project-C#\CassieWordCheck\Resources\Locales\zh-CN.json`

在末尾（`detail.suggestions` 条目之后）添加：
```json
  "wordlist_browser.tooltip": "词库浏览器"
```

**文件**: `d:\Project\Project-C#\CassieWordCheck\Resources\Locales\en-US.json`

类似位置添加：
```json
  "wordlist_browser.tooltip": "Word List Browser"
```

**文件**: `d:\Project\Project-C#\CassieWordCheck\Resources\Locales\ja-JP.json`

类似位置添加：
```json
  "wordlist_browser.tooltip": "単語一覧ブラウザ"
```

**文件**: `d:\Project\Project-C#\CassieWordCheck\Resources\Locales\ko-KR.json`

```json
  "wordlist_browser.tooltip": "단어 목록 브라우저"
```

**文件**: `d:\Project\Project-C#\CassieWordCheck\Resources\Locales\de-DE.json`

```json
  "wordlist_browser.tooltip": "Wortlisten-Browser"
```

**文件**: `d:\Project\Project-C#\CassieWordCheck\Resources\Locales\ru-RU.json`

```json
  "wordlist_browser.tooltip": "Обозреватель словаря"
```

**文件**: `d:\Project\Project-C#\CassieWordCheck\Resources\Locales\fr-FR.json`

```json
  "wordlist_browser.tooltip": "Navigateur de dictionnaire"
```

### Task 3.4: 更新 Changelog

**文件**: `d:\Project\Project-C#\CassieWordCheck\Views\AboutWindow.xaml.cs`

在 `ChangelogText` 常量的顶部（`## \`v2.3.2\`` 那条之前）插入新版本记录：

```markdown
## `v2.3.3`（2026-06-18）— 词库浏览器

- **新增词库浏览器窗口** — 可视化查看已加载词库的全部单词
- **实时搜索过滤** — 顶部搜索框，大小写不敏感实时过滤
- **四种排序模式** — 字母升降序、词长升降序切换
- **词长分布柱状图** — 按词长统计单词数量，Rectangle 渲染
- **首字母分布柱状图** — 按首字母 a-z 统计单词数量
- **底部状态栏** — 显示总词数 / 匹配词数
- **主窗口工具栏新增按钮** — 📖 一键打开词库浏览器
```

**验证**: 
```
dotnet build d:\Project\Project-C#\CassieWordCheck\CassieWordCheck.csproj
dotnet test d:\Project\Project-C#\CassieWordCheck\CassieWordCheck.Tests\CassieWordCheck.Tests.csproj
```
