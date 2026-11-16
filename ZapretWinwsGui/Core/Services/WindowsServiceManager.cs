using ZapretWinwsGui.Core.Process;
using ZapretWinwsGui.Core.Strategies;

namespace ZapretWinwsGui.Core.Services;

public enum WindowsServiceStatus
{
    NotInstalled,
    Stopped,
    Running,
    StartPending,
    StopPending,
    Unknown
}

public class WindowsServiceManager
{
    public const string ServiceName = "zapret";
    public const string ServiceDisplayName = "Zapret DPI Bypass Service";

    private readonly IProcessRunner _runner;

    public event Action<string>? LogReceived;

    public WindowsServiceManager(IProcessRunner? runner = null)
    {
        _runner = runner ?? new SystemProcessRunner();
    }

    public async Task<WindowsServiceStatus> GetStatusAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _runner.RunAsync("sc.exe", $"query {ServiceName}", ct);
            if (result.ExitCode != 0)
            {
                if (result.StandardOutput.Contains("1060") || result.StandardError.Contains("1060") ||
                    result.StandardOutput.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
                {
                    return WindowsServiceStatus.NotInstalled;
                }
            }

            var output = result.StandardOutput;
            if (output.Contains("RUNNING", StringComparison.OrdinalIgnoreCase))
                return WindowsServiceStatus.Running;
            if (output.Contains("STOPPED", StringComparison.OrdinalIgnoreCase))
                return WindowsServiceStatus.Stopped;
            if (output.Contains("START_PENDING", StringComparison.OrdinalIgnoreCase))
                return WindowsServiceStatus.StartPending;
            if (output.Contains("STOP_PENDING", StringComparison.OrdinalIgnoreCase))
                return WindowsServiceStatus.StopPending;

            return WindowsServiceStatus.Unknown;
        }
        catch (Exception ex)
        {
            LogReceived?.Invoke($"[SERVICE] Ошибка запроса статуса службы: {ex.Message}");
            return WindowsServiceStatus.Unknown;
        }
    }

    public async Task<bool> InstallAsync(StrategyModel strategy, string binDirectory, bool startImmediately = true, CancellationToken ct = default)
    {
        var winwsPath = WinwsProcessManager.ResolveWinwsPath(binDirectory);
        LogReceived?.Invoke($"[SERVICE] Установка системной службы '{ServiceName}'...");

        // Remove old service if already exists
        var currentStatus = await GetStatusAsync(ct);
        if (currentStatus != WindowsServiceStatus.NotInstalled)
        {
            await RemoveAsync(ct);
        }

        // Winws service binPath syntax: sc create <name> binPath= "\"<exe>\" <args>" start= auto
        var escapedBinPath = $"\"\\\"{winwsPath}\\\" {strategy.RawArguments}\"";
        var createArgs = $"create {ServiceName} binPath= {escapedBinPath} start= auto DisplayName= \"{ServiceDisplayName}\"";

        var createResult = await _runner.RunAsync("sc.exe", createArgs, ct);
        if (createResult.ExitCode != 0)
        {
            LogReceived?.Invoke($"[SERVICE-ERR] Не удалось создать службу: {createResult.StandardError} {createResult.StandardOutput}");
            return false;
        }

        // Set description
        await _runner.RunAsync("sc.exe", $"description {ServiceName} \"Служба Zapret для обхода замедления Discord и YouTube\"", ct);

        LogReceived?.Invoke($"[SERVICE] Служба '{ServiceName}' успешно зарегистрирована в системе.");

        if (startImmediately)
        {
            return await StartAsync(ct);
        }

        return true;
    }

    public async Task<bool> StartAsync(CancellationToken ct = default)
    {
        LogReceived?.Invoke($"[SERVICE] Запуск службы '{ServiceName}'...");
        var result = await _runner.RunAsync("sc.exe", $"start {ServiceName}", ct);
        if (result.ExitCode == 0 || result.StandardOutput.Contains("START_PENDING") || result.StandardOutput.Contains("RUNNING"))
        {
            LogReceived?.Invoke($"[SERVICE] Команда запуска отправлена успешно.");
            return true;
        }

        LogReceived?.Invoke($"[SERVICE-ERR] Ошибка запуска службы: {result.StandardError} {result.StandardOutput}");
        return false;
    }

    public async Task<bool> StopAsync(CancellationToken ct = default)
    {
        LogReceived?.Invoke($"[SERVICE] Остановка службы '{ServiceName}'...");
        var result = await _runner.RunAsync("sc.exe", $"stop {ServiceName}", ct);
        if (result.ExitCode == 0 || result.StandardOutput.Contains("STOP_PENDING") || result.StandardOutput.Contains("STOPPED"))
        {
            LogReceived?.Invoke($"[SERVICE] Команда остановки отправлена успешно.");
            return true;
        }

        LogReceived?.Invoke($"[SERVICE-ERR] Ошибка остановки службы: {result.StandardError} {result.StandardOutput}");
        return false;
    }

    public async Task<bool> RemoveAsync(CancellationToken ct = default)
    {
        LogReceived?.Invoke($"[SERVICE] Удаление службы '{ServiceName}'...");
        // Ensure stopped before deleting
        await StopAsync(ct);

        var result = await _runner.RunAsync("sc.exe", $"delete {ServiceName}", ct);
        if (result.ExitCode == 0 || result.StandardOutput.Contains("SUCCESS"))
        {
            LogReceived?.Invoke($"[SERVICE] Служба '{ServiceName}' успешно удалена.");
            return true;
        }

        LogReceived?.Invoke($"[SERVICE-ERR] Ошибка удаления службы: {result.StandardError} {result.StandardOutput}");
        return false;
    }
}
