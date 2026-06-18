# 拼写建议增强 — 执行计划

## 架构概览

将现有的 `LevenshteinHelper` 静态工具类重构为 `SuggestionEngine` 类，统一管理三种匹配策略（通配搜索、编辑距离、复合拆词）。建议面板 UI 从"3 个平铺"升级为"分类卡片式"布局。

### 设计决策（已由 Leader 确认）

| 决策点 | 结论 |
|--------|------|
| 架构 | 新建 SuggestionEngine 类，接收 `IReadOnlySet<string>`，返回 `List<SuggestionResult>` |
| 通配符 | 仅 `*`（任意字符序列）和 `?`（单个字符），不做完整 Glob |
| 复合拆词 | 从最长前缀开始尝试，只拆 2 段，每段最少 3 字符 |
| 建议上限 | 通配 6 个、编辑距离 5 个、拆词 3 个，总计最多 14 个 |
| UI 布局 | 分类卡片式，三区纵向排列（通配/拼写/拆分） |
| 旧文件 | LevenshteinHelper.cs 废弃保留，不移除 |

### 涉及文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `Resources/Services/LevenshteinHelper.cs` | 不改 | 废弃保留，`FindClosest` 仍被 `FormatSuggestions` 使用（兼容旧代码） |
| `Resources/Services/SuggestionEngine.cs` | **新建** | 统一建议引擎（通配 + 编辑距离 + 复合拆词） |
| `CassieWordCheck.Tests/SuggestionEngineTests.cs` | **新建** | 三种匹配策略的单元测试 |
| `Views/MainWindow.xaml` | 修改 | 建议面板从 WrapPanel 改为三区分类布局 |
| `Views/MainWindow.xaml.cs` | 修改 | 建议逻辑替换、缓存更新、点击候选词替换输入 |
| `Resources/Locales/*.json` | 修改 | 新增 3 个翻译 key |
| `Views/AboutWindow.xaml.cs` | 修改 | 更新日志 |

---

## 切片 1: SuggestionEngine + 单元测试

### Task 1.1: 新建 SuggestionResult 记录和 SuggestionEngine 类

**文件**: `d:\Project\Project-C#\CassieWordCheck\Resources\Services\SuggestionEngine.cs`

