using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CassieWordCheck.Models;
using CassieWordCheck.Services;

namespace CassieWordCheck.Views;

public partial class WordListBrowserWindow : Window
{
    private readonly WordList _wordlist;
    private readonly LocalizationService _localization;
    private List<string> _allWords = [];
    private string _currentSort = "alpha-asc";
    private string _searchText = "";

    // 统计面板颜色
    private static readonly Brush BarBrush = new SolidColorBrush(Color.FromRgb(0x6C, 0x5C, 0xE7));
    private static readonly Brush BarBgBrush = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x24));
    private static readonly Brush LabelBrush = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));
    private static readonly Brush ValueBrush = new SolidColorBrush(Color.FromRgb(0x8B, 0x7C, 0xF0));

    public WordListBrowserWindow(WordList wordlist, LocalizationService localization)
    {
        InitializeComponent();
        _wordlist = wordlist;
        _localization = localization;
        this.EnableDarkTitleBar();
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        // 本地化标题和标签
        Title = _localization["wordlist_browser.title"];
        LengthDistTitle.Text = _localization["wordlist_browser.length_dist"];
        FirstLetterDistTitle.Text = _localization["wordlist_browser.first_letter_dist"];
        _allWords = [.. _wordlist.Words];
        ApplyFilterAndSort();
        RenderStatistics();

        // 窗口入场动画
        var sb = new System.Windows.Media.Animation.Storyboard();
        var scaleX = new System.Windows.Media.Animation.DoubleAnimation(0.95, 1, new Duration(TimeSpan.FromMilliseconds(350)));
        scaleX.EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut };
        System.Windows.Media.Animation.Storyboard.SetTarget(scaleX, this);
        System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleX, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));

        var scaleY = new System.Windows.Media.Animation.DoubleAnimation(0.95, 1, new Duration(TimeSpan.FromMilliseconds(350)));
        scaleY.EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut };
        System.Windows.Media.Animation.Storyboard.SetTarget(scaleY, this);
        System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleY, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));

        var fade = new System.Windows.Media.Animation.DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(300)));
        System.Windows.Media.Animation.Storyboard.SetTarget(fade, this);
        System.Windows.Media.Animation.Storyboard.SetTargetProperty(fade, new PropertyPath(OpacityProperty));

        sb.Children.Add(scaleX);
        sb.Children.Add(scaleY);
        sb.Children.Add(fade);
        sb.Begin(this);
    }

    // ── 搜索 ──────────────────────────────────────────────
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = SearchBox.Text.Trim();
        ApplyFilterAndSort();
    }

    // ── 排序 ──────────────────────────────────────────────
    private void OnSortChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SortCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            _currentSort = tag;
            ApplyFilterAndSort();
        }
    }

    // ── 刷新 ──────────────────────────────────────────────
    private void OnRefresh(object sender, RoutedEventArgs e)
    {
        _allWords = [.. _wordlist.Words];
        ApplyFilterAndSort();
        RenderStatistics();
    }

    // ── 核心：过滤 + 排序 ─────────────────────────────────
    private void ApplyFilterAndSort()
    {
        // 过滤
        var filtered = string.IsNullOrEmpty(_searchText)
            ? _allWords
            : _allWords.Where(w => w.Contains(_searchText, StringComparison.OrdinalIgnoreCase)).ToList();

        // 排序
        var sorted = _currentSort switch
        {
            "alpha-desc" => filtered.OrderByDescending(w => w).ToList(),
            "length-asc" => filtered.OrderBy(w => (w.Length, w)).ToList(),
            "length-desc" => filtered.OrderByDescending(w => (w.Length, w)).ToList(),
            _ => filtered.OrderBy(w => w).ToList(), // alpha-asc 默认
        };

        // 绑定到 ListView
        WordListView.ItemsSource = sorted.Select(w => new { Text = w, Length = w.Length, LengthLabel = $"{w.Length}字" }).ToList();

        // 空数据提示
        EmptyListLabel.Visibility = sorted.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        // 状态栏
        TotalWordsLabel.Text = string.Format(_localization["wordlist_browser.total"], _allWords.Count);
        FilteredWordsLabel.Text = string.Format(_localization["wordlist_browser.filtered"], sorted.Count);
    }

    // ── 统计面板渲染 ─────────────────────────────────────
    private void RenderStatistics()
    {
        RenderLengthDistribution();
        RenderFirstLetterDistribution();
    }

    private void RenderLengthDistribution()
    {
        LengthDistPanel.Children.Clear();
        var dist = _wordlist.GetWordLengthDistribution();
        if (dist.Count == 0)
        {
            LengthDistPanel.Children.Add(new TextBlock
            {
                Text = _localization["stats.no_data"],
                Foreground = LabelBrush,
                FontSize = 11,
            });
            return;
        }

        var maxCount = dist.Values.Max();
        var maxBarWidth = 220.0;

        foreach (var kv in dist.OrderBy(k => k.Key))
        {
            var barWidth = Math.Max(4, maxBarWidth * kv.Value / maxCount);
            var row = new Grid { Margin = new Thickness(0, 0, 0, 3) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // 标签：词长
            row.Children.Add(new TextBlock
            {
                Text = kv.Key.ToString(),
                Foreground = LabelBrush,
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI, Consolas"),
                VerticalAlignment = VerticalAlignment.Center,
            });

            // 柱状图背景 + 前景
            var barBorder = new Border
            {
                CornerRadius = new CornerRadius(4),
                Background = BarBgBrush,
                Height = 14,
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            var barFill = new Border
            {
                CornerRadius = new CornerRadius(4),
                Background = BarBrush,
                Width = barWidth,
                Height = 14,
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            barBorder.Child = barFill;

            Grid.SetColumn(barBorder, 1);
            row.Children.Add(barBorder);

            // 数值标签
            var valueLabel = new TextBlock
            {
                Text = kv.Value.ToString(),
                Foreground = ValueBrush,
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI, Consolas"),
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(valueLabel, 2);
            row.Children.Add(valueLabel);

            LengthDistPanel.Children.Add(row);
        }
    }

    private void RenderFirstLetterDistribution()
    {
        FirstLetterDistPanel.Children.Clear();
        var dist = _wordlist.GetFirstLetterDistribution();
        if (dist.Count == 0)
        {
            FirstLetterDistPanel.Children.Add(new TextBlock
            {
                Text = _localization["stats.no_data"],
                Foreground = LabelBrush,
                FontSize = 11,
            });
            return;
        }

        var maxCount = dist.Values.Max();
        var maxBarWidth = 220.0;

        foreach (var kv in dist.OrderBy(k => k.Key))
        {
            var barWidth = Math.Max(4, maxBarWidth * kv.Value / maxCount);
            var row = new Grid { Margin = new Thickness(0, 0, 0, 2) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // 标签：首字母
            row.Children.Add(new TextBlock
            {
                Text = kv.Key.ToString().ToUpperInvariant(),
                Foreground = LabelBrush,
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI, Consolas"),
                VerticalAlignment = VerticalAlignment.Center,
            });

            // 柱状图
            var barBorder = new Border
            {
                CornerRadius = new CornerRadius(4),
                Background = BarBgBrush,
                Height = 12,
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            var barFill = new Border
            {
                CornerRadius = new CornerRadius(4),
                Background = BarBrush,
                Width = barWidth,
                Height = 12,
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            barBorder.Child = barFill;

            Grid.SetColumn(barBorder, 1);
            row.Children.Add(barBorder);

            // 数值
            var valueLabel = new TextBlock
            {
                Text = kv.Value.ToString(),
                Foreground = ValueBrush,
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI, Consolas"),
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(valueLabel, 2);
            row.Children.Add(valueLabel);

            FirstLetterDistPanel.Children.Add(row);
        }
    }
}
