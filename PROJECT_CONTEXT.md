# 🧭 Контекст проекта и руководство по передаче (Handover Document)

> **Документ для продолжения разработки с другого компьютера или в новом диалоге с ИИ-ассистентом.**  
> Дата фиксации: 16 ноября 2026 г.  
> Автор / Архитектор: **anntr1k3** & AI Assistant (Antigravity).

---

## 📌 1. Общая картина и концепция

Пользователь (**anntr1k3**) ведёт разработку двух независимых, но идейно взаимосвязанных проектов обхода замедления (DPI) YouTube и Discord на базе наработок **Flowseal** (`zapret-discord-youtube`) и **bol-van** (`zapret` / `nfqws` / `winws`):

1. **`zapret-discord-youtube-linux` (Linux-порт v1.0):**
   - **Репозиторий на GitHub:** Приватный `https://github.com/anntr1k3/zapret-discord-youtube-linux.git` (ветка `master`).
   - **Статус:** 100% паритет с Flowseal `service.bat` v1.10.4 (12/12 пунктов матрицы соответствия).
   - **Стек:** Python 3, нативный GTK4 / libadwaita, Polkit pkexec root-помощник, systemd-сервис, встроенный потоковый тестировщик стратегий, глубокая диагностика конфликтов (cgroup, VPN, прокси), векторная иконка и ярлык рабочего стола `.desktop`.
   - **Тесты:** 503 юнит-теста проходят (100% PASS), GTK4 демо smoke test проходит (34 операции).
   - **Готовность:** Репозиторий полностью подготовлен к переключению в **публичный (Public)** статус (добавлена лицензия MIT, очищены личные пути, оформлен README). Пользователь проводит локальное тестирование на своём ПК перед нажатием кнопки «Make public» в настройках GitHub.

2. **`zapret-winws-gui` (Windows GUI — текущий репозиторий):**
   - **Репозиторий на GitHub:** `https://github.com/anntr1k3/zapret-winws-gui.git` (приватный).
   - **Цель:** Создать современный, красивый и удобный графический интерфейс для Windows-версии Zapret (`winws.exe` + Flowseal стратегии). Избавить обычных пользователей Windows от необходимости запускать `.bat` файлы в консоли от администратора, править параметры в Блокноте и гадать, какая стратегия работает.
   - **Стек:** **C# / .NET 9 SDK**, **Avalonia UI** (Fluent Design под Windows 11 с темами и скруглениями), **CommunityToolkit.Mvvm**, **xUnit** для модульного тестирования.

---

## 🏗️ 2. Текущее состояние `zapret-winws-gui`

### Архитектура решения (.NET 9 Solution):
```
zapret-winws-gui/
├── ZapretWinwsGui.sln               # Главное решение .NET
├── ZapretWinwsGui/                  # Проект графического интерфейса Avalonia
│   ├── Assets/                      # Графика, иконки
│   ├── Core/                        # Бизнес-логика (не зависит от UI!)
│   │   ├── Strategies/              # StrategyModel.cs, StrategyParser.cs
│   │   ├── Process/                 # IProcessRunner.cs, SystemProcessRunner.cs, WinwsProcessManager.cs
│   │   ├── Services/                # WindowsServiceManager.cs (управление sc.exe)
│   │   ├── Diagnostics/             # ConflictDiagnostics.cs (поиск GoodbyeDPI, VPN, WinDivert)
│   │   ├── Discord/                 # DiscordCacheCleaner.cs (очистка %APPDATA%\discord\Cache)
│   │   └── Tester/                  # StrategyTester.cs (тест googlevideo.com и Discord WebRTC)
│   ├── ViewModels/                  # MainViewModel.cs, ViewModelBase.cs
│   └── Views/                       # MainWindow.axaml (разметка Fluent UI), MainWindow.axaml.cs
└── ZapretWinwsGui.Tests/            # 14 модульных xUnit тестов (100% PASS)
    ├── StrategyParserTests.cs
    ├── WinwsProcessManagerTests.cs
    ├── WindowsServiceManagerTests.cs
    ├── ConflictDiagnosticsTests.cs
    ├── DiscordCacheCleanerTests.cs
    ├── StrategyTesterTests.cs
    └── MockProcessRunner.cs
```

### Что уже реализовано и работает:
1. **Парсер Flowseal `.bat` файлов (`StrategyParser`):**
   - Корректно раскрывает многострочные команды с кареткой `^`.
   - Заменяет переменные окружения `%BIN%`, `%LISTS%`, `%~dp0`.
   - Удаляет пустые параметры `%GameFilterTCP%` и `%GameFilterUDP%`.
   - Автоматически распознаёт флаги десинхронизации: `fake TLS`, `split/multisplit`, `disorder`, `fake UDP`.
