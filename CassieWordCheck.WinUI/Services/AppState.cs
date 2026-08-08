using CassieWordCheck.Models;

namespace CassieWordCheck.Services;

/// <summary>
/// WinUI 应用共享状态，统一持有 Core 服务和用户可写数据路径喵
/// </summary>
public sealed class AppState : IDisposable
{
    private readonly WinUiStoragePathProvider _paths;

    public Settings Settings { get; }
    public WordList WordList { get; }
    public Checker Checker { get; }
    public LocalizationService Localization { get; }
    public HistoryStore History { get; }
    public SuggestionEngine SuggestionEngine { get; private set; }
    public string? WordListError { get; private set; }
    public string CurrentText { get; private set; } = string.Empty;
    public IReadOnlyList<CheckResult> CurrentResults { get; private set; } = [];
    public IReadOnlyList<ResultSegment> CurrentSegments { get; private set; } = [];
    public CheckSummary LastSummary { get; private set; } = CheckSummary.Empty;

    public event Action? WordListChanged;
    public event Action? SettingsChanged;

    public AppState(WinUiStoragePathProvider paths)
    {
        _paths = paths;
        Settings = new Settings(Path.Combine(paths.LocalDataDirectory, "appsettings.json"));
        History = new HistoryStore(Path.Combine(paths.LocalDataDirectory, "history.json"));
        Localization = new LocalizationService();
        Localization.SetLanguage(Settings.Language);

        WordList = new WordList();
        WordList.WordListChanged += OnWordListChanged;
        WordList.SetWhitelist(Settings.Whitelist);

        Checker = new Checker(WordList)
        {
            IgnoreChinese = Settings.IgnoreChinese,
            FilterFormatting = Settings.FilterFormatting,
            FilterNaming = Settings.FilterNaming,
        };

        SuggestionEngine = new SuggestionEngine(WordList.Words);
        LoadConfiguredWordList();
    }

    public void CheckText(string text)
    {
        CurrentText = text;
        CurrentResults = Checker.CheckText(text);
        CurrentSegments = ResultSegmentBuilder.Build(CurrentResults);

        var statistics = Checker.GetStatistics(CurrentResults.ToList(), text);
        var coverage = text.Length == 0 ? 0 : Convert.ToDouble(statistics["coverage"]);
        LastSummary = new CheckSummary(
            Convert.ToInt32(statistics["total"]),
            Convert.ToInt32(statistics["available"]),
            Convert.ToInt32(statistics["unavailable"]),
            Convert.ToInt32(statistics["ignored"]),
            coverage,
            text.Length);
    }

    public void SaveCurrentHistory()
    {
        if (string.IsNullOrWhiteSpace(CurrentText))
            return;

        History.Add(
            CurrentText,
            string.Concat(CurrentResults.Select(result => result.Text)),
            LastSummary.Available,
            LastSummary.Unavailable,
            LastSummary.Ignored,
            LastSummary.Coverage);
    }

    public bool LoadWordList(string path)
    {
        try
        {
            WordList.LoadFromFile(path);
            Settings.WordlistPath = path;
            WordListError = null;
            RebuildSuggestionEngine();
            return true;
        }
        catch (Exception exception)
        {
            WordListError = exception.Message;
            return false;
        }
    }

    public int ImportWords(string path)
    {
        var count = WordList.AddFromFile(path);
        RebuildSuggestionEngine();
        return count;
    }

    public void ApplySettings()
    {
        Checker.IgnoreChinese = Settings.IgnoreChinese;
        Checker.FilterFormatting = Settings.FilterFormatting;
        Checker.FilterNaming = Settings.FilterNaming;
        WordList.SetWhitelist(Settings.Whitelist);
        Settings.Save();
        CheckText(CurrentText);
        SettingsChanged?.Invoke();
    }

    public void RebuildSuggestionEngine()
    {
        SuggestionEngine = new SuggestionEngine(WordList.Words);
    }

    private void LoadConfiguredWordList()
    {
        var configuredPath = Settings.WordlistPath;
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
        {
            LoadWordList(configuredPath);
            return;
        }

        var bundledPath = Path.Combine(AppContext.BaseDirectory, "data", "cassie-text.txt");
        if (File.Exists(bundledPath))
            LoadWordList(bundledPath);
        else
            WordListError = "未找到内置词库文件";
    }

    private void OnWordListChanged()
    {
        RebuildSuggestionEngine();
        WordListChanged?.Invoke();
    }

    public void Dispose()
    {
        WordList.WordListChanged -= OnWordListChanged;
        WordList.Dispose();
    }
}

public sealed record CheckSummary(
    int Total,
    int Available,
    int Unavailable,
    int Ignored,
    double Coverage,
    int CharacterCount)
{
    public static CheckSummary Empty { get; } = new(0, 0, 0, 0, 0, 0);
}
