using System.IO;
using CassieWordCheck.Models;

namespace CassieWordCheck.Tests;

/// <summary>
/// Settings.ResultMode 持久化测试
/// </summary>
public class SettingsTests
{
    /// <summary>创建指向临时路径的 Settings 实例</summary>
    private static Settings CreateSettingsWithTempPath()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tmpDir);
        var tmpFile = Path.Combine(tmpDir, "appsettings.json");
        return new Settings(tmpFile);
    }

    // ========== ResultMode 默认值 ==========

    [Fact]
    public void ResultMode_默认值为inline()
    {
        var settings = CreateSettingsWithTempPath();
        // 新文件不存在时使用默认值
        Assert.Equal("inline", settings.ResultMode);
    }

    // ========== ResultMode 读写 ==========

    [Fact]
    public void ResultMode_设置并保存_重新加载后值保持不变()
    {
        var settings = CreateSettingsWithTempPath();
        settings.ResultMode = "list";
        settings.Save();

        // 重新加载
        var reloaded = CreateSettingsWithTempPath();
        // 注意：这里 reloaded 是新实例，路径相同但不继承旧实例的内存状态
        // 我们需要通过文件路径复用
        var tmpDir = Path.GetDirectoryName(settings.GetType().GetField("_filePath",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(settings)!.ToString()!)!;
        var tmpFile = Path.Combine(tmpDir, "appsettings.json");
        var reloadedSettings = new Settings(tmpFile);
        reloadedSettings.Load();

        Assert.Equal("list", reloadedSettings.ResultMode);
    }

    [Fact]
    public void ResultMode_设置compare_保存后正确恢复()
    {
        using var tmpFile = new TempFile();
        var settings = new Settings(tmpFile.Path);
        settings.ResultMode = "compare";
        settings.Save();

        var loaded = new Settings(tmpFile.Path);
        Assert.Equal("compare", loaded.ResultMode);
    }

    [Fact]
    public void ResultMode_设置inline_保存后正确恢复()
    {
        using var tmpFile = new TempFile();
        var settings = new Settings(tmpFile.Path);
        settings.ResultMode = "inline";
        settings.Save();

        var loaded = new Settings(tmpFile.Path);
        Assert.Equal("inline", loaded.ResultMode);
    }

    // ========== ResultMode 使用枚举值以外的值 ==========

    [Fact]
    public void ResultMode_支持任意字符串值()
    {
        using var tmpFile = new TempFile();
        var settings = new Settings(tmpFile.Path);
        settings.ResultMode = "custom_mode";
        settings.Save();

        var loaded = new Settings(tmpFile.Path);
        Assert.Equal("custom_mode", loaded.ResultMode);
    }

    // ========== 非 ResultMode 字段不受影响 ==========

    [Fact]
    public void ResultMode_设置不影响其他字段()
    {
        using var tmpFile = new TempFile();
        var settings = new Settings(tmpFile.Path);
        settings.FontSize = 18;
        settings.ResultMode = "list";
        settings.Save();

        var loaded = new Settings(tmpFile.Path);
        Assert.Equal(18, loaded.FontSize);      // 其他字段不受影响
        Assert.Equal("list", loaded.ResultMode);
    }

    // ========== 空文件 / 文件不存在 ==========

    [Fact]
    public void Settings_文件不存在_使用默认值()
    {
        var settings = new Settings(Path.Combine(Path.GetTempPath(), "nonexistent", "settings.json"));
        Assert.Equal("inline", settings.ResultMode);
    }
}

/// <summary>
/// 临时文件辅助类，自动清理
/// </summary>
public class TempFile : IDisposable
{
    public string Path { get; }

    public TempFile()
    {
        Path = System.IO.Path.GetTempFileName();
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(Path))
                File.Delete(Path);
        }
        catch
        {
            // 静默
        }
    }
}