```csharp
using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace CassieWordCheck.Services;

/// <summary>建议结果：包含建议词、来源标签和置信度</summary>
public record SuggestionResult(
    string Word,
    string Source,    // "wildcard" / "fuzzy" / "compound"
    int? EditDistance,
    double Confidence
);

/// <summary>统一建议引擎：通配搜索 + 编辑距离 + 复合拆词</summary>
public partial class SuggestionEngine
{
    private readonly FrozenSet<string> _words;

    public SuggestionEngine(IReadOnlySet<string> words)
    {
        _words = words as FrozenSet<string>
            ?? words.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>获取建议，三种策略混合，按置信度降序排列</summary>
    public List<SuggestionResult> GetSuggestions(string word, int maxTotal = 14)
    {
        var results = new List<SuggestionResult>();

        // 1. 通配匹配（最高优先级）
        var wildcard = WildcardSearch(word, 6);
        results.AddRange(wildcard);

        // 2. 编辑距离（中等优先级）
        var fuzzy = LevenshteinSearch(word, 5);
        // 去重：排除通配已匹配到的词
        var wildcardSet = new HashSet<string>(wildcard.Select(r => r.Word), StringComparer.OrdinalIgnoreCase);
        results.AddRange(fuzzy.Where(r => !wildcardSet.Contains(r.Word)));

        // 3. 复合拆词（最低优先级）
        var compound = CompoundSplit(word, 3);
        var fuzzySet = new HashSet<string>(results.Select(r => r.Word), StringComparer.OrdinalIgnoreCase);
        results.AddRange(compound.Where(r => !fuzzySet.Contains(r.Word)));

        return results.OrderByDescending(r => r.Confidence).Take(maxTotal).ToList();
    }

    // ===== 通配搜索 =====

    /// <summary>通配搜索：支持 *（任意多字符）和 ?（单个字符）</summary>
    public List<SuggestionResult> WildcardSearch(string pattern, int maxResults = 6)
    {
        if (string.IsNullOrEmpty(pattern) || (!pattern.Contains('*') && !pattern.Contains('?')))
            return [];

        var regex = WildcardToRegex(pattern);
        var matches = new List<SuggestionResult>();

        foreach (var word in _words)
        {
            if (regex.IsMatch(word))
            {
                // 置信度：通配符数量越少越高（精确度更高）
                var wildcardCount = pattern.Count(c => c is '*' or '?');
                var confidence = 100.0 / (1 + wildcardCount * 0.5 + Math.Abs(word.Length - pattern.Replace("*", "").Replace("?", "").Length) * 0.1);
                matches.Add(new SuggestionResult(word, "wildcard", null, confidence));
                if (matches.Count >= maxResults) break;
            }
        }

        return matches;
    }

    /// <summary>将通配模式（* ?）转换为正则表达式</summary>
    private static Regex WildcardToRegex(string pattern)
    {
        var escaped = Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".");
        return new Regex($"^{escaped}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    // ===== 编辑距离 =====

    /// <summary>编辑距离搜索：使用 Levenshtein 距离查找相似词</summary>
    public List<SuggestionResult> LevenshteinSearch(string word, int maxResults = 5)
    {
        if (string.IsNullOrEmpty(word)) return [];

        var scored = new List<(string Word, int Distance)>();

        foreach (var candidate in _words)
        {
            // 长度差超过 3 的跳过（编辑距离至少差 3 以上，不可能进前 5）
            if (Math.Abs(candidate.Length - word.Length) > 3) continue;

            var dist = LevenshteinDistance(word, candidate);
            // 只保留距离 <= 3 的候选
            if (dist <= 3)
                scored.Add((candidate, dist));
        }

        return scored
            .OrderBy(s => s.Distance)
            .Take(maxResults)
            .Select(s => new SuggestionResult(s.Word, "fuzzy", s.Distance, 100.0 / (1 + s.Distance * 2)))
            .ToList();
    }

    /// <summary>Levenshtein 编辑距离计算（和 LevenshteinHelper 相同的实现，内联到此处避免跨类依赖）</summary>
    private static int LevenshteinDistance(string a, string b)
    {
        var lenA = a.Length;
        var lenB = b.Length;
        var d = new int[lenA + 1, lenB + 1];

        for (int i = 0; i <= lenA; d[i, 0] = i++) { }
        for (int j = 0; j <= lenB; d[0, j] = j++) { }

        for (int i = 1; i <= lenA; i++)
        {
            for (int j = 1; j <= lenB; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }

        return d[lenA, lenB];
    }

    // ===== 复合拆词 =====

    /// <summary>复合拆词：尝试将输入拆为已知词 + 已知词（从最长前缀开始，只拆 2 段，每段最少 3 字符）</summary>
    public List<SuggestionResult> CompoundSplit(string word, int maxResults = 3)
    {
        if (string.IsNullOrEmpty(word) || word.Length < 6) return []; // 最少 3+3 字符

        var results = new List<SuggestionResult>();

        // 从最长前缀开始尝试（避免拆出太短的前缀）
        for (int i = word.Length - 3; i >= 3; i--)
        {
            var prefix = word[..i];
            var suffix = word[i..];

            if (suffix.Length < 3) continue;

            if (_words.Contains(prefix) && _words.Contains(suffix))
            {
                // 置信度：两段长度越均衡越高
                var balance = 1.0 - Math.Abs(prefix.Length - suffix.Length) / (double)word.Length;
                var confidence = 50.0 * balance;
                results.Add(new SuggestionResult($"{prefix} + {suffix}", "compound", null, confidence));

                if (results.Count >= maxResults) break;
            }
        }

        return results;
    }
}
```

### Task 1.2: 编写 SuggestionEngine 单元测试

**文件**: `d:\Project\Project-C#\CassieWordCheck\CassieWordCheck.Tests\SuggestionEngineTests.cs`

