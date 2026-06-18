using System.IO;
using CassieWordCheck.Models;

namespace CassieWordCheck.Tests;

/// <summary>
/// Checker 核心检查逻辑的测试
/// </summary>
public class CheckerTests
{
    /// <summary>构造一个包含给定单词的 WordList</summary>
    private static WordList CreateWordList(params string[] words)
    {
        // 创建临时词库文件
        var path = Path.GetTempFileName();
        File.WriteAllLines(path, words);
        var wl = new WordList();
        wl.LoadFromFile(path);
        return wl;
    }

    // ========== 可用词检查 ==========

    [Fact]
    public void CheckText_单词在词库中_标记为Available()
    {
        var wl = CreateWordList("hello", "world");
        var checker = new Checker(wl);

        var results = checker.CheckText("hello");

        Assert.Single(results);
        Assert.Equal("hello", results[0].Text);
        Assert.Equal(CheckStatus.Available, results[0].Status);
    }

    [Fact]
    public void CheckText_单词不在词库中_标记为Unavailable()
    {
        var wl = CreateWordList("hello");
        var checker = new Checker(wl);

        var results = checker.CheckText("world");

        Assert.Single(results);
        Assert.Equal("world", results[0].Text);
        Assert.Equal(CheckStatus.Unavailable, results[0].Status);
    }

    // ========== 多单词 / 多行 ==========

    [Fact]
    public void CheckText_多个单词_分别正确检查()
    {
        var wl = CreateWordList("hello", "world");
        var checker = new Checker(wl);

        var results = checker.CheckText("hello world");

        Assert.Equal(3, results.Count); // 两个单词 + 中间分隔
        Assert.Equal(CheckStatus.Available, results[0].Status); // hello
        Assert.Equal(CheckStatus.Separator, results[1].Status);  // 空格分隔
        Assert.Equal(CheckStatus.Available, results[2].Status);  // world
    }

    [Fact]
    public void CheckText_多行文本_行间插入换行分隔()
    {
        var wl = CreateWordList("hello", "world");
        var checker = new Checker(wl);

        var results = checker.CheckText("hello\nworld");

        Assert.Equal(3, results.Count);
        Assert.Equal(CheckStatus.Available, results[0].Status); // hello
        Assert.Equal(CheckStatus.Separator, results[1].Status); // \n
        Assert.Equal("world", results[2].Text);
    }

    // ========== 中文忽略 ==========

    [Fact]
    public void CheckText_中文忽略开启_中文标记为Ignored()
    {
        var wl = CreateWordList("test");
        var checker = new Checker(wl) { IgnoreChinese = true };

        var results = checker.CheckText("你好");

        Assert.Single(results);
        Assert.Equal(CheckStatus.Ignored, results[0].Status);
    }

    [Fact]
    public void CheckText_中文忽略关闭_中文不标记Ignored()
    {
        var wl = CreateWordList("test");
        var checker = new Checker(wl) { IgnoreChinese = false };

        var results = checker.CheckText("你好");

        Assert.Single(results);
        Assert.Equal(CheckStatus.Separator, results[0].Status);
    }

    // ========== 格式标记过滤 ==========

    [Fact]
    public void CheckText_格式标记link_标记为Ignored()
    {
        var wl = CreateWordList("test");
        var checker = new Checker(wl) { FilterFormatting = true };

        // "link" 是格式标记
        var results = checker.CheckText("link");

        Assert.Single(results);
        Assert.Equal(CheckStatus.Ignored, results[0].Status);
    }

    [Fact]
    public void CheckText_格式标记关闭_link视为普通词()
    {
        var wl = CreateWordList();
        var checker = new Checker(wl) { FilterFormatting = false };

        var results = checker.CheckText("link");

        Assert.Single(results);
        Assert.Equal(CheckStatus.Unavailable, results[0].Status); // 不在词库中
    }

    [Fact]
    public void CheckText_split标签_标记为Ignored()
    {
        var wl = CreateWordList("test");
        var checker = new Checker(wl) { FilterFormatting = true };

        // WordRegex [a-zA-Z0-9_.-]+ 不会匹配尖括号，所以 <split> 被拆为三个 token
        // 真正测试的是 split 本身应被 Ignored
        var results = checker.CheckText("split");

        Assert.Single(results);
        Assert.Equal(CheckStatus.Ignored, results[0].Status);
    }

    [Fact]
    public void CheckText_十六进制色值_标记为Ignored()
    {
        var wl = CreateWordList("test");
        var checker = new Checker(wl) { FilterFormatting = true };

        var results = checker.CheckText("FF0000");

        Assert.Single(results);
        Assert.Equal(CheckStatus.Ignored, results[0].Status);
    }

