using Windows.ApplicationModel.DataTransfer;

namespace CassieWordCheck.Services;

public sealed class ClipboardService
{
    public Task SetTextAsync(string text)
    {
        var package = new DataPackage();
        package.SetText(text);
        Clipboard.SetContent(package);
        return Task.CompletedTask;
    }
}