```csharp
using CassieWordCheck.Services;

namespace CassieWordCheck.Tests;

public class SuggestionEngineTests
{
    private static SuggestionEngine CreateEngine(params string[] words)
    {
        return new SuggestionEngine(new HashSet<string>(words, StringComparer.OrdinalIgnoreCase));
    }

    // ===== 通配搜索 =====

    [Fact]
    public void WildcardSearch_后缀通配_返回匹配结果()
    {
        var engine = CreateEngine("announce", "announcement", "announcer", "annual", "animal");

        var results = engine.WildcardSearch("announ*");

        Assert.Contains(results, r => r.Word == "announce");
        Assert.Contains(results, r => r.Word == "announcement");
        Assert.Contains(results, r => r.Word == "announcer");
        Assert.DoesNotContain(results, r => r.Word == "annual");
        Assert.DoesNotContain(results, r => r.Word == "animal");
        Assert.All(results, r => Assert.Equal("wildcard", r.Source));
    }

    [Fact]
    public void WildcardSearch_前缀通配_返回匹配结果()
    {
        var engine = CreateEngine("allow", "allowed", "allowing", "allowance", "ally");

        var results = engine.WildcardSearch("*allow*");

        Assert.Contains(results, r => r.Word == "allow");
        Assert.Contains(results, r => r.Word == "allowed");
        Assert.Contains(results, r => r.Word == "allowing");
        Assert.Contains(results, r => r.Word == "allowance");
    }

    [Fact]
    public void WildcardSearch_单字符通配_返回匹配结果()
    {
        var engine = CreateEngine("cat", "cut", "cot", "cit", "cart");

        var results = engine.WildcardSearch("c?t");

        Assert.Contains(results, r => r.Word == "cat");
        Assert.Contains(results, r => r.Word == "cut");
        Assert.Contains(results, r => r.Word == "cot");
        Assert.Contains(results, r => r.Word == "cit");
        Assert.DoesNotContain(results, r => r.Word == "cart");
    }

    [Fact]
    public void WildcardSearch_无通配符_返回空()
    {
        var engine = CreateEngine("test", "testing");

        var results = engine.WildcardSearch("test");

        Assert.Empty(results);
    }

    [Fact]
    public void WildcardSearch_无匹配通配_返回空()
    {
        var engine = CreateEngine("cat", "dog");

        var results = engine.WildcardSearch("xyz*");

        Assert.Empty(results);
    }

    // ===== 编辑距离 =====

    [Fact]
    public void LevenshteinSearch_距离1_返回相近词()
    {
        var engine = CreateEngine("announcement", "announcer", "announce", "management");

        var results = engine.LevenshteinSearch("announcement", 3);

        Assert.Contains(results, r => r.Word == "announcement" && r.EditDistance == 0);
    }

    [Fact]
    public void LevenshteinSearch_拼写错误_返回建议()
    {
        var engine = CreateEngine("receive", "deceive", "perceive", "conceive");

        var results = engine.LevenshteinSearch("recieve", 3);

        Assert.Contains(results, r => r.Word == "receive");
        Assert.All(results, r => Assert.Equal("fuzzy", r.Source));
    }

    [Fact]
    public void LevenshteinSearch_无相似词_返回空()
    {
        var engine = CreateEngine("cat", "dog", "bird");

        var results = engine.LevenshteinSearch("elephant", 3);

        Assert.Empty(results);
    }

    [Fact]
    public void LevenshteinSearch_空词_返回空()
    {
        var engine = CreateEngine("cat", "dog");

        var results = engine.LevenshteinSearch("");

        Assert.Empty(results);
    }

    // ===== 复合拆词 =====

    [Fact]
    public void CompoundSplit_两个已知词连接_返回拆分建议()
    {
        var engine = CreateEngine("cancel", "override", "red", "announcement");

        var results = engine.CompoundSplit("canceloverride", 3);

        Assert.Contains(results, r => r.Word == "cancel + override");
        Assert.All(results, r => Assert.Equal("compound", r.Source));
    }

    [Fact]
    public void CompoundSplit_最短段少于3字符_不返回结果()
    {
        var engine = CreateEngine("a", "test", "at", "est");

        // "atest" -> "a"(1) + "test"(4)，段太短不返回
        var results = engine.CompoundSplit("atest", 3);

        Assert.Empty(results);
    }

    [Fact]
    public void CompoundSplit_总长小于6_不返回结果()
    {
        var engine = CreateEngine("cat", "dog");

        var results = engine.CompoundSplit("catdog", 3);

        Assert.Empty(results); // 6 字符 = 刚好到边界，传 5 字符的
    }

    [Fact]
    public void CompoundSplit_无匹配组合_返回空()
    {
        var engine = CreateEngine("hello", "world");

        var results = engine.CompoundSplit("helloworld", 3);

        Assert.Empty(results);
    }

    // ===== 综合建议 =====

    [Fact]
    public void GetSuggestions_混合策略_按置信度排序()
    {
        var engine = CreateEngine("announcement", "announcer", "announce",
            "announcements", "management", "announcing", "anno", "annoy");

        var results = engine.GetSuggestions("announc*", 10);

        // 通配匹配应在结果中
        Assert.Contains(results, r => r.Source == "wildcard");
        Assert.True(results.Count > 0);
    }

    [Fact]
    public void GetSuggestions_去重_同词不重复出现()
    {
        var engine = CreateEngine("announcement", "announce");

        // "announcement" 同时匹配通配和编辑距离，但应只出现一次
        var results = engine.GetSuggestions("announc*", 10);

        var announceCount = results.Count(r => r.Word == "announcement");
        Assert.True(announceCount <= 1);
    }

    [Fact]
    public void GetSuggestions_空输入_返回空()
    {
        var engine = CreateEngine("test", "testing");

        var results = engine.GetSuggestions("");

        Assert.Empty(results);
    }

    [Fact]
    public void GetSuggestions_无匹配_返回空()
    {
        var engine = CreateEngine("cat", "dog", "bird");

        var results = engine.GetSuggestions("xyz", 10);

        Assert.Empty(results);
    }
}
```