2. **Менеджер процесса `winws.exe` (`WinwsProcessManager`):**
   - Запускает процесс скрытно без мелькающих черных окон командной строки (`CreateNoWindow = true`).
   - Перехватывает потоки вывода `stdout` и `stderr` и направляет их в журнал событий GUI.
   - Безопасно останавливает и перезапускает процесс при смене стратегии.
3. **Менеджер системной службы Windows (`WindowsServiceManager`):**
   - Управление службой через Windows SCM (`sc.exe`): регистрация службы с автозапуском при старте ПК (`start= auto`), запуск, остановка, чтение статусов и удаление.
4. **Глубокая диагностика конфликтов (`ConflictDiagnostics`):**
   - Обнаруживает запущенные процессы: GoodbyeDPI, ByeByeDPI, Cloudflare WARP, AmneziaVPN, Outline, AdGuard, v2ray, xray, clash.
   - Проверяет статус драйвера ядра `WinDivert` и сообщает о зависших экземплярах.
5. **Очистка кэша Discord (`DiscordCacheCleaner`):**
   - Завершает процессы Discord, удаляет папки кэша (`%APPDATA%\discord\Cache`, `Code Cache`, `GPUCache`), подсчитывает освобождённое место.
6. **Потоковый тестировщик доступности (`StrategyTester`):**
   - Проверяет `https://www.youtube.com`, чанки потокового видео `googlevideo.com` (с Range-запросами) и шлюзы Discord.
   - Замеряет задержку в миллисекундах и фиксирует факт прохождения трафика через ТСПУ.
7. **Пользовательский интерфейс Fluent Design (`MainWindow.axaml`):**
   - Навигационный сайдбар с 6 вкладками: *Управление*, *Стратегии*, *Тестировщик*, *Диагностика*, *Кэш Discord*, *Журнал*.
   - Тёмная тема в стиле Windows 11 с карточками и индикаторами статуса.
8. **Кросс-компиляция и тесты:**
   - Все 14 тестов xUnit проходят за ~300 мс.
   - Сборка автономного исполняемого файла Windows x64 (`ZapretWinwsGui.exe`) проверена и компилируется прямо из Linux через `dotnet publish -r win-x64`.

---

## 🎯 3. Дорожная карта следующих шагов (Backlog для нового компьютера)

Если вы открываете проект на компьютере с Windows (или продолжаете с другим ИИ-ассистентом):

1. **Интеграция с системным треем Windows (System Tray):**
   - Добавить `TrayIcon` в Avalonia UI (`App.axaml`), чтобы при закрытии окна крестиком приложение сворачивалось к часам в трей, а по правому клику открывалось контекстное меню: «Запустить / Остановить», быстрый список стратегий, «Выход».
2. **Скачивание и бандлинг `winws.exe` и списков Flowseal:**
   - Добавить встроенный менеджер загрузки актуальных бинарников `winws.exe`, `windivert64.sys`, `WinDivert.dll` и списков доменов (`list-general.txt`, `list-google.txt` и т.д.) из официального репозитория Flowseal, либо упаковывать их рядом в подпапку `bin/` и `lists/`.
3. **Запуск от имени Администратора (UAC Manifest):**
   - Для работы WinDivert и `sc.exe` требуются права администратора. В `app.manifest` настроить `<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />`.
4. **Упаковщик установщика (Installer):**
   - Настроить Inno Setup или WiX Toolset / MSIX для создания удобного установщика (`Setup.exe`) в один клик.

---

## 🛠️ 4. Полезные команды разработчика

```bash
# Клонирование репозитория
git clone https://github.com/anntr1k3/zapret-winws-gui.git
cd zapret-winws-gui

# Восстановление зависимостей и сборка
dotnet restore
dotnet build

# Запуск тестов
dotnet test

# Запуск GUI приложения (на Windows или Linux)
dotnet run --project ZapretWinwsGui/ZapretWinwsGui.csproj

# Публикация готового автономного .exe для Windows x64
dotnet publish ZapretWinwsGui/ZapretWinwsGui.csproj -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true -o ./publish
```

---

## 💬 5. Сообщение-затравка для нового диалога с ИИ

Если вы переходите на другой компьютер и открываете новый диалог с ИИ, просто отправьте ему следующий текст:

> *"Привет! Я работаю над проектом **zapret-winws-gui** (C# / .NET 9 + Avalonia UI) — нативным графическим интерфейсом под Windows для утилиты zapret/winws со стратегиями Flowseal. Исходный код и полный контекст находятся в репозитории: https://github.com/anntr1k3/zapret-winws-gui. Пожалуйста, прочитай `PROJECT_CONTEXT.md` и помоги продолжить разработку по следующему пункту: [укажите задачу, например: интеграция с System Tray / упаковка winws / доработка UI]."*
