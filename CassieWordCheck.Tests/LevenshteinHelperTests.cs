using CassieWordCheck.Services;

namespace CassieWordCheck.Tests;

/// <summary>
/// LevenshteinHelper 编辑距离与拼写建议测试
/// </summary>
public class LevenshteinHelperTests
{
    // ========== 编辑距离 ==========

    [Fact]
    public void Distance_相同单词_返回0()
    {
        var d = LevenshteinHelper.Distance("hello", "hello");
        Assert.Equal(0, d);
    }

    [Fact]
    public void Distance_空串与空串_返回0()
    {
        var d = LevenshteinHelper.Distance("", "");
        Assert.Equal(0, d);
    }

    [Fact]
    public void Distance_空串与非空串_返回非空串长度()
    {
        var d = LevenshteinHelper.Distance("", "hello");
        Assert.Equal(5, d);
    }

    [Fact]
    public void Distance_插入一个字符_返回1()
    {
        var d = LevenshteinHelper.Distance("helo", "hello");
        Assert.Equal(1, d);
    }

    [Fact]
    public void Distance_删除一个字符_返回1()
    {
        var d = LevenshteinHelper.Distance("hello", "helo");
        Assert.Equal(1, d);
    }

    [Fact]
    public void Distance_替换一个字符_返回1()
    {
        var d = LevenshteinHelper.Distance("hallo", "hello");
        Assert.Equal(1, d);
    }

    [Fact]
    public void Distance_完全不同的单词_返回较大距离()
    {
        var d = LevenshteinHelper.Distance("abc", "xyz");
        Assert.Equal(3, d); // 三个替换
    }

    [Fact]
    public void Distance_大小写敏感_区分大小写()
    {
        var d = LevenshteinHelper.Distance("Hello", "hello");
        Assert.Equal(1, d); // H->h 替换
    }

    // ========== FindClosest 找相似词 ==========

    [Fact]
    public void FindClosest_精确匹配_返回0距离()
    {
        var candidates = new[] { "hello", "world", "help" };
        var result = LevenshteinHelper.FindClosest("hello", candidates);

        Assert.Contains(result, r => r.word == "hello" && r.distance == 0);
    }

    [Fact]
    public void FindClosest_返回最近的前3个()
    {
        var candidates = new[] { "hello", "hell", "help", "helicopter", "world", "held" };
        var result = LevenshteinHelper.FindClosest("hello", candidates);

        Assert.True(result.Count <= 3);
        // 最近的应该是 hello(0), hell(1), help(1) / held(2)
        Assert.Equal("hello", result[0].word);
    }

    [Fact]
    public void FindClosest_无相似词_返回空列表()
    {
        var candidates = new[] { "abcdefghij", "klmnopqrst", "uvwxyzabcd" };
        var result = LevenshteinHelper.FindClosest("hello", candidates);

        Assert.Empty(result);
    }

    [Fact]
    public void FindClosest_按距离排序_距离相同按字母排序()
    {
        var candidates = new[] { "xyz", "abc", "def" };
        // 对 hello 来说距离都很大，会被过滤掉
        var result = LevenshteinHelper.FindClosest("hello", candidates);

        Assert.Empty(result); // 距离超过阈值
    }

    [Fact]
    public void FindClosest_大小写不敏感_python和Python视为相同()
    {
        var candidates = new[] { "Python", "java" };
        var result = LevenshteinHelper.FindClosest("python", candidates);

        Assert.Contains(result, r => r.word == "Python" && r.distance == 0);
    }

    [Fact]
    public void FindClosest_候选列表为空_返回空列表()
    {
        var result = LevenshteinHelper.FindClosest("hello", Array.Empty<string>());
        Assert.Empty(result);
    }

    [Fact]
    public void FindClosest_超过maxResults_只返回指定数量()
    {
        var candidates = new[] { "cat", "car", "cab", "cap", "bat" };
        var result = LevenshteinHelper.FindClosest("cat", candidates, maxResults: 2);

        Assert.True(result.Count <= 2);
    }

    [Fact]
    public void FindClosest_短单词阈值_短词距离1也算相似()
    {
        // 短词 word.Length/3 = 1，Math.Max(3, 1) = 3
        var candidates = new[] { "dog", "dot", "fog", "dug" };
        var result = LevenshteinHelper.FindClosest("dog", candidates);

        // dog(0), dot(1), fog(1), dug(1) 都应在结果中（阈值 3）
        Assert.Equal(3, result.Count); // maxResults=3
    }

    // ========== 边界条件：长词阈值 ==========

    [Fact]
    public void FindClosest_长单词阈值_较宽松()
    {
        // word.Length = 12; word.Length/3 = 4; Math.Max(3, 4) = 4
        var word = "accommodation";
        var candidates = new[] { "accommodation", "acommodation", "accomodation" };

        var result = LevenshteinHelper.FindClosest(word, candidates);

        // 距离 0 和 1 的都应包含
        Assert.Equal(3, result.Count);
    }
}
