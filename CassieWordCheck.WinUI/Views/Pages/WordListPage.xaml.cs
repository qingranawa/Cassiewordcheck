using CassieWordCheck.Models;
using CassieWordCheck.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Text;
using Windows.System;

namespace CassieWordCheck.Views.Pages;

public sealed partial class WordListPage : AppPage
{
    private string _sortMode = "alpha-asc";
    private List<string> _allWords = [];
    private WordListDiff? _lastDiff;

    public WordListPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        State.WordListChanged += OnWordListChanged;
        Refresh();
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        State.WordListChanged -= OnWordListChanged;
    }

    private void Refresh()
    {
        UpdateTexts();
        _allWords = State.WordList.Words.ToList();
        RenderWordList();
        RenderWhitelist();
        RenderExcludeList();
        RenderDistributions();
    }

    private void UpdateTexts()
    {
        TitleTextBlock.Text = Text("wordlist_browser.title");
        SubtitleTextBlock.Text = Text("wordlist.subtitle");
        SearchBox.PlaceholderText = Text("wordlist.search");
        ReloadButton.Content = Text("wordlist.reload");
        ImportButton.Content = Text("menu.import_words");
        CompareButton.Content = Text("diff.tooltip");
        ExportCompareButton.Content = Text("diff.export");
        ExportCompareButton.IsEnabled = _lastDiff is not null;
        SortCombo.Items.Clear();
        SortCombo.Items.Add(new ComboBoxItem { Content = Text("wordlist.sort_alpha"), Tag = "alpha-asc" });
        SortCombo.Items.Add(new ComboBoxItem { Content = Text("wordlist.sort_alpha_desc"), Tag = "alpha-desc" });
        SortCombo.Items.Add(new ComboBoxItem { Content = Text("wordlist.sort_length"), Tag = "length-asc" });
        SortCombo.Items.Add(new ComboBoxItem { Content = Text("wordlist.sort_length_desc"), Tag = "length-desc" });
        SortCombo.SelectedIndex = _sortMode switch { "alpha-desc" => 1, "length-asc" => 2, "length-desc" => 3, _ => 0 };
        WhitelistTitleTextBlock.Text = Text("whitelist.title");
        WhitelistInput.PlaceholderText = Text("whitelist.input_hint");
        WhitelistAddButton.Content = Text("whitelist.add");
        WhitelistImportButton.Content = Text("whitelist.import");
        WhitelistExportButton.Content = Text("whitelist.export");
        WhitelistClearButton.Content = Text("whitelist.clear");
        DistributionTitleTextBlock.Text = Text("wordlist_browser.length_dist");
        FirstLetterTitleTextBlock.Text = Text("wordlist_browser.first_letter_dist");
        EmptyTextBlock.Text = Text("wordlist_browser.empty");
        ExcludeTitleTextBlock.Text = Text("exclude.title");
        ExcludeClearButton.Content = Text("exclude.clear");
        ExcludeEmptyTextBlock.Text = Text("exclude.empty");
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs args) => RenderWordList();

    private void OnSortChanged(object sender, SelectionChangedEventArgs args)
    {
        if (SortCombo.SelectedItem is ComboBoxItem { Tag: string tag })
        {
            _sortMode = tag;
            RenderWordList();
        }
    }

    private void RenderWordList()
    {
        var search = SearchBox.Text.Trim();
        var filtered = _allWords.Where(word => word.Contains(search, StringComparison.OrdinalIgnoreCase));
        var sorted = _sortMode switch
        {
            "alpha-desc" => filtered.OrderByDescending(word => word),
            "length-asc" => filtered.OrderBy(word => word.Length).ThenBy(word => word),
            "length-desc" => filtered.OrderByDescending(word => word.Length).ThenBy(word => word),
            _ => filtered.OrderBy(word => word),
        };
        var list = sorted.ToList();
        TotalTextBlock.Text = string.Format(Text("wordlist_browser.total"), _allWords.Count);
        FilteredTextBlock.Text = string.Format(Text("wordlist_browser.filtered"), list.Count);
        EmptyTextBlock.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        WordListView.Items.Clear();
        foreach (var word in list.Take(5000))
        {
            var row = new Grid { ColumnSpacing = 8, Margin = new Thickness(2) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(new TextBlock
            {
                Text = word,
                FontFamily = new FontFamily("Consolas"),
                VerticalAlignment = VerticalAlignment.Center,
            });
            var toggle = new Button
            {
                Content = State.WordList.IsExcluded(word) ? Text("exclude.remove") : Text("exclude.add"),
                Tag = word,
                Padding = new Thickness(8, 2, 8, 2),
                Style = (Style)Application.Current.Resources["SecondaryButtonStyle"],
            };
            toggle.Click += OnToggleExclude;
            Grid.SetColumn(toggle, 1);
            row.Children.Add(toggle);
            WordListView.Items.Add(row);
        }
    }

    private void RenderWhitelist()
    {
        WhitelistList.Items.Clear();
        foreach (var word in State.WordList.Whitelist.OrderBy(word => word))
        {
            var row = new Grid { ColumnSpacing = 8 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(new TextBlock { Text = word, VerticalAlignment = VerticalAlignment.Center });
            var remove = new Button { Content = "×", Tag = word, Padding = new Thickness(8, 2, 8, 2) };
            remove.Click += OnRemoveWhitelist;
            Grid.SetColumn(remove, 1);
            row.Children.Add(remove);
            WhitelistList.Items.Add(row);
        }
    }

    private void RenderExcludeList()
    {
        ExcludeList.Items.Clear();
        var words = State.WordList.ExcludeList.OrderBy(word => word).ToList();
        ExcludeEmptyTextBlock.Visibility = words.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        foreach (var word in words)
        {
            var row = new Grid { ColumnSpacing = 8 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(new TextBlock { Text = word, VerticalAlignment = VerticalAlignment.Center });
            var remove = new Button
            {
                Content = Text("exclude.remove"),
                Tag = word,
                Padding = new Thickness(8, 2, 8, 2),
                Style = (Style)Application.Current.Resources["SecondaryButtonStyle"],
            };
            remove.Click += OnToggleExclude;
            Grid.SetColumn(remove, 1);
            row.Children.Add(remove);
            ExcludeList.Items.Add(row);
        }
    }

    private void RenderDistributions()
    {
        RenderDistribution(LengthDistributionPanel, State.WordList.GetWordLengthDistribution().OrderBy(item => item.Key).Select(item => (item.Key.ToString(), item.Value)));
        RenderDistribution(FirstLetterPanel, State.WordList.GetFirstLetterDistribution().OrderBy(item => item.Key).Select(item => (item.Key.ToString().ToUpperInvariant(), item.Value)));
    }

    private static void RenderDistribution(StackPanel panel, IEnumerable<(string Label, int Count)> values)
    {
        panel.Children.Clear();
        var items = values.ToList();
        var maximum = Math.Max(1, items.Select(item => item.Count).DefaultIfEmpty().Max());
        foreach (var item in items)
        {
            var row = new Grid { ColumnSpacing = 8 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            row.Children.Add(new TextBlock { Text = item.Label });
            var bar = new ProgressBar { Maximum = maximum, Value = item.Count, Height = 7, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(bar, 1);
            row.Children.Add(bar);
            var count = new TextBlock { Text = item.Count.ToString(), HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(count, 2);
            row.Children.Add(count);
            panel.Children.Add(row);
        }
    }

    private void OnReload(object sender, RoutedEventArgs args)
    {
        if (State.WordList.SourcePath is not null)
            State.WordList.Reload();
        Refresh();
    }

    private async void OnImport(object sender, RoutedEventArgs args)
    {
        var picker = new FilePickerService(App.MainWindow!);
        var paths = await picker.PickFilesAsync(
            [new FilePickerChoice(Text("import.supplement"), [".txt", ".csv", ".xlsx"])],
            allowMultiple: true);
        var count = 0;
        foreach (var path in paths)
        {
            try { count += await Task.Run(() => State.ImportWords(path)); }
            catch (Exception exception) { await ShowMessageAsync(Text("diff.error"), exception.Message); }
        }
        if (count > 0)
            Refresh();
    }

    private async void OnCompare(object sender, RoutedEventArgs args)
    {
        var picker = new FilePickerService(App.MainWindow!);
        var paths = await picker.PickFilesAsync([new FilePickerChoice(Text("diff.select_file"), [".txt"])], false);
        if (paths.Count == 0 || State.WordList.SourcePath is null)
            return;

        try
        {
            using var other = new WordList();
            other.LoadFromFile(paths[0]);
            var diff = State.WordList.DiffWith(other);
            _lastDiff = diff;
            ExportCompareButton.IsEnabled = true;
            CompareTextBlock.Text = string.Format(Text("diff.done"), Path.GetFileName(paths[0]), diff.LeftOnlyCount, diff.RightOnlyCount, diff.CommonCount);
        }
        catch (Exception exception)
        {
            await ShowMessageAsync(Text("diff.error"), exception.Message);
        }
    }

    private async void OnExportCompare(object sender, RoutedEventArgs args)
    {
        if (_lastDiff is null)
            return;

        var picker = new FilePickerService(App.MainWindow!);
        var path = await picker.PickSaveFileAsync("wordlist-diff", ".txt");
        if (path is null)
            return;

        var report = new StringBuilder()
            .AppendLine($"{_lastDiff.LeftLabel} <> {_lastDiff.RightLabel}")
            .AppendLine()
            .AppendLine($"{Text("diff.left_only")}: {_lastDiff.LeftOnlyCount}")
            .AppendLine(string.Join(Environment.NewLine, _lastDiff.LeftOnly.OrderBy(word => word)))
            .AppendLine()
            .AppendLine($"{Text("diff.right_only")}: {_lastDiff.RightOnlyCount}")
            .AppendLine(string.Join(Environment.NewLine, _lastDiff.RightOnly.OrderBy(word => word)))
            .AppendLine()
            .AppendLine($"{Text("diff.common")}: {_lastDiff.CommonCount}")
            .ToString();
        await File.WriteAllTextAsync(path, report);
        await ShowMessageAsync(Text("diff.export"), Text("diff.exported"));
    }

    private void OnWhitelistKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (args.Key == VirtualKey.Enter)
            AddWhitelist();
    }

    private void OnAddWhitelist(object sender, RoutedEventArgs args) => AddWhitelist();

    private void AddWhitelist()
    {
        if (State.WordList.AddToWhitelist(WhitelistInput.Text))
        {
            State.Settings.Whitelist = State.WordList.Whitelist.ToList();
            State.Settings.Save();
            WhitelistInput.Text = string.Empty;
            RenderWhitelist();
        }
    }

    private void OnRemoveWhitelist(object sender, RoutedEventArgs args)
    {
        if (sender is Button { Tag: string word })
        {
            State.WordList.RemoveFromWhitelist(word);
            State.Settings.Whitelist = State.WordList.Whitelist.ToList();
            State.Settings.Save();
            RenderWhitelist();
        }
    }

    private async void OnImportWhitelist(object sender, RoutedEventArgs args)
    {
        var picker = new FilePickerService(App.MainWindow!);
        var paths = await picker.PickFilesAsync([new FilePickerChoice(Text("whitelist.import"), [".txt"])], false);
        if (paths.Count == 0)
            return;
        foreach (var line in File.ReadLines(paths[0]))
            State.WordList.AddToWhitelist(line);
        State.Settings.Whitelist = State.WordList.Whitelist.ToList();
        State.Settings.Save();
        RenderWhitelist();
    }

    private async void OnExportWhitelist(object sender, RoutedEventArgs args)
    {
        var picker = new FilePickerService(App.MainWindow!);
        var path = await picker.PickSaveFileAsync("whitelist", ".txt");
        if (path is not null)
            await File.WriteAllLinesAsync(path, State.WordList.Whitelist.OrderBy(word => word));
    }

    private async void OnClearWhitelist(object sender, RoutedEventArgs args)
    {
        if (State.WordList.Whitelist.Count == 0)
            return;
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = Text("whitelist.clear"),
            Content = Text("whitelist.confirm_clear"),
            PrimaryButtonText = Text("whitelist.clear"),
            CloseButtonText = Text("common.cancel"),
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            State.WordList.ClearWhitelist();
            State.Settings.Whitelist = [];
            State.Settings.Save();
            RenderWhitelist();
        }
    }

    private void OnToggleExclude(object sender, RoutedEventArgs args)
    {
        if (sender is not Button { Tag: string word })
            return;

        if (State.WordList.IsExcluded(word))
            State.WordList.UnExclude(word);
        else
            State.WordList.Exclude(word);

        State.CheckText(State.CurrentText);
        Refresh();
    }

    private async void OnClearExclude(object sender, RoutedEventArgs args)
    {
        if (State.WordList.ExcludeCount == 0)
            return;

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = Text("exclude.clear"),
            Content = Text("exclude.title"),
            PrimaryButtonText = Text("exclude.clear"),
            CloseButtonText = Text("common.cancel"),
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            State.WordList.ClearExclude();
            State.CheckText(State.CurrentText);
            Refresh();
        }
    }

    private void OnWordListChanged()
    {
        DispatcherQueue.TryEnqueue(Refresh);
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog { XamlRoot = XamlRoot, Title = title, Content = message, CloseButtonText = Text("common.close") };
        await dialog.ShowAsync();
    }
}
