using ShootPorter.Core.Cli;

namespace ShootPorter.Core.Tests.Cli;

/// <summary>
/// Tests for <see cref="CliParser"/> argument parsing.
/// </summary>
public sealed class CliParserTests
{
    private readonly CliParser _parser = new();

    [Fact]
    public void WhenParsingSourceArgThenSetsSourcePath()
    {
        var options = _parser.Parse(["--source", "/mnt/card"]);

        Assert.Equal("/mnt/card", options.SourcePath);
    }

    [Fact]
    public void WhenParsingMultipleArgsThenSetsAll()
    {
        var options = _parser.Parse([
            "--source", "/mnt/card",
            "--dest", "/photos",
            "--profile", "default",
            "--jobcode", "JOB001",
            "--parallel", "4",
            "--auto-delete",
            "--headless",
        ]);

        Assert.Equal("/mnt/card", options.SourcePath);
        Assert.Equal("/photos", options.DestinationRoot);
        Assert.Equal("default", options.ProfileName);
        Assert.Equal("JOB001", options.JobCode);
        Assert.Equal(4, options.MaxParallelism);
        Assert.True(options.AutoDeleteSource);
        Assert.True(options.Headless);
    }

    [Fact]
    public void WhenParsingHelpFlagThenShowHelpIsTrue()
    {
        var options = _parser.Parse(["--help"]);

        Assert.True(options.ShowHelp);
    }

    [Theory]
    [InlineData("-s", "/src", "-d", "/dst", "-p", "myprofile", "-j", "ABC", "-h")]
    public void WhenParsingShortFlagsThenWorks(
        string sFlag, string src, string dFlag, string dst,
        string pFlag, string profile, string jFlag, string job,
        string hFlag)
    {
        var options = _parser.Parse([sFlag, src, dFlag, dst, pFlag, profile, jFlag, job, hFlag]);

        Assert.Equal(src, options.SourcePath);
        Assert.Equal(dst, options.DestinationRoot);
        Assert.Equal(profile, options.ProfileName);
        Assert.Equal(job, options.JobCode);
        Assert.True(options.ShowHelp);
    }

    [Fact]
    public void WhenParsingNoArgsThenReturnsDefaults()
    {
        var options = _parser.Parse([]);

        Assert.Null(options.SourcePath);
        Assert.Null(options.DestinationRoot);
        Assert.Null(options.ProfileName);
        Assert.Null(options.JobCode);
        Assert.True(options.Recursive);
        Assert.Equal(2, options.MaxParallelism);
        Assert.True(options.VerifyAfterCopy);
        Assert.False(options.AutoDeleteSource);
        Assert.False(options.ShowHelp);
        Assert.False(options.Headless);
    }

    [Fact]
    public void WhenParsingNoVerifyThenVerifyIsFalse()
    {
        var options = _parser.Parse(["--no-verify"]);

        Assert.False(options.VerifyAfterCopy);
    }

    [Fact]
    public void WhenParsingNoRecursiveThenRecursiveIsFalse()
    {
        var options = _parser.Parse(["--no-recursive"]);

        Assert.False(options.Recursive);
    }

    [Fact]
    public void WhenGettingHelpTextThenReturnsNonEmpty()
    {
        var text = CliParser.GetHelpText();

        Assert.False(string.IsNullOrWhiteSpace(text));
    }
}
