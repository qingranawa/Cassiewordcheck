namespace CassieWordCheck.Models;

/// <summary>字数统计结果</summary>
public class WordCountResult
{
    // 字符统计
    public int TotalChars { get; set; }
    public int CharsNoSpaces { get; set; }
    public int ChineseChars { get; set; }
    public int EnglishLetters { get; set; }
    public int DigitChars { get; set; }
    public int PunctuationChars { get; set; }

    // 单词统计
    public int TotalWords { get; set; }
    public int UniqueWords { get; set; }
    public double AvgWordLength { get; set; }
    public List<WordFreqItem> TopFrequentWords { get; set; } = [];
    public List<WordLengthBucket> WordLengthDistribution { get; set; } = [];

    // 行统计
    public int TotalLines { get; set; }
    public int NonEmptyLines { get; set; }
}

/// <summary>词频条目</summary>
public class WordFreqItem
{
    public string Word { get; set; } = "";
    public int Count { get; set; }
}

/// <summary>词长分布桶</summary>
public class WordLengthBucket
{
    public string Label { get; set; } = ""; // "1-3", "4-6", "7-9", "10+"
    public int Count { get; set; }
}
