using ZapretWinwsGui.Core.Strategies;

namespace ZapretWinwsGui.Core.Process;

public enum ProcessState
{
    Stopped,
    Starting,
    Running,
    Failed
}

public class WinwsProcessManager
{
    private readonly IProcessRunner _runner;
    private IActiveProcess? _currentProcess;

    public ProcessState State { get; private set; } = ProcessState.Stopped;
    public StrategyModel? ActiveStrategy { get; private set; }
    public DateTime? StartedAt { get; private set; }

    public event Action<ProcessState>? StateChanged;
    public event Action<string>? LogReceived;

    public WinwsProcessManager(IProcessRunner? runner = null)
    {
        _runner = runner ?? new SystemProcessRunner();
    }

    public IProcessRunner Runner => _runner;

    public bool IsRunning => State == ProcessState.Running && _currentProcess is { HasExited: false };

    public static string ResolveWinwsPath(string binDirectory)
    {
        var inBin = Path.Combine(binDirectory, "bin", "winws.exe");
        if (File.Exists(inBin))
            return inBin;

        var inRoot = Path.Combine(binDirectory, "winws.exe");
        if (File.Exists(inRoot))
            return inRoot;

        return inBin;
    }

    public void Start(StrategyModel strategy, string binDirectory)
    {
        if (IsRunning)
        {
            Stop();
        }

        State = ProcessState.Starting;
        StateChanged?.Invoke(State);

        var winwsPath = ResolveWinwsPath(binDirectory);
        var workingDir = Path.GetDirectoryName(winwsPath);
        if (string.IsNullOrEmpty(workingDir) || !Directory.Exists(workingDir))
            workingDir = binDirectory;

        LogReceived?.Invoke($"[MANAGER] Запуск winws: {strategy.Name}");
        LogReceived?.Invoke($"[MANAGER] Путь: {winwsPath}");
        LogReceived?.Invoke($"[MANAGER] Аргументы: {strategy.RawArguments}");

        try
        {
            // Kill any stray or orphaned winws instances before starting
            _runner.KillProcessesByName("winws");

            _currentProcess = _runner.Start(winwsPath, strategy.RawArguments, workingDir);
            ActiveStrategy = strategy;
            StartedAt = DateTime.UtcNow;
            State = ProcessState.Running;
            StateChanged?.Invoke(State);

            _currentProcess.OutputDataReceived += line => LogReceived?.Invoke($"[WINWS] {line}");
            _currentProcess.ErrorDataReceived += line => LogReceived?.Invoke($"[WINWS-ERR] {line}");
            _currentProcess.Exited += exitCode =>
            {
                LogReceived?.Invoke($"[MANAGER] Процесс winws завершился с кодом {exitCode}");
                State = exitCode == 0 ? ProcessState.Stopped : ProcessState.Failed;
                _currentProcess?.Dispose();
                _currentProcess = null;
                ActiveStrategy = null;
                StartedAt = null;
                StateChanged?.Invoke(State);
            };
        }
        catch (Exception ex)
        {
            LogReceived?.Invoke($"[ERROR] Ошибка запуска winws: {ex.Message}");
            State = ProcessState.Failed;
            StateChanged?.Invoke(State);
            throw;
        }
    }

    public void Stop()
    {
        LogReceived?.Invoke("[MANAGER] Остановка winws...");
        if (_currentProcess != null)
        {
            _currentProcess.Kill();
            _currentProcess.Dispose();
            _currentProcess = null;
        }

        _runner.KillProcessesByName("winws");
        ActiveStrategy = null;
        StartedAt = null;
        State = ProcessState.Stopped;
        StateChanged?.Invoke(State);
        LogReceived?.Invoke("[MANAGER] Процесс winws остановлен.");
    }
}
