namespace CassieWordCheck.Models;

/// <summary>
/// 应用数据路径管理——只读资源留在安装目录，用户数据写入 LocalAppData。
/// </summary>
public static class AppDataPaths
{
    public const string ProductDirectoryName = "CassieWordCheck";

    public static string GetUserDataDirectory()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(root))
            root = Path.GetTempPath();

        return Path.Combine(root, ProductDirectoryName);
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
