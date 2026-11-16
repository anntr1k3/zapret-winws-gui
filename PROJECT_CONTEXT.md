# 🧭 Контекст проекта и руководство по передаче (Handover Document)

> **Документ для продолжения разработки с любого компьютера или в диалоге с ИИ-ассистентом.**  
> Дата фиксации: 16 ноября 2026 г.  
> Автор / Архитектор: **anntr1k3** & AI Assistant (Antigravity).

---

## 📌 1. Общая картина и концепция

Пользователь (**anntr1k3**) ведёт разработку проектов обхода замедления (DPI) YouTube и Discord на базе наработок **Flowseal** (`zapret-discord-youtube`) и **bol-van** (`zapret` / `nfqws` / `winws`):

1. **`zapret-discord-youtube-linux` (Linux-порт v1.0):**
   - **Репозиторий:** Приватный `https://github.com/anntr1k3/zapret-discord-youtube-linux.git` (ветка `master`).
   - **Стек:** Python 3, GTK4 / libadwaita, Polkit pkexec root-помощник, systemd-сервис, 503 юнит-теста.

2. **`zapret-winws-gui` (Windows GUI — текущий репозиторий):**
   - **Репозиторий на GitHub:** `https://github.com/anntr1k3/zapret-winws-gui.git` (приватный).
   - **Цель:** Создать современный, красивый и удобный графический интерфейс для Windows-версии Zapret (`winws.exe` + Flowseal стратегии).
   - **Стек:** **C# / .NET 9 SDK**, **Avalonia UI** (Fluent Design под Windows 11 с темами и скруглениями), **CommunityToolkit.Mvvm**, **xUnit** (19 тестов, 100% PASS).

---

## 🏗️ 2. Текущее состояние `zapret-winws-gui`

### Архитектура решения (.NET 9 Solution):
```
zapret-winws-gui/
├── ZapretWinwsGui.sln               # Главное решение .NET
├── build.ps1                        # Пайплайн сборки, тестов и создания инсталлятора
├── installer/
│   └── zapret-winws-gui.iss         # Конфигурация Inno Setup для сборки Setup.exe
├── ZapretWinwsGui/                  # Проект графического интерфейса Avalonia
│   ├── Assets/                      # Графика, иконки
│   ├── Core/                        # Бизнес-логика (не зависит от UI!)
│   │   ├── Bundling/                # FlowsealBundleManager.cs (проверка и скачивание winws/списков)
│   │   ├── Strategies/              # StrategyModel.cs, StrategyParser.cs
│   │   ├── Process/                 # IProcessRunner.cs, SystemProcessRunner.cs, WinwsProcessManager.cs
│   │   ├── Services/                # WindowsServiceManager.cs (управление sc.exe)
│   │   ├── Diagnostics/             # ConflictDiagnostics.cs (поиск GoodbyeDPI, VPN, проверка прав UAC)
│   │   ├── Discord/                 # DiscordCacheCleaner.cs (очистка %APPDATA%\discord\Cache)
│   │   └── Tester/                  # StrategyTester.cs (тест googlevideo.com и Discord WebRTC)
│   ├── ViewModels/                  # MainViewModel.cs, ViewModelBase.cs
│   └── Views/                       # MainWindow.axaml (разметка Fluent UI), MainWindow.axaml.cs
└── ZapretWinwsGui.Tests/            # 19 модульных xUnit тестов (100% PASS)
    ├── FlowsealBundleManagerTests.cs
    ├── StrategyParserTests.cs
    ├── WinwsProcessManagerTests.cs
    ├── WindowsServiceManagerTests.cs
    ├── ConflictDiagnosticsTests.cs
    ├── DiscordCacheCleanerTests.cs
    ├── StrategyTesterTests.cs
    └── MockProcessRunner.cs
```

