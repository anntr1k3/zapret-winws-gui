<#
.SYNOPSIS
    Build and publish script for Zapret WinWS GUI.
.DESCRIPTION
    Runs tests, publishes standalone single-file x64 executable, and optionally packages Inno Setup installer.
#>

param(
    [string]$Configuration = "Release",
    [switch]$SkipTests,
    [switch]$PackageInstaller
)

$ErrorActionPreference = "Stop"
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition

Write-Host "=== 🛡️ Zapret WinWS GUI Build Pipeline ===" -ForegroundColor Cyan
Write-Host "Project Root: $ScriptRoot"
Write-Host "Configuration: $Configuration"

# 1. Run Tests
if (-not $SkipTests) {
    Write-Host "`n[1/3] Запуск автоматических тестов xUnit..." -ForegroundColor Yellow
    dotnet test "$ScriptRoot\ZapretWinwsGui.Tests\ZapretWinwsGui.Tests.csproj" -c $Configuration --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Тесты завершились с ошибкой!"
    }
    Write-Host "✓ Все тесты успешно пройдены." -ForegroundColor Green
}

# 2. Publish Standalone Win-x64
$PublishDir = Join-Path $ScriptRoot "publish"
Write-Host "`n[2/3] Публикация автономного приложения win-x64 в '$PublishDir'..." -ForegroundColor Yellow

if (Test-Path $PublishDir) {
    Remove-Item -Recurse -Force $PublishDir
}

dotnet publish "$ScriptRoot\ZapretWinwsGui\ZapretWinwsGui.csproj" `
    -c $Configuration `
    -r win-x64 `
    -p:PublishSingleFile=true `
    --self-contained true `
    -o $PublishDir `
    --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Error "Ошибка при публикации dotnet publish!"
}

$exePath = Join-Path $PublishDir "ZapretWinwsGui.exe"
if (Test-Path $exePath) {
    $exeSize = (Get-Item $exePath).Length / 1MB
    Write-Host ("✓ Исполняемый файл успешно собран: ZapretWinwsGui.exe ({0:N2} MB)" -f $exeSize) -ForegroundColor Green
}

# 3. Optional Inno Setup Compiler
if ($PackageInstaller) {
    Write-Host "`n[3/3] Сборка установщика Inno Setup..." -ForegroundColor Yellow
    $isccPaths = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
    )
    $iscc = $isccPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

    if ($iscc) {
        $issFile = Join-Path $ScriptRoot "installer\zapret-winws-gui.iss"
        & $iscc $issFile
        Write-Host "✓ Установщик успешно собран в папке dist/" -ForegroundColor Green
    } else {
        Write-Warning "Inno Setup (ISCC.exe) не найден в системе. Пропускаю генерацию Setup.exe."
    }
}

Write-Host "`n=== ✨ Сборка завершена успешно! ===" -ForegroundColor Cyan
