using System.Collections.Frozen;
using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace CassieWordCheck.Models;

/// <summary>词库差异对比结果</summary>
public record WordListDiff(
    string LeftLabel,
    string RightLabel,
    int LeftOnlyCount,
    int RightOnlyCount,
    int CommonCount,
    IReadOnlySet<string> LeftOnly,
    IReadOnlySet<string> RightOnly
);

/// <summary>
/// 词库——用 FrozenSet 做 O(1) 查询，加载后不可变喵~
/// 支持白名单、多格式导入（TXT/CSV/Excel）喵
/// </summary>
public partial class WordList : IDisposable
{
    // 核心词库，加载后不可变，查询极快喵！
    private FrozenSet<string> _words = FrozenSet<string>.Empty;

    // 白名单——用户手动添加的豁免词，运行时可变喵~
    private HashSet<string> _whitelist = [];

    // 排除列表——用户手动标记要隐藏的词（即使词库中有），运行时可变喵~
    private readonly HashSet<string> _excludeList = new(StringComparer.OrdinalIgnoreCase);

    // 当前词库文件的路径，重载时需要喵~
    private string? _sourcePath;

    // === 文件系统监控 ===
    private FileSystemWatcher? _watcher;
    private System.Timers.Timer? _debounceTimer;
    private const int DebounceDelayMs = 500;

    /// <summary>词库文件变更时触发（已自动重载）喵~</summary>
    public event Action? WordListChanged;

    public int WordCount => _words.Count;
    public int WhitelistCount => _whitelist.Count;
    public string? SourcePath => _sourcePath;
    public IReadOnlySet<string> Words => _words;
    public IReadOnlySet<string> Whitelist => _whitelist;

    /// <summary>排除列表的只读视图喵~</summary>
    public IReadOnlySet<string> ExcludeList => _excludeList;
    /// <summary>排除列表中的词数喵~</summary>
    public int ExcludeCount => _excludeList.Count;

    /// <summary>从文件加载词库（先清空再加载）喵~</summary>
    public int LoadFromFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Word list file not found.", path);

        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;