    // ========== 命名过滤 ==========

    [Fact]
    public void CheckText_阵营缩写MTF_标记为Ignored()
    {
        var wl = CreateWordList("test");
        var checker = new Checker(wl) { FilterNaming = true };

        var results = checker.CheckText("MTF");

        Assert.Single(results);
        Assert.Equal(CheckStatus.Ignored, results[0].Status);
    }

    [Fact]
    public void CheckText_命名过滤关闭_MTF视为普通词()
    {
        var wl = CreateWordList();
        var checker = new Checker(wl) { FilterNaming = false };

        var results = checker.CheckText("MTF");

        Assert.Single(results);
        Assert.Equal(CheckStatus.Unavailable, results[0].Status);
    }

    [Fact]
    public void CheckText_希腊字母Alpha_标记为Ignored()
    {
        var wl = CreateWordList("test");
        var checker = new Checker(wl) { FilterNaming = true };

        var results = checker.CheckText("Alpha");

        Assert.Single(results);
        Assert.Equal(CheckStatus.Ignored, results[0].Status);
    }

    [Fact]
    public void CheckText_北约代号Alpha1_标记为Ignored()
    {
        var wl = CreateWordList("test");
        var checker = new Checker(wl) { FilterNaming = true };

        var results = checker.CheckText("Alpha-1");

        Assert.Single(results);
        Assert.Equal(CheckStatus.Ignored, results[0].Status);
    }

    // ========== 空文本 ==========

    [Fact]
    public void CheckText_空文本_返回空列表()
    {
        var wl = CreateWordList("test");
        var checker = new Checker(wl);

        var results = checker.CheckText("");

        Assert.Empty(results);
    }

    [Fact]
    public void CheckText_纯空白_只有分隔符()
    {
        var wl = CreateWordList("test");
        var checker = new Checker(wl);

        var results = checker.CheckText("   ");

        // 空白被 tokenize 为 "other" 类型，标记为 Separator
        Assert.Single(results);
        Assert.Equal(CheckStatus.Separator, results[0].Status);
    }

    // ========== 统计信息 ==========

    [Fact]
    public void GetStatistics_返回正确计数()
    {
        var wl = CreateWordList("hello", "world", "foo", "bar");
        var checker = new Checker(wl);
        var results = checker.CheckText("hello unknown foo");

        var stats = checker.GetStatistics(results);

        Assert.Equal(3, (int)stats["total"]);
        Assert.Equal(2, (int)stats["available"]);  // hello, foo
        Assert.Equal(1, (int)stats["unavailable"]); // unknown
        Assert.Equal(0, (int)stats["ignored"]);
        Assert.Equal(66.66666666666667, (double)stats["coverage"], 5); // 2/3 ≈ 66.67%
    }

    [Fact]
    public void GetStatistics_空文本_覆盖率为100()
    {
        var wl = CreateWordList("test");
        var checker = new Checker(wl);
        var results = checker.CheckText("");

        var stats = checker.GetStatistics(results);

        Assert.Equal(0, (int)stats["total"]);
        Assert.Equal(0, (int)stats["available"]);
        Assert.Equal(0, (int)stats["unavailable"]);
        Assert.Equal(100.0, (double)stats["coverage"], 5); // total=0 返回 100
    }

    // ========== 词频统计 ==========

    [Fact]
    public void GetWordFrequency_统计不可用词出现次数()
    {
        var wl = CreateWordList("hello");
        var checker = new Checker(wl);
        var results = checker.CheckText("hello unknown test unknown");

        var freq = checker.GetWordFrequency(results);

        Assert.Equal(2, freq["unknown"]);
        Assert.False(freq.ContainsKey("hello")); // 可用词不计入
    }

    // ========== 白名单 ==========

    [Fact]
    public void CheckText_白名单中的词_标记为Available()
    {
        var wl = CreateWordList("hello");
        wl.AddToWhitelist("customword");
        var checker = new Checker(wl);

        var results = checker.CheckText("customword");

        Assert.Single(results);
        Assert.Equal(CheckStatus.Available, results[0].Status);
    }

    // ========== 大小写不敏感 ==========

    [Fact]
    public void CheckText_大小写不敏感_HELLO匹配hello()
    {
        var wl = CreateWordList("hello");
        var checker = new Checker(wl);

        var results = checker.CheckText("HELLO");

        Assert.Single(results);
        Assert.Equal(CheckStatus.Available, results[0].Status);
    }
}
