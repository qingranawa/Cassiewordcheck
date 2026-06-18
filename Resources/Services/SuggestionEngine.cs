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
        if (string.IsNullOrEmpty(word))
            return [];

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

    /// <summary>Levenshtein 编辑距离计算（内联实现，独立于 LevenshteinHelper）</summary>
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
