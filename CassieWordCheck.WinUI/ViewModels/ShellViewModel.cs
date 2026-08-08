using CommunityToolkit.Mvvm.ComponentModel;

namespace CassieWordCheck;

/// <summary>
/// 管理主窗口当前导航页面喵
/// </summary>
public partial class ShellViewModel : ObservableObject
{
    private string _selectedPage = "home";

    public string SelectedPage
    {
        get => _selectedPage;
        set => SetProperty(ref _selectedPage, value);
    }
}