            // 格式 "word:phoneme" —— 只取冒号前的部分喵~
            if (trimmed.Contains(':') && !trimmed.StartsWith('.'))
            {
                var word = trimmed.Split(':', 2)[0].Trim();
                AddParts(word, words);
            }
            else
            {
                AddParts(trimmed, words);
            }
        }

        // 转为 FrozenSet，之后不可变，查询性能更好喵！
        _words = words.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        _sourcePath = path;
        SetupWatcher();
        return _words.Count;
    }

    // 按空白符拆分后逐个加入（一行可能有多个单词喵~）
    private static void AddParts(string text, HashSet<string> words)
    {
        foreach (var part in WordSplitRegex().Split(text))
        {
            var p = part.Trim();
            if (p.Length > 0 && !p.StartsWith('#'))
                words.Add(p);
        }
    }

    /// <summary>重新加载当前词库喵~</summary>
    public int Reload()
    {
        return _sourcePath is not null ? LoadFromFile(_sourcePath) : 0;
    }

    /// <summary>查询单词是否在词库或白名单中喵~</summary>
    public bool Check(string word)
    {
        var w = word.Trim();
        if (w.Length == 0) return true;
        // 白名单优先于排除列表：白名单中的词始终可用喵~
        if (_whitelist.Contains(w)) return true;
        // 排除列表优先级高于词库：被排除的词即使词库中有也返回 false 喵~
        if (_excludeList.Contains(w)) return false;
        return _words.Contains(w);
    }

    /// <summary>添加单词到白名单喵~</summary>
    public bool AddToWhitelist(string word)
    {
        var w = word.Trim().ToLowerInvariant();
        return w.Length > 0 && _whitelist.Add(w);
    }

    /// <summary>从白名单移除单词喵~</summary>
    public bool RemoveFromWhitelist(string word)
    {
        return _whitelist.Remove(word.Trim().ToLowerInvariant());
    }

    /// <summary>批量设置白名单喵~</summary>
    public void SetWhitelist(IEnumerable<string> words)
    {
        _whitelist = words
            .Select(w => w.Trim().ToLowerInvariant())
            .Where(w => w.Length > 0)
            .ToHashSet();
    }

    /// <summary>清空白名单喵~</summary>
    public void ClearWhitelist()
    {
        _whitelist.Clear();
    }

    /// <summary>将单词加入排除列表喵~</summary>
    public bool Exclude(string word)
    {
        var w = word.Trim().ToLowerInvariant();
        return w.Length > 0 && _excludeList.Add(w);
    }

    /// <summary>从排除列表移除单词喵~</summary>
    public bool UnExclude(string word)
    {
        return _excludeList.Remove(word.Trim().ToLowerInvariant());
    }

    /// <summary>清空排除列表喵~</summary>
    public void ClearExclude()
    {
        _excludeList.Clear();
    }

    // ── 文件系统监控（自动重载） ─────────────────────────────

    /// <summary>设置或重启 FileSystemWatcher，监控词库文件变化喵~</summary>
    private void SetupWatcher()
    {
        _watcher?.Dispose();
        _debounceTimer?.Dispose();

        if (_sourcePath is null) return;

        var dir = Path.GetDirectoryName(_sourcePath);
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;

        _watcher = new FileSystemWatcher(dir)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            Filter = Path.GetFileName(_sourcePath),
            EnableRaisingEvents = true,
        };

        _debounceTimer = new System.Timers.Timer(DebounceDelayMs)
        {
            AutoReset = false,
        };
        _debounceTimer.Elapsed += OnDebounceElapsed;

        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
    }

    /// <summary>文件变更时触发，重启防抖计时器喵~</summary>
    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Start();
    }

    /// <summary>防抖到期后执行重载并通知 UI 喵~</summary>
    private void OnDebounceElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            Reload();
            WordListChanged?.Invoke();
        }
        catch
        {
            // 静默处理重载异常
        }
    }

    /// <summary>释放文件监控资源喵~</summary>
    public void Dispose()
    {
        _watcher?.Dispose();
        _debounceTimer?.Dispose();
    }

    /// <summary>检查单词是否在排除列表中喵~</summary>
    public bool IsExcluded(string word)
    {
        return _excludeList.Contains(word.Trim().ToLowerInvariant());
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WordSplitRegex();

    // ===== 批量导入（支持 TXT/CSV/Excel） =====

    /// <summary>从文件导入附加单词，合并到已有词库喵~</summary>
    public int AddFromFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("File not found.", path);

        var ext = Path.GetExtension(path).ToLowerInvariant();
        var newWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 按后缀名选择解析方式喵~
        switch (ext)
        {
            case ".txt":
                LoadFromTxt(path, newWords);
                break;
            case ".csv":
                LoadFromCsv(path, newWords);
                break;
            case ".xlsx":
                LoadFromXlsx(path, newWords);
                break;
            default:
                throw new NotSupportedException($"Unsupported format: {ext}");
        }

        if (newWords.Count == 0) return 0;

        // 合并到已有词库，重新冻结喵~
        var merged = new HashSet<string>(_words, StringComparer.OrdinalIgnoreCase);
        foreach (var w in newWords) merged.Add(w);
        _words = merged.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        return newWords.Count;
    }

    // 读取 TXT：和 LoadFromFile 一样的格式喵~
    private static void LoadFromTxt(string path, HashSet<string> words)
    {
        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;

            if (trimmed.Contains(':') && !trimmed.StartsWith('.'))
            {
                var word = trimmed.Split(':', 2)[0].Trim();
                AddPartsDirect(word, words);
            }
            else
            {
                AddPartsDirect(trimmed, words);
            }
        }
    }

    // 读取 CSV：取第一列（逗号或分号分隔）喵~
    private static void LoadFromCsv(string path, HashSet<string> words)
    {
        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#') || trimmed.StartsWith(','))
                continue;

            var first = trimmed.Split(',', ';')[0].Trim().Trim('"', '\'');
            if (first.Length > 0)
                words.Add(first.ToLowerInvariant());
        }
    }

    // 读取 Excel：取第一个工作表的 A 列喵~
    private static void LoadFromXlsx(string path, HashSet<string> words)
    {
        using var workbook = new XLWorkbook(path);
        var sheet = workbook.Worksheet(1);
        var rows = sheet.RangeUsed();

        if (rows is null) return;

        foreach (var row in rows.Rows())
        {
            var cell = row.Cell(1).GetString().Trim();
            if (cell.Length > 0)
                words.Add(cell.ToLowerInvariant());
        }
    }

    private static void AddPartsDirect(string text, HashSet<string> words)
    {
        foreach (var part in WordSplitRegex().Split(text))
        {
            var p = part.Trim();
            if (p.Length > 0 && !p.StartsWith('#'))
                words.Add(p.ToLowerInvariant());
        }
    }

    /// <summary>获取词长分布：词长 -> 单词数量</summary>
    public Dictionary<int, int> GetWordLengthDistribution()
    {
        var dist = new Dictionary<int, int>();
        foreach (var word in _words)
        {
            var len = word.Length;
            dist[len] = dist.GetValueOrDefault(len) + 1;
        }
        return dist;
    }

    /// <summary>获取首字母分布：首字母 -> 单词数量（不区分大小写，统一小写）</summary>
    public Dictionary<char, int> GetFirstLetterDistribution()
    {
        var dist = new Dictionary<char, int>();
        foreach (var word in _words)
        {
            if (word.Length == 0) continue;
            var ch = char.ToLowerInvariant(word[0]);
            // 只统计字母 a-z
            if (ch is >= 'a' and <= 'z')
                dist[ch] = dist.GetValueOrDefault(ch) + 1;
        }
        return dist;
    }

    /// <summary>与另一个词库对比差异，返回新增/移除/共有关信息</summary>
    public WordListDiff DiffWith(WordList other, string? leftLabel = null, string? rightLabel = null)
    {
        var leftOnly = _words.Except(other._words).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        var rightOnly = other._words.Except(_words).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        var common = _words.Intersect(other._words).ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        return new WordListDiff(
            leftLabel ?? Path.GetFileName(_sourcePath) ?? "左词库",
            rightLabel ?? Path.GetFileName(other._sourcePath) ?? "右词库",
            leftOnly.Count,
            rightOnly.Count,
            common.Count,
            leftOnly,
            rightOnly
        );
    }
}
