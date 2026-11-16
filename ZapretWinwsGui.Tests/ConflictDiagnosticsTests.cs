using ZapretWinwsGui.Core.Diagnostics;
using Xunit;

namespace ZapretWinwsGui.Tests;

public class ConflictDiagnosticsTests
{
    [Fact]
    public async Task RunDiagnosticsAsync_ConflictingAppRunning_ReturnsWarning()
    {
        var runner = new MockProcessRunner();
        runner.RunningProcessNames.Add("goodbyedpi");

        var diagnostics = new ConflictDiagnostics(runner);
        var issues = await diagnostics.RunDiagnosticsAsync();

        Assert.NotEmpty(issues);
        Assert.Contains(issues, i => i.Severity == DiagnosticSeverity.Warning && i.Title.Contains("GoodbyeDPI"));
    }

    [Fact]
    public async Task RunDiagnosticsAsync_NoConflicts_ReturnsInfo()
    {
        var runner = new MockProcessRunner();

        var diagnostics = new ConflictDiagnostics(runner);
        var issues = await diagnostics.RunDiagnosticsAsync();

        Assert.Single(issues);
        Assert.Equal(DiagnosticSeverity.Info, issues[0].Severity);
        Assert.Contains("Конфликтов не обнаружено", issues[0].Title);
    }

    [Fact]
    public async Task RunDiagnosticsAsync_NotAdmin_ReturnsError()
    {
        var runner = new MockProcessRunner { IsAdministrator = false };

        var diagnostics = new ConflictDiagnostics(runner);
        var issues = await diagnostics.RunDiagnosticsAsync();

        Assert.Contains(issues, i => i.Severity == DiagnosticSeverity.Error && i.Title.Contains("Отсутствуют права"));
    }
}