**验证**:
```bash
dotnet test d:\Project\Project-C#\CassieWordCheck\CassieWordCheck.Tests\CassieWordCheck.Tests.csproj --filter "FullyQualifiedName~SuggestionEngineTests"
```

---

## 切片 2: 集成到 MainWindow + 建议面板 UI 升级

### Task 2.1: 改造建议面板 XAML

**文件**: `d:\Project\Project-C#\CassieWordCheck\Views\MainWindow.xaml`

将原来的建议面板（`SuggestionsPanel` 内 `WrapPanel` 的 `<ItemsControl x:Name="SuggestionsList">`，约第 554-584 行）替换为三区分类布局：

```xml
                    <Border Grid.Column="0" x:Name="SuggestionLabel"
                            Text="💡 建议"
                            Style="{StaticResource SectionLabel}"
                            VerticalAlignment="Top"
                            Margin="0,2,16,0" />

                    <!-- 建议面板内容：三区纵向排列 -->
                    <StackPanel Grid.Column="1" x:Name="SuggestionPanel"
                                Margin="0,0,0,0">
                        <!-- 通配匹配区 -->
                        <Border x:Name="WildcardSection"
                                Margin="0,0,0,8"
                                Visibility="Collapsed">
                            <Border.Style>
                                <Style TargetType="Border">
                                    <Setter Property="Background" Value="Transparent" />
                                </Style>
                            </Border.Style>
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="Auto" />
                                    <RowDefinition Height="Auto" />
                                </Grid.RowDefinitions>
                                <TextBlock Grid.Row="0"
                                           x:Name="WildcardSectionLabel"
                                           Text="通配匹配"
                                           Foreground="{StaticResource AccentLightBrush}"
                                           FontSize="11" FontWeight="SemiBold"
                                           Margin="0,0,0,4" />
                                <WrapPanel Grid.Row="1"
                                           x:Name="WildcardItems" />
                            </Grid>
                        </Border>

                        <!-- 拼写修正区 -->
                        <Border x:Name="FuzzySection"
                                Margin="0,0,0,8"
                                Visibility="Collapsed">
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="Auto" />
                                    <RowDefinition Height="Auto" />
                                </Grid.RowDefinitions>
                                <TextBlock Grid.Row="0"
                                           x:Name="FuzzySectionLabel"
                                           Text="相似拼写"
                                           Foreground="{StaticResource WarningBrush}"
                                           FontSize="11" FontWeight="SemiBold"
                                           Margin="0,0,0,4" />
                                <WrapPanel Grid.Row="1"
                                           x:Name="FuzzyItems" />
                            </Grid>
                        </Border>

                        <!-- 拆分建议区 -->
                        <Border x:Name="CompoundSection"
                                Visibility="Collapsed">
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="Auto" />
                                    <RowDefinition Height="Auto" />
                                </Grid.RowDefinitions>
                                <TextBlock Grid.Row="0"
                                           x:Name="CompoundSectionLabel"
                                           Text="拆分建议"
                                           Foreground="{StaticResource IgnoredBrush}"
                                           FontSize="11" FontWeight="SemiBold"
                                           Margin="0,0,0,4" />
                                <WrapPanel Grid.Row="1"
                                           x:Name="CompoundItems" />
                            </Grid>
                        </Border>
                    </StackPanel>
```

