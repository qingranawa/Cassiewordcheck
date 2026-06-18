using CassieWordCheck.Models;

namespace CassieWordCheck.Services;

/// <summary>字数统计服务（纯静态，无副作用）</summary>
public static class WordCountService
{
    public static WordCountResult Count(string text)
    {
        var result = new WordCountResult();

        if (string.IsNullOrEmpty(text))
            return result;

        // ── 字符统计 ──
        result.TotalChars = text.Length;
        result.CharsNoSpaces = text.Count(c => !char.IsWhiteSpace(c));
        result.ChineseChars = text.Count(c => c >= 0x4E00 && c <= 0x9FFF);
        result.EnglishLetters = text.Count(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'));
        result.DigitChars = text.Count(char.IsDigit);
        result.PunctuationChars = text.Count(char.IsPunctuation);

        // ── 行统计 ──
        var lines = text.Split('\n');
        result.TotalLines = lines.Length;
        result.NonEmptyLines = lines.Count(l => !string.IsNullOrWhiteSpace(l));

        // ── 单词统计 ──
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        result.TotalWords = words.Length;

        if (words.Length > 0)
        {
            result.UniqueWords = words.Distinct(StringComparer.OrdinalIgnoreCase).Count();
            result.AvgWordLength = Math.Round((double)words.Sum(w => w.Length) / words.Length, 1);

            // 词频 Top 12
            result.TopFrequentWords = words
                .GroupBy(w => w, StringComparer.OrdinalIgnoreCase)
                .Select(g => new WordFreqItem { Word = g.Key, Count = g.Count() })
                .OrderByDescending(f => f.Count)
                .Take(12)
                .ToList();

            // 词长分布（1-3 / 4-6 / 7-9 / 10+）
            var buckets = new Dictionary<string, int>
            {
                ["1-3"] = 0, ["4-6"] = 0, ["7-9"] = 0, ["10+"] = 0,
            };
            foreach (var w in words)
            {
                var len = w.Length;
                if (len <= 3) buckets["1-3"]++;
                else if (len <= 6) buckets["4-6"]++;
                else if (len <= 9) buckets["7-9"]++;
                else buckets["10+"]++;
            }
            result.WordLengthDistribution = buckets
                .Select(kv => new WordLengthBucket { Label = kv.Key, Count = kv.Value })
                .ToList();
        }

        return result;
    }
}
