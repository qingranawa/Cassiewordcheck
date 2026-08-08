using Windows.Storage;

namespace CassieWordCheck.Services;

/// <summary>
/// 统一管理 WinUI 应用的可写数据目录和旧版数据目录喵
/// </summary>
public sealed class WinUiStoragePathProvider
{
    public string LocalDataDirectory => Path.Combine(ApplicationData.Current.LocalFolder.Path, "data");

    public string LegacyDataDirectory => Path.Combine(AppContext.BaseDirectory, "data");

    public string LocaleDirectory => Path.Combine(AppContext.BaseDirectory, "Resources", "Locales");

    public StorageMigrationResult MigrateLegacyData()
    {
        return StorageMigrationService.Migrate(LegacyDataDirectory, LocalDataDirectory);
    }
}
