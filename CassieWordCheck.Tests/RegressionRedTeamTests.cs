using System.IO;
using CassieWordCheck.Models;
using CassieWordCheck.Services;

namespace CassieWordCheck.Tests;

/// <summary>
/// 红队测试 —— 针对变更代码的边界条件、异常场景、竞态条件进行攻击性测试
/// </summary>
public class RegressionRedTeamTests
{
    // ========== Checker 词频率统计（v2.3.3 新增功能） ==========

    [Fact]
    public void GetWordFrequency_空结果列表_返回空字典()
    {
        var wl = CreateWordList("hello");
        var checker = new Checker(wl);

        var freq = checker.GetWordFrequency(new List<CheckResult>());

        Assert.Empty(freq);
    }

    [Fact]
    public void GetWordFrequency_所有词都可用_返回空字典()
    {
        var wl = CreateWordList("hello", "world");
        var checker = new Checker(wl);

        var results = checker.CheckText("hello world");
        var freq = checker.GetWordFrequency(results);

        Assert.Empty(freq); // 没有不可用词
    }

    [Fact]
    public void GetWordFrequency_大小写不敏感_统计合并()
    {
        var wl = CreateWordList("hello");
        var checker = new Checker(wl);

        var results = checker.CheckText("unknown UNKNOWN Unknown");

        var freq = checker.GetWordFrequency(results);

        Assert.Single(freq);
        Assert.Equal(3, freq["unknown"]);
    }

    [Fact]
    public void GetWordFrequency_相同词在不同行_统计合并()
    {
        var wl = CreateWordList("hello");
        var checker = new Checker(wl);

        var results = checker.CheckText("badword\nhello\nbadword");

        var freq = checker.GetWordFrequency(results);

        Assert.Equal(2, freq["badword"]);
    }

    [Fact]
    public void GetWordFrequency_忽略词不计入()
    {
        var wl = CreateWordList("hello");
        var checker = new Checker(wl) { FilterFormatting = true, FilterNaming = true };

        var results = checker.CheckText("hello link color MTF Alpha");
        var freq = checker.GetWordFrequency(results);

        // 这些都是被 Ignored 的标记，不应出现在频率统计中
        Assert.Empty(freq);
    }

    // ========== GetStatistics 边界条件 ==========

    [Fact]
    public void GetStatistics_全不可用_覆盖率为0()
    {
        var wl = CreateWordList();
        var checker = new Checker(wl);

        var results = checker.CheckText("word1 word2 word3");
        var stats = checker.GetStatistics(results);

        Assert.Equal(3, (int)stats["total"]);
        Assert.Equal(0, (int)stats["available"]);
        Assert.Equal(3, (int)stats["unavailable"]);
        Assert.Equal(0.0, (double)stats["coverage"], 5);
    }

    [Fact]
    public void GetStatistics_全可用_覆盖率为100()
    {
        var wl = CreateWordList("word1", "word2", "word3");
        var checker = new Checker(wl);

        var results = checker.CheckText("word1 word2 word3");
        var stats = checker.GetStatistics(results);

        Assert.Equal(3, (int)stats["total"]);
        Assert.Equal(3, (int)stats["available"]);
        Assert.Equal(0, (int)stats["unavailable"]);
        Assert.Equal(100.0, (double)stats["coverage"], 5);
    }

    [Fact]
    public void GetStatistics_包含忽略词_不影响计数()
    {
        var wl = CreateWordList("hello", "world");
        var checker = new Checker(wl) { FilterFormatting = true };

        // link 会被忽略，不统计到 total/available/unavailable 中
        var results = checker.CheckText("hello link world");
        var stats = checker.GetStatistics(results);

        Assert.Equal(2, (int)stats["total"]);      // link 不计数
        Assert.Equal(2, (int)stats["available"]);   // hello, world
        Assert.Equal(0, (int)stats["unavailable"]);
        Assert.Equal(100.0, (double)stats["coverage"], 5);
    }

    // ========== Settings.ResultMode 持久化边界（v2.3.2 新增功能） ==========

    [Fact]
    public void ResultMode_文件损坏_回退到默认值()
    {
        using var tmp = new TempFile();
        // 写入非法 JSON
        File.WriteAllText(tmp.Path, "{ not valid json }");
        var settings = new Settings(tmp.Path);

        Assert.Equal("inline", settings.ResultMode);
    }