需要替换 `SuggestionsPanel` 内 `Grid` 中约第 546-585 行的现有 `ItemsControl`。具体操作：找到 `SuggestionLabel` 和 `SuggestionsList` 所在的 `Grid.ColumnDefinitions` 及内容，替换为上述布局。

注意保留 `SuggestionsPanel` 外层 Border 的现有结构和动画逻辑，只替换内部的内容控件。

### Task 2.2: 改造建议逻辑

**文件**: `d:\Project\Project-C#\CassieWordCheck\Views\MainWindow.xaml.cs`

修改点：

1. **新增字段**（构造函数中初始化）：
```csharp
private SuggestionEngine _suggestionEngine = null!;
// 替换旧的 _suggestionCache（仍然保留但不用于新引擎）
// private readonly Dictionary<string, string> _suggestionCache  ← 保留不动，兼容旧 FormatSuggestions
```

在构造函数中，`LoadWordListAsync()` 之后初始化：
```csharp
_suggestionEngine = new SuggestionEngine(_wordlist.Words);
```

2. **修改 `UpdateResult()` 中的建议面板填充逻辑**（约第 250-296 行）：

找到以下代码段（现有 `UpdateResult` 方法中建议面板部分）：
```csharp
        var unavailableWords = results
            .Where(r => r.Status == CheckStatus.Unavailable)
            .Select(r => r.Text)
            .Distinct()
            .ToList();

        if (unavailableWords.Count > 0)
        {
            // ... 现有建议面板逻辑
            SuggestionsList.ItemsSource = unavailableWords
                .Select(w => new { Original = w, SuggestionText = FormatSuggestions(w) })
                .ToList();
        }
```

替换为：
```csharp
        var unavailableWords = results
            .Where(r => r.Status == CheckStatus.Unavailable)
            .Select(r => r.Text)
            .Distinct()
            .ToList();

        if (unavailableWords.Count > 0)
        {
            if (SuggestionsPanel.Visibility != Visibility.Visible)
            {
                // 现有点亮动画保持不动
                SuggestionsPanel.Visibility = Visibility.Visible;
                SuggestionsPanel.MaxHeight = 0;
                SuggestionsPanel.Opacity = 0;

                var st = (TranslateTransform)SuggestionsPanel.RenderTransform;
                st.Y = 20;

                Animate(SuggestionsPanel, UIElement.OpacityProperty, 0, 1, 250, new QuadraticEase());
                Animate(st, TranslateTransform.YProperty, 20, 0, 300, new QuadraticEase());

                var heightAnim = new DoubleAnimation(350, new Duration(TimeSpan.FromMilliseconds(350)));
                heightAnim.EasingFunction = new QuadraticEase();
                SuggestionsPanel.BeginAnimation(FrameworkElement.MaxHeightProperty, heightAnim);
            }

            BuildSuggestionPanel(unavailableWords);
        }
        else if (SuggestionsPanel.Visibility == Visibility.Visible)
        {
            // ... 现有隐藏动画保持不变
        }
```

3. **新增 `BuildSuggestionPanel` 方法**：

