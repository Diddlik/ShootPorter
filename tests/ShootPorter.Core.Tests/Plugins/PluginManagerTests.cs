using ShootPorter.Core.Plugins;

namespace ShootPorter.Core.Tests.Plugins;

/// <summary>
/// Tests for <see cref="PluginManager"/> registration, ordering, and execution.
/// </summary>
public sealed class PluginManagerTests
{
    private sealed class TestPlugin(string name, int order, bool result = true) : IPostDownloadPlugin
    {
        public string Name => name;
        public int Order => order;
        public bool IsEnabled { get; set; } = true;
        public bool WasExecuted { get; private set; }

        public Task<bool> ProcessAsync(string filePath, CancellationToken cancellationToken = default)
        {
            WasExecuted = true;
            return Task.FromResult(result);
        }
    }

    private sealed class ThrowingPlugin : IPostDownloadPlugin
    {
        public string Name => "Thrower";
        public int Order => 50;
        public bool IsEnabled { get; set; } = true;

        public Task<bool> ProcessAsync(string filePath, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("test failure");
    }

    [Fact]
    public void WhenRegisteringPluginsThenSortedByOrder()
    {
        var manager = new PluginManager();
        var high = new TestPlugin("High", order: 100);
        var low = new TestPlugin("Low", order: 5);
        var mid = new TestPlugin("Mid", order: 50);

        manager.Register(high);
        manager.Register(low);
        manager.Register(mid);

        Assert.Equal(3, manager.Plugins.Count);
        Assert.Equal("Low", manager.Plugins[0].Name);
        Assert.Equal("Mid", manager.Plugins[1].Name);
        Assert.Equal("High", manager.Plugins[2].Name);
    }

    [Fact]
    public async Task WhenRunningAllThenOnlyEnabledPluginsExecute()
    {
        var manager = new PluginManager();
        var enabled = new TestPlugin("Enabled", order: 10);
        var disabled = new TestPlugin("Disabled", order: 20) { IsEnabled = false };

        manager.Register(enabled);
        manager.Register(disabled);

        var results = await manager.RunAllAsync("any.jpg");

        Assert.Single(results);
        Assert.Equal("Enabled", results[0].PluginName);
        Assert.True(results[0].Success);
        Assert.True(enabled.WasExecuted);
        Assert.False(disabled.WasExecuted);
    }

    [Fact]
    public async Task WhenPluginThrowsThenCapturedAsFailed()
    {
        var manager = new PluginManager();
        manager.Register(new ThrowingPlugin());

        var results = await manager.RunAllAsync("any.jpg");

        Assert.Single(results);
        Assert.Equal("Thrower", results[0].PluginName);
        Assert.False(results[0].Success);
    }

    [Fact]
    public void WhenRemovingPluginThenNoLongerInList()
    {
        var manager = new PluginManager();
        manager.Register(new TestPlugin("Alpha", order: 10));
        manager.Register(new TestPlugin("Beta", order: 20));

        manager.Remove("Alpha");

        Assert.Single(manager.Plugins);
        Assert.Equal("Beta", manager.Plugins[0].Name);
    }
}
