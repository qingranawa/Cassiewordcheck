using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace CassieWordCheck.WinUI.Interop;

/// <summary>
/// 将 WinUI Window 转换为 Win32 窗口句柄和 WindowId 喵
/// </summary>
public static class WindowInterop
{
    public static IntPtr GetWindowHandle(Window window)
    {
        return WindowNative.GetWindowHandle(window);
    }

    public static WindowId GetWindowId(Window window)
    {
        return Win32Interop.GetWindowIdFromWindow(GetWindowHandle(window));
    }
}
