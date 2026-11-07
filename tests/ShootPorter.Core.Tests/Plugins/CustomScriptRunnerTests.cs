using ShootPorter.Core.Plugins;

namespace ShootPorter.Core.Tests.Plugins;

/// <summary>
/// Tests for <see cref="CustomScriptRunner"/>.
/// </summary>
public sealed class CustomScriptRunnerTests
{
    private static readonly string FilePath = Path.Combine("dest", "2026", "IMG 1.jpg");

    [Fact]
    public void WhenScriptIsExecutableThenItIsStartedDirectlyWithFileAndFolder()
    {
        var startInfo = CustomScriptRunner.CreateStartInfo("action.cmd", FilePath);

        Assert.Equal("action.cmd", startInfo.FileName);
        Assert.Equal([FilePath, Path.Combine("dest", "2026")], startInfo.ArgumentList);
        Assert.False(startInfo.RedirectStandardOutput);
    }

    [Fact]
    public void WhenScriptIsPowerShellThenItRunsThroughPowerShellFile()
    {
        var startInfo = CustomScriptRunner.CreateStartInfo("action.PS1", FilePath);

        Assert.Equal(OperatingSystem.IsWindows() ? "powershell.exe" : "pwsh", startInfo.FileName);
        Assert.Equal(["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "action.PS1", FilePath, Path.Combine("dest", "2026")],
            startInfo.ArgumentList);
    }
}
