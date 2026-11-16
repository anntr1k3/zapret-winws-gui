# 🛡️ Zapret WinWS GUI

<div align="center">

### Современный графический интерфейс для Windows-версии Zapret (WinWS / Flowseal)

[![Build & Test](https://github.com/anntr1k3/zapret-winws-gui/actions/workflows/ci.yml/badge.svg)](https://github.com/anntr1k3/zapret-winws-gui/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/anntr1k3/zapret-winws-gui?color=blue)](https://github.com/anntr1k3/zapret-winws-gui/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Avalonia UI](https://img.shields.io/badge/UI-Avalonia_Fluent-FF5500)](https://avaloniaui.net/)
[![Platform](https://img.shields.io/badge/Platform-Windows_10_|_11-0078D6?logo=windows)](https://microsoft.com)

**Автономный графический порт интерфейса управления winws (zapret) с поддержкой стратегий Flowseal.**  
Избавляет от необходимости запускать `.bat` файлы в черных окнах консоли от администратора, настраивать службы вручную и гадать, какая стратегия работает.

📖 **[Руководство разработчика и контекст проекта (PROJECT_CONTEXT.md)](PROJECT_CONTEXT.md)**

</div>

---

## ⚡ Быстрый старт для пользователей

1. **Скачайте программу:**
   * Перейдите во вкладку [Releases](https://github.com/anntr1k3/zapret-winws-gui/releases) и скачайте последнюю версию:
     - Либо портативный архив `ZapretWinwsGui-win-x64.zip` (содержит единый автономный `.exe`).
     - Либо установщик `ZapretWinwsGui-Setup.exe`.
2. **Запустите `ZapretWinwsGui.exe`:**
   * При первом запуске Windows запросит подтверждение прав администратора (UAC). Это необходимо для работы драйвера перехвата пакетов WinDivert.
3. **Загрузите компоненты в 1 клик:**
   * На главной вкладке нажмите кнопку **«📥 Скачать / обновить Flowseal»** — программа автоматически загрузит свежие бинарники `winws.exe`, `WinDivert` и актуальные списки заблокированных ресурсов.
4. **Запустите обход:**
   * Выберите стратегию (по умолчанию рекомендуется **General**) и нажмите **«▶ Запустить winws»**!
   * Или нажмите **«Установить и запустить службу»**, чтобы обход работал в фоне даже после перезагрузки компьютера без открытых окон.

---

## 🌟 Основные возможности

* 🚀 **Управление в один клик:**
  - Запуск, остановка и смена стратегии без мелькающих консольных окон.
  - Полноценная интеграция с **Windows Service Control Manager (`sc.exe`)**: установка службы с автозапуском при старте системы (`start= auto`).
  - **Сворачивание в системный трей (System Tray):** при нажатии крестика приложение аккуратно прячется к часам, показывает статус и управляется по правому клику.

* 📦 **Встроенный менеджер компонентов:**
  - Автоматическая проверка целостности `winws.exe`, драйвера `WinDivert` и каталога списков `lists/`.
  - Скачивание и обновление архива Flowseal прямо из интерфейса.

* 📜 **Умный каталог стратегий:**
  - Автоматический парсинг `.bat` файлов: раскрытие кареток `^`, подстановка путей `%BIN%`, `%LISTS%`, очистка игровых фильтров.
  - Наглядное отображение флагов десинхронизации: *fake TLS*, *split/multisplit*, *disorder*, *fake UDP*.

* 🧪 **Интерактивный тестировщик доступности:**
  - Проверка реальной отдачи видеопотоков YouTube (`googlevideo.com`) через Range-запросы.
  - Проверка голосовых и текстовых шлюзов Discord (WebRTC).
  - Замер времени отклика (latency / ping) в миллисекундах.

* 🩺 **Диагностика сетевых конфликтов:**
  - Мгновенное обнаружение конкурирующих инструментов: GoodbyeDPI, ByeByeDPI, Cloudflare WARP, AmneziaVPN, Outline, AdGuard, v2ray, xray, clash.
  - Проверка зависших экземпляров драйвера ядра `WinDivert`.

* 🧹 **Очистка кэша Discord:**
  - Быстрое закрытие процессов Discord и очистка временных файлов (`Cache`, `Code Cache`, `GPUCache`) для решения проблем с загрузкой медиа.

---

## 🧭 Стратегии Flowseal: какую выбрать?

| Стратегия | Режим десинхронизации | Назначение и рекомендации |
| :--- | :--- | :--- |
| **General** ⭐ | `fake, multisplit` | **Основная рекомендация.** Подходит большинству провайдеров. Обходит блокировки YouTube и Discord подменой TLS ClientHello. |
| **General Alt** | `disorder2, split-pos=2` | **Альтернативная стратегия.** Используется, если провайдер научился распознавать стандартный fake-пакет. Меняет порядок следования TCP-сегментов. |
| **General Alt 2** | `multisplit (midsld)` | **Для жестких ТСПУ.** Агрессивная фрагментация SNI в середине пакета для обхода глубокого инспектирования. |
| **Discord Only** | `fake UDP repeats` | Специализированная стратегия только для шлюзов Discord (голосовые каналы, видеосвязь, звонки). |

---

## 🔒 Безопасность и частые вопросы (FAQ)

<details>
<summary><b>❓ Почему программе требуются права администратора?</b></summary>
<br>
Для работы утилиты <code>winws.exe</code> используется сертифицированный драйвер фильтрации сетевых пакетов <b>WinDivert</b> (Windows Packet Divert). В операционных системах Windows установка и взаимодействие с низкоуровневыми сетевыми драйверами архитектурно разрешены исключительно учётным записям с правами Администратора.
</details>

<details>
<summary><b>❓ Антивирус или SmartScreen ругается на файл. Это безопасно?</b></summary>
<br>
Да. Исходный код проекта полностью открыт и компилируется на серверах GitHub Actions. 
Сетевой драйвер <code>WinDivert</code> часто детектируется антивирусами (Windows Defender и др.) по эвристическим сигнатурам как <i>RiskTool / HackTool</i> именно потому, что он умеет перехватывать и модифицировать системные сетевые пакеты. Это стандартное поведение для сетевых утилит обхода DPI.
</details>

<details>
<summary><b>❓ Что делать, если YouTube или Discord всё равно не работают?</b></summary>
<br>
1. Перейдите на вкладку <b>«🩺 Диагностика»</b> и убедитесь, что в системе не запущены другие VPN-сервисы, AdGuard или GoodbyeDPI.<br>
2. Перейдите на вкладку <b>«📜 Стратегии»</b> и попробуйте переключиться на <code>General Alt</code> или <code>General Alt 2</code>.<br>
3. Перейдите на вкладку <b>«🧪 Тестировщик»</b> и нажмите <i>«Запустить тест»</i>, чтобы увидеть статус ответа серверов.
</details>

---

## 🏗️ Архитектура проекта

```
zapret-winws-gui/
├── ZapretWinwsGui.sln               # Решение .NET 9
├── build.ps1                        # Пайплайн сборки, тестирования и публикации
├── installer/
│   └── zapret-winws-gui.iss         # Конфигурация Inno Setup (Setup.exe)
├── ZapretWinwsGui/                  # Основное приложение Avalonia UI
│   ├── Assets/                      # Графика, иконка трея
│   ├── Core/                        # Ядро бизнес-логики (автономное)
│   │   ├── Bundling/                # FlowsealBundleManager (загрузка winws и списков)
│   │   ├── Diagnostics/             # ConflictDiagnostics (проверка конфликтов и прав)
│   │   ├── Discord/                 # DiscordCacheCleaner (очистка кэша Discord)
│   │   ├── Process/                 # WinwsProcessManager & SystemProcessRunner
│   │   ├── Services/                # WindowsServiceManager (управление sc.exe)
│   │   ├── Strategies/              # StrategyParser & StrategyModel
│   │   └── Tester/                  # StrategyTester (HTTP Range тесты)
│   ├── ViewModels/                  # MVVM ViewModels (CommunityToolkit.Mvvm)
│   └── Views/                       # XAML Fluent Design интерфейс
└── ZapretWinwsGui.Tests/            # Автоматизированные модульные тесты xUnit (19 тестов)
```

---

## 🛠️ Сборка для разработчиков

### Требования
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Windows 10 / 11 (или Linux / macOS для разработки и прогона тестов)

### Сборка и запуск:
```powershell
# Запуск автоматических тестов
dotnet test

# Запуск приложения из исходного кода
dotnet run --project ZapretWinwsGui/ZapretWinwsGui.csproj

# Автономная публикация win-x64 в папку ./publish
.\build.ps1

# Сборка установщика Inno Setup (при наличии Inno Setup 6)
.\build.ps1 -PackageInstaller
```

---

## 📄 Лицензия

Проект распространяется под свободной лицензией [MIT](LICENSE).  
Основан на разработках [bol-van](https://github.com/bol-van/zapret) и сборках [Flowseal](https://github.com/Flowseal/zapret-discord-youtube).
