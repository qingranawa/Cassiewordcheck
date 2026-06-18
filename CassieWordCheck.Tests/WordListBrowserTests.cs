using System.IO;
using CassieWordCheck.Models;

namespace CassieWordCheck.Tests;

public class WordListBrowserTests
{
    private static WordList CreateWordList(params string[] words)
    {
        var path = Path.GetTempFileName();
        File.WriteAllLines(path, words);
        var wl = new WordList();
        wl.LoadFromFile(path);
        return wl;
    }

    [Fact]
    public void GetWordLengthDistribution_基本分布_返回正确计数()
    {
        // 给定
        var wl = CreateWordList("cat", "dog", "elephant", "bird");

        // 当
        var dist = wl.GetWordLengthDistribution();

        // 则
        Assert.Equal(2, dist[3]); // cat, dog 长度=3
        Assert.Equal(1, dist[4]); // bird 长度=4
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
        var wl = CreateWordList("apple", "ant", "bear", "cat", "dog");

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
        var wl = CreateWordList("hello", "123abc", "_test");

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
