using ShootPorter.Core.Tokens;

namespace ShootPorter.Core.Tests.Tokens;

/// <summary>
/// Tests for <see cref="TokenParser"/> template string parsing and collision handling.
/// </summary>
public sealed class TokenParserTests
{
    private readonly TokenParser _parser = new(new TokenRegistry());

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
    public void WhenParsingSimpleDateTemplateThenSubstitutesTokens()
    {
        var ctx = CreateContext();
        var result = _parser.Parse("{Y}-{m}-{D}", ctx);
        Assert.Equal("2025-06-15", result);
    }

    [Fact]
    public void WhenParsingMultipleTokensThenSubstitutesAll()
    {
        var ctx = CreateContext(jobCode: "Wedding");
        var result = _parser.Parse(@"{Y}\{J}\{F}.{e}", ctx);
        Assert.Equal(@"2025\Wedding\IMG_0001.jpg", result);
    }

    [Fact]
    public void WhenParsingUnknownTokenThenLeavesAsIs()
    {
        var ctx = CreateContext();
        var result = _parser.Parse("{Y}-{Unknown}-{D}", ctx);
        Assert.Equal("2025-{Unknown}-15", result);
    }

    [Fact]
    public void WhenParsingSeqWithFormatThenAppliesFormat()
    {
        var ctx = CreateContext(sequenceNumber: 42);
        var result = _parser.Parse("photo_{seq#4}", ctx);
        Assert.Equal("photo_0042", result);
    }

    [Fact]
    public void WhenParsingEmptyTemplateThenReturnsEmpty()
    {
        var ctx = CreateContext();
        var result = _parser.Parse("", ctx);
        Assert.Equal("", result);
    }

    [Fact]
    public void WhenParsingNoTokensThenReturnsOriginal()
    {
        var ctx = CreateContext();
        var result = _parser.Parse("plain-text-no-tokens", ctx);
        Assert.Equal("plain-text-no-tokens", result);
    }

    [Fact]
    public void WhenMakeUniqueAndPathAvailableThenReturnsOriginal()
    {
        var result = TokenParser.MakeUnique("photo.jpg", _ => false);
        Assert.Equal("photo.jpg", result);
    }

    [Fact]
    public void WhenMakeUniqueAndPathExistsThenAppendsSuffix()
    {
        var result = TokenParser.MakeUnique("photo.jpg", p => p == "photo.jpg");
        Assert.Equal("photo_01.jpg", result);
    }

    [Fact]
    public void WhenMakeUniqueAndMultipleExistThenIncrementsCounter()
    {
        var existing = new HashSet<string> { "photo.jpg", "photo_01.jpg", "photo_02.jpg" };
        var result = TokenParser.MakeUnique("photo.jpg", p => existing.Contains(p));
        Assert.Equal("photo_03.jpg", result);
    }

    [Fact]
    public void WhenParsingStringFunctionLeftThenExtractsLeftChars()
    {
        var ctx = CreateContext();
        var result = _parser.Parse("{left,3,Hello}", ctx);
        Assert.Equal("Hel", result);
    }

    [Fact]
    public void WhenParsingStringFunctionRightThenExtractsRightChars()
    {
        var ctx = CreateContext();
        var result = _parser.Parse("{right,2,Hello}", ctx);
        Assert.Equal("lo", result);
    }

    [Fact]
    public void WhenParsingStringFunctionUpperThenConvertsToUpper()
    {
        var ctx = CreateContext();
        var result = _parser.Parse("{upper,hello}", ctx);
        Assert.Equal("HELLO", result);
    }

    [Fact]
    public void WhenParsingStringFunctionLowerThenConvertsToLower()
    {
        var ctx = CreateContext();
        var result = _parser.Parse("{lower,HELLO}", ctx);
        Assert.Equal("hello", result);
    }

    [Fact]
    public void WhenParsingFileSliceThenExtractsChars()
    {
        var ctx = CreateContext(fileName: "IMG_0123");
        var result = _parser.Parse("{file[0-2]}", ctx);
        Assert.Equal("IMG", result);
    }

    [Fact]
    public void WhenParsingFileSliceFromThenExtractsToEnd()
    {
        var ctx = CreateContext(fileName: "IMG_0123");
        var result = _parser.Parse("{file[4-]}", ctx);
        Assert.Equal("0123", result);
    }
}
