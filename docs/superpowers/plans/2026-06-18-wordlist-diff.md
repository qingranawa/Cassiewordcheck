# 词库差异对比 — 执行计划

## 架构概览

在 `WordList` 上新增 `DiffWith` 方法返回差异结果集，新建极简对比窗口 `DiffWindow` 显示差异和导出报告。主窗口菜单新增入口。

### 涉及文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `Models/WordList.cs` | 修改 | 新增 `DiffWith` 方法和 `WordListDiff` 记录 |
| `Views/DiffWindow.xaml` | **新建** | 极简对比窗口布局 |
| `Views/DiffWindow.xaml.cs` | **新建** | 窗口逻辑（显示差异 + 导出） |
| `Views/MainWindow.xaml` | 修改 | 工具栏新增入口按钮 |
| `Views/MainWindow.xaml.cs` | 修改 | 按钮事件 |
| `Resources/Locales/*.json` (8 个) | 修改 | 新增少量翻译 key |
| `Views/AboutWindow.xaml.cs` | 修改 | 更新日志 |

---

## 切片 1: WordListDiff 记录 + DiffWith 方法 + 入口

### Task 1.1: 新增 WordListDiff 记录和 DiffWith 方法

**文件**: `d:\Project\Project-C#\CassieWordCheck\Models\WordList.cs`

在文件顶部 namespace 声明后插入：

```csharp
/// <summary>词库差异对比结果</summary>
public record WordListDiff(
    string LeftLabel,          // 左词库的描述（文件名或自定义标签）
    string RightLabel,         // 右词库的描述
    int LeftOnlyCount,         // 仅在左词库中
    int RightOnlyCount,        // 仅在右词库中
    int CommonCount,           // 两词库共有
    IReadOnlySet<string> LeftOnly,    // 仅在左的词集合
    IReadOnlySet<string> RightOnly    // 仅在右的词集合
);
```

在 `WordList` 类末尾（`GetFirstLetterDistribution` 方法之后）插入：

```csharp
    /// <summary>与另一个词库对比差异，返回新增/移除/共有关信息</summary>
    public WordListDiff DiffWith(WordList other, string? leftLabel = null, string? rightLabel = null)
    {
        var leftOnly = _words.Except(other._words).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        var rightOnly = other._words.Except(_words).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        var common = _words.Intersect(other._words).ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        return new WordListDiff(
            leftLabel ?? Path.GetFileName(_sourcePath) ?? "左词库",
            rightLabel ?? Path.GetFileName(other._sourcePath) ?? "右词库",
            leftOnly.Count,
            rightOnly.Count,
            common.Count,
            leftOnly,
            rightOnly
        );
    }
```

### Task 1.2: 主窗口菜单新增入口

**文件**: `d:\Project\Project-C#\CassieWordCheck\Views\MainWindow.xaml`

在已有工具栏按钮后追加一个文字按钮（不加图标，避免工具栏拥挤）。用 Grid.Column="10"（已有 0-9 共 10 列，需扩展到 11 列）。或者更简单——放在 `AboutButton` 旁边，把菜单按钮排得更紧凑。

最简单的方式：在 `WordListBrowserButton`（Grid.Column=9）后面再加一列和按钮。在 Grid.ColumnDefinitions 里增加第 11 列。

**修改步骤**（约第 40-50 行）：
在最后一个 `<ColumnDefinition Width="Auto" />` 后面再插入一个 `<ColumnDefinition Width="Auto" />`

**新增按钮**（在 WordListBrowserButton 后面）：
```xml
                    <Button Grid.Column="10"
                            x:Name="DiffButton"
                            Style="{StaticResource ModernButton}"
                            Content="⇄"
                            Padding="8,6"
                            Width="32"
                            FontSize="15"
                            Margin="4,0,0,0"
                            VerticalAlignment="Center"
                            ToolTip="词库对比"
                            Click="OnOpenDiff" />
```

**文件**: `d:\Project\Project-C#\CassieWordCheck\Views\MainWindow.xaml.cs`

在 `OnOpenWordListBrowser` 方法之后插入：

```csharp
    // ── 词库对比 ─────────────────────────────────────────
    private void OnOpenDiff(object sender, RoutedEventArgs e)
    {
        var dialog = new DiffWindow(_wordlist, _localization)
        {
            Owner = this,
        };
        dialog.ShowDialog();
    }
```

在 `UpdateUILanguage` 方法中追加 tooltip 国际化：
```csharp
        DiffButton.ToolTip = _localization["diff.tooltip"];
```

---