### Что уже полностью реализовано и работает:
1. **Интеграция с System Tray (системным треем Windows):**
   - Настроен `TrayIcon` в `App.axaml` с динамической подсказкой статуса (`TrayToolTip`).
   - Нативное меню трея: открытие панели, запуск/остановка `winws.exe`, выход.
   - Сворачивание приложения в трей при закрытии (кнопка [X] перехвачена в `MainWindow.axaml.cs`).
2. **Менеджер бинарников и списков Flowseal (`FlowsealBundleManager`):**
   - Автоматическая проверка целостности файлов: `bin/winws.exe`, `WinDivert.dll`, `WinDivert64.sys`, папки `lists/` и `.bat` файлов.
   - Загрузка и распаковка актуального архива Flowseal в 1 клик прямо из графического интерфейса.
3. **UAC-манифест и проверка прав администратора:**
   - В `app.manifest` включен `<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />`.
   - В `ConflictDiagnostics` и `MainViewModel` добавлена проверка `IsAdministrator` с предупреждающим баннером в UI.
4. **Умное разрешение путей ядра (`WinwsProcessManager.ResolveWinwsPath`):**
   - Автоматический поиск `winws.exe` как в корне, так и в подпапке `bin/`, установка корректной рабочей директории для загрузки драйвера WinDivert.
5. **Парсер Flowseal `.bat` файлов (`StrategyParser`):**
   - Корректно раскрывает многострочные команды с кареткой `^`.
   - Заменяет переменные окружения `%BIN%`, `%LISTS%`, `%~dp0`.
   - Удаляет пустые параметры `%GameFilterTCP%` и `%GameFilterUDP%`.
   - Автоматически распознаёт флаги десинхронизации: `fake TLS`, `split/multisplit`, `disorder`, `fake UDP`.
6. **Менеджер процесса `winws.exe` (`WinwsProcessManager`):**
   - Запускает процесс скрытно без мелькающих окон командной строки (`CreateNoWindow = true`).
   - Перехватывает потоки вывода `stdout` и `stderr` и направляет их в журнал событий GUI.
   - Безопасно останавливает и перезапускает процесс при смене стратегии.
7. **Менеджер системной службы Windows (`WindowsServiceManager`):**
   - Управление службой через Windows SCM (`sc.exe`): регистрация службы с автозапуском (`start= auto`), запуск, остановка и удаление.
8. **Глубокая диагностика конфликтов (`ConflictDiagnostics`):**
   - Обнаруживает запущенные процессы: GoodbyeDPI, ByeByeDPI, Cloudflare WARP, AmneziaVPN, Outline, AdGuard, v2ray, xray, clash.
   - Проверяет статус драйвера ядра `WinDivert`.
9. **Очистка кэша Discord (`DiscordCacheCleaner`):**
   - Завершает процессы Discord, удаляет папки кэша (`Cache`, `Code Cache`, `GPUCache`), подсчитывает освобождённое место.
10. **Потоковый тестировщик доступности (`StrategyTester`):**
    - Проверяет доступность потоков YouTube (`googlevideo.com`) и шлюзов Discord с замером задержки.
11. **Сборка и инсталлятор:**
    - Подготовлен скрипт пайплайна `build.ps1` для запуска тестов и публикации единого исполняемого файла win-x64 (`ZapretWinwsGui.exe`).
    - Подготовлен скрипт Inno Setup `installer/zapret-winws-gui.iss` для создания дистрибутива `Setup.exe`.

---

## 🛠️ 3. Полезные команды разработчика

```powershell
# Восстановление зависимостей и сборка
dotnet restore
dotnet build

# Запуск тестов (19 тестов)
dotnet test

# Запуск GUI приложения
dotnet run --project ZapretWinwsGui/ZapretWinwsGui.csproj

# Полная сборка автономного .exe в папку ./publish
.\build.ps1

# Сборка с созданием установщика Inno Setup (если установлен ISCC)
.\build.ps1 -PackageInstaller
```
