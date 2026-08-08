using CassieWordCheck.Models;
using CassieWordCheck.Services;

namespace CassieWordCheck.Tests;

public class ResultSegmentBuilderTests
{
    [Fact]
    public void Build_MixedStatuses_PreservesTextAndInteractiveFlag()
    {
        var results = new List<CheckResult>
        {
            new("hello", CheckStatus.Available),
            new("badword", CheckStatus.Unavailable),
            new(" ", CheckStatus.Separator),
        };

        var segments = ResultSegmentBuilder.Build(results);

        Assert.Equal(["hello", "badword", " "], segments.Select(segment => segment.Text));
        Assert.False(segments[0].IsInteractive);
        Assert.True(segments[1].IsInteractive);
        Assert.Equal(CheckStatus.Separator, segments[2].Status);
    }
}
