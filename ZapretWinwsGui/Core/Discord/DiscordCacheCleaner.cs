using ZapretWinwsGui.Core.Process;

namespace ZapretWinwsGui.Core.Discord;

public record DiscordCleanResult(
    bool Success,
    int TerminatedProcesses,
    int RemovedDirectories,
    long CleanedBytes,
    string Message
);

public class DiscordCacheCleaner
{
    private static readonly string[] DiscordProcessNames =
    [
        "Discord",
        "DiscordCanary",
        "DiscordPTB",
        "DiscordDevelopment"
    ];

    private static readonly string[] DiscordSubfolders =
    [
        "discord",
        "discordcanary",
        "discordptb",
        "discorddevelopment"
    ];

    private static readonly string[] TargetCacheFolders =
    [
        "Cache",
        "Code Cache",
        "GPUCache",
        "DawnCache"
    ];

    private readonly IProcessRunner _runner;
    private readonly string? _customAppDataPath;

    public DiscordCacheCleaner(IProcessRunner? runner = null, string? customAppDataPath = null)
    {
        _runner = runner ?? new SystemProcessRunner();
        _customAppDataPath = customAppDataPath;
    }

    public DiscordCleanResult CleanCache(bool restartDiscord = false)
    {
        int killed = 0;

        // 1. Terminate running Discord processes
        foreach (var proc in DiscordProcessNames)
        {
            if (_runner.IsProcessRunning(proc))
            {
                _runner.KillProcessesByName(proc);
                killed++;
            }
        }

        // Wait a moment for file locks to release
        Thread.Sleep(300);

        // 2. Discover cache directories
        var appData = _customAppDataPath ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrEmpty(appData) || !Directory.Exists(appData))
        {
            return new DiscordCleanResult(false, killed, 0, 0, "Каталог AppData недоступен.");
        }

        int removedDirs = 0;
        long totalBytes = 0;

        foreach (var discordDir in DiscordSubfolders)
        {
            var basePath = Path.Combine(appData, discordDir);
            if (!Directory.Exists(basePath))
                continue;

            foreach (var cacheName in TargetCacheFolders)
            {
                var targetPath = Path.Combine(basePath, cacheName);
                if (Directory.Exists(targetPath))
                {
                    try
                    {
                        totalBytes += GetDirectorySize(targetPath);
                        Directory.Delete(targetPath, recursive: true);
                        removedDirs++;
                    }
                    catch (Exception ex)
                    {
                        // File might still be locked
                        Console.WriteLine($"[DiscordCleaner] Could not remove {targetPath}: {ex.Message}");
                    }
                }
            }
        }

        double megabytes = totalBytes / (1024.0 * 1024.0);
        var msg = $"Очищено {removedDirs} каталогов ({megabytes:F1} МБ). Завершено процессов: {killed}.";

        return new DiscordCleanResult(true, killed, removedDirs, totalBytes, msg);
    }

    private static long GetDirectorySize(string directoryPath)
    {
        long size = 0;
        try
        {
            var dir = new DirectoryInfo(directoryPath);
            foreach (var file in dir.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                size += file.Length;
            }
        }
        catch
        {
            // Ignore access errors on individual files
        }
        return size;
    }
}
