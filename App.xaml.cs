using System.IO;
using System.Threading;
using System.Windows;

namespace CassieWordCheck;

/// <summary>
/// 应用入口——负责单实例检查、全局异常捕获和全局样式覆盖喵~
/// </summary>
public partial class App : Application
{
    // 全局 Mutex，确保只有一个实例在运行喵！
    private static Mutex? _mutex;
    private const string MutexId = "CassieWordCheck_SingleInstance";

    /// <summary>
    /// 启动日志文件路径（%TEMP%\CassieWordCheck-startup.log）
    /// </summary>
    private static readonly string LogPath = Path.Combine(Path.GetTempPath(), "CassieWordCheck-startup.log");

    /// <summary>
    /// 写启动日志（用于诊断静默崩溃）
    /// </summary>
    private static void Log(string msg)
    {
        try
        {
            File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff} [{Environment.CurrentManagedThreadId}] {msg}\n");
        }
        catch { /* 日志写不了也就算了 */ }
    }

    /// <summary>
    /// 静态构造：预留——目前不需要特殊初始化
    /// </summary>
    static App()
    {
        Log("=== App 启动 ===");
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        Log($"BaseDir: {baseDir}");
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        Log("OnStartup 开始");

        // 全局异常捕获——防止未捕获异常导致崩溃
        this.DispatcherUnhandledException += (_, args) =>
        {
            var ex = args.Exception;
            Log($"DispatcherUnhandledException: {ex.Message}\n{ex.StackTrace}");
            MessageBox.Show(
                $"程序遇到了意外的错误。\n\n{ex.Message}",
                "CASSIE CWC Tool",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var msg = args.ExceptionObject is Exception ex
                ? ex.Message : "未知错误";
            Log($"UnhandledException: {msg}");
            MessageBox.Show(
                $"程序遇到了意外的错误。\n\n{msg}",
                "CASSIE CWC Tool",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        };

        try
        {
            // 尝试创建 Mutex，如果已存在说明有另一个实例在跑
            _mutex = new Mutex(true, MutexId, out var createdNew);
            Log($"Mutex createdNew={createdNew}");

            if (!createdNew)
            {
                MessageBox.Show("程序已在运行中。\nThe app is already running.",
                    "CASSIE CWC Tool", MessageBoxButton.OK, MessageBoxImage.Information);
                _mutex = null;
                Shutdown();
                return;
            }

            Log("OnStartup 调用 base.OnStartup");
            base.OnStartup(e);
            Log("OnStartup base.OnStartup 完成");

            // 覆盖 Window 默认样式元数据，让全局样式对 Window 生效
            FrameworkElement.StyleProperty.OverrideMetadata(
                typeof(Window),
                new FrameworkPropertyMetadata(null));
            Log("OnStartup 完成");
        }
        catch (Exception ex)
        {
            Log($"OnStartup 异常: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log("OnExit");
        if (_mutex is not null)
        {
            _mutex.ReleaseMutex();
            _mutex.Close();
            Log("Mutex 已释放");
        }
        base.OnExit(e);
    }
}
