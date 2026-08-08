using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace CassieWordCheck.Views.Pages;

public sealed partial class PlaceholderPage : Page
{
    private static readonly IReadOnlyDictionary<string, string> PageTitles =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["history"] = "检查历史",
            ["statistics"] = "统计",
            ["wordcount"] = "字数统计",
            ["wordlist"] = "词库管理",
            ["settings"] = "设置",
            ["about"] = "关于",
        };

    public PlaceholderPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs args)
    {
        base.OnNavigatedTo(args);
        var tag = args.Parameter as string ?? string.Empty;
        TitleTextBlock.Text = PageTitles.TryGetValue(tag, out var title) ? title : "功能页面";
    }
}
