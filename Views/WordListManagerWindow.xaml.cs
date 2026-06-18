using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CassieWordCheck.Models;
using CassieWordCheck.Services;
using Microsoft.Win32;

namespace CassieWordCheck.Views;

public partial class WordListManagerWindow : Window
{
    private readonly WordList _wordlist;
    private readonly LocalizationService _localization;
    private readonly Settings _settings;
    private List<string> _allWords = [];
    private string _currentSort = "alpha-asc";
    private string _searchText = "";
    private WordList? _diffTarget;
    private string _diffTargetPath = "";

    private static readonly Brush BarBrush = new SolidColorBrush(Color.FromRgb(0x6C, 0x5C, 0xE7));
    private static readonly Brush BarBgBrush = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x24));
    private static readonly Brush LabelBrush = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));
    private static readonly Brush ValueBrush = new SolidColorBrush(Color.FromRgb(0x8B, 0x7C, 0xF0));

    public WordListManagerWindow(WordList wordlist, LocalizationService localization, Settings settings)
    {
        InitializeComponent();
        _wordlist = wordlist;
        _localization = localization;
        _settings = settings;
        this.EnableDarkTitleBar();
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        Title = "词库管理";
        SourceInfoLabel.Text = $"当前词库：{_wordlist.SourcePath ?? "未加载"} ({_wordlist.WordCount} 词)";
        LengthDistTitle.Text = "词长分布";
        FirstLetterDistTitle.Text = "首字母分布";
        _allWords = [.. _wordlist.Words];
        ApplyFilterAndSort();
        RenderStatistics();
        UpdateTabStyles(0);
    }

    // ── 标签页切换 ──
    private void OnSwitchToBrowser(object sender, RoutedEventArgs e)
    {
        BrowserPage.Visibility = Visibility.Visible;
        DiffPage.Visibility = Visibility.Collapsed;
        UpdateTabStyles(0);
    }

    private void OnSwitchToDiff(object sender, RoutedEventArgs e)
    {
        BrowserPage.Visibility = Visibility.Collapsed;
        DiffPage.Visibility = Visibility.Visible;
        UpdateTabStyles(1);
    }

    private void UpdateTabStyles(int active)
    {
        BrowserTabBtn.Tag = active == 0 ? "Active" : null;
        DiffTabBtn.Tag = active == 1 ? "Active" : null;
    }

    // ── 更换词库 ──
    private void OnChangeWordList(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择词库文件",
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var count = _wordlist.LoadFromFile(dialog.FileName);
                _settings.Save();
                _allWords = [.. _wordlist.Words];
                ApplyFilterAndSort();
                RenderStatistics();
                SourceInfoLabel.Text = $"当前词库：{dialog.FileName} ({count} 词)";
            }
            catch { }
        }
    }

    // ── 浏览器：搜索 ──
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = SearchBox.Text.Trim();
        ApplyFilterAndSort();
    }

    // ── 浏览器：排序 ──
    private void OnSortChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SortCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            _currentSort = tag;
            ApplyFilterAndSort();
        }
    }

    private void ApplyFilterAndSort()
    {
        var filtered = string.IsNullOrEmpty(_searchText)
            ? _allWords
            : _allWords.Where(w => w.Contains(_searchText, StringComparison.OrdinalIgnoreCase)).ToList();

        var sorted = _currentSort switch
        {
            "alpha-desc" => filtered.OrderByDescending(w => w).ToList(),
            "length-asc" => filtered.OrderBy(w => (w.Length, w)).ToList(),
            "length-desc" => filtered.OrderByDescending(w => (w.Length, w)).ToList(),
            _ => filtered.OrderBy(w => w).ToList(),
        };

        WordListView.ItemsSource = sorted.Select(w => new { Text = w, Length = w.Length, LengthLabel = $"{w.Length}字" }).ToList();
        EmptyListLabel.Visibility = sorted.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        TotalWordsLabel.Text = $"总词数：{_allWords.Count}";
        FilteredWordsLabel.Text = $"匹配：{sorted.Count}";
    }

    // ── 浏览器：统计 ──
    private void RenderStatistics()
    {
        LengthDistPanel.Children.Clear();
        FirstLetterDistPanel.Children.Clear();

        var lenDist = _wordlist.GetWordLengthDistribution();
        if (lenDist.Count > 0)
        {
            var maxCount = lenDist.Values.Max();
            var maxBarWidth = 200.0;
            foreach (var kv in lenDist.OrderBy(k => k.Key))
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 3) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                row.Children.Add(new TextBlock { Text = kv.Key.ToString(), Foreground = LabelBrush, FontSize = 11, FontFamily = new FontFamily("Consolas"), VerticalAlignment = VerticalAlignment.Center });

                var barBorder = new Border { CornerRadius = new CornerRadius(4), Background = BarBgBrush, Height = 14, Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
                barBorder.Child = new Border { CornerRadius = new CornerRadius(4), Background = BarBrush, Width = Math.Max(4, maxBarWidth * kv.Value / maxCount), Height = 14, HorizontalAlignment = HorizontalAlignment.Left };
                Grid.SetColumn(barBorder, 1);
                row.Children.Add(barBorder);

                row.Children.Add(new TextBlock { Text = kv.Value.ToString(), Foreground = ValueBrush, FontSize = 11, FontFamily = new FontFamily("Consolas"), Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
                Grid.SetColumn(row.Children[^1], 2);
                LengthDistPanel.Children.Add(row);
            }
        }

        var firstDist = _wordlist.GetFirstLetterDistribution();
        if (firstDist.Count > 0)
        {
            var maxCount = firstDist.Values.Max();
            var maxBarWidth = 200.0;
            foreach (var kv in firstDist.OrderBy(k => k.Key))
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 2) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                row.Children.Add(new TextBlock { Text = kv.Key.ToString().ToUpperInvariant(), Foreground = LabelBrush, FontSize = 11, FontFamily = new FontFamily("Consolas"), VerticalAlignment = VerticalAlignment.Center });

                var barBorder = new Border { CornerRadius = new CornerRadius(4), Background = BarBgBrush, Height = 12, Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
                barBorder.Child = new Border { CornerRadius = new CornerRadius(4), Background = BarBrush, Width = Math.Max(4, maxBarWidth * kv.Value / maxCount), Height = 12, HorizontalAlignment = HorizontalAlignment.Left };
                Grid.SetColumn(barBorder, 1);
                row.Children.Add(barBorder);

                row.Children.Add(new TextBlock { Text = kv.Value.ToString(), Foreground = ValueBrush, FontSize = 11, FontFamily = new FontFamily("Consolas"), Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
                Grid.SetColumn(row.Children[^1], 2);
                FirstLetterDistPanel.Children.Add(row);
            }
        }
    }

    // ── 对比：选择对比词库 ──
    private void OnSelectDiffTarget(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择对比词库文件",
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                _diffTarget = new WordList();
                _diffTarget.LoadFromFile(dialog.FileName);
                _diffTargetPath = dialog.FileName;
                DiffTargetLabel.Text = System.IO.Path.GetFileName(dialog.FileName);
                RunDiff();
            }
            catch { }
        }
    }

    private void RunDiff()
    {
        if (_diffTarget is null) return;

        var diff = _wordlist.DiffWith(_diffTarget);
        LeftDiffLabel.Text = $"仅当前词库（{diff.LeftOnlyCount}）";
        RightDiffLabel.Text = $"仅对比词库（{diff.RightOnlyCount}）";
        LeftDiffList.ItemsSource = diff.LeftOnly.OrderBy(w => w).ToList();
        RightDiffList.ItemsSource = diff.RightOnly.OrderBy(w => w).ToList();
        DiffSummaryLabel.Text = $"共同：{diff.CommonCount} 词 | 当前独有：{diff.LeftOnlyCount} 词 | 对比独有：{diff.RightOnlyCount} 词";
    }
}
