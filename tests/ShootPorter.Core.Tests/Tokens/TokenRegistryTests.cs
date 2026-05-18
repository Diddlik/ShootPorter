using ShootPorter.Core.Tokens;

namespace ShootPorter.Core.Tests.Tokens;

/// <summary>
/// Tests for <see cref="TokenRegistry"/> token resolution and registration behaviour.
/// </summary>
public sealed class TokenRegistryTests
{
    private static TokenContext CreateContext(
        DateTimeOffset? dateTime = null,
        string fileName = "IMG_0001",
        string extension = ".jpg",
        string? jobCode = null,
        int sequenceNumber = 1) =>
        new(
            dateTime ?? new DateTimeOffset(2025, 6, 15, 14, 30, 45, TimeSpan.Zero),
            fileName,
            extension,
            jobCode,
            sequenceNumber,
            new Dictionary<string, string>());

    [Fact]
    public void WhenResolvingYearTokenThenReturnsFourDigitYear()
    {
        var registry = new TokenRegistry();
        var context = CreateContext(new DateTimeOffset(2025, 6, 15, 14, 30, 45, TimeSpan.Zero));

        var result = registry.Resolve("Y", context);

        Assert.Equal("2025", result);
    }

    [Fact]
    public void WhenResolvingMonthTokenThenReturnsTwoDigitMonth()
    {
        var registry = new TokenRegistry();
        var context = CreateContext(new DateTimeOffset(2025, 6, 15, 14, 30, 45, TimeSpan.Zero));

        var result = registry.Resolve("m", context);

        Assert.Equal("06", result);
    }

    [Fact]
    public void WhenResolvingUnknownTokenThenReturnsNull()
    {
        var registry = new TokenRegistry();
        var context = CreateContext();

        var result = registry.Resolve("NoSuchToken", context);

        Assert.Null(result);
    }

    [Fact]
    public void WhenCheckingRegisteredTokenThenReturnsTrue()
    {
        var registry = new TokenRegistry();

        var result = registry.IsRegistered("Y");

        Assert.True(result);
    }

    [Fact]
    public void WhenCheckingUnregisteredTokenThenReturnsFalse()
    {
        var registry = new TokenRegistry();

        var result = registry.IsRegistered("NoSuchToken");

        Assert.False(result);
    }

    [Fact]
    public void WhenGettingAllDefinitionsThenReturnsBuiltInTokens()
    {
        var registry = new TokenRegistry();
        var expectedNames = new[]
        {
            "Y", "y", "m", "B", "D", "H", "M", "S",
            "F", "e", "o", "r",
            "seq#", "J"
        };

        var definitions = registry.GetAllDefinitions();
        var actualNames = definitions.Select(d => d.Name).ToHashSet();

        foreach (var name in expectedNames)
            Assert.Contains(name, actualNames);

        Assert.True(definitions.Count >= expectedNames.Length);
    }

    [Fact]
    public void WhenResolvingOriginalFilenameThenReturnsFileName()
    {
        var registry = new TokenRegistry();
        var context = CreateContext(fileName: "DSC_7842");

        var result = registry.Resolve("F", context);

        Assert.Equal("DSC_7842", result);
    }

    [Fact]
    public void WhenResolvingExtensionThenReturnsWithoutDot()
    {
        var registry = new TokenRegistry();
        var context = CreateContext(extension: ".cr3");

        var result = registry.Resolve("e", context);

        Assert.Equal("cr3", result);
    }

    [Fact]
    public void WhenResolvingJobCodeThenReturnsJobCode()
    {
        var registry = new TokenRegistry();
        var context = CreateContext(jobCode: "Wedding2025");

        var result = registry.Resolve("J", context);

        Assert.Equal("Wedding2025", result);
    }

    [Fact]
    public void WhenResolvingJobCodeWithNullThenReturnsEmpty()
    {
        var registry = new TokenRegistry();
        var context = CreateContext(jobCode: null);

        var result = registry.Resolve("J", context);

        Assert.Equal(string.Empty, result);
    }
}
