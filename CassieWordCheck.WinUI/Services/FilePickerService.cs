using CassieWordCheck.WinUI.Interop;
using WinRT.Interop;
using Windows.Storage.Pickers;

namespace CassieWordCheck.Services;

public sealed record FilePickerChoice(string DisplayName, IReadOnlyList<string> Extensions);

public sealed class FilePickerService
{
    private readonly Microsoft.UI.Xaml.Window _owner;

    public FilePickerService(Microsoft.UI.Xaml.Window owner)
    {
        _owner = owner;
    }

    public async Task<IReadOnlyList<string>> PickFilesAsync(
        IReadOnlyList<FilePickerChoice> choices,
        bool allowMultiple,
        CancellationToken cancellationToken = default)
    {
        var picker = new FileOpenPicker
        {
            ViewMode = PickerViewMode.List,
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
        };
        foreach (var choice in choices)
        {
            foreach (var extension in choice.Extensions)
                picker.FileTypeFilter.Add(extension);
        }

        InitializeWithWindow.Initialize(picker, WindowInterop.GetWindowHandle(_owner));
        cancellationToken.ThrowIfCancellationRequested();
        var files = allowMultiple
            ? await picker.PickMultipleFilesAsync()
            : null;
        if (allowMultiple)
            return files is null ? [] : files.Select(file => file.Path).ToArray();

        var file = await picker.PickSingleFileAsync();
        return file is null ? [] : [file.Path];
    }

    public async Task<string?> PickSaveFileAsync(
        string suggestedFileName,
        string extension,
        CancellationToken cancellationToken = default)
    {
        var picker = new FileSavePicker
        {
            SuggestedFileName = suggestedFileName,
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
        };
        picker.FileTypeChoices.Add("文本文件", [extension]);
        InitializeWithWindow.Initialize(picker, WindowInterop.GetWindowHandle(_owner));
        cancellationToken.ThrowIfCancellationRequested();
        var file = await picker.PickSaveFileAsync();
        return file?.Path;
    }
}
