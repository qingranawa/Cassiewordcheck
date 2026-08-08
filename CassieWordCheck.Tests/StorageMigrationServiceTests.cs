using System.Text.Json;
using CassieWordCheck.Services;

namespace CassieWordCheck.Tests;

public class StorageMigrationServiceTests
{
    [Fact]
    public void Migrate_LegacyFiles_CopiesOnlyMissingFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "CassieWordCheckTests", Guid.NewGuid().ToString("N"));
        var legacy = Path.Combine(root, "legacy");
        var local = Path.Combine(root, "local");
        Directory.CreateDirectory(legacy);
        Directory.CreateDirectory(local);
        File.WriteAllText(Path.Combine(legacy, "appsettings.json"), "{\"Language\":\"ja-JP\"}");
        File.WriteAllText(Path.Combine(legacy, "history.json"), "[]");
        File.WriteAllText(Path.Combine(local, "history.json"), "[\"new\"]");

        try
        {
            var result = StorageMigrationService.Migrate(legacy, local);

            Assert.True(result.SettingsMigrated);
            Assert.False(result.HistoryMigrated);
            Assert.Equal("{\"Language\":\"ja-JP\"}", File.ReadAllText(Path.Combine(local, "appsettings.json")));
            Assert.Equal("[\"new\"]", File.ReadAllText(Path.Combine(local, "history.json")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Migrate_IsIdempotent_WhenDestinationFilesAlreadyExist()
    {
        var root = Path.Combine(Path.GetTempPath(), "CassieWordCheckTests", Guid.NewGuid().ToString("N"));
        var legacy = Path.Combine(root, "legacy");
        var local = Path.Combine(root, "local");
        Directory.CreateDirectory(legacy);
        File.WriteAllText(Path.Combine(legacy, "appsettings.json"), JsonSerializer.Serialize(new { Language = "de-DE" }));

        try
        {
            var first = StorageMigrationService.Migrate(legacy, local);
            var second = StorageMigrationService.Migrate(legacy, local);

            Assert.True(first.SettingsMigrated);
            Assert.False(second.SettingsMigrated);
            Assert.Equal("{\"Language\":\"de-DE\"}", File.ReadAllText(Path.Combine(local, "appsettings.json")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
