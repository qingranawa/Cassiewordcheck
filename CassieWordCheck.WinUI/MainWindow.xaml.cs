using CassieWordCheck.Services;
using CassieWordCheck.Views.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CassieWordCheck;

public sealed partial class MainWindow : Window
{
    private readonly AppState _state;
    private readonly ShellViewModel _shellViewModel = new();

    public MainWindow(AppState state)
    {
        _state = state;
        InitializeComponent();
        UpdateNavigationTexts();
        RootNavigationView.SelectedItem = RootNavigationView.MenuItems[0];
        ContentFrame.Navigate(typeof(HomePage));
        Closed += OnClosed;
    }

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string tag)
            return;

        _shellViewModel.SelectedPage = tag;
        var pageType = tag switch
        {
            "home" => typeof(HomePage),
            "history" => typeof(HistoryPage),
            "statistics" => typeof(StatisticsPage),
            "wordcount" => typeof(WordCountPage),
            "wordlist" => typeof(WordListPage),
            "settings" => typeof(SettingsPage),
            "about" => typeof(AboutPage),
            _ => typeof(PlaceholderPage),
        };
        ContentFrame.Navigate(pageType, tag);
    }

    public void NavigateTo(string tag)
    {
        var items = RootNavigationView.MenuItems.Concat(RootNavigationView.FooterMenuItems)
            .OfType<NavigationViewItem>();
        var item = items.FirstOrDefault(candidate => string.Equals(candidate.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase));
        if (item is not null)
            RootNavigationView.SelectedItem = item;
        else
            ContentFrame.Navigate(typeof(PlaceholderPage), tag);
    }

    public void UpdateNavigationTexts()
    {
        HomeNavigationItem.Content = _state.Localization["nav.home"];
        HistoryNavigationItem.Content = _state.Localization["menu.history"];
        StatisticsNavigationItem.Content = _state.Localization["menu.statistics"];
        WordCountNavigationItem.Content = _state.Localization["menu.wordcount"];
        WordListNavigationItem.Content = _state.Localization["wordlist_browser.title"];
        SettingsNavigationItem.Content = _state.Localization["settings.title"];
        AboutNavigationItem.Content = _state.Localization["menu.about"];
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        if (Application.Current is App app)
            app.ReleaseSingleInstance();
    }
}
