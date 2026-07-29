using System.Text.Json;

namespace CassieWordCheck.Services;

public sealed record IntegrityIssue(string File, string Message);

public sealed class IntegrityReport
{
    public IntegrityReport(IReadOnlyList<IntegrityIssue> issues) => Issues = issues;

    public IReadOnlyList<IntegrityIssue> Issues { get; }
    public bool IsValid => Issues.Count == 0;
}

/// <summary>
/// 检查仓库中随应用发布的词库和本地化资源是否完整。
/// </summary>
public static class ProjectIntegrityValidator
{
    public static IntegrityReport Validate(string projectRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        var issues = new List<IntegrityIssue>();

        ValidateWordlist(projectRoot, issues);
        ValidateLocales(projectRoot, issues);

        return new IntegrityReport(issues);
    }

    private static void ValidateWordlist(string projectRoot, ICollection<IntegrityIssue> issues)
    {
        const string relativePath = "data/cassie-text.txt";
        var path = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            issues.Add(new IntegrityIssue(relativePath, "词库文件不存在。"));
            return;
        }

        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;

            var wordPart = trimmed.Contains(':') && !trimmed.StartsWith('.')
                ? trimmed.Split(':', 2)[0].Trim()
                : trimmed;

            foreach (var word in wordPart.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                words.Add(word);
        }

        if (words.Count == 0)
            issues.Add(new IntegrityIssue(relativePath, "词库中没有可用单词。"));
    }

    private static void ValidateLocales(string projectRoot, ICollection<IntegrityIssue> issues)
    {
        const string relativeDirectory = "Resources/Locales";
        var directory = Path.Combine(projectRoot,
            relativeDirectory.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(directory))
        {
            issues.Add(new IntegrityIssue(relativeDirectory, "本地化目录不存在。"));
            return;
        }

        var files = Directory.GetFiles(directory, "*.json").OrderBy(path => path).ToArray();
        if (files.Length == 0)
        {
            issues.Add(new IntegrityIssue(relativeDirectory, "没有找到本地化 JSON 文件。"));
            return;
        }

        var locales = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in files)
        {
            var relativePath = Path.GetRelativePath(projectRoot, path).Replace(Path.DirectorySeparatorChar, '/');
            try
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
                if (data is null)
                {
                    issues.Add(new IntegrityIssue(relativePath, "JSON 内容为空。"));
                    continue;
                }

                foreach (var (key, value) in data)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        issues.Add(new IntegrityIssue(relativePath, "存在空的本地化 key。"));
                    if (string.IsNullOrWhiteSpace(value))
                        issues.Add(new IntegrityIssue(relativePath, $"key '{key}' 的翻译为空。"));
                }

                var languageCode = Path.GetFileNameWithoutExtension(path);
                var descriptorKey = $"_{languageCode}";
                if (!data.ContainsKey(descriptorKey))
                    issues.Add(new IntegrityIssue(relativePath, $"缺少语言自描述 key '{descriptorKey}'。"));

                locales[relativePath] = data;
            }
            catch (JsonException ex)
            {
                issues.Add(new IntegrityIssue(relativePath, $"JSON 无法解析：{ex.Message}"));
            }
        }

        if (locales.Count == 0)
            return;

        var requiredKeys = locales.Values
            .SelectMany(data => data.Keys)
            .Where(key => !key.StartsWith('_'))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var (file, data) in locales)
        {
            foreach (var missingKey in requiredKeys.Except(data.Keys).OrderBy(key => key))
                issues.Add(new IntegrityIssue(file, $"缺少本地化 key '{missingKey}'。"));
        }
    }
}
