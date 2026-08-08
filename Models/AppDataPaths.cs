namespace CassieWordCheck.Models;

/// <summary>
/// 应用数据路径管理——只读资源留在安装目录，用户数据写入 LocalAppData。
/// </summary>
public static class AppDataPaths
{
    public const string ProductDirectoryName = "CassieWordCheck";

    public static string GetUserDataDirectory()
    {
        return Path.Combine(GetLocalApplicationDataDirectory(), ProductDirectoryName);
    }

    public static string GetUserLocalDataDirectory() =>
        Path.Combine(GetUserDataDirectory(), "data");

    public static string GetUserFilePath(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        if (!string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal))
            throw new ArgumentException("Only a file name is allowed.", nameof(fileName));

        var userDirectory = GetUserDataDirectory();
        MigrateLegacyFile(fileName, userDirectory, GetLegacyDataDirectory());
        return Path.Combine(userDirectory, fileName);
    }

    public static string GetLegacyDataDirectory() =>
        Path.Combine(AppContext.BaseDirectory, "data");

    /// <summary>
    /// 按安装目录优先、旧 MSIX 目录其次返回所有旧数据目录喵
    /// </summary>
    public static IReadOnlyList<string> GetLegacyDataDirectories() =>
        [GetLegacyDataDirectory(), .. GetLegacyMsixDataDirectories()];

    /// <summary>
    /// 查找旧 MSIX 在 LocalState 下保存的数据目录喵
    /// </summary>
    public static IReadOnlyList<string> GetLegacyMsixDataDirectories(string? localAppDataRoot = null)
    {
        var root = string.IsNullOrWhiteSpace(localAppDataRoot)
            ? GetLocalApplicationDataDirectory()
            : localAppDataRoot!;

        try
        {
            var packagesDirectory = Path.Combine(root, "Packages");
            return Directory.GetDirectories(
                    packagesDirectory,
                    $"{ProductDirectoryName}_*",
                    SearchOption.TopDirectoryOnly)
                .Select(directory => Path.Combine(directory, "LocalState", "data"))
                .Where(Directory.Exists)
                .OrderByDescending(GetLatestLegacyDataFileWriteTimeUtc)
                .ThenBy(directory => directory, StringComparer.OrdinalIgnoreCase)
                .ThenBy(directory => directory, StringComparer.Ordinal)
                .ToArray();
        }
        catch (UnauthorizedAccessException)
        {
            return Array.Empty<string>();
        }
        catch (IOException)
        {
            return Array.Empty<string>();
        }
    }

    private static DateTime GetLatestLegacyDataFileWriteTimeUtc(string dataDirectory)
    {
        try
        {
            var latestWriteTime = DateTime.MinValue;
            foreach (var filePath in Directory.EnumerateFiles(dataDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                latestWriteTime = Max(latestWriteTime, File.GetLastWriteTimeUtc(filePath));
            }

            return latestWriteTime;
        }
        catch (UnauthorizedAccessException)
        {
            return DateTime.MinValue;
        }
        catch (IOException)
        {
            return DateTime.MinValue;
        }
    }

    private static DateTime Max(DateTime first, DateTime second) =>
        first >= second ? first : second;

    private static string GetLocalApplicationDataDirectory()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return string.IsNullOrWhiteSpace(root) ? Path.GetTempPath() : root;
    }

    /// <summary>
    /// 将旧安装目录中的用户文件复制到新目录。
    /// 目标已存在或旧文件不存在时不执行覆盖/删除操作。
    /// </summary>
    public static bool MigrateLegacyFile(
        string fileName,
        string destinationDirectory,
        string legacyDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        var destination = Path.Combine(destinationDirectory, fileName);
        var legacy = Path.Combine(legacyDirectory, fileName);

        if (File.Exists(destination) || !File.Exists(legacy))
            return false;

        try
        {
            Directory.CreateDirectory(destinationDirectory);
            File.Copy(legacy, destination);
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