## 切片 2: DiffWindow 窗口（极简）

### Task 2.1: XAML 布局

**文件**: `d:\Project\Project-C#\CassieWordCheck\Views\DiffWindow.xaml`

```xml
<Window x:Class="CassieWordCheck.Views.DiffWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="词库对比"
        Width="700" Height="480"
        MinWidth="520" MinHeight="360"
        ResizeMode="CanResizeWithGrip"
        WindowStartupLocation="CenterOwner"
        Background="{StaticResource BgBrush}"
        Foreground="{StaticResource TextPrimaryBrush}"
        Loaded="OnWindowLoaded">

    <Border Margin="1" CornerRadius="8"
            Background="{StaticResource SurfaceBrush}"
            BorderBrush="{StaticResource BorderBrush}"
            BorderThickness="1">
        <Grid Margin="16,12,16,12">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
                <RowDefinition Height="Auto" />
            </Grid.RowDefinitions>

            <!-- 标题 -->
            <Grid Grid.Row="0" Margin="0,0,0,8">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto" />
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="Auto" />
                </Grid.ColumnDefinitions>
                <TextBlock Grid.Column="0" x:Name="TitleLabel"
                           Text="⇄ 词库对比"
                           Style="{StaticResource SectionLabel}"
                           FontSize="14" />
                <Button Grid.Column="2"
                        x:Name="LoadRightButton"
                        Style="{StaticResource ModernButton}"
                        Content="📂 加载对比词库"
                        Padding="14,6"
                        FontSize="12"
                        Click="OnLoadRightWordlist" />
            </Grid>

            <!-- 统计概览 -->
            <Border Grid.Row="1"
                    x:Name="SummaryBar"
                    CornerRadius="8"
                    Background="{StaticResource BgBrush}"
                    BorderBrush="{StaticResource BorderBrush}"
                    BorderThickness="1"
                    Padding="12,8"
                    Margin="0,0,0,8">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto" />
                        <ColumnDefinition Width="Auto" />
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="Auto" />
                    </Grid.ColumnDefinitions>
                    <Border Grid.Column="0" CornerRadius="6" Background="#0A2E1A" Padding="12,6">
                        <TextBlock x:Name="LeftCountLabel"
                                   Text="左: 0"
                                   Foreground="{StaticResource SuccessBrush}"
                                   FontSize="11" FontFamily="{StaticResource MonoFont}" />
                    </Border>
                    <Border Grid.Column="1" CornerRadius="6" Background="#2D1B1B" Padding="12,6" Margin="8,0,0,0">
                        <TextBlock x:Name="RightOnlyCountLabel"
                                   Text="新增: 0"
                                   Foreground="{StaticResource ErrorBrush}"
                                   FontSize="11" FontFamily="{StaticResource MonoFont}" />
                    </Border>
                    <Border Grid.Column="3" CornerRadius="6" Background="#1A1A2E" Padding="12,6">
                        <TextBlock x:Name="CommonCountLabel"
                                   Text="共有: 0"
                                   Foreground="{StaticResource AccentLightBrush}"
                                   FontSize="11" FontFamily="{StaticResource MonoFont}" />
                    </Border>
                </Grid>
            </Border>

            <!-- 差异列表（双栏） -->
            <Grid Grid.Row="2">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="12" />
                    <ColumnDefinition Width="*" />
                </Grid.ColumnDefinitions>

                <!-- 左栏：仅在左词库 -->
                <Border Grid.Column="0"
                        CornerRadius="8"
                        Background="{StaticResource BgBrush}"
                        BorderBrush="{StaticResource BorderBrush}"
                        BorderThickness="1"
                        Padding="0">
                    <Grid>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto" />
                            <RowDefinition Height="*" />
                        </Grid.RowDefinitions>
                        <Border Grid.Row="0"
                                BorderBrush="{StaticResource BorderBrush}"
                                BorderThickness="0,0,0,1"
                                Padding="12,6">
                            <TextBlock x:Name="LeftSectionLabel"
                                       Text="仅左侧存在"
                                       Foreground="{StaticResource SuccessBrush}"
                                       FontSize="11" FontWeight="SemiBold" />
                        </Border>
                        <ListBox x:Name="LeftOnlyList"
                                 Grid.Row="1"
                                 Background="Transparent"
                                 BorderThickness="0"
                                 ScrollViewer.VerticalScrollBarVisibility="Auto"
                                 Padding="4">
                            <ListBox.ItemTemplate>
                                <DataTemplate>
                                    <TextBlock Text="{Binding}"
                                               FontSize="12"
                                               FontFamily="{StaticResource MonoFont}"
                                               Foreground="{StaticResource SuccessBrush}"
                                               Padding="8,3" />
                                </DataTemplate>
                            </ListBox.ItemTemplate>
                        </ListBox>
                    </Grid>
                </Border>

                <!-- 右栏：仅在右词库 -->
                <Border Grid.Column="2"
                        CornerRadius="8"
                        Background="{StaticResource BgBrush}"
                        BorderBrush="{StaticResource BorderBrush}"
                        BorderThickness="1"
                        Padding="0">
                    <Grid>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto" />
                            <RowDefinition Height="*" />
                        </Grid.RowDefinitions>
                        <Border Grid.Row="0"
                                BorderBrush="{StaticResource BorderBrush}"
                                BorderThickness="0,0,0,1"
                                Padding="12,6">
                            <TextBlock x:Name="RightSectionLabel"
                                       Text="仅右侧存在"
                                       Foreground="{StaticResource ErrorBrush}"
                                       FontSize="11" FontWeight="SemiBold" />
                        </Border>
                        <ListBox x:Name="RightOnlyList"
                                 Grid.Row="1"
                                 Background="Transparent"
                                 BorderThickness="0"
                                 ScrollViewer.VerticalScrollBarVisibility="Auto"
                                 Padding="4">
                            <ListBox.ItemTemplate>
                                <DataTemplate>
                                    <TextBlock Text="{Binding}"
                                               FontSize="12"
                                               FontFamily="{StaticResource MonoFont}"
                                               Foreground="{StaticResource ErrorBrush}"
                                               Padding="8,3" />
                                </DataTemplate>
                            </ListBox.ItemTemplate>
                        </ListBox>
                    </Grid>
                </Border>
            </Grid>

            <!-- 底部：导出按钮 -->
            <Border Grid.Row="3"
                    Margin="0,8,0,0"
                    CornerRadius="8"
                    Background="{StaticResource BgBrush}"
                    BorderBrush="{StaticResource BorderBrush}"
                    BorderThickness="1"
                    Padding="12,6">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="Auto" />
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0"
                               x:Name="StatusLabel"
                               Text="请点击「加载对比词库」选择要对比的词库文件"
                               Foreground="{StaticResource TextMutedBrush}"
                               FontSize="11"
                               VerticalAlignment="Center" />
                    <Button Grid.Column="1"
                            x:Name="ExportButton"
                            Style="{StaticResource ModernButton}"
                            Content="📥 导出差异报告"
                            Padding="14,6"
                            FontSize="11"
                            IsEnabled="False"
                            Click="OnExportDiff" />
                </Grid>
            </Border>
        </Grid>
    </Border>
</Window>
```

