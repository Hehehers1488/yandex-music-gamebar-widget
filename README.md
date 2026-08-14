# Yandex Music widget for Xbox Game Bar

[![CI](https://github.com/Hehehers1488/ymusic-gamebar-widget/actions/workflows/ci.yml/badge.svg)](https://github.com/Hehehers1488/ymusic-gamebar-widget/actions/workflows/ci.yml)

A small UWP widget that shows the currently playing Yandex Music track and lets you control it
(play/pause, next, previous) right inside the Xbox Game Bar overlay (Win + G) — without switching
out of the game.

The widget reads track info through the system media session (SMTC) published by the
Yandex Music desktop app. No tokens, no accounts, no extra services.

---

## Features

- Current track: title, artist, album art, progress bar with elapsed/total time
- Controls: play/pause, next, previous (with a `WM_APPCOMMAND` fallback if SMTC is unresponsive)
- Auto-picks the Yandex Music media session (won't touch Spotify/Groove/etc.)
- Works on Windows 10 2004+ (build 19041), x64 / ARM64
- Distributed as a signed MSIX package

## Screenshot

_Coming soon._

## Installation

> Windows may show a SmartScreen / "Unknown publisher" warning — the package is signed with a
> self-signed certificate. This is expected for a hobby project; review and accept.

1. Open PowerShell (not an elevated window is enough on most systems):
   ```powershell
   Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
   irm https://github.com/Hehehers1488/ymusic-gamebar-widget/releases/latest/download/install.ps1 | iex
   ```
2. Or download `install.ps1` from the [Releases](https://github.com/Hehehers1488/ymusic-gamebar-widget/releases)
   page and run it:
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\install.ps1
   ```
3. Open **Xbox Game Bar** (Win + G) → **Widgets** → find **Yandex Music** → click to add it.
4. Play a track in the Yandex Music app. The widget updates automatically.

Manual install: grab `YMusicGameBarWidget_x64.msix` + `YMusicGameBarWidget.cer` from a release,
import the `.cer` into **Trusted People** (`certmgr.msc` → Current User → Trusted People),
then `Add-AppxPackage -Path .\YMusicGameBarWidget_x64.msix`.

## Building from source

Requirements: Visual Studio 2022 Build Tools with **Universal Windows Platform** workload
(`Microsoft.VisualStudio.Workload.UniversalBuildTools`) and Windows 10 SDK 19041.

```powershell
# create a local signing certificate (tools/make-cert.ps1)
.\tools\make-cert.ps1

# build an installable MSIX
msbuild src\YMusicGameBarWidget\YMusicGameBarWidget.csproj `
  /t:Restore,Build `
  /p:Configuration=Release /p:Platform=x64 `
  /p:AppxPackageSigningEnabled=true `
  /p:PackageCertificateKeyFile="..\..\certs\YMusicWidget.pfx" `
  /p:PackageCertificatePassword=YMusicWidget `
  /p:UapAppxPackageBuildMode=SideloadOnly `
  /p:AppxPackageDir="..\..\AppPackages" `
  /p:AppxBundle=Never /p:GenerateAppxPackageOnBuild=true
```

Then install with `.\tools\install-local.ps1` (or the manual steps above).

## Roadmap

- [ ] Phase 2: built-in player inside the widget via the unofficial Yandex Music API
      (OAuth Device Flow) — search, playlists, full queue control
- [ ] Proper widget icons / tile branding
- [ ] Settings (session override, theme)

## License

[MIT](LICENSE). Not affiliated with Yandex. Yandex Music is a trademark of Yandex LLC.

---

# Виджет Яндекс Музыки для Xbox Game Bar

[![CI](https://github.com/Hehehers1488/ymusic-gamebar-widget/actions/workflows/ci.yml/badge.svg)](https://github.com/Hehehers1488/ymusic-gamebar-widget/actions/workflows/ci.yml)

Небольшой UWP-виджет, который показывает текущий трек Яндекс Музыки и позволяет управлять им
(играть/пауза, следующий, предыдущий) прямо в оверлее Xbox Game Bar (Win + G) — не выходя из игры.

Виджет читает информацию о треке через системную медиа-сессию (SMTC), которую публикует
десктопное приложение Яндекс Музыки. Никаких токенов, аккаунтов и сторонних сервисов.

## Возможности

- Текущий трек: название, исполнитель, обложка, прогресс с временем
- Управление: play/pause, следующий, предыдущий (с фолбэком через `WM_APPCOMMAND`)
- Автоматически выбирает сессию именно Яндекс Музыки (Spotify/Groove и др. не трогает)
- Windows 10 2004+ (build 19041), x64 / ARM64
- Распространяется как подписанный MSIX-пакет

## Установка

> Windows может показать предупреждение SmartScreen / «Неизвестный издатель» — пакет подписан
> самоподписанным сертификатом. Для пет-проекта это нормально; проверьте и примите.

1. PowerShell (не обязательно от администратора):
   ```powershell
   Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
   irm https://github.com/Hehehers1488/ymusic-gamebar-widget/releases/latest/download/install.ps1 | iex
   ```
2. Либо скачайте `install.ps1` со страницы [Releases](https://github.com/Hehehers1488/ymusic-gamebar-widget/releases) и запустите:
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\install.ps1
   ```
3. Откройте **Xbox Game Bar** (Win + G) → **Виджеты** → найдите **Yandex Music** → добавьте.
4. Включите трек в приложении Яндекс Музыки — виджет обновится автоматически.

Ручная установка: возьмите `YMusicGameBarWidget_x64.msix` + `YMusicGameBarWidget.cer` из релиза,
импортируйте `.cer` в **Доверенные люди** (`certmgr.msc` → Текущий пользователь → Доверенные люди),
затем `Add-AppxPackage -Path .\YMusicGameBarWidget_x64.msix`.

## Сборка из исходников

Требуется Visual Studio 2022 Build Tools с нагрузкой **Универсальная платформа Windows**
(`Microsoft.VisualStudio.Workload.UniversalBuildTools`) и Windows 10 SDK 19041.

```powershell
# создать локальный сертификат подписи (tools/make-cert.ps1)
.\tools\make-cert.ps1

# собрать устанавливаемый MSIX
msbuild src\YMusicGameBarWidget\YMusicGameBarWidget.csproj `
  /t:Restore,Build `
  /p:Configuration=Release /p:Platform=x64 `
  /p:AppxPackageSigningEnabled=true `
  /p:PackageCertificateKeyFile="..\..\certs\YMusicWidget.pfx" `
  /p:PackageCertificatePassword=YMusicWidget `
  /p:UapAppxPackageBuildMode=SideloadOnly `
  /p:AppxPackageDir="..\..\AppPackages" `
  /p:AppxBundle=Never /p:GenerateAppxPackageOnBuild=true
```

Установка собранного пакета: `.\tools\install-local.ps1` (или ручные шаги выше).

## Планы

- [ ] Фаза 2: встроенный плеер в виджете через неофициальный API Яндекс Музыки
      (OAuth Device Flow) — поиск, плейлисты, полный контроль очереди
- [ ] Собственные иконки виджета / тайлов
- [ ] Настройки (переопределение сессии, тема)

## Лицензия

[MIT](LICENSE). Не аффилирован с Яндексом. Яндекс Музыка — товарный знак ООО «Яндекс».