```csharp
    private void BuildSuggestionPanel(List<string> unavailableWords)
    {
        // 清空所有分类容器
        WildcardItems.Children.Clear();
        FuzzyItems.Children.Clear();
        CompoundItems.Children.Clear();

        // 对每个不可用词获取建议
        var allWildcard = new List<SuggestionResult>();
        var allFuzzy = new List<SuggestionResult>();
        var allCompound = new List<SuggestionResult>();

        foreach (var word in unavailableWords)
        {
            var suggestions = _suggestionEngine.GetSuggestions(word, 14);

            foreach (var s in suggestions.Where(s => s.Source == "wildcard" && !allWildcard.Any(ex => ex.Word == s.Word)))
                allWildcard.Add(s);
            foreach (var s in suggestions.Where(s => s.Source == "fuzzy" && !allFuzzy.Any(ex => ex.Word == s.Word)))
                allFuzzy.Add(s);
            foreach (var s in suggestions.Where(s => s.Source == "compound" && !allCompound.Any(ex => ex.Word == s.Word)))
                allCompound.Add(s);
        }

        // 限制每类数量
        var topWildcard = allWildcard.OrderByDescending(r => r.Confidence).Take(6).ToList();
        var topFuzzy = allFuzzy.OrderByDescending(r => r.Confidence).Take(5).ToList();
        var topCompound = allCompound.OrderByDescending(r => r.Confidence).Take(3).ToList();

        // 渲染通配匹配区
        WildcardSection.Visibility = topWildcard.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        foreach (var s in topWildcard)
            WildcardItems.Children.Add(BuildSuggestionChip(s.Word, "#8B7CF0"));

        // 渲染拼写修正区
        FuzzySection.Visibility = topFuzzy.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        foreach (var s in topFuzzy)
            FuzzyItems.Children.Add(BuildSuggestionChip(s.Word, "#F59E0B"));

        // 渲染拆分建议区
        CompoundSection.Visibility = topCompound.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        foreach (var s in topCompound)
            CompoundItems.Children.Add(BuildSuggestionChip(s.Word, "#6B7280"));
    }

    private Border BuildSuggestionChip(string word, string accentColor)
    {
        var color = (Color)ColorConverter.ConvertFromString(accentColor);
        var brush = new SolidColorBrush(color);

        var chip = new Border
        {
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 6),
            Margin = new Thickness(0, 0, 8, 6),
            Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x35)),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Tag = word,
        };

        var textBlock = new TextBlock
        {
            Text = word,
            FontSize = 12,
            FontFamily = new FontFamily("Segoe UI, Consolas"),
            Foreground = brush,
        };
        chip.Child = textBlock;

        chip.MouseLeftButtonUp += (_, _) =>
        {
            // 替换输入框中对应的词
            var currentText = InputBox.Text;
            // 简单替换：在当前位置前后查找原词并替换
            // 实际场景中可能需要更精确的替换逻辑
            var caretPos = InputBox.CaretIndex;
            if (word.Contains(" + "))
            {
                // 复合词拆分建议：将输入中对应的不可用词替换为多个词
                var parts = word.Split(" + ");
                var replacement = string.Join(" ", parts);
                // 查找光标附近的词
                var (start, len) = FindWordAtPosition(currentText, caretPos);
                if (start >= 0)
                {
                    InputBox.Text = currentText[..start] + replacement + currentText[(start + len)..];
                    InputBox.CaretIndex = start + replacement.Length;
                }
            }
            else
            {
                var (start, len) = FindWordAtPosition(currentText, caretPos);
                if (start >= 0)
                {
                    InputBox.Text = currentText[..start] + word + currentText[(start + len)..];
                    InputBox.CaretIndex = start + word.Length;
                }
            }
        };

        return chip;
    }

    /// <summary>查找光标位置所在的单词的起止索引</summary>
    private static (int start, int length) FindWordAtPosition(string text, int caretIndex)
    {
        if (string.IsNullOrEmpty(text) || caretIndex < 0) return (-1, 0);

        // 从光标位置向左找到单词开头
        int start = caretIndex;
        while (start > 0 && !char.IsWhiteSpace(text[start - 1])) start--;

        // 从光标位置向右找到单词结尾
        int end = caretIndex;
        while (end < text.Length && !char.IsWhiteSpace(text[end])) end++;

        if (start == end) return (-1, 0);
        return (start, end - start);
    }
```

4. **添加 `using`**：文件顶部需添加：
```csharp
using CassieWordCheck.Services;
```
（如果尚未引入——检查现有 using 列表，`LevenshteinHelper` 和 `LocalizationService` 已在使用，可能已存在）

5. **处理 `LoadWordListAsync` 或 `AddFromFile` 后重建引擎**：

在 `OnFileOpenOrImport` 的导入成功后（约第 589-594 行）和 `OnReloadWordlist`（约第 727-741 行）末尾添加：
```csharp
_suggestionEngine = new SuggestionEngine(_wordlist.Words);
```

### Task 2.3: 更新本地化文件

**文件**: `d:\Project\Project-C#\CassieWordCheck\Resources\Locales\zh-CN.json`

