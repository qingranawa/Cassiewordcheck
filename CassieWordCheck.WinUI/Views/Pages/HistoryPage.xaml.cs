using CassieWordCheck.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CassieWordCheck.Views.Pages;

public sealed partial class HistoryPage : AppPage
{
    public HistoryPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
    }

    private void Refresh()
    {
        TitleTextBlock.Text = Text("history.title");
        SubtitleTextBlock.Text = Text("history.subtitle");
        ClearButton.Content = Text("history.clear");
        EmptyTextBlock.Text = Text("stats.no_data");
        EmptyTextBlock.Visibility = State.History.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        HistoryList.Items.Clear();
        foreach (var item in State.History.Items.OrderByDescending(item => item.Timestamp))
        {
            var card = new Border { Style = (Style)Application.Current.Resources["CardBorderStyle"] };
            var grid = new Grid { ColumnSpacing = 16 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var body = new StackPanel { Spacing = 6 };
            body.Children.Add(new TextBlock
            {
                Text = item.InputText,
                MaxHeight = 54,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
            });
            body.Children.Add(new TextBlock
            {
                Text = $"{item.Timestamp:g} · {string.Format(Text("stats.available"), item.Available)} · {string.Format(Text("stats.unavailable"), item.Unavailable)} · {item.Coverage:F1}%",
                Style = (Style)Application.Current.Resources["PageSubtitleTextBlockStyle"],
            });
            grid.Children.Add(body);

            var restore = new Button
            {
                Content = Text("history.restore"),
                Tag = item,
                Style = (Style)Application.Current.Resources["SecondaryButtonStyle"],
                VerticalAlignment = VerticalAlignment.Center,
            };
            restore.Click += OnRestore;
            Grid.SetColumn(restore, 1);
            grid.Children.Add(restore);
            card.Child = grid;
            HistoryList.Items.Add(card);
        }
    }

    private void OnRestore(object sender, RoutedEventArgs args)
    {
        if (sender is not Button { Tag: HistoryItem item })
            return;

        State.CheckText(item.InputText);
        App.MainWindow?.NavigateTo("home");
    }

    private async void OnClear(object sender, RoutedEventArgs args)
    {
        if (State.History.Items.Count == 0)
            return;

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = Text("history.clear"),
            Content = Text("history.confirm_clear"),
            PrimaryButtonText = Text("history.clear"),
            CloseButtonText = Text("common.cancel"),
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            State.History.Clear();
            Refresh();
        }
    }
}
