using ZapretWinwsGui.Core.Process;
using ZapretWinwsGui.Core.Services;
using ZapretWinwsGui.Core.Strategies;
using Xunit;

namespace ZapretWinwsGui.Tests;

public class WindowsServiceManagerTests
{
    [Fact]
    public async Task GetStatusAsync_Running_ReturnsRunning()
    {
        var runner = new MockProcessRunner();
        runner.RunResults["sc.exe query zapret"] = new ProcessRunResult(0, "STATE : 4 RUNNING", "");

        var manager = new WindowsServiceManager(runner);
        var status = await manager.GetStatusAsync();

        Assert.Equal(WindowsServiceStatus.Running, status);
    }

    [Fact]
    public async Task GetStatusAsync_NotInstalled_ReturnsNotInstalled()
    {
        var runner = new MockProcessRunner();
        runner.RunResults["sc.exe query zapret"] = new ProcessRunResult(1060, "", "[SC] EnumQueryServicesStatus:OpenService FAILED 1060");

        var manager = new WindowsServiceManager(runner);
        var status = await manager.GetStatusAsync();

        Assert.Equal(WindowsServiceStatus.NotInstalled, status);
    }

    [Fact]
    public async Task InstallAsync_ExecutesScCreateAndStart()
    {
        var runner = new MockProcessRunner();
        // Initially not installed
        runner.RunResults["sc.exe query zapret"] = new ProcessRunResult(1060, "", "1060");

        var manager = new WindowsServiceManager(runner);
        var strategy = new StrategyModel
        {
            Id = "general",
            Name = "General",
            FileName = "general.bat",
            RawArguments = "--wf-tcp=80,443"
        };

        var ok = await manager.InstallAsync(strategy, @"C:\zapret\bin", startImmediately: true);

        Assert.True(ok);
        Assert.Contains(runner.Executions, e => e.FileName == "sc.exe" && e.Arguments.StartsWith("create zapret"));
        Assert.Contains(runner.Executions, e => e.FileName == "sc.exe" && e.Arguments == "start zapret");
    }

    [Fact]
    public async Task RemoveAsync_ExecutesScDelete()
    {
        var runner = new MockProcessRunner();
        var manager = new WindowsServiceManager(runner);

        var ok = await manager.RemoveAsync();

        Assert.True(ok);
        Assert.Contains(runner.Executions, e => e.FileName == "sc.exe" && e.Arguments == "delete zapret");
    }
}