在末尾添加：
```json
  "suggestion.wildcard": "通配匹配",
  "suggestion.fuzzy": "相似拼写",
  "suggestion.compound": "拆分建议"
```

**文件**: `d:\Project\Project-C#\CassieWordCheck\Resources\Locales\en-US.json`

```json
  "suggestion.wildcard": "Wildcard Match",
  "suggestion.fuzzy": "Similar Spelling",
  "suggestion.compound": "Compound Split"
```

**文件**: `d:\Project\Project-C#\CassieWordCheck\Resources\Locales\ja-JP.json`

```json
  "suggestion.wildcard": "ワイルドカード一致",
  "suggestion.fuzzy": "類似スペル",
  "suggestion.compound": "複合語分割"
```

**文件**: `d:\Project\Project-C#\CassieWordCheck\Resources\Locales\ko-KR.json`

```json
  "suggestion.wildcard": "와일드카드 일치",
  "suggestion.fuzzy": "유사 철자",
  "suggestion.compound": "복합어 분할"
```

**文件**: `d:\Project\Project-C#\CassieWordCheck\Resources\Locales\de-DE.json`

```json
  "suggestion.wildcard": "Platzhalter-Treffer",
  "suggestion.fuzzy": "Ähnliche Schreibweise",
  "suggestion.compound": "Zusammensetzung"
```

**文件**: `d:\Project\Project-C#\CassieWordCheck\Resources\Locales\ru-RU.json`

```json
  "suggestion.wildcard": "Подстановочный знак",
  "suggestion.fuzzy": "Похожее написание",
  "suggestion.compound": "Разделение составных"
```

**文件**: `d:\Project\Project-C#\CassieWordCheck\Resources\Locales\fr-FR.json`

```json
  "suggestion.wildcard": "Correspondance générique",
  "suggestion.fuzzy": "Orthographe similaire",
  "suggestion.compound": "Division composée"
```

### Task 2.4: 更新 Changelog

**文件**: `d:\Project\Project-C#\CassieWordCheck\Views\AboutWindow.xaml.cs`

在 `ChangelogText` 顶部插入：
```markdown
## `v2.3.4`（2026-06-18）— 拼写建议增强

- **新增智能建议引擎** — 三种匹配策略统一管理：通配搜索、编辑距离、复合拆词
- **通配搜索** — 支持 `*`（任意多字符）和 `?`（单个字符）模式匹配词库
- **复合拆词** — 自动识别用户连续输入的已知词组合（如 `cancelOverride`→`cancel + override`）
- **分类建议面板** — 按来源分三区展示（通配匹配/相似拼写/拆分建议），带颜色标签
- **点击替换** — 点击建议词自动替换输入框中对应的不可用词
- **性能优化** — 编辑距离跳过长度差 >3 的候选，减少无谓计算
```

---

## 切片 3: 集成测试验证

### Task 3.1: 确认旧代码兼容

确保现有调用 `LevenshteinHelper.FindClosest` 的代码不受影响：
- `MainWindow.xaml.cs` 第 395 行的 `FormatSuggestions` 方法仍然调用 `LevenshteinHelper.FindClosest`（用于单词详情弹窗的拼写建议，不在建议面板中使用）
- 保留该方法不动

### Task 3.2: 运行全部测试

```bash
dotnet test d:\Project\Project-C#\CassieWordCheck\CassieWordCheck.Tests\CassieWordCheck.Tests.csproj
```

预期：原有测试全部通过 + 新增 16 个 SuggestionEngineTests 全部通过。

---

## 注意事项

1. **`_suggestionCache` 保留不动** — 它用于单词详情弹窗（`ShowWordDetailPopup`）中的 `FormatSuggestions` 调用，仍然使用旧的 `LevenshteinHelper`。建议面板不再使用它。
2. **词库变更后重建引擎** — `AddFromFile` 和 `Reload` 后需重新 `new SuggestionEngine(_wordlist.Words)`，因为底层 _words 引用已变。
3. **`SuggestionEngine` 内 LevenshteinDistance 是内联实现** — 不依赖 `LevenshteinHelper` 类，避免循环依赖。
4. **复合拆词不处理 3 段以上** — 设计决策，避免过度组合导致噪音。
5. **替换逻辑** — `FindWordAtPosition` 基于空格分割的简单实现，对复杂文本（如带标点）可能不精确，但覆盖 90% 场景。
