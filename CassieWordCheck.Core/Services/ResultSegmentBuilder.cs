using CassieWordCheck.Models;

namespace CassieWordCheck.Services;

/// <summary>
/// 将检查结果转换为 UI 无关的片段集合喵
/// </summary>
public static class ResultSegmentBuilder
{
    public static IReadOnlyList<ResultSegment> Build(IReadOnlyList<CheckResult> results)
    {
        return results
            .Select(result => new ResultSegment(
                result.Text,
                result.Status,
                result.Status == CheckStatus.Unavailable,
                result.Status == CheckStatus.Unavailable ? result.Text : null))
            .ToArray();
    }
}
