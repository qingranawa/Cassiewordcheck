using System.Text.Json;
using CassieWordCheck.Services;

namespace CassieWordCheck.Tests;

public class ProjectIntegrityValidatorTests
{
    [Fact]
    public void Validate_当前项目资源完整()
    {
        var report = ProjectIntegrityValidator.Validate(FindProjectRoot());

        Assert.True(report.IsValid, string.Join(Environment.NewLine,
            report.Issues.Select(issue => $"{issue.File}: {issue.Message}")));
    }

    [Fact]
    public void Validate_发现缺少本地化键()
    {
        var root = CreateProjectFixture("hello");
        var localeDir = Path.Combine(root, "Resources", "Locales");
        File.WriteAllText(Path.Combine(localeDir, "en-US.json"),
            JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["app.title"] = "Title",
                ["menu.copy"] = "Copy",
            }));
        File.WriteAllText(Path.Combine(localeDir, "zh-CN.json"),
            JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["app.title"] = "标题",
            }));

        var report = ProjectIntegrityValidator.Validate(root);

        Assert.Contains(report.Issues, issue =>
            issue.File.EndsWith("zh-CN.json", StringComparison.Ordinal) &&
            issue.Message.Contains("menu.copy", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_拒绝空词库()
    {
        var root = CreateProjectFixture(string.Empty);
        var localeDir = Path.Combine(root, "Resources", "Locales");
        var locale = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["app.title"] = "Title",
        });
        File.WriteAllText(Path.Combine(localeDir, "en-US.json"), locale);
        File.WriteAllText(Path.Combine(localeDir, "zh-CN.json"), locale);

        var report = ProjectIntegrityValidator.Validate(root);

        Assert.Contains(report.Issues, issue => issue.File == "data/cassie-text.txt");
    }

    private static string FindProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CassieWordCheck.csproj")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the project root.");
    }

    private static string CreateProjectFixture(string wordlist)
    {
        var root = Path.Combine(Path.GetTempPath(), "CassieWordCheckTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "data"));
        Directory.CreateDirectory(Path.Combine(root, "Resources", "Locales"));
        File.WriteAllText(Path.Combine(root, "data", "cassie-text.txt"), wordlist);
        return root;
    }
}
