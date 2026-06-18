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
    public void LevenshteinSearch_距离0_返回自身()
    {
        var engine = CreateEngine("announcement", "announcer", "management");

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
        var engine = CreateEngine("a", "test");

        var results = engine.CompoundSplit("atest", 3);

        Assert.Empty(results);
    }

    [Fact]
    public void CompoundSplit_总长小于6_不返回结果()
    {
        var engine = CreateEngine("cat", "dog");

        var results = engine.CompoundSplit("catdo", 3);

        Assert.Empty(results);
    }

    [Fact]
    public void CompoundSplit_无匹配组合_返回空()
    {
        var engine = CreateEngine("cat", "dog");

        var results = engine.CompoundSplit("zzzzzz", 3);

        Assert.Empty(results);
    }

    // ===== 综合建议 =====

    [Fact]
    public void GetSuggestions_混合策略_包含通配结果()
    {
        var engine = CreateEngine("announcement", "announcer", "announce",
            "announcements", "management", "announcing", "anno", "annoy");

        var results = engine.GetSuggestions("announc*", 10);

        Assert.Contains(results, r => r.Source == "wildcard");
        Assert.True(results.Count > 0);
    }

    [Fact]
    public void GetSuggestions_去重_同词不重复出现()
    {
        var engine = CreateEngine("announcement", "announce");

        var results = engine.GetSuggestions("announc*", 10);

        var announceCount = results.Count(r => r.Word == "announcement");
        Assert.True(announceCount <= 1, "同一建议词不应重复出现");
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

        var results = engine.GetSuggestions("abcdefgh", 10);

        Assert.Empty(results);
    }

    [Fact]
    public void GetSuggestions_结果上限_不超过指定数量()
    {
        var words = Enumerable.Range(0, 50).Select(i => $"word{i}").ToArray();
        var engine = CreateEngine(words);

        var results = engine.GetSuggestions("word*", 10);

        Assert.True(results.Count <= 10);
    }
}
