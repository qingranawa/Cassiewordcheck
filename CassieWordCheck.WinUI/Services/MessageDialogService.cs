using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CassieWordCheck.Services;

public sealed class MessageDialogService
{
    private readonly Microsoft.UI.Xaml.Window _owner;

    public MessageDialogService(Microsoft.UI.Xaml.Window owner)
    {
        _owner = owner;
    }

    public async Task ShowAsync(string title, string message, string closeText)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = (_owner.Content as FrameworkElement)?.XamlRoot,
            Title = title,
            Content = message,
            CloseButtonText = closeText,
        };
        await dialog.ShowAsync();
    }
}
