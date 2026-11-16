# 🛡️ Zapret WinWS GUI

<div align="center">

### Современный графический интерфейс для Windows-версии Zapret (WinWS / Flowseal)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Avalonia UI](https://img.shields.io/badge/UI-Avalonia_Fluent-FF5500)](https://avaloniaui.net/)
[![Platform](https://img.shields.io/badge/Platform-Windows_10_|_11-blue?logo=windows)](https://microsoft.com)

**Автономный порт интерфейса управления zapret/winws с поддержкой стратегий Flowseal.**  
📖 **[Полный контекст проекта и руководство по передаче (PROJECT_CONTEXT.md)](PROJECT_CONTEXT.md)**

</div>

---

## 🌟 Основные возможности

- 🚀 **Управление в 1 клик:**
  - Запуск, остановка и перезапуск процесса `winws.exe` без открытых консольных окон.
  - Управление постоянной системной службой Windows (`sc.exe` / SCM) с поддержкой автозагрузки при старте системы.
  - Интеграция с системным треем Windows: сворачивание приложения при закрытии окна, индикация статуса в подсказке и нативное контекстное меню.

- 📦 **Встроенный менеджер компонентов Flowseal:**
  - Проверка наличия `winws.exe`, сетевого драйвера `WinDivert` и списков сайтов (`lists/`).
  - Скачивание и обновление актуального бандла Flowseal в один клик прямо из программы.

- 📜 **Автоматический каталог стратегий:**
  - Парсинг стратегий Flowseal (`general.bat`, `discord.bat` и др.) прямо из каталога.
  - Отображение активных параметров десинхронизации (fake TLS, split, disorder, TTL).

- ⚡ **Интерактивный тестировщик доступности:**
  - Проверка потокового воспроизведения YouTube (`googlevideo.com`).
  - Проверка голосовых и текстовых шлюзов Discord (WebRTC / Voice Gateway).
  - Замер задержки (ping / latency) и вывод статуса для каждой стратегии.

- 🩺 **Глубокая диагностика конфликтов и UAC:**
  - Проверка прав администратора и автозапрос через встроенный UAC-манифест.
  - Обнаружение конкурирующих инструментов: GoodbyeDPI, ByeByeDPI, WARP, AmneziaVPN, Outline.
  - Проверка системных прокси и состояния службы/драйвера `WinDivert`.

- 🧹 **Очистка кэша Discord:**
  - Безопасное завершение процессов Discord и очистка каталогов кэша в один клик.

---

## 🏗️ Архитектура проекта

```
zapret-winws-gui/
├── ZapretWinwsGui.sln               # Решение .NET 9
├── build.ps1                        # Пайплайн сборки, тестирования и публикации
├── installer/
│   └── zapret-winws-gui.iss         # Скрипт Inno Setup для сборки Setup.exe
├── ZapretWinwsGui/                  # Основное приложение Avalonia UI
│   ├── Assets/                      # Иконки, графика и ресурсы
│   ├── Core/                        # Бизнес-логика и сервисы ядра
│   │   ├── Bundling/                # Проверка и скачивание компонентов Flowseal
│   │   ├── Diagnostics/             # Диагностика конфликтов и UAC
│   │   ├── Discord/                 # Очистка кэша и процессов Discord
│   │   ├── Process/                 # Управление процессом winws.exe
│   │   ├── Services/                # Управление службами Windows (SCM)
│   │   ├── Strategies/              # Парсинг и валидация .bat стратегий
│   │   └── Tester/                  # Потоковый тестировщик YouTube/Discord
│   ├── ViewModels/                  # MVVM ViewModels (CommunityToolkit.Mvvm)
│   └── Views/                       # XAML-разметка окон (Fluent Design)
└── ZapretWinwsGui.Tests/            # 19 модульных xUnit тестов (100% PASS)
```

---

## 🛠️ Сборка и разработка

### Требования
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Сборка и тестирование:
```powershell
# Запуск тестов
dotnet test

# Запуск приложения
dotnet run --project ZapretWinwsGui/ZapretWinwsGui.csproj

# Автономная публикация win-x64 (в папку ./publish)
.\build.ps1

# Сборка установщика Inno Setup (в папку ./dist)
.\build.ps1 -PackageInstaller
```

---

## 📄 Лицензия

Проект распространяется под лицензией [MIT](LICENSE).
Базируется на разработках [bol-van](https://github.com/bol-van/zapret) и [Flowseal](https://github.com/Flowseal/zapret-discord-youtube).
