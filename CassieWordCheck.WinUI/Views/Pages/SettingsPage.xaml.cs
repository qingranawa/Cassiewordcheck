using CassieWordCheck.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

namespace CassieWordCheck.Views.Pages;

public sealed partial class SettingsPage : AppPage
{
    private bool _loading;

    public SettingsPage()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadSettings();
    }

    private void LoadSettings()
    {
        _loading = true;
        UpdateTexts();
        IgnoreChineseCheck.IsChecked = State.Settings.IgnoreChinese;
        FilterFormattingCheck.IsChecked = State.Settings.FilterFormatting;
        FilterNamingCheck.IsChecked = State.Settings.FilterNaming;
        WordWrapCheck.IsChecked = State.Settings.WordWrap;
        WordlistPathBox.Text = State.Settings.WordlistPath;
        FontSizeCombo.Items.Clear();
        foreach (var size in new[] { 12, 14, 16, 18, 20 })
            FontSizeCombo.Items.Add(size.ToString());
        FontSizeCombo.SelectedItem = State.Settings.FontSize.ToString();

        LanguageCombo.Items.Clear();
        foreach (var language in Localization.AvailableLanguages().OrderBy(pair => pair.Key))
            LanguageCombo.Items.Add(new LanguageOption(language.Key, language.Value));
        var selected = LanguageCombo.Items.OfType<LanguageOption>().FirstOrDefault(item => item.Code == Localization.CurrentLanguage);
        LanguageCombo.SelectedItem = selected ?? LanguageCombo.Items.FirstOrDefault();
        _loading = false;
    }

    private void UpdateTexts()
    {
        TitleTextBlock.Text = Text("settings.title");
        SubtitleTextBlock.Text = Text("settings.subtitle");
        IgnoreChineseCheck.Content = Text("settings.ignore_chinese");
        FilterFormattingCheck.Content = Text("settings.ignore_angle");
        FilterNamingCheck.Content = Text("settings.filter_naming");
        WordWrapCheck.Content = Text("settings.word_wrap");
        LanguageLabel.Text = Text("settings.language");
        FontSizeLabel.Text = Text("settings.font_size");
        WordlistPathLabel.Text = Text("settings.wordlist_path");
        BrowseButton.Content = Text("settings.browse");
        ResetButton.Content = Text("settings.reset");
        UpdateButton.Content = Text("update.check");
        SaveButton.Content = Text("common.save");
    }

    private void OnLanguageChanged(object sender, SelectionChangedEventArgs args)
    {
        if (_loading || LanguageCombo.SelectedItem is not LanguageOption option)
            return;
        Localization.SetLanguage(option.Code);
        State.Settings.Language = option.Code;
        UpdateTexts();
        App.MainWindow?.UpdateNavigationTexts();
    }

    private async void OnBrowse(object sender, RoutedEventArgs args)
    {
        var picker = new FilePickerService(App.MainWindow!);
        var paths = await picker.PickFilesAsync([new FilePickerChoice(Text("settings.wordlist_path"), [".txt"])], false);
        if (paths.Count > 0)
            WordlistPathBox.Text = paths[0];
    }

    private void OnReset(object sender, RoutedEventArgs args)
    {
        IgnoreChineseCheck.IsChecked = true;
        FilterFormattingCheck.IsChecked = true;
        FilterNamingCheck.IsChecked = true;
        WordWrapCheck.IsChecked = true;
        FontSizeCombo.SelectedItem = "14";
        WordlistPathBox.Text = string.Empty;
        var chinese = LanguageCombo.Items.OfType<LanguageOption>().FirstOrDefault(item => item.Code == "zh-CN");
        if (chinese is not null)
            LanguageCombo.SelectedItem = chinese;
    }

    private void OnSave(object sender, RoutedEventArgs args)
    {
        State.Settings.IgnoreChinese = IgnoreChineseCheck.IsChecked ?? true;
        State.Settings.FilterFormatting = FilterFormattingCheck.IsChecked ?? true;
        State.Settings.FilterNaming = FilterNamingCheck.IsChecked ?? true;
        State.Settings.WordWrap = WordWrapCheck.IsChecked ?? true;
        State.Settings.FontSize = int.TryParse(FontSizeCombo.SelectedItem?.ToString(), out var fontSize) ? fontSize : 14;
        var path = WordlistPathBox.Text.Trim();
        if (path.Length > 0 && File.Exists(path))
            State.LoadWordList(path);
        else if (path.Length == 0)
            State.Settings.WordlistPath = string.Empty;
        State.ApplySettings();
    }

    private async void OnCheckUpdate(object sender, RoutedEventArgs args)
    {
        var service = new UpdateService();
        var info = await service.CheckForUpdateAsync();
        if (info is { HasUpdate: true })
        {
            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = Text("update.available"),
                Content = string.Format(Text("update.new_version"), info.LatestVersion, service.CurrentVersion),
                PrimaryButtonText = Text("update.download"),
                CloseButtonText = Text("common.close"),
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                Process.Start(new ProcessStartInfo(info.HtmlUrl) { UseShellExecute = true });
            return;
        }

        await ShowMessageAsync(Text("update.check"), info is null ? Text("update.error") : Text("update.current"));
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog { XamlRoot = XamlRoot, Title = title, Content = message, CloseButtonText = Text("common.close") };
        await dialog.ShowAsync();
    }
}

public sealed record LanguageOption(string Display, string Code)
{
    public override string ToString() => Display;
}
