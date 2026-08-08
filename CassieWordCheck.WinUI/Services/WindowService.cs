using CassieWordCheck.WinUI.Interop;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace CassieWordCheck.Services;

/// <summary>
/// 负责 WinUI 窗口初始化和 Windows App SDK 窗口能力喵
/// </summary>
public static class WindowService
{
    private const int DefaultWidth = 1080;
    private const int DefaultHeight = 720;

    public static void Initialize(Window window)
    {
        window.Title = "CASSIE CWC Tool";

        var appWindow = GetAppWindow(window);
        appWindow.Resize(new SizeInt32(DefaultWidth, DefaultHeight));

        window.SystemBackdrop = new MicaBackdrop();
    }

    private static AppWindow GetAppWindow(Window window)
    {
        var windowId = WindowInterop.GetWindowId(window);
        return AppWindow.GetFromWindowId(windowId);
    }
}
