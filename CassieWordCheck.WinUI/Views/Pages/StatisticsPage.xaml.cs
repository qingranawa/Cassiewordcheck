using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CassieWordCheck.Views.Pages;

public sealed partial class StatisticsPage : AppPage
{
    private string _viewMode = "coverage";

    public StatisticsPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
    }

    private void Refresh()
    {
        TitleTextBlock.Text = Text("stats.title");
        SubtitleTextBlock.Text = Text("stats.subtitle");
        ChecksLabel.Text = Text("stats.checks");
        AverageLabel.Text = Text("stats.average");
        LowestLabel.Text = Text("stats.lowest");
        UnavailableLabel.Text = Text("stats.unavailable_total");
        ViewSelector.Items.Clear();
        ViewSelector.Items.Add(new ComboBoxItem { Content = Text("stats.coverage_trend"), Tag = "coverage" });
        ViewSelector.Items.Add(new ComboBoxItem { Content = Text("stats.unavailable_trend"), Tag = "unavailable" });
        ViewSelector.SelectedIndex = _viewMode == "coverage" ? 0 : 1;

        var items = State.History.Items;
        ChecksValue.Text = items.Count.ToString();
        AverageValue.Text = items.Count == 0 ? "0%" : $"{items.Average(item => item.Coverage):F1}%";
        LowestValue.Text = items.Count == 0 ? "0%" : $"{items.Min(item => item.Coverage):F1}%";
        UnavailableValue.Text = items.Sum(item => item.Unavailable).ToString();
        RenderTrend();
    }

    private void OnViewChanged(object sender, SelectionChangedEventArgs args)
    {
        if (ViewSelector.SelectedItem is ComboBoxItem { Tag: string tag })
        {
            _viewMode = tag;
            RenderTrend();
        }
    }

    private void RenderTrend()
    {
        TrendPanel.Children.Clear();
        var items = State.History.Items.OrderBy(item => item.Timestamp).ToList();
        if (items.Count == 0)
        {
            TrendPanel.Children.Add(new TextBlock { Text = Text("stats.no_data") });
            return;
        }

        var maximum = _viewMode == "coverage"
            ? 100
            : Math.Max(1, items.Max(item => item.Unavailable));
        foreach (var item in items)
        {
            var value = _viewMode == "coverage" ? item.Coverage : item.Unavailable;
            var row = new Grid { ColumnSpacing = 10 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            row.Children.Add(new TextBlock { Text = item.Timestamp.ToString("MM/dd"), VerticalAlignment = VerticalAlignment.Center });

            var bar = new ProgressBar
            {
                Minimum = 0,
                Maximum = maximum,
                Value = value,
                Height = 8,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(bar, 1);
            row.Children.Add(bar);
            var valueText = _viewMode == "coverage" ? $"{value:F1}%" : value.ToString("F0");
            var valueBlock = new TextBlock { Text = valueText, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(valueBlock, 2);
            row.Children.Add(valueBlock);
            TrendPanel.Children.Add(row);
        }
    }
}
