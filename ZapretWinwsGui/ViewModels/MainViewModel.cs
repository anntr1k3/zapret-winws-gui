using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZapretWinwsGui.Core.Diagnostics;
using ZapretWinwsGui.Core.Discord;
using ZapretWinwsGui.Core.Process;
using ZapretWinwsGui.Core.Services;
using ZapretWinwsGui.Core.Strategies;
using ZapretWinwsGui.Core.Tester;

namespace ZapretWinwsGui.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly StrategyParser _strategyParser = new();
    private readonly WinwsProcessManager _processManager;
    private readonly WindowsServiceManager _serviceManager;
    private readonly ConflictDiagnostics _diagnostics;
    private readonly DiscordCacheCleaner _discordCleaner;
    private readonly StrategyTester _tester;

    [ObservableProperty]
    private int _selectedTabIndex = 0;

    [ObservableProperty]
    private string _statusText = "Остановлен";

    [ObservableProperty]
    private bool _isRunning = false;

    [ObservableProperty]
    private string _uptimeText = "—";

    [ObservableProperty]
    private StrategyModel? _selectedStrategy;

    [ObservableProperty]
    private WindowsServiceStatus _serviceStatus = WindowsServiceStatus.NotInstalled;

    [ObservableProperty]
    private string _serviceStatusText = "Служба не установлена";

    [ObservableProperty]
    private bool _isBusy = false;

    [ObservableProperty]
    private string _busyStatusText = string.Empty;

    public ObservableCollection<StrategyModel> Strategies { get; } = [];
    public ObservableCollection<DiagnosticIssue> DiagnosticIssues { get; } = [];
    public ObservableCollection<ProbeResult> TestResults { get; } = [];
    public ObservableCollection<string> Logs { get; } = [];

    public MainViewModel()
    {
        _processManager = new WinwsProcessManager();
        _serviceManager = new WindowsServiceManager();
        _diagnostics = new ConflictDiagnostics();
        _discordCleaner = new DiscordCacheCleaner();
        _tester = new StrategyTester();

        _processManager.StateChanged += OnProcessStateChanged;
        _processManager.LogReceived += AddLog;
        _serviceManager.LogReceived += AddLog;

        LoadDefaultStrategies();
        _ = RefreshServiceStatusAsync();
        RunDiagnosticsCommand.Execute(null);
    }

    private void AddLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        Logs.Add($"[{timestamp}] {message}");
        if (Logs.Count > 1000)
            Logs.RemoveAt(0);
    }

    private void OnProcessStateChanged(ProcessState state)
    {
        IsRunning = state == ProcessState.Running;
        StatusText = state switch
        {
            ProcessState.Running => $"Работает ({_processManager.ActiveStrategy?.Name ?? "winws"})",
            ProcessState.Starting => "Запуск...",
            ProcessState.Failed => "Ошибка запуска",
            _ => "Остановлен"
        };
    }

    private void LoadDefaultStrategies()
    {
        Strategies.Clear();
        // Embedded curated default Flowseal strategies
        Strategies.Add(new StrategyModel
        {
            Id = "general",
            Name = "General (Основная рекомендация)",
            FileName = "general.bat",
            Category = StrategyCategory.General,
            Description = "Универсальная стратегия Flowseal для YouTube и Discord. Подменяет TLS ClientHello и нарезает пакеты.",
            RawArguments = "--wf-tcp=80,443,2053,2083,2087,2096,8443 --wf-udp=443,19294-19344,50000-50100 --dpi-desync=fake,multisplit --dpi-desync-split-pos=1",
            UsesFakeTls = true,
            UsesSplit = true,
            UsesFakeUdp = true,
            IsRecommended = true
        });

        Strategies.Add(new StrategyModel
        {
            Id = "general_alt",
            Name = "General Alt (Альтернативная 1)",
            FileName = "general_alt.bat",
            Category = StrategyCategory.General,
            Description = "Альтернативная стратегия при блокировках стандартного fake. Использует disorder и смещение sequence.",
            RawArguments = "--wf-tcp=80,443 --dpi-desync=disorder2 --dpi-desync-split-pos=2",
            UsesDisorder = true,
            UsesSplit = true,
            IsRecommended = true
        });

        Strategies.Add(new StrategyModel
        {
            Id = "general_alt2",
            Name = "General Alt 2 (Агрессивная нарезка)",
            FileName = "general_alt2.bat",
            Category = StrategyCategory.General,
            Description = "Стратегия для жёстких провайдеров с глубоким анализом SNI.",
            RawArguments = "--wf-tcp=80,443 --dpi-desync=multisplit --dpi-desync-split-pos=1,midsld",
            UsesSplit = true
        });

        Strategies.Add(new StrategyModel
        {
            Id = "discord",
            Name = "Discord Only (Голос + Медиа)",
            FileName = "discord.bat",
            Category = StrategyCategory.Discord,
            Description = "Фокусируется на UDP-шлюзах и медиа-серверах Discord.",
            RawArguments = "--wf-udp=50000-50100,19294-19344 --dpi-desync=fake --dpi-desync-repeats=6",
            UsesFakeUdp = true
        });

        SelectedStrategy = Strategies.FirstOrDefault(s => s.IsRecommended) ?? Strategies.FirstOrDefault();
    }

    [RelayCommand]
    private void StartProcess()
    {
        if (SelectedStrategy == null)
            return;

        try
        {
            _processManager.Start(SelectedStrategy, AppContext.BaseDirectory);
        }
        catch (Exception ex)
        {
            AddLog($"[ERROR] {ex.Message}");
        }
    }

    [RelayCommand]
    private void StopProcess()
    {
        _processManager.Stop();
    }

    [RelayCommand]
    private async Task RefreshServiceStatusAsync()
    {
        ServiceStatus = await _serviceManager.GetStatusAsync();
        ServiceStatusText = ServiceStatus switch
        {
            WindowsServiceStatus.Running => "Служба работает в фоне",
            WindowsServiceStatus.Stopped => "Служба остановлена",
            WindowsServiceStatus.StartPending => "Служба запускается...",
            WindowsServiceStatus.StopPending => "Служба останавливается...",
            WindowsServiceStatus.NotInstalled => "Служба не установлена",
            _ => "Статус неизвестен"
        };
    }

    [RelayCommand]
    private async Task InstallServiceAsync()
    {
        if (SelectedStrategy == null)
            return;

        IsBusy = true;
        BusyStatusText = "Установка службы Windows...";
        try
        {
            await _serviceManager.InstallAsync(SelectedStrategy, AppContext.BaseDirectory, startImmediately: true);
            await RefreshServiceStatusAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RemoveServiceAsync()
    {
        IsBusy = true;
        BusyStatusText = "Удаление службы Windows...";
        try
        {
            await _serviceManager.RemoveAsync();
            await RefreshServiceStatusAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RunDiagnosticsAsync()
    {
        IsBusy = true;
        BusyStatusText = "Проверка сетевых конфликтов...";
        try
        {
            DiagnosticIssues.Clear();
            var issues = await _diagnostics.RunDiagnosticsAsync();
            foreach (var issue in issues)
            {
                DiagnosticIssues.Add(issue);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RunStrategyTestAsync()
    {
        IsBusy = true;
        BusyStatusText = "Тестирование YouTube и Discord...";
        TestResults.Clear();
        try
        {
            var progress = new Progress<ProbeResult>(res => TestResults.Add(res));
            await _tester.TestEndpointsAsync(progress);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CleanDiscordCache()
    {
        IsBusy = true;
        BusyStatusText = "Очистка кэша Discord...";
        try
        {
            var result = _discordCleaner.CleanCache();
            AddLog($"[DISCORD] {result.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ClearLogs()
    {
        Logs.Clear();
    }
}
