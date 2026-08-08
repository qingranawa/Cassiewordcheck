using System.IO;
using CassieWordCheck.Models;
using CassieWordCheck.Services;

namespace CassieWordCheck.Tests;

public class AppDataPathsTests
{
    [Fact]
    public void GetUserLocalDataDirectory_位于产品用户数据目录下()
    {
        var expected = Path.Combine(
            AppDataPaths.GetUserDataDirectory(),
            "data");

        var actual = AppDataPaths.GetUserLocalDataDirectory();

        Assert.Equal(expected, actual);
        Assert.StartsWith(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            actual,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            Path.GetFullPath(AppContext.BaseDirectory),
            actual,
            StringComparison.OrdinalIgnoreCase);
    }

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

    [Fact]
    public void LegacyMsixData_按旧数据最新修改时间优先并忽略非匹配目录()
    {
        var root = CreateTempDirectory();
        var firstData = Path.Combine(root, "Packages", "CassieWordCheck_a-publisher", "LocalState", "data");
        var secondData = Path.Combine(root, "Packages", "CassieWordCheck_z-publisher", "LocalState", "data");
        var ignoredData = Path.Combine(root, "Packages", "OtherProduct_publisher", "LocalState", "data");
        var destination = Path.Combine(root, "CassieWordCheck", "data");
        var olderWriteTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var newerWriteTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        try
        {
            Directory.CreateDirectory(firstData);
            Directory.CreateDirectory(secondData);
            Directory.CreateDirectory(ignoredData);
            File.WriteAllText(Path.Combine(firstData, "appsettings.json"), "first");
            File.WriteAllText(Path.Combine(secondData, "appsettings.json"), "second");
            File.WriteAllText(Path.Combine(secondData, "history.json"), "history");
            File.WriteAllText(Path.Combine(ignoredData, "history.json"), "ignored");
            File.SetLastWriteTimeUtc(Path.Combine(firstData, "appsettings.json"), olderWriteTime);
            File.SetLastWriteTimeUtc(Path.Combine(secondData, "appsettings.json"), newerWriteTime);
            File.SetLastWriteTimeUtc(Path.Combine(secondData, "history.json"), newerWriteTime);

            var legacyDirectories = AppDataPaths.GetLegacyMsixDataDirectories(root);
            var result = StorageMigrationService.MigrateFromDirectories(legacyDirectories, destination);

            Assert.Equal(new[] { secondData, firstData }, legacyDirectories);
            Assert.DoesNotContain(ignoredData, legacyDirectories);
            Assert.True(result.SettingsMigrated);
            Assert.True(result.HistoryMigrated);
            Assert.Equal("second", File.ReadAllText(Path.Combine(destination, "appsettings.json")));
            Assert.Equal("history", File.ReadAllText(Path.Combine(destination, "history.json")));
            Assert.Equal("first", File.ReadAllText(Path.Combine(firstData, "appsettings.json")));
            Assert.Equal("second", File.ReadAllText(Path.Combine(secondData, "appsettings.json")));
            Assert.Equal("history", File.ReadAllText(Path.Combine(secondData, "history.json")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void LegacyMsixData_Packages目录不存在时返回空集合()
    {
        var root = CreateTempDirectory();

        try
        {
            Assert.Empty(AppDataPaths.GetLegacyMsixDataDirectories(Path.Combine(root, "missing")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void LegacyMsixData_修改时间相同时按路径稳定排序()
    {
        var root = CreateTempDirectory();
        var firstData = Path.Combine(root, "Packages", "CassieWordCheck_a-publisher", "LocalState", "data");
        var secondData = Path.Combine(root, "Packages", "CassieWordCheck_z-publisher", "LocalState", "data");
        var sameWriteTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        try
        {
            Directory.CreateDirectory(firstData);
            Directory.CreateDirectory(secondData);
            File.WriteAllText(Path.Combine(firstData, "appsettings.json"), "first");
            File.WriteAllText(Path.Combine(secondData, "appsettings.json"), "second");
            File.SetLastWriteTimeUtc(Path.Combine(firstData, "appsettings.json"), sameWriteTime);
            File.SetLastWriteTimeUtc(Path.Combine(secondData, "appsettings.json"), sameWriteTime);

            var legacyDirectories = AppDataPaths.GetLegacyMsixDataDirectories(root);

            Assert.Equal(new[] { firstData, secondData }, legacyDirectories);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void LegacyDataDirectories_安装目录始终优先于旧MSIX候选()
    {
        var legacyDirectories = AppDataPaths.GetLegacyDataDirectories();

        Assert.Equal(AppDataPaths.GetLegacyDataDirectory(), legacyDirectories[0]);
    }

    [Fact]
    public void MigrateFromDirectories_目标路径为文件时返回未迁移且不抛出()
    {
        var root = CreateTempDirectory();
        var source = Path.Combine(root, "legacy");
        var destination = Path.Combine(root, "destination");

        try
        {
            Directory.CreateDirectory(source);
            File.WriteAllText(destination, "existing file");
            File.WriteAllText(Path.Combine(source, "appsettings.json"), "legacy settings");
            File.WriteAllText(Path.Combine(source, "history.json"), "legacy history");

            StorageMigrationResult? result = null;
            var exception = Record.Exception(() =>
                result = StorageMigrationService.MigrateFromDirectories([source], destination));

            Assert.Null(exception);
            Assert.NotNull(result);
            Assert.False(result!.SettingsMigrated);
            Assert.False(result.HistoryMigrated);
            Assert.Equal("existing file", File.ReadAllText(destination));
            Assert.Equal("legacy settings", File.ReadAllText(Path.Combine(source, "appsettings.json")));
            Assert.Equal("legacy history", File.ReadAllText(Path.Combine(source, "history.json")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "CassieWordCheckTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
