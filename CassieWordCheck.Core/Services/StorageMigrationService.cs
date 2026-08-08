namespace CassieWordCheck.Services;

/// <summary>
/// 将旧安装目录中的可变数据复制到 MSIX LocalFolder 喵
/// </summary>
public static class StorageMigrationService
{
    private static readonly string[] MutableFiles = ["appsettings.json", "history.json"];

    public static StorageMigrationResult Migrate(string legacyDataDirectory, string localDataDirectory)
    {
        Directory.CreateDirectory(localDataDirectory);
        var settingsMigrated = CopyIfMissing(legacyDataDirectory, localDataDirectory, MutableFiles[0]);
        var historyMigrated = CopyIfMissing(legacyDataDirectory, localDataDirectory, MutableFiles[1]);
        return new StorageMigrationResult(settingsMigrated, historyMigrated);
    }

    private static bool CopyIfMissing(string sourceDirectory, string destinationDirectory, string fileName)
    {
        var sourcePath = Path.Combine(sourceDirectory, fileName);
        var destinationPath = Path.Combine(destinationDirectory, fileName);
        if (!File.Exists(sourcePath) || File.Exists(destinationPath))
            return false;

        File.Copy(sourcePath, destinationPath, overwrite: false);
        return true;
    }
}

public sealed record StorageMigrationResult(bool SettingsMigrated, bool HistoryMigrated);
