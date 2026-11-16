using System.IO.Compression;

namespace ZapretWinwsGui.Core.Bundling;

public record BundleStatus(
    bool IsComplete,
    bool HasWinws,
    bool HasWinDivert,
    bool HasLists,
    int StrategyBatCount,
    string Details
);

public class FlowsealBundleManager
{
    public const string DefaultFlowsealZipUrl = "https://github.com/Flowseal/zapret-discord-youtube/archive/refs/heads/main.zip";

    private readonly HttpClient _httpClient;

    public FlowsealBundleManager(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    }

    public BundleStatus CheckBundle(string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory) || !Directory.Exists(baseDirectory))
        {
            return new BundleStatus(false, false, false, false, 0, "Рабочий каталог не найден");
        }

        var hasWinws = File.Exists(Path.Combine(baseDirectory, "bin", "winws.exe")) ||
                       File.Exists(Path.Combine(baseDirectory, "winws.exe"));

        var hasWinDivertDll = File.Exists(Path.Combine(baseDirectory, "bin", "WinDivert.dll")) ||
                              File.Exists(Path.Combine(baseDirectory, "WinDivert.dll"));

        var hasWinDivertSys = File.Exists(Path.Combine(baseDirectory, "bin", "WinDivert64.sys")) ||
                              File.Exists(Path.Combine(baseDirectory, "WinDivert64.sys")) ||
                              File.Exists(Path.Combine(baseDirectory, "bin", "windivert64.sys")) ||
                              File.Exists(Path.Combine(baseDirectory, "windivert64.sys"));

        var hasWinDivert = hasWinDivertDll && hasWinDivertSys;

        var listsDir = Path.Combine(baseDirectory, "lists");
        var hasLists = Directory.Exists(listsDir) && Directory.GetFiles(listsDir, "*.txt").Length > 0;

        var batFiles = Directory.GetFiles(baseDirectory, "*.bat");
        var strategyBatCount = batFiles.Count(f =>
        {
            var name = Path.GetFileName(f);
            return !name.StartsWith("service", StringComparison.OrdinalIgnoreCase) &&
                   !name.StartsWith("check", StringComparison.OrdinalIgnoreCase);
        });

        var isComplete = hasWinws && hasWinDivert && hasLists;
        string details = isComplete
            ? $"Компоненты ядра готовы: winws.exe, WinDivert, {Directory.GetFiles(listsDir, "*.txt").Length} списков, {strategyBatCount} стратегий."
            : $"Отсутствуют компоненты: {(hasWinws ? "" : "winws.exe ")}{(hasWinDivert ? "" : "WinDivert ")}{(hasLists ? "" : "списки доменов lists/")}".Trim();

        return new BundleStatus(isComplete, hasWinws, hasWinDivert, hasLists, strategyBatCount, details);
    }

    public async Task<bool> DownloadAndDeployAsync(
        string targetDirectory,
        string? zipUrl = null,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var url = zipUrl ?? DefaultFlowsealZipUrl;
        var tempZip = Path.Combine(Path.GetTempPath(), $"flowseal-{Guid.NewGuid():N}.zip");
        var extractTempDir = Path.Combine(Path.GetTempPath(), $"flowseal-extract-{Guid.NewGuid():N}");

        try
        {
            progress?.Report($"Загрузка архива Flowseal из {url}...");

            using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct))
            {
                response.EnsureSuccessStatusCode();
                await using var fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fs, ct);
            }

            progress?.Report("Распаковка архива во временную папку...");
            ZipFile.ExtractToDirectory(tempZip, extractTempDir, overwriteFiles: true);

            progress?.Report("Перемещение компонентов в рабочий каталог...");
            // The zip may contain a root directory like zapret-discord-youtube-main
            var subDirs = Directory.GetDirectories(extractTempDir);
            var sourceRoot = subDirs.Length == 1 ? subDirs[0] : extractTempDir;

            Directory.CreateDirectory(targetDirectory);

            // Copy bin/ folder
            var srcBin = Path.Combine(sourceRoot, "bin");
            if (Directory.Exists(srcBin))
            {
                var dstBin = Path.Combine(targetDirectory, "bin");
                Directory.CreateDirectory(dstBin);
                foreach (var file in Directory.GetFiles(srcBin))
                {
                    var destFile = Path.Combine(dstBin, Path.GetFileName(file));
                    File.Copy(file, destFile, overwrite: true);
                }
            }

            // Copy lists/ folder
            var srcLists = Path.Combine(sourceRoot, "lists");
            if (Directory.Exists(srcLists))
            {
                var dstLists = Path.Combine(targetDirectory, "lists");
                Directory.CreateDirectory(dstLists);
                foreach (var file in Directory.GetFiles(srcLists))
                {
                    var destFile = Path.Combine(dstLists, Path.GetFileName(file));
                    File.Copy(file, destFile, overwrite: true);
                }
            }

            // Copy .bat strategy files
            foreach (var batFile in Directory.GetFiles(sourceRoot, "*.bat"))
            {
                var destFile = Path.Combine(targetDirectory, Path.GetFileName(batFile));
                File.Copy(batFile, destFile, overwrite: true);
            }

            progress?.Report("Компоненты Flowseal успешно установлены.");
            return true;
        }
        catch (Exception ex)
        {
            progress?.Report($"Ошибка загрузки Flowseal: {ex.Message}");
            return false;
        }
        finally
        {
            try
            {
                if (File.Exists(tempZip))
                    File.Delete(tempZip);
                if (Directory.Exists(extractTempDir))
                    Directory.Delete(extractTempDir, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }
}
