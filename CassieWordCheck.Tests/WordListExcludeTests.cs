using System.IO;
using CassieWordCheck.Models;

namespace CassieWordCheck.Tests;

public class WordListExcludeTests
{
    private static WordList CreateWordList(params string[] words)
    {
        var path = Path.GetTempFileName();
        File.WriteAllLines(path, words);
        var wl = new WordList();
        wl.LoadFromFile(path);
        return wl;
    }

    // ===== 排除/取消排除 =====

    [Fact]
    public void Exclude_添加排除词_返回True()
    {
        // 给定
        var wl = CreateWordList("hello", "world");

        // 当
        var result = wl.Exclude("hello");

        // 则
        Assert.True(result);
        Assert.Contains("hello", wl.ExcludeList);
    }

    [Fact]
    public void Exclude_重复排除同一词_返回False()
    {
        // 给定
        var wl = CreateWordList("hello", "world");
        wl.Exclude("hello");

        // 当
        var result = wl.Exclude("hello");

        // 则
        Assert.False(result);
    }

    [Fact]
    public void Exclude_空字符串_返回False()
    {
        // 给定
        var wl = CreateWordList("hello");

        // 当
        var result = wl.Exclude("");

        // 则
        Assert.False(result);
    }

    [Fact]
    public void UnExclude_移除排除词_返回True()
    {
        // 给定
        var wl = CreateWordList("hello", "world");
        wl.Exclude("hello");

        // 当
        var result = wl.UnExclude("hello");

        // 则
        Assert.True(result);
        Assert.DoesNotContain("hello", wl.ExcludeList);
    }

    [Fact]
    public void UnExclude_不存在的排除词_返回False()
    {
        // 给定
        var wl = CreateWordList("hello");

        // 当
        var result = wl.UnExclude("nonexistent");

        // 则
        Assert.False(result);
    }

    [Fact]
    public void ClearExclude_清除所有排除词()
    {
        // 给定
        var wl = CreateWordList("hello", "world", "test");
        wl.Exclude("hello");
        wl.Exclude("world");

        // 当
        wl.ClearExclude();

        // 则
        Assert.Empty(wl.ExcludeList);
        Assert.Equal(0, wl.ExcludeCount);
    }

    // ===== IsExcluded 方法 =====

    [Fact]
    public void IsExcluded_排除的词_返回True()
    {
        // 给定
        var wl = CreateWordList("hello");
        wl.Exclude("hello");

        // 当
        var result = wl.IsExcluded("hello");

        // 则
        Assert.True(result);
    }

    [Fact]
    public void IsExcluded_未排除的词_返回False()
    {
        // 给定
        var wl = CreateWordList("hello");

        // 当
        var result = wl.IsExcluded("hello");

        // 则
        Assert.False(result);
    }

    [Fact]
    public void IsExcluded_不区分大小写()
    {
        // 给定
        var wl = CreateWordList("Hello");
        wl.Exclude("hello");

        // 当
        var result = wl.IsExcluded("HELLO");

        // 则
        Assert.True(result);
    }

    // ===== Check 方法（结合排除列表） =====

    [Fact]
    public void Check_排除的词_返回False()
    {
        // 给定
        var wl = CreateWordList("hello", "world");
        wl.Exclude("hello");

        // 当
        var result = wl.Check("hello");

        // 则
        Assert.False(result);
    }

    [Fact]
    public void Check_词库中有但未排除_返回True()
    {
        // 给定
        var wl = CreateWordList("hello", "world");
        wl.Exclude("hello");

        // 当
        var result = wl.Check("world");

        // 则
        Assert.True(result);
    }

    [Fact]
    public void Check_白名单优先于排除列表()
    {
        // 给定
        var wl = CreateWordList("hello");
        wl.Exclude("hello");
        wl.AddToWhitelist("hello");

        // 当
        var result = wl.Check("hello");

        // 则
        Assert.True(result);
    }

    [Fact]
    public void Check_排除后取消排除_恢复可用()
    {
        // 给定
        var wl = CreateWordList("hello");
        wl.Exclude("hello");
        wl.UnExclude("hello");

        // 当
        var result = wl.Check("hello");

        // 则
        Assert.True(result);
    }

    [Fact]
    public void Check_排除后添加白名单再移除白名单_仍被排除()
    {
        // 给定
        var wl = CreateWordList("hello");
        wl.Exclude("hello");
        wl.AddToWhitelist("hello");  // 白名单覆盖排除
        wl.RemoveFromWhitelist("hello"); // 白名单移除，排除仍在

        // 当
        var result = wl.Check("hello");

        // 则
        Assert.False(result);
    }

    // ===== 排除列表计数/只读属性 =====

    [Fact]
    public void ExcludeCount_初始为0()
    {
        // 给定
        var wl = CreateWordList("hello", "world");

        // 当
        var count = wl.ExcludeCount;

        // 则
        Assert.Equal(0, count);
    }

    [Fact]
    public void ExcludeCount_排除后增加()
    {
        // 给定
        var wl = CreateWordList("hello", "world", "test");
        wl.Exclude("hello");
        wl.Exclude("world");

        // 当
        var count = wl.ExcludeCount;

        // 则
        Assert.Equal(2, count);
    }

    [Fact]
    public void ExcludeList_通过只读接口暴露()
    {
        // 给定
        var wl = CreateWordList("hello");
        wl.Exclude("hello");

        // 则
        Assert.IsAssignableFrom<IReadOnlySet<string>>(wl.ExcludeList);
    }

    // ===== 序列化兼容 =====

    [Fact]
    public void Exclude_不影响词库Words集合()
    {
        // 给定
        var wl = CreateWordList("hello", "world");
        var originalCount = wl.WordCount;

        // 当
        wl.Exclude("hello");

        // 则
        Assert.Equal(originalCount, wl.WordCount);
        Assert.Contains("hello", wl.Words);
    }

    [Fact]
    public void 排除后重新加载词库_排除列表保留()
    {
        // 给定
        var wl = CreateWordList("hello", "world");
        wl.Exclude("hello");
        wl.Exclude("world");

        // 当
        wl.Reload();

        // 则
        Assert.Equal(2, wl.ExcludeCount);
        Assert.True(wl.IsExcluded("hello"));
        Assert.True(wl.IsExcluded("world"));
    }

    [Fact]
    public void SetWhitelist_不影响排除列表()
    {
        // 给定
        var wl = CreateWordList("hello", "world", "test");
        wl.Exclude("hello");
        wl.Exclude("test");

        // 当
        wl.SetWhitelist(new[] { "world" });

        // 则
        Assert.True(wl.IsExcluded("hello"));
        Assert.False(wl.IsExcluded("world"));
        Assert.True(wl.IsExcluded("test"));
    }
}
