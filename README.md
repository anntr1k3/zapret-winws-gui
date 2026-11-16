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
  - Интеграция с системным треем Windows: сворачивание, индикация статуса и быстрое переключение стратегий.

- 📜 **Автоматический каталог стратегий:**
  - Парсинг стратегий Flowseal (`general.bat`, `discord.bat` и др.) прямо из каталога.
  - Отображение активных параметров десинхронизации (fake TLS, split, disorder, TTL).

- ⚡ **Интерактивный тестировщик доступности:**
  - Проверка потокового воспроизведения YouTube (`googlevideo.com`).
  - Проверка голосовых и текстовых шлюзов Discord (WebRTC / Voice Gateway).
  - Замер задержки (ping / latency) и вывод статуса для каждой стратегии.

- 🩺 **Глубокая диагностика конфликтов:**
  - Обнаружение конкурирующих инструментов: GoodbyeDPI, ByeByeDPI, WARP, AmneziaVPN, Outline.
  - Проверка системных прокси (WinINet / Windows Settings).
  - Проверка статуса драйвера `WinDivert`.

- 🧹 **Очистка кэша Discord:**
  - Безопасное завершение процессов Discord и очистка каталогов кэша в один клик.

---

## 🏗️ Архитектура проекта

```
zapret-winws-gui/
├── ZapretWinwsGui.sln               # Решение .NET 9
├── ZapretWinwsGui/                  # Основное приложение Avalonia UI
│   ├── Assets/                      # Иконки, графика и ресурсы
│   ├── Core/                        # Бизнес-логика и сервисы ядра
│   │   ├── Diagnostics/             # Диагностика конфликтов и сети
│   │   ├── Discord/                 # Очистка кэша и процессов Discord
│   │   ├── Process/                 # Управление процессом winws.exe
│   │   ├── Services/                # Управление службами Windows (SCM)
│   │   ├── Strategies/              # Парсинг и валидация .bat стратегий
│   │   └── Tester/                  # Потоковый тестировщик YouTube/Discord
│   ├── Models/                      # Модели данных
│   ├── ViewModels/                  # MVVM ViewModels (CommunityToolkit.Mvvm)
│   └── Views/                       # XAML-разметка окон (Fluent Design)
└── ZapretWinwsGui.Tests/            # Автоматизированные xUnit тесты
```

---

## 🛠️ Сборка и разработка

### Требования
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Сборка решения:
```bash
dotnet build
```

### Запуск тестов:
```bash
dotnet test
```

### Публикация автономного .exe под Windows (x64):
```bash
dotnet publish ZapretWinwsGui/ZapretWinwsGui.csproj -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true
```

---

## 📄 Лицензия

Проект распространяется под лицензией [MIT](LICENSE).
Базируется на разработках [bol-van](https://github.com/bol-van/zapret) и [Flowseal](https://github.com/Flowseal/zapret-discord-youtube).
