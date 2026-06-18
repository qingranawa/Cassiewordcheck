using System.Windows;
using System.Windows.Media;
using CassieWordCheck.Models;
using CassieWordCheck.Services;

namespace CassieWordCheck.Views;

public partial class WordCountWindow : Window
{
    private static readonly Brush BarBrush = new SolidColorBrush(Color.FromRgb(0x8B, 0x7C, 0xF0));
    private static readonly Brush LengthBarBrush = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
    private static readonly Brush ChartLabelBrush = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));
    private static readonly Brush ChartValueBrush = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));

    private const double FreqBarHeight = 22;
    private const double LengthBarHeight = 26;
    private const double RowSpacing = 4;
    private const double LabelWidth = 100;
    private const double ValueWidth = 50;
    private const double BarPadding = 8;

    private readonly string _text;
    private readonly LocalizationService _localization;
    private bool _loaded;

    public WordCountWindow(string text, LocalizationService localization)
    {
        InitializeComponent();
        _text = text;
        _localization = localization;
        this.EnableDarkTitleBar();
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (_loaded) return;
        _loaded = true;

        UpdateLabels();
        PopulateStats();

        // 延迟一帧绘制图表，确保 Canvas 已布局
        Dispatcher.BeginInvoke(() =>
        {
            DrawFreqChart();
            DrawLengthChart();
        });
    }

    private void UpdateLabels()
    {
        TitleLabel.Text = "📊 " + _localization["stats.title"];
        Title = _localization["stats.title"];
        FreqLabel.Text = _localization["wordcount.freq"];
        LengthLabel.Text = _localization["wordcount.length"];

        TotalCharsLabel.Text = _localization["wordcount.total_chars"];
        CharsNoSpacesLabel.Text = _localization["wordcount.chars_no_spaces"];
        ChineseCharsLabel.Text = _localization["wordcount.chinese_chars"];
        EnglishLettersLabel.Text = _localization["wordcount.english_letters"];
        DigitCharsLabel.Text = _localization["wordcount.digit_chars"];
        TotalWordsLabel.Text = _localization["wordcount.total_words"];
        UniqueWordsLabel.Text = _localization["wordcount.unique_words"];
        AvgLengthLabel.Text = _localization["wordcount.avg_length"];
        TotalLinesLabel.Text = _localization["wordcount.total_lines"];
        NonEmptyLinesLabel.Text = _localization["wordcount.nonempty_lines"];
    }

    private void PopulateStats()
    {
        var result = WordCountService.Count(_text);

        TotalCharsVal.Text = result.TotalChars.ToString("N0");
        CharsNoSpacesVal.Text = result.CharsNoSpaces.ToString("N0");
        ChineseCharsVal.Text = result.ChineseChars.ToString("N0");
        EnglishLettersVal.Text = result.EnglishLetters.ToString("N0");
        DigitCharsVal.Text = result.DigitChars.ToString("N0");
        TotalWordsVal.Text = result.TotalWords.ToString("N0");
        UniqueWordsVal.Text = result.UniqueWords.ToString("N0");
        AvgLengthVal.Text = result.AvgWordLength.ToString("F1");
        TotalLinesVal.Text = result.TotalLines.ToString("N0");
        NonEmptyLinesVal.Text = result.NonEmptyLines.ToString("N0");
    }

    // ── 词频分布横向条形图 ─────────────────────────
    private void DrawFreqChart()
    {
        var result = WordCountService.Count(_text);
        FreqCanvas.Children.Clear();

        if (result.TopFrequentWords.Count == 0)
        {
            FreqEmptyLabel.Visibility = Visibility.Visible;
            return;
        }
        FreqEmptyLabel.Visibility = Visibility.Collapsed;

        var cw = FreqCanvas.ActualWidth;
        if (cw < 50) cw = 400;

        var maxCount = result.TopFrequentWords.Max(f => f.Count);
        var barArea = cw - LabelWidth - ValueWidth - BarPadding * 2;
        if (barArea < 50) barArea = 50;

        double y = 0;
        foreach (var item in result.TopFrequentWords)
        {
            var barW = Math.Max(4, barArea * item.Count / maxCount);

            // 单词标签（右对齐）
            var label = MakeLabel(item.Word, 0, y, LabelWidth - 4, FreqBarHeight, HorizontalAlignment.Right);
            FreqCanvas.Children.Add(label);

            // 条形
            var bar = new System.Windows.Shapes.Rectangle
            {
                Width = barW,
                Height = FreqBarHeight - 4,
                Fill = BarBrush,
                RadiusX = 3,
                RadiusY = 3,
            };
            Canvas.SetLeft(bar, LabelWidth + BarPadding);
            Canvas.SetTop(bar, y + 2);
            FreqCanvas.Children.Add(bar);

            // 数值（右端）
            var val = MakeLabel(item.Count.ToString(), LabelWidth + BarPadding + barW + 4, y,
                ValueWidth, FreqBarHeight, HorizontalAlignment.Left, ChartValueBrush);
            FreqCanvas.Children.Add(val);

            y += FreqBarHeight + RowSpacing;
        }

        FreqCanvas.Height = y;
    }

    // ── 词长分布横向条形图 ─────────────────────────
    private void DrawLengthChart()
    {
        var result = WordCountService.Count(_text);
        LengthCanvas.Children.Clear();

        if (result.WordLengthDistribution.Count == 0) return;

        var cw = LengthCanvas.ActualWidth;
        if (cw < 50) cw = 400;

        var maxCount = result.WordLengthDistribution.Max(d => d.Count);
        var barArea = cw - LabelWidth - ValueWidth - BarPadding * 2;
        if (barArea < 50) barArea = 50;

        double y = 0;
        foreach (var item in result.WordLengthDistribution)
        {
            var barW = Math.Max(4, barArea * item.Count / Math.Max(maxCount, 1));

            // 桶标签
            var label = MakeLabel(item.Label, 0, y, LabelWidth - 4, LengthBarHeight, HorizontalAlignment.Right);
            LengthCanvas.Children.Add(label);

            // 条形
            var bar = new System.Windows.Shapes.Rectangle
            {
                Width = barW,
                Height = LengthBarHeight - 4,
                Fill = LengthBarBrush,
                RadiusX = 3,
                RadiusY = 3,
            };
            Canvas.SetLeft(bar, LabelWidth + BarPadding);
            Canvas.SetTop(bar, y + 2);
            LengthCanvas.Children.Add(bar);

            // 数值
            var val = MakeLabel(item.Count.ToString(), LabelWidth + BarPadding + barW + 4, y,
                ValueWidth, LengthBarHeight, HorizontalAlignment.Left, ChartValueBrush);
            LengthCanvas.Children.Add(val);

            y += LengthBarHeight + RowSpacing;
        }

        LengthCanvas.Height = y;
    }

    // ── 辅助：生成条形图的标签 TextBlock ─────────────
    private static TextBlock MakeLabel(string text, double left, double top,
        double width, double height, HorizontalAlignment align,
        Brush? foreground = null)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = 12,
            FontFamily = new FontFamily("Consolas"),
            Foreground = foreground ?? ChartLabelBrush,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = align == HorizontalAlignment.Right ? TextAlignment.Right : TextAlignment.Left,
        };
        // 用边距控制宽度，使 TextBlock 撑满指定区域
        Canvas.SetLeft(tb, left);
        Canvas.SetTop(tb, top);
        tb.Width = width;
        tb.Height = height;
        return tb;
    }
}