### Task 2.2: 窗口逻辑

**文件**: `d:\Project\Project-C#\CassieWordCheck\Views\DiffWindow.xaml.cs`

```csharp
using System.Windows;
using System.Windows.Controls;
using CassieWordCheck.Models;
using CassieWordCheck.Services;

namespace CassieWordCheck.Views;

public partial class DiffWindow : Window
{
    private readonly WordList _leftWordlist;
    private readonly LocalizationService _localization;
    private WordList? _rightWordlist;
    private WordListDiff? _diffResult;

    public DiffWindow(WordList leftWordlist, LocalizationService localization)
    {
        InitializeComponent();
        _leftWordlist = leftWordlist;
        _localization = localization;
        this.EnableDarkTitleBar();
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        Title = _localization["diff.title"];
        TitleLabel.Text = "⇄ " + _localization["diff.title"];
        LoadRightButton.Content = "📂 " + _localization["diff.load"];
        LeftSectionLabel.Text = _localization["diff.left_only"];
        RightSectionLabel.Text = _localization["diff.right_only"];
        ExportButton.Content = "📥 " + _localization["diff.export"];
        StatusLabel.Text = _localization["diff.prompt"];

        // 如果当前词库为空，显示提示
        if (_leftWordlist.WordCount == 0)
        {
            StatusLabel.Text = _localization["wordlist_browser.empty"];
            LoadRightButton.IsEnabled = false;
        }
    }

    private void OnLoadRightWordlist(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = _localization["diff.select_file"],
            Filter = "词库文件 (*.txt;*.csv;*.xlsx)|*.txt;*.csv;*.xlsx|文本文件 (*.txt)|*.txt|CSV 文件 (*.csv)|*.csv|Excel 文件 (*.xlsx)|*.xlsx",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            _rightWordlist = new WordList();
            var count = _rightWordlist.LoadFromFile(dialog.FileName);
            if (count == 0)
            {
                StatusLabel.Text = _localization["diff.empty_file"];
                return;
            }

            // 计算差异
            _diffResult = _leftWordlist.DiffWith(_rightWordlist);

            // 更新统计概览
            LeftCountLabel.Text = $"{_localization["diff.left"]}: {_diffResult.LeftOnlyCount}";
            RightOnlyCountLabel.Text = $"{_localization["diff.right_only_count"]}: {_diffResult.RightOnlyCount}";
            CommonCountLabel.Text = $"{_localization["diff.common"]}: {_diffResult.CommonCount}";

            // 更新列表
            LeftOnlyList.ItemsSource = _diffResult.LeftOnly.OrderBy(w => w).ToList();
            RightOnlyList.ItemsSource = _diffResult.RightOnly.OrderBy(w => w).ToList();

            // 启用导出
            ExportButton.IsEnabled = true;

            // 状态提示
            StatusLabel.Text = string.Format(
                _localization["diff.done"],
                Path.GetFileName(dialog.FileName),
                _diffResult.LeftOnlyCount,
                _diffResult.RightOnlyCount,
                _diffResult.CommonCount);
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"{_localization["diff.error"]}: {ex.Message}";
        }
    }

    private void OnExportDiff(object sender, RoutedEventArgs e)
    {
        if (_diffResult is null) return;

        var saveDialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = _localization["diff.export_title"],
            Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            DefaultExt = ".txt",
            FileName = $"wordlist_diff_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
        };

        if (saveDialog.ShowDialog() != true) return;

        try
        {
            var lines = new List<string>
            {
                $"=== 词库差异报告 ===",
                $"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"",
                $"左词库: {_diffResult.LeftLabel} ({_leftWordlist.WordCount} 词)",
                $"右词库: {_diffResult.RightLabel} ({(_rightWordlist?.WordCount ?? 0)} 词)",
                $"",
                $"--- 仅左词库存在: {_diffResult.LeftOnlyCount} 词 ---",
            };

            foreach (var word in _diffResult.LeftOnly.OrderBy(w => w))
                lines.Add(word);

            lines.Add("");
            lines.Add($"--- 仅右词库存在: {_diffResult.RightOnlyCount} 词 ---");
            foreach (var word in _diffResult.RightOnly.OrderBy(w => w))
                lines.Add(word);

            lines.Add("");
            lines.Add($"--- 共有: {_diffResult.CommonCount} 词 ---");
            lines.Add($"(共 {_diffResult.CommonCount} 个单词在两词库中均存在)");

            File.WriteAllLines(saveDialog.FileName, lines);
            StatusLabel.Text = $"{_localization["diff.exported"]}";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"{_localization["diff.error"]}: {ex.Message}";
        }
    }
}
```

