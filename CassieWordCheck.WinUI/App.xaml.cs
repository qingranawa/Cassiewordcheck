using System.Threading;
using CassieWordCheck.Services;
using Microsoft.UI.Xaml;

namespace CassieWordCheck;

/// <summary>
/// WinUI 应用入口，负责单实例和全局异常边界喵
/// </summary>
public partial class App : Application
{
    private const string MutexName = "CassieWordCheck_SingleInstance";
    private Mutex? _mutex;

    public static MainWindow? MainWindow { get; private set; }
    public static AppState? State { get; private set; }

    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (!createdNew)
        {
            _mutex.Dispose();
            _mutex = null;
            Environment.Exit(0);
            return;
        }

        var storage = new WinUiStoragePathProvider();
        storage.MigrateLegacyData();
        State = new AppState(storage);

        MainWindow = new MainWindow(State);
        WindowService.Initialize(MainWindow);
        MainWindow.Activate();
    }

    private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs args)
    {
        StartupLog.Write($"WinUI exception: {args.Exception}");
        args.Handled = true;
    }

    private static void OnDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs args)
    {
        StartupLog.Write($"Domain exception: {args.ExceptionObject}");
    }

    public void ReleaseSingleInstance()
    {
        State?.Dispose();
        State = null;
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        _mutex = null;
    }
}

internal static class StartupLog
{
    private static readonly string LogPath = Path.Combine(Path.GetTempPath(), "CassieWordCheck-startup.log");

    public static void Write(string message)
    {
        try
        {
            File.AppendAllText(LogPath, $"{DateTime.Now:O} {message}{Environment.NewLine}");
        }
        catch (IOException)
        {
            // 启动日志不可写时不阻止应用运行喵
        }
    }
}
