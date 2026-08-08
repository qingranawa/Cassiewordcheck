using CassieWordCheck.Models;
using CassieWordCheck.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

namespace CassieWordCheck.Views.Pages;

public sealed partial class AboutPage : AppPage
{
    private readonly string[] _sectionKeys = ["about.features", "about.changelog", "about.info", "about.disclaimer"];

    public AboutPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
    }

    private void Refresh()
    {
        TitleTextBlock.Text = Text("about.title");
        SubtitleTextBlock.Text = Text("about.subtitle");
        VersionTextBlock.Text = $"CASSIE CWC Tool · v{AppInfo.Version}";
        DescriptionTextBlock.Text = Text("about.description");
        GitHubButton.Content = Text("about.github");
        SectionCombo.Items.Clear();
        foreach (var key in _sectionKeys)
            SectionCombo.Items.Add(Text(key));
        if (SectionCombo.SelectedIndex < 0)
            SectionCombo.SelectedIndex = 0;
        RenderSection();
    }

    private void OnSectionChanged(object sender, SelectionChangedEventArgs args) => RenderSection();

    private void RenderSection()
    {
        var index = SectionCombo.SelectedIndex;
        if (index < 0 || index >= _sectionKeys.Length)
            return;
        ContentTextBlock.Text = index == 1
            ? AboutWindowChangelog.ChangelogText
            : Text(_sectionKeys[index]);
    }

    private void OnGitHub(object sender, RoutedEventArgs args)
    {
        Process.Start(new ProcessStartInfo("https://github.com/qingranawa/Cassiewordcheck") { UseShellExecute = true });
    }
}
