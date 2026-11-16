using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZapretWinwsGui.Core.Bundling;
using ZapretWinwsGui.Core.Config;
using ZapretWinwsGui.Core.Diagnostics;
using ZapretWinwsGui.Core.Discord;
using ZapretWinwsGui.Core.Process;
using ZapretWinwsGui.Core.Services;
using ZapretWinwsGui.Core.Strategies;
using ZapretWinwsGui.Core.Tester;

namespace ZapretWinwsGui.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly Action? _showWindowAction;
    private readonly Action? _exitAppAction;
    private readonly StrategyParser _strategyParser = new();
    private readonly WinwsProcessManager _processManager;
    private readonly WindowsServiceManager _serviceManager;
    private readonly ConflictDiagnostics _diagnostics;
    private readonly DiscordCacheCleaner _discordCleaner;
    private readonly StrategyTester _tester;
    private readonly FlowsealBundleManager _bundleManager = new();
    private readonly SettingsManager _settingsManager;
    private readonly AppSettings _settings;

    [ObservableProperty]
    private BundleStatus? _bundleStatus;

    [ObservableProperty]
    private bool _isBundleComplete = false;

    [ObservableProperty]
    private string _bundleDetailsText = "Проверка компонентов...";

    [ObservableProperty]
    private string _trayToolTip = "Zapret WinWS · Остановлен";

    [ObservableProperty]
    private bool _isAdministrator = true;

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

    public MainViewModel(
        IProcessRunner? runner = null,
        Action? showWindow = null,
        Action? exitApp = null,
        SettingsManager? settingsManager = null)
    {
        _showWindowAction = showWindow;
        _exitAppAction = exitApp;
        _settingsManager = settingsManager ?? new SettingsManager();
        _settings = _settingsManager.Load();

        var processRunner = runner ?? new SystemProcessRunner();
        _isAdministrator = processRunner.IsAdministrator;

        _processManager = new WinwsProcessManager(processRunner);
        _serviceManager = new WindowsServiceManager(processRunner);
        _diagnostics = new ConflictDiagnostics(processRunner);
        _discordCleaner = new DiscordCacheCleaner();
        _tester = new StrategyTester();

        _processManager.StateChanged += OnProcessStateChanged;
        _processManager.LogReceived += AddLog;
        _serviceManager.LogReceived += AddLog;

        LoadDefaultStrategies();
        RefreshBundleStatus();
        _ = RefreshServiceStatusAsync();
        RunDiagnosticsCommand.Execute(null);

        if (_settings.AutoStartWinwsOnLaunch && SelectedStrategy != null)
        {
            StartProcess();
        }
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
        TrayToolTip = $"Zapret WinWS · {StatusText}";
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

        SelectedStrategy = Strategies.FirstOrDefault(s => s.Id == _settings.LastSelectedStrategyId)
            ?? Strategies.FirstOrDefault(s => s.IsRecommended)
            ?? Strategies.FirstOrDefault();
    }

    partial void OnSelectedStrategyChanged(StrategyModel? value)
    {
        if (value != null && _settings != null && _settingsManager != null)
        {
            _settings.LastSelectedStrategyId = value.Id;
            _settingsManager.Save(_settings);
        }
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

    [RelayCommand]
    public void ShowMainWindow()
    {
        _showWindowAction?.Invoke();
    }

    [RelayCommand]
    public void ExitApplication()
    {
        _processManager.Stop();
        _exitAppAction?.Invoke();
    }

    [RelayCommand]
    public void ToggleProcess()
    {
        if (IsRunning)
        {
            StopProcess();
        }
        else
        {
            StartProcess();
        }
    }

    [RelayCommand]
    public void RefreshBundleStatus()
    {
        var baseDir = AppContext.BaseDirectory;
        var status = _bundleManager.CheckBundle(baseDir);
        BundleStatus = status;
        IsBundleComplete = status.IsComplete;
        BundleDetailsText = status.Details;

        var diskStrategies = _strategyParser.ScanDirectory(baseDir);
        if (diskStrategies.Count > 0)
        {
            Strategies.Clear();
            foreach (var s in diskStrategies)
            {
                Strategies.Add(s);
            }
            SelectedStrategy = Strategies.FirstOrDefault(s => s.Id == _settings.LastSelectedStrategyId)
                ?? Strategies.FirstOrDefault(s => s.IsRecommended)
                ?? Strategies.FirstOrDefault();
        }
    }

    [RelayCommand]
    public async Task DownloadFlowsealBundleAsync()
    {
        IsBusy = true;
        BusyStatusText = "Загрузка компонентов Flowseal...";
        try
        {
            var progress = new Progress<string>(msg =>
            {
                BusyStatusText = msg;
                AddLog($"[BUNDLE] {msg}");
            });

            var success = await _bundleManager.DownloadAndDeployAsync(AppContext.BaseDirectory, progress: progress);
            RefreshBundleStatus();
            if (success)
            {
                AddLog("[BUNDLE] Компоненты Flowseal успешно установлены и готовы к работе.");
            }
            else
            {
                AddLog("[BUNDLE-ERR] Не удалось загрузить или распаковать компоненты Flowseal.");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
