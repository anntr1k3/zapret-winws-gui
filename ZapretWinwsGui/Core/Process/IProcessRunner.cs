namespace ZapretWinwsGui.Core.Process;

public record ProcessRunResult(int ExitCode, string StandardOutput, string StandardError);

public interface IProcessRunner
{
    Task<ProcessRunResult> RunAsync(string fileName, string arguments, CancellationToken cancellationToken = default);
    IActiveProcess Start(string fileName, string arguments, string workingDirectory = "");
    bool IsProcessRunning(string processName);
    void KillProcessesByName(string processName);
}

public interface IActiveProcess : IDisposable
{
    int Id { get; }
    bool HasExited { get; }
    event Action<string>? OutputDataReceived;
    event Action<string>? ErrorDataReceived;
    event Action<int>? Exited;
    void Kill();
}