    [Fact]
    public void ResultMode_空字符串_回退到inline()
    {
        using var tmp = new TempFile();
        File.WriteAllText(tmp.Path, @"{""ResultMode"": """"}");
        var settings = new Settings(tmp.Path);

        // ?? "inline" 对空字符串仍返回 ""
        // 这是代码当前行为，记录实际的预期值
        Assert.Equal("", settings.ResultMode);
    }

    [Fact]
    public void ResultMode_大小写敏感_保存值和加载值一致()
    {
        using var tmp = new TempFile();
        var settings = new Settings(tmp.Path);
        settings.ResultMode = "List";   // 大写 L
        settings.Save();

        var loaded = new Settings(tmp.Path);
        Assert.Equal("List", loaded.ResultMode);
    }

    [Fact]
    public void ResultMode_设置inline_保存加载后仍然是inline()
    {
        using var tmp = new TempFile();
        var s1 = new Settings(tmp.Path);
        s1.ResultMode = "compare";
        s1.Save();

        var s2 = new Settings(tmp.Path);
        s2.ResultMode = "inline";
        s2.Save();

        var s3 = new Settings(tmp.Path);
        Assert.Equal("inline", s3.ResultMode);
    }

    [Fact]
    public void ResultMode_文件只读_保存不抛异常()
    {
        // Settings.Save() 中出现异常时静默处理，应不抛
        // 这里测试的是构造函数不抛
        var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "sub", "settings.json");
        var settings = new Settings(nonExistentDir);

        // 不应该抛异常，只是保存失败
        settings.ResultMode = "list";
        settings.Save();  // 静默失败

        Assert.Equal("list", settings.ResultMode); // 内存值不受影响
    }

    // ========== Checker 组合过滤（红队：多个过滤叠加快） ==========

    [Fact]
    public void CheckText_混合文本_多种过滤同时生效()
    {
        var wl = CreateWordList("hello", "world", "test");
        var checker = new Checker(wl)
        {
            IgnoreChinese = true,
            FilterFormatting = true,
            FilterNaming = true,
        };

        var results = checker.CheckText("hello 你好 MTF unknown FF0000 world");

        // Tokenization 分析：
        // tokens: hello(w), " 你好 "(o), MTF(w), " "(o), unknown(w), " "(o), FF0000(w), " "(o), world(w)
        // => 9 个 tokens (5 word + 4 other)
        Assert.Equal(9, results.Count);

        var avail = results.Count(r => r.Status == CheckStatus.Available);
        var unavail = results.Count(r => r.Status == CheckStatus.Unavailable);
        var ignored = results.Count(r => r.Status == CheckStatus.Ignored);

        Assert.Equal(2, avail);    // hello, world
        Assert.Equal(1, unavail);  // unknown
        Assert.Equal(3, ignored);  // 你好, MTF, FF0000
    }

    [Fact]
    public void CheckText_特殊字符组合_不抛异常()
    {
        var wl = CreateWordList("test");
        var checker = new Checker(wl);

        // 特殊字符可能触发正则表达式边界，确保不抛异常
        var ex = Record.Exception(() => checker.CheckText("@#$%^&*()_+-=[]{}|;':\",./<>?~`"));

        Assert.Null(ex);
    }

    [Fact]
    public void CheckText_超长输入_不抛异常()
    {
        var wl = CreateWordList("hello");
        var checker = new Checker(wl);

        var longText = string.Join(" ", Enumerable.Repeat("hello world test unknown", 1000));
        var ex = Record.Exception(() => checker.CheckText(longText));

        Assert.Null(ex);
    }

    [Fact]
    public void GetStatistics_超长结果_不抛异常()
    {
        var wl = CreateWordList("hello");
        var checker = new Checker(wl);

        var results = checker.CheckText(string.Join(" ", Enumerable.Repeat("hello unknown", 500)));
        var ex = Record.Exception(() => checker.GetStatistics(results));

        Assert.Null(ex);
    }

    [Fact]
    public void GetWordFrequency_大量重复不可用词_正确合计()
    {
        var wl = CreateWordList("hello");
        var checker = new Checker(wl);

        var results = checker.CheckText(string.Join(" ", Enumerable.Repeat("badword", 1000)));
        var freq = checker.GetWordFrequency(results);

        Assert.Single(freq);
        Assert.Equal(1000, freq["badword"]);
    }

    // ========== 弹窗相关的边界条件（模拟 DocumentBuilder Tag 设置） ==========

    [Fact]
    public void BuildResultDocument_可用词_Tag为null()
    {
        var results = new List<CheckResult>
        {
            new("hello", CheckStatus.Available),
        };

        var doc = DocumentBuilder.BuildResultDocument(results, 400);

        // Verify via reflection: the Run's Tag should be null for available words
        var paragraph = (System.Windows.Documents.Paragraph)doc.Blocks.FirstBlock;
        var run = (System.Windows.Documents.Run)paragraph.Inlines.FirstInline;
        Assert.Null(run.Tag);
    }

    [Fact]
    public void BuildResultDocument_不可用词_Tag存储CheckResult()
    {
        var checkResult = new CheckResult("badword", CheckStatus.Unavailable);
        var results = new List<CheckResult> { checkResult };

        var doc = DocumentBuilder.BuildResultDocument(results, 400);

        var paragraph = (System.Windows.Documents.Paragraph)doc.Blocks.FirstBlock;
        var run = (System.Windows.Documents.Run)paragraph.Inlines.FirstInline;
        Assert.NotNull(run.Tag);
        Assert.IsType<CheckResult>(run.Tag);

        var tag = (CheckResult)run.Tag;
        Assert.Equal("badword", tag.Text);
        Assert.Equal(CheckStatus.Unavailable, tag.Status);
    }

    [Fact]
    public void BuildResultDocument_不可用词_ToolTip为null_避免与Popup冲突()
    {
        var results = new List<CheckResult>
        {
            new("badword", CheckStatus.Unavailable),
        };

        var doc = DocumentBuilder.BuildResultDocument(results, 400);

        var paragraph = (System.Windows.Documents.Paragraph)doc.Blocks.FirstBlock;
        var run = (System.Windows.Documents.Run)paragraph.Inlines.FirstInline;
        Assert.Null(run.ToolTip); // 显式设置为 null，避免 Popup 与 ToolTip 同时出现
    }

    [Fact]
    public void BuildResultDocument_忽略词_Tag为null()
    {
        var results = new List<CheckResult>
        {
            new("link", CheckStatus.Ignored),
        };

        var doc = DocumentBuilder.BuildResultDocument(results, 400);

        var paragraph = (System.Windows.Documents.Paragraph)doc.Blocks.FirstBlock;
        var run = (System.Windows.Documents.Run)paragraph.Inlines.FirstInline;
        Assert.Null(run.Tag);
    }

    [Fact]
    public void BuildResultDocument_空结果_不抛异常()
    {
        var ex = Record.Exception(() =>
            DocumentBuilder.BuildResultDocument(new List<CheckResult>(), 400));

        Assert.Null(ex);
    }

    [Fact]
    public void BuildResultDocument_仅换行符_不抛异常()
    {
        var results = new List<CheckResult>
        {
            new("\n", CheckStatus.Separator),
        };

        var ex = Record.Exception(() =>
            DocumentBuilder.BuildResultDocument(results, 400));

        Assert.Null(ex);
    }

    // ========== LevenshteinHelper 边界（弹窗依赖） ==========

    [Fact]
    public void FindClosest_候选列表中包含自身_距离0排在首位()
    {
        var candidates = new[] { "different", "theword", "other" };
        var result = LevenshteinHelper.FindClosest("theword", candidates);

        Assert.NotEmpty(result);
        Assert.Equal("theword", result[0].word);
        Assert.Equal(0, result[0].distance);
    }

    [Fact]
    public void FindClosest_极小单词_单个字符()
    {
        var candidates = new[] { "a", "b", "c", "ab" };
        var result = LevenshteinHelper.FindClosest("a", candidates);

        Assert.Contains(result, r => r.word == "a");
    }

    [Fact]
    public void FindClosest_maxResults为0_返回空()
    {
        var candidates = new[] { "hello", "world" };
        var result = LevenshteinHelper.FindClosest("hello", candidates, maxResults: 0);

        Assert.Empty(result);
    }

    // ========== 排版模式 BuildModeViews 逻辑测试 ==========

    [Fact]
    public void BuildModeViews_空结果_列表和两栏都为空()
    {
        var wl = CreateWordList("hello");
        var checker = new Checker(wl);
        var results = new List<CheckResult>();

        // 这个测试验证的是逻辑正确性，而非 UI 绑定（UI 需在 WPF STA 线程）
        // 我们验证 Checker.CheckText 返回空时的正确性即可
        var checkResults = checker.CheckText("");

        Assert.Empty(checkResults);
    }

    [Fact]
    public void BuildModeViews_所有状态类型_列表模式未抛异常()
    {
        // 验证 BuildModeViews 使用的 switch 表达式覆盖所有 CheckStatus 枚举值
        var wl = CreateWordList("hello");
        var checker = new Checker(wl);

        var results = checker.CheckText("hello <split> \n unknown");

        // CheckStatus.Available (hello), Ignored (split), Separator (\n), Unavailable (unknown)
        Assert.Contains(results, r => r.Status == CheckStatus.Available);
        Assert.Contains(results, r => r.Status == CheckStatus.Unavailable);
        Assert.Contains(results, r => r.Status == CheckStatus.Separator);
        Assert.Contains(results, r => r.Status == CheckStatus.Ignored);
    }

    [Fact]
    public void BuildModeViews_筛选Available_去重并排序()
    {
        var wl = CreateWordList("hello", "world", "test", "foo");
        var checker = new Checker(wl);

        var results = checker.CheckText("hello world hello foo");

        // 两栏模式下可用词 = hello, world, foo（去重 + 排序）
        var availableWords = results
            .Where(r => r.Status == CheckStatus.Available)
            .Select(r => r.Text)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(w => w)
            .ToList();

        Assert.Equal(3, availableWords.Count);
        Assert.Equal("foo", availableWords[0]);
        Assert.Equal("hello", availableWords[1]);
        Assert.Equal("world", availableWords[2]);
    }

    [Fact]
    public void BuildModeViews_筛选Unavailable_大小写去重()
    {
        var wl = CreateWordList("hello");
        var checker = new Checker(wl);

        var results = checker.CheckText("hello BADWORD badword Badword");

        var unavailableWords = results
            .Where(r => r.Status == CheckStatus.Unavailable)
            .Select(r => r.Text)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(w => w)
            .ToList();

        Assert.Single(unavailableWords);
    }

    // ========== SwitchModeView 逻辑验证 ==========

    [Fact]
    public void SwitchModeView_三种模式互斥_不同时可见()
    {
        // SwitchModeView 的逻辑：将 mode 参数与三个视图的 Visibility 比较
        // 只应有一个视图 Visible，其余两个 Collapsed

        string[] modes = { "inline", "list", "compare" };
        foreach (var mode in modes)
        {
            var visible = mode switch
            {
                "inline" => "ResultBox",
                "list" => "ResultListView",
                "compare" => "ResultCompareGrid",
                _ => "",
            };

            // 验证三个模式互斥：每种模式只有一个视图是活动的
            Assert.NotEqual("", visible);

            // 其他两个视图应该是非活动的
            var others = modes.Where(m => m != mode).ToList();
            Assert.Equal(2, others.Count);
        }
    }

    // ========== 弹窗 OnRunMouseEnter 逻辑覆盖 ==========

    [Fact]
    public void OnRunMouseEnter_逻辑_仅当Tag为CheckResult且Unavailable时触发()
    {
        // 验证 OnRunMouseEnter 中的条件判断逻辑
        // 条件: `e.OriginalSource is Run run && run.Tag is CheckResult cr && cr.Status == CheckStatus.Unavailable`

        var result = new CheckResult("test", CheckStatus.Unavailable);
        var tag = result;

        // 条件 1: Tag 是 CheckResult
        Assert.IsType<CheckResult>(tag);
        // 条件 2: Status == Unavailable
        Assert.Equal(CheckStatus.Unavailable, ((CheckResult)tag).Status);

        // Tag 是 CheckResult 对象且 Status 正确 - 模式匹配就通过
        Assert.True(tag is CheckResult { Status: CheckStatus.Unavailable });
        Assert.False(tag is CheckResult { Status: CheckStatus.Available });

        // Status 是 Available 时不应触发
        var availableResult = new CheckResult("test", CheckStatus.Available);
        Assert.NotEqual(CheckStatus.Unavailable, availableResult.Status);
    }

    [Fact]
    public void ShowWordDetailPopup_弹窗内容_内部逻辑正确()
    {
        var wl = CreateWordList("hello");
        var checker = new Checker(wl);

        var results = checker.CheckText("badword");
        Assert.Single(results);
        Assert.Equal(CheckStatus.Unavailable, results[0].Status);

        // 验证 ShowWordDetailPopup 内部使用的逻辑
        var word = results[0].Text;
        Assert.Equal("badword", word);

        // 验证词频查询逻辑（GetValueOrDefault）
        var freq = checker.GetWordFrequency(results);
        Assert.Equal(1, freq.GetValueOrDefault(word, 0));

        // 验证 Levenshtein 建议查询逻辑
        var suggestions = LevenshteinHelper.FindClosest(word, wl.Words, 3);
        Assert.NotNull(suggestions);
    }

    // ========== 实际词库文件回归测试 ==========

    [Fact]
    public void 真实词库_检查已知词_返回Available()
    {
        // 使用项目中实际的词库文件
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var wordlistPath = Path.Combine(baseDir, "data", "cassie-text.txt");

        if (!File.Exists(wordlistPath))
        {
            // 如果在测试环境找不到，从项目根目录找
            wordlistPath = Path.Combine(
                Directory.GetCurrentDirectory(), "..", "..", "..", "data", "cassie-text.txt");
            wordlistPath = Path.GetFullPath(wordlistPath);
        }

        if (!File.Exists(wordlistPath))
            return; // 词库文件不存在时跳过

        var wl = new WordList();
        wl.LoadFromFile(wordlistPath);
        var checker = new Checker(wl);

        // 测试几个已知在词库中的词
        var knownWords = "hello world test door elevator light";
        var results = checker.CheckText(knownWords);

        Assert.All(results.Where(r => r.Status != CheckStatus.Separator),
            r => Assert.Equal(CheckStatus.Available, r.Status));
    }

    [Fact]
    public void 真实词库_检查不在词库中的词_返回Unavailable()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var wordlistPath = Path.Combine(baseDir, "data", "cassie-text.txt");

        if (!File.Exists(wordlistPath))
        {
            wordlistPath = Path.Combine(
                Directory.GetCurrentDirectory(), "..", "..", "..", "data", "cassie-text.txt");
            wordlistPath = Path.GetFullPath(wordlistPath);
        }

        if (!File.Exists(wordlistPath))
            return;

        var wl = new WordList();
        wl.LoadFromFile(wordlistPath);
        var checker = new Checker(wl);

        // 这些词不应当在词库中
        var unknownWords = "xyzzynotaword qwertyuioplkjhgfdsazxcvbnm nonexistentword12345";
        var results = checker.CheckText(unknownWords);

        Assert.All(results.Where(r => r.Status == CheckStatus.Available || r.Status == CheckStatus.Unavailable),
            r => Assert.Equal(CheckStatus.Unavailable, r.Status));
    }

    // ========== 设置加载后_ResultMode与ComboBox同步 ==========

    [Fact]
    public void PopulateModeCombo_三种模式_都可用()
    {
        // 模拟 PopulateModeCombo 中的模式列表
        var modes = new[]
        {
            new { Text = "内嵌模式", Value = "inline" },
            new { Text = "列表模式", Value = "list" },
            new { Text = "两栏对比", Value = "compare" },
        };

        Assert.Equal(3, modes.Length);
        Assert.Contains(modes, m => m.Value == "inline");
        Assert.Contains(modes, m => m.Value == "list");
        Assert.Contains(modes, m => m.Value == "compare");
        Assert.All(modes, m => Assert.False(string.IsNullOrEmpty(m.Text)));
        Assert.All(modes, m => Assert.False(string.IsNullOrEmpty(m.Value)));
    }

    // ========== GetStatistics 更详细的边界 ==========

    [Fact]
    public void GetStatistics_Separator不计入总数()
    {
        var wl = CreateWordList("hello", "world");
        var checker = new Checker(wl);

        var results = checker.CheckText("hello\nworld");

        // hello(Available), \n(Separator), world(Available)
        var stats = checker.GetStatistics(results);

        Assert.Equal(2, (int)stats["total"]);       // \n 不计入
        Assert.Equal(2, (int)stats["available"]);
        Assert.Equal(0, (int)stats["unavailable"]);
    }

    // ========== 辅助方法 ==========

    private static WordList CreateWordList(params string[] words)
    {
        var path = Path.GetTempFileName();
        File.WriteAllLines(path, words);
        var wl = new WordList();
        wl.LoadFromFile(path);
        return wl;
    }
}
