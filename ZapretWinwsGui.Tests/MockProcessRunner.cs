using ZapretWinwsGui.Core.Process;

namespace ZapretWinwsGui.Tests;

public class MockProcessRunner : IProcessRunner
{
    public bool IsAdministrator { get; set; } = true;
    public List<(string FileName, string Arguments)> Executions { get; } = [];
    public HashSet<string> RunningProcessNames { get; } = [];
    public Dictionary<string, ProcessRunResult> RunResults { get; } = [];
    public List<string> KilledProcessNames { get; } = [];

    public Task<ProcessRunResult> RunAsync(string fileName, string arguments, CancellationToken cancellationToken = default)
    {
        Executions.Add((fileName, arguments));
        var key = $"{fileName} {arguments}".Trim();
        if (RunResults.TryGetValue(key, out var result))
            return Task.FromResult(result);

        // Fallback default success
        return Task.FromResult(new ProcessRunResult(0, "SUCCESS", ""));
    }

    public IActiveProcess Start(string fileName, string arguments, string workingDirectory = "")
    {
        Executions.Add((fileName, arguments));
        return new MockActiveProcess();
    }

    public bool IsProcessRunning(string processName)
    {
        return RunningProcessNames.Contains(processName);
    }

    public void KillProcessesByName(string processName)
    {
        KilledProcessNames.Add(processName);
        RunningProcessNames.Remove(processName);
    }

    public sealed class MockActiveProcess : IActiveProcess
    {
        public int Id { get; set; } = 1234;
        public bool HasExited { get; set; }

        public event Action<string>? OutputDataReceived;
        public event Action<string>? ErrorDataReceived;
        public event Action<int>? Exited;

        public void EmitOutput(string text) => OutputDataReceived?.Invoke(text);
        public void EmitError(string text) => ErrorDataReceived?.Invoke(text);
        public void EmitExit(int code)
        {
            HasExited = true;
            Exited?.Invoke(code);
        }

        public void Kill() => HasExited = true;
        public void Dispose() { }
    }
}
