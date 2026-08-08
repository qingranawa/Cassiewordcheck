namespace CassieWordCheck.Services;

/// <summary>
/// 将旧安装目录或旧 MSIX 数据目录中的可变数据复制到每用户 LocalAppData 数据目录喵
/// </summary>
public static class StorageMigrationService
{
    private static readonly string[] MutableFiles = ["appsettings.json", "history.json"];

    public static StorageMigrationResult Migrate(string legacyDataDirectory, string localDataDirectory) =>
        MigrateFromDirectories([legacyDataDirectory], localDataDirectory);

    /// <summary>
    /// 按给定顺序从多个旧目录复制尚不存在的可变文件喵
    /// </summary>
    public static StorageMigrationResult MigrateFromDirectories(
        IEnumerable<string> legacyDataDirectories,
        string localDataDirectory)
    {
        ArgumentNullException.ThrowIfNull(legacyDataDirectories);
        try
        {
            Directory.CreateDirectory(localDataDirectory);
        }
        catch (IOException)
        {
            return new StorageMigrationResult(false, false);
        }
        catch (UnauthorizedAccessException)
        {
            return new StorageMigrationResult(false, false);
        }

        var settingsMigrated = false;
        var historyMigrated = false;
        foreach (var legacyDataDirectory in legacyDataDirectories)
        {
            if (string.IsNullOrWhiteSpace(legacyDataDirectory))
                continue;

            settingsMigrated |= CopyIfMissing(legacyDataDirectory, localDataDirectory, MutableFiles[0]);
            historyMigrated |= CopyIfMissing(legacyDataDirectory, localDataDirectory, MutableFiles[1]);
            if (settingsMigrated && historyMigrated)
                break;
        }

        return new StorageMigrationResult(settingsMigrated, historyMigrated);
    }

    private static bool CopyIfMissing(string sourceDirectory, string destinationDirectory, string fileName)
    {
        var sourcePath = Path.Combine(sourceDirectory, fileName);
        var destinationPath = Path.Combine(destinationDirectory, fileName);
        if (!File.Exists(sourcePath) || File.Exists(destinationPath))
            return false;

        try
        {
            File.Copy(sourcePath, destinationPath, overwrite: false);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}

public sealed record StorageMigrationResult(bool SettingsMigrated, bool HistoryMigrated);
