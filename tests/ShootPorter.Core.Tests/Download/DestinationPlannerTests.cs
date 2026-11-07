using ShootPorter.Core.Download;
using ShootPorter.Core.Tokens;

namespace ShootPorter.Core.Tests.Download;

/// <summary>
/// Tests for <see cref="DestinationPlanner"/>.
/// </summary>
public sealed class DestinationPlannerTests
{
    private static readonly DateTimeOffset Date = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);

    private static TokenContext Context(string path) =>
        TokenContext.ForFile(path, null, Date, null, 1, new Dictionary<string, string>());

    [Fact]
    public void WhenTwoFilesResolveToTheSamePathIgnoringCaseThenBothAreClashes()
    {
        var planned = DestinationPlanner.Plan(new TokenParser(new TokenRegistry()), "{Y}{m}{D}", "root",
            [Context("a/IMG_1.jpg"), Context("b/IMG_2.JPG"), Context("c/IMG_3.cr3")]);

        Assert.Equal(
            [(Path.Combine("root", "20261107.jpg"), true), (Path.Combine("root", "20261107.JPG"), true), (Path.Combine("root", "20261107.cr3"), false)],
            planned.Select(p => (p.Path, p.IsClash)));
    }
}
