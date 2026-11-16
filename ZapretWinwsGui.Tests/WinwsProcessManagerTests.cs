using ZapretWinwsGui.Core.Process;
using ZapretWinwsGui.Core.Strategies;
using Xunit;

namespace ZapretWinwsGui.Tests;

public class WinwsProcessManagerTests
{
    [Fact]
    public void Start_ChangesStateToRunning_AndCallsRunner()
    {
        var runner = new MockProcessRunner();
        var manager = new WinwsProcessManager(runner);

        var strategy = new StrategyModel
        {
            Id = "general",
            Name = "General",
            FileName = "general.bat",
            RawArguments = "--wf-tcp=80,443 --dpi-desync=fake"
        };

        var stateChanges = new List<ProcessState>();
        manager.StateChanged += stateChanges.Add;

        manager.Start(strategy, @"C:\zapret\bin");

        Assert.True(manager.IsRunning);
        Assert.Equal(ProcessState.Running, manager.State);
        Assert.Equal(strategy, manager.ActiveStrategy);
        Assert.NotNull(manager.StartedAt);
        Assert.Contains(ProcessState.Starting, stateChanges);
        Assert.Contains(ProcessState.Running, stateChanges);
        Assert.NotEmpty(runner.Executions);
    }

    [Fact]
    public void Stop_ChangesStateToStopped()
    {
        var runner = new MockProcessRunner();
        var manager = new WinwsProcessManager(runner);

        var strategy = new StrategyModel
        {
            Id = "general",
            Name = "General",
            FileName = "general.bat",
            RawArguments = "--wf-tcp=80,443"
        };

        manager.Start(strategy, @"C:\zapret\bin");
        Assert.True(manager.IsRunning);

        manager.Stop();

        Assert.False(manager.IsRunning);
        Assert.Equal(ProcessState.Stopped, manager.State);
        Assert.Null(manager.ActiveStrategy);
    }
}
