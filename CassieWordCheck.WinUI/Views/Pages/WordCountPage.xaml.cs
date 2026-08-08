using CassieWordCheck.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CassieWordCheck.Views.Pages;

public sealed partial class WordCountPage : AppPage
{
    private bool _loaded;

    public WordCountPage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            _loaded = true;
            UpdateTexts();
            InputBox.Text = State.CurrentText;
            Refresh();
        };
    }

    private void UpdateTexts()
    {
        TitleTextBlock.Text = Text("wordcount.title");
        SubtitleTextBlock.Text = Text("wordcount.subtitle");
        InputBox.PlaceholderText = Text("input.placeholder");
        TotalCharsLabel.Text = Text("wordcount.total_chars");
        CharsNoSpacesLabel.Text = Text("wordcount.chars_no_spaces");
        ChineseCharsLabel.Text = Text("wordcount.chinese_chars");
        EnglishLettersLabel.Text = Text("wordcount.english_letters");
        TotalWordsLabel.Text = Text("wordcount.total_words");
        UniqueWordsLabel.Text = Text("wordcount.unique_words");
        AvgLengthLabel.Text = Text("wordcount.avg_length");
        TotalLinesLabel.Text = Text("wordcount.total_lines");
        TopWordsTitle.Text = Text("wordcount.freq");
        LengthTitle.Text = Text("wordcount.length");
    }

    private void OnTextChanged(object sender, TextChangedEventArgs args)
    {
        if (!_loaded)
            return;

        State.CheckText(InputBox.Text);
        Refresh();
    }

    private void Refresh()
    {
        var result = WordCountService.Count(InputBox.Text);
        TotalCharsValue.Text = result.TotalChars.ToString("N0");
        CharsNoSpacesValue.Text = result.CharsNoSpaces.ToString("N0");
        ChineseCharsValue.Text = result.ChineseChars.ToString("N0");
        EnglishLettersValue.Text = result.EnglishLetters.ToString("N0");
        TotalWordsValue.Text = result.TotalWords.ToString("N0");
        UniqueWordsValue.Text = result.UniqueWords.ToString("N0");
        AvgLengthValue.Text = result.AvgWordLength.ToString("F1");
        TotalLinesValue.Text = result.TotalLines.ToString("N0");

        TopWordsPanel.Children.Clear();
        foreach (var item in result.TopFrequentWords)
            TopWordsPanel.Children.Add(CreateBarRow(item.Word, item.Count, "AccentBrush"));

        LengthPanel.Children.Clear();
        foreach (var item in result.WordLengthDistribution)
            LengthPanel.Children.Add(CreateBarRow(item.Label, item.Count, "AvailableBrush"));
    }

    private static Grid CreateBarRow(string label, int value, string brushKey)
    {
        var row = new Grid { ColumnSpacing = 10 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
        row.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
        var bar = new ProgressBar
        {
            Minimum = 0,
            Maximum = Math.Max(value, 1),
            Value = value,
            Height = 8,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources[brushKey],
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(bar, 1);
        row.Children.Add(bar);
        var count = new TextBlock { Text = value.ToString(), HorizontalAlignment = HorizontalAlignment.Right };
        Grid.SetColumn(count, 2);
        row.Children.Add(count);
        return row;
    }
}
