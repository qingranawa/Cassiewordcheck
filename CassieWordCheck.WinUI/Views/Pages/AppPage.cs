using CassieWordCheck;
using CassieWordCheck.Services;
using Microsoft.UI.Xaml.Controls;

namespace CassieWordCheck.Views.Pages;

/// <summary>
/// 所有功能页共享应用状态和本地化服务喵
/// </summary>
public abstract class AppPage : Page
{
    protected AppState State => App.State
        ?? throw new InvalidOperationException("应用状态尚未初始化");

    protected LocalizationService Localization => State.Localization;

    protected string Text(string key)
    {
        return Localization[key];
    }
}
