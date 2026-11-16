using ZapretWinwsGui.Core.Process;

namespace ZapretWinwsGui.Core.Diagnostics;

public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public record DiagnosticIssue(
    DiagnosticSeverity Severity,
    string Title,
    string Description,
    string Remediation
);

public class ConflictDiagnostics
{
    private static readonly (string ProcessName, string AppName)[] ConflictingApps =
    [
        ("goodbyedpi", "GoodbyeDPI (конфликт драйвера WinDivert)"),
        ("byebyedpi", "ByeByeDPI"),
        ("warp-svc", "Cloudflare WARP"),
        ("Cloudflare WARP", "Cloudflare WARP GUI"),
        ("AmneziaVPN", "Amnezia VPN"),
        ("outline", "Outline VPN"),
        ("v2ray", "v2ray core"),
        ("xray", "Xray core"),
        ("clash", "Clash core"),
        ("Adguard", "AdGuard для Windows"),
        ("AdguardSvc", "Служба AdGuard")
    ];

    private readonly IProcessRunner _runner;

    public ConflictDiagnostics(IProcessRunner? runner = null)
    {
        _runner = runner ?? new SystemProcessRunner();
    }

    public async Task<List<DiagnosticIssue>> RunDiagnosticsAsync(CancellationToken ct = default)
    {
        var issues = new List<DiagnosticIssue>();

        // 0. Check administrator privileges
        if (!_runner.IsAdministrator)
        {
            issues.Add(new DiagnosticIssue(
                DiagnosticSeverity.Error,
                "Отсутствуют права администратора",
                "Приложение запущено без повышенных привилегий. Для работы драйвера WinDivert и управления системной службой требуются права администратора.",
                "Перезапустите приложение от имени Администратора (ПКМ -> Запуск от имени администратора)."
            ));
        }

        // 1. Check for running conflicting processes
        foreach (var (processName, appName) in ConflictingApps)
        {
            if (_runner.IsProcessRunning(processName))
            {
                issues.Add(new DiagnosticIssue(
                    DiagnosticSeverity.Warning,
                    $"Обнаружен запущенный процесс: {appName}",
                    $"Приложение '{appName}' перехватывает сетевой трафик, что может блокировать работу Zapret и WinDivert.",
                    $"Закройте '{appName}' или отключите его фильтрацию перед запуском Zapret."
                ));
            }
        }

        // 2. Check WinDivert driver status
        try
        {
            var divertCheck = await _runner.RunAsync("sc.exe", "query windivert", ct);
            if (divertCheck.StandardOutput.Contains("RUNNING", StringComparison.OrdinalIgnoreCase))
            {
                // If winws itself is NOT running, but windivert IS running, driver is orphaned
                if (!_runner.IsProcessRunning("winws"))
                {
                    issues.Add(new DiagnosticIssue(
                        DiagnosticSeverity.Warning,
                        "Зависший драйвер WinDivert",
                        "Драйвер WinDivert активен в системе, но процесс winws не запущен. Это может указывать на аварийное завершение предыдущей сессии.",
                        "Выполните команду 'sc stop windivert' или перезагрузите систему, если winws не может запуститься."
                    ));
                }
            }
        }
        catch
        {
            // Ignore on non-Windows environments
        }

        // 3. Healthy notice if no conflicts found
        if (issues.Count == 0)
        {
            issues.Add(new DiagnosticIssue(
                DiagnosticSeverity.Info,
                "Конфликтов не обнаружено",
                "Сторонние программы обхода DPI, конфликтующие VPN-сервисы и зависшие драйверы WinDivert не активны.",
                "Система готова к комфортной работе с Zapret."
            ));
        }

        return issues;
    }
}
