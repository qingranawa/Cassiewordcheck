namespace CassieWordCheck.Models;

/// <summary>应用全局信息喵~</summary>
public static class AppInfo
{
    /// <summary>当前版本号，来自程序集元数据，避免界面与发布版本漂移。</summary>
    public static string Version =>
        typeof(AppInfo).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
}