### Task 2.3: 本地化 key

**zh-CN.json** 追加：
```json
  "diff.tooltip": "词库对比",
  "diff.title": "词库对比",
  "diff.load": "加载对比词库",
  "diff.select_file": "选择要对比的词库文件",
  "diff.left": "左侧独有",
  "diff.left_only": "仅左侧存在",
  "diff.right_only": "仅右侧存在",
  "diff.right_only_count": "右侧独有",
  "diff.common": "共有",
  "diff.export": "导出差异报告",
  "diff.export_title": "导出词库差异报告",
  "diff.prompt": "请点击「加载对比词库」选择要对比的词库文件",
  "diff.done": "已加载 {0}，差异：左侧独有 {1} 词，右侧独有 {2} 词，共有 {3} 词",
  "diff.exported": "差异报告已导出",
  "diff.error": "操作失败",
  "diff.empty_file": "选择的文件不包含有效单词"
```

其他 7 个语言文件对应追加。

### Task 2.4: 更新日志

```markdown
## `v2.3.6`（2026-06-18）— 词库差异对比

- **新增词库对比功能** — 将当前词库与另一词库文件对比，显示新增/移除/共有词
- **双栏差异列表** — 左栏"仅当前词库存在"，右栏"仅对比词库存在"
- **差异报告导出** — 一键导出 .txt 格式的完整差异报告
- **工具栏新增按钮** — ⇄ 一键打开词库对比窗口
```

---

## 验证

```bash
dotnet build d:\Project\Project-C#\CassieWordCheck\CassieWordCheck.csproj
dotnet test d:\Project\Project-C#\CassieWordCheck\CassieWordCheck.Tests\CassieWordCheck.Tests.csproj
```
