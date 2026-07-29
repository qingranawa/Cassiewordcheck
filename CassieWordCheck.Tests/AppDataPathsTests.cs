using System.IO;
using CassieWordCheck.Models;

namespace CassieWordCheck.Tests;

public class AppDataPathsTests
{
    [Fact]
    public void MigrateLegacyFile_目标不存在时复制旧文件()
    {
        var root = CreateTempDirectory();
        var legacy = Path.Combine(root, "legacy");
        var destination = Path.Combine(root, "appdata");
        Directory.CreateDirectory(legacy);
        File.WriteAllText(Path.Combine(legacy, "appsettings.json"), "legacy");

        var migrated = AppDataPaths.MigrateLegacyFile(
            "appsettings.json", destination, legacy);

        Assert.True(migrated);
        Assert.Equal("legacy", File.ReadAllText(Path.Combine(destination, "appsettings.json")));
    }

    [Fact]
    public void MigrateLegacyFile_目标已存在时不覆盖用户数据()
    {
        var root = CreateTempDirectory();
        var legacy = Path.Combine(root, "legacy");
        var destination = Path.Combine(root, "appdata");
        Directory.CreateDirectory(legacy);
        Directory.CreateDirectory(destination);
        File.WriteAllText(Path.Combine(legacy, "history.json"), "old");
        File.WriteAllText(Path.Combine(destination, "history.json"), "new");

        var migrated = AppDataPaths.MigrateLegacyFile(
            "history.json", destination, legacy);

        Assert.False(migrated);
        Assert.Equal("new", File.ReadAllText(Path.Combine(destination, "history.json")));
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "CassieWordCheckTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
