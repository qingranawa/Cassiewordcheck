using CassieWordCheck.Models;
using CassieWordCheck.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Text.RegularExpressions;

namespace CassieWordCheck.Views.Pages;

public sealed partial class HomePage : AppPage
{
    private bool _isRendering;
    private bool _isUpdatingTexts;
    private readonly DispatcherQueueTimer _historyTimer;

    public HomePage()
    {
        InitializeComponent();
        _historyTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _historyTimer.Interval = TimeSpan.FromMinutes(3);
        _historyTimer.Tick += OnHistoryTimerTick;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        State.WordListChanged += OnWordListChanged;
        State.SettingsChanged += OnSettingsChanged;
        _historyTimer.Start();
        UpdateTexts();
        ApplyDisplaySettings();
        InputBox.Text = State.CurrentText;
        RenderState();
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        State.WordListChanged -= OnWordListChanged;
        State.SettingsChanged -= OnSettingsChanged;
        _historyTimer.Stop();
    }

    private void OnHistoryTimerTick(DispatcherQueueTimer sender, object args)
    {
        State.SaveCurrentHistory();
    }

    private void OnSettingsChanged()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateTexts();
            ApplyDisplaySettings();
            State.CheckText(InputBox.Text);
            RenderState();
        });
    }

    private void UpdateTexts()
    {
        _isUpdatingTexts = true;
        try
        {
            TitleTextBlock.Text = Text("home.title");
            SubtitleTextBlock.Text = Text("home.subtitle");
            InputLabelTextBlock.Text = Text("input.label");
            InputBox.PlaceholderText = Text("input.placeholder");
            CheckButton.Content = Text("home.check");
            ClearButton.Content = Text("home.clear");
            ImportButton.Content = Text("home.import");
            ResultLabelTextBlock.Text = Text("result.label");
            CopyResultButton.Content = Text("menu.copy_result");
            ExportResultButton.Content = Text("home.export");
            SuggestionTitleTextBlock.Text = Text("suggestion.title");
            ResultModeCombo.Items.Clear();
            ResultModeCombo.Items.Add(new ComboBoxItem { Content = Text("mode.inline"), Tag = "inline" });
            ResultModeCombo.Items.Add(new ComboBoxItem { Content = Text("mode.list"), Tag = "list" });
            ResultModeCombo.Items.Add(new ComboBoxItem { Content = Text("mode.compare"), Tag = "compare" });
            ResultModeCombo.SelectedIndex = State.Settings.ResultMode switch
            {
                "list" => 1,
                "compare" => 2,
                _ => 0,
            };
            WordListStatusTextBlock.Text = State.WordListError is null
                ? string.Format(Text("status.words"), State.WordList.WordCount)
                : string.Format(Text("status.load_failed"), State.WordListError);
        }
        finally
        {
            _isUpdatingTexts = false;
        }
    }

    private void ApplyDisplaySettings()
    {
        InputBox.TextWrapping = State.Settings.WordWrap ? TextWrapping.Wrap : TextWrapping.NoWrap;
        InputBox.FontSize = State.Settings.FontSize;
        ResultList.FontSize = State.Settings.FontSize;
    }

    private void OnModeChanged(object sender, SelectionChangedEventArgs args)
    {
        if (_isUpdatingTexts || ResultModeCombo.SelectedItem is not ComboBoxItem { Tag: string mode })
            return;

        State.Settings.ResultMode = mode;
        State.Settings.Save();
        RenderState();
    }

    private void OnInputTextChanged(object sender, TextChangedEventArgs args)
    {
        if (_isRendering)
            return;

        State.CheckText(InputBox.Text);
        RenderState();
    }

    private void OnCheck(object sender, RoutedEventArgs args)
    {
        State.CheckText(InputBox.Text);
        State.SaveCurrentHistory();
        RenderState();
    }

    private void OnClear(object sender, RoutedEventArgs args)
    {
        InputBox.Text = string.Empty;
        State.CheckText(string.Empty);
        RenderState();
        InputBox.Focus(FocusState.Programmatic);
    }

    private async void OnImport(object sender, RoutedEventArgs args)
    {
        var picker = new FilePickerService(App.MainWindow!);
        var paths = await picker.PickFilesAsync(
            [
                new FilePickerChoice(Text("import.txt"), [".txt"]),
                new FilePickerChoice(Text("import.csv"), [".csv"]),
                new FilePickerChoice(Text("import.excel"), [".xlsx"]),
            ],
            allowMultiple: true);

        var textFiles = paths.Where(path => path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)).ToList();
        var importFiles = paths.Except(textFiles, StringComparer.OrdinalIgnoreCase).ToList();
        var importedCount = 0;

        foreach (var path in importFiles)
        {
            try
            {
                importedCount += await Task.Run(() => State.ImportWords(path));
            }
            catch (Exception exception)
            {
                await ShowMessageAsync(Text("import.title"), exception.Message);
            }
        }

        if (textFiles.Count > 0)
        {
            var content = textFiles.Count == 1
                ? await File.ReadAllTextAsync(textFiles[0])
                : string.Join(Environment.NewLine + Environment.NewLine,
                    await Task.WhenAll(textFiles.Select(async path =>
                        $"===== {Path.GetFileName(path)} ====={Environment.NewLine}{await File.ReadAllTextAsync(path)}")));
            InputBox.Text = content;
        }

        if (importedCount > 0)
            await ShowMessageAsync(Text("import.title"), string.Format(Text("import.done"), importedCount));

        UpdateTexts();
        RenderState();
    }

    private async void OnCopyResult(object sender, RoutedEventArgs args)
    {
        await new ClipboardService().SetTextAsync(string.Concat(State.CurrentResults.Select(result => result.Text)));
        await ShowMessageAsync(Text("menu.copy_result"), Text("common.done"));
    }

    private async void OnExportResult(object sender, RoutedEventArgs args)
    {
        var picker = new FilePickerService(App.MainWindow!);
        var path = await picker.PickSaveFileAsync("cassie-result", ".txt");
        if (path is null)
            return;

        await File.WriteAllTextAsync(path, string.Concat(State.CurrentResults.Select(result => result.Text)));
        await ShowMessageAsync(Text("home.export"), Text("common.done"));
    }

    private async void OnUnavailableWordClick(object sender, RoutedEventArgs args)
    {
        if (sender is not Button button || button.Tag is not string word)
            return;

        var suggestions = State.SuggestionEngine.GetSuggestions(word)
            .Select(suggestion => suggestion.Word)
            .Where(candidate => !candidate.Contains(" + ", StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();

        var replacementBox = new TextBox
        {
            PlaceholderText = Text("replace.find"),
            Text = suggestions.FirstOrDefault() ?? string.Empty,
            MinWidth = 280,
        };
        var content = new StackPanel { Spacing = 10 };
        content.Children.Add(new TextBlock { Text = string.Format(Text("replace.label"), word), TextWrapping = TextWrapping.Wrap });
        if (suggestions.Count > 0)
            content.Children.Add(new TextBlock { Text = string.Join("、", suggestions), TextWrapping = TextWrapping.Wrap });
        content.Children.Add(replacementBox);

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = Text("suggestion.title"),
            Content = content,
            PrimaryButtonText = Text("replace.replace"),
            CloseButtonText = Text("common.cancel"),
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            return;

        var replacement = replacementBox.Text.Trim();
        if (replacement.Length == 0)
            return;

        InputBox.Text = Regex.Replace(
            InputBox.Text,
            $"(?<![A-Za-z0-9_.-]){Regex.Escape(word)}(?![A-Za-z0-9_.-])",
            replacement,
            RegexOptions.IgnoreCase);
        State.CheckText(InputBox.Text);
        RenderState();
    }

    private void OnWordListChanged()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateTexts();
            State.CheckText(InputBox.Text);
            RenderState();
        });
    }

    private void RenderState()
    {
        _isRendering = true;
        try
        {
            var summary = State.LastSummary;
            CoverageProgressBar.Value = summary.Coverage;
            CoverageTextBlock.Text = string.Format(Text("stats.coverage"), summary.Coverage);
            AvailableTextBlock.Text = string.Format(Text("stats.available"), summary.Available);
            UnavailableTextBlock.Text = string.Format(Text("stats.unavailable"), summary.Unavailable);
            IgnoredTextBlock.Text = string.Format(Text("stats.ignored"), summary.Ignored);

            ResultList.Items.Clear();
            if (State.Settings.ResultMode == "compare")
                RenderComparison();
            else if (State.Settings.ResultMode == "inline")
                RenderInlineResult();
            else
                RenderListResult();

            SuggestionsPanel.Children.Clear();
            var unavailableWords = State.CurrentResults
                .Where(result => result.Status == CheckStatus.Unavailable)
                .Select(result => result.Text)
                .Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var word in unavailableWords)
            {
                var suggestions = State.SuggestionEngine.GetSuggestions(word)
                    .Select(suggestion => suggestion.Word)
                    .Take(5)
                    .ToList();
                SuggestionsPanel.Children.Add(new TextBlock
                {
                    Text = suggestions.Count == 0
                        ? $"{word} — {Text("suggestion.no_suggestions")}"
                        : $"{word} → {string.Join(", ", suggestions)}",
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                });
            }
        }
        finally
        {
            _isRendering = false;
        }
    }

    private void RenderListResult()
    {
        foreach (var segment in State.CurrentSegments)
            ResultList.Items.Add(CreateSegmentElement(segment));
    }

    private void RenderInlineResult()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 1 };
        foreach (var segment in State.CurrentSegments)
            panel.Children.Add(CreateSegmentElement(segment));
        ResultList.Items.Add(panel);
    }

    private UIElement CreateSegmentElement(ResultSegment segment)
    {
        if (segment.IsInteractive && segment.Word is not null)
        {
            var button = new Button
            {
                Content = segment.Text,
                Tag = segment.Word,
                Padding = new Thickness(2, 0, 2, 0),
                Margin = new Thickness(0, 0, 3, 3),
                Foreground = (Brush)Application.Current.Resources["UnavailableBrush"],
                Background = null,
            };
            button.Click += OnUnavailableWordClick;
            return button;
        }

        return new TextBlock
        {
            Text = segment.Text,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 3, 3),
            Foreground = GetStatusBrush(segment.Status),
        };
    }

    private void RenderComparison()
    {
        var grid = new Grid { ColumnSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var available = new StackPanel { Spacing = 4 };
        available.Children.Add(new TextBlock { Text = Text("result.available"), FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        foreach (var word in State.CurrentResults.Where(result => result.Status == CheckStatus.Available).Select(result => result.Text).Distinct(StringComparer.OrdinalIgnoreCase))
            available.Children.Add(new TextBlock { Text = word, Foreground = GetStatusBrush(CheckStatus.Available) });

        var unavailable = new StackPanel { Spacing = 4 };
        unavailable.Children.Add(new TextBlock { Text = Text("result.unavailable"), FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        foreach (var word in State.CurrentResults.Where(result => result.Status == CheckStatus.Unavailable).Select(result => result.Text).Distinct(StringComparer.OrdinalIgnoreCase))
            unavailable.Children.Add((UIElement)CreateSegmentElement(new ResultSegment(word, CheckStatus.Unavailable, true, word)));

        grid.Children.Add(available);
        Grid.SetColumn(unavailable, 1);
        grid.Children.Add(unavailable);
        ResultList.Items.Add(grid);
    }

    private static Brush GetStatusBrush(CheckStatus status)
    {
        var key = status switch
        {
            CheckStatus.Available => "AvailableBrush",
            CheckStatus.Unavailable => "UnavailableBrush",
            CheckStatus.Ignored => "IgnoredBrush",
            _ => "TextPrimaryBrush",
        };
        return (Brush)Application.Current.Resources[key];
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = title,
            Content = message,
            CloseButtonText = Text("common.close"),
        };
        await dialog.ShowAsync();
    }
}
