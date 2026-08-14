@echo off
setlocal
title Yandex Music Widget - Installer
echo ================================================
echo   Yandex Music widget for Xbox Game Bar
echo   Installing... please wait.
echo ================================================
echo.
powershell -NoProfile -ExecutionPolicy Bypass -Command "try { irm https://github.com/Hehehers1488/yandex-music-gamebar-widget/releases/latest/download/install.ps1 | iex } catch { Write-Host ('Install failed: ' + $_.Exception.Message) -ForegroundColor Red }"
echo.
echo ================================================
echo If you saw no red error above - installation is done.
echo Open Xbox Game Bar (Win + G) ^> Widgets ^> Yandex Music.
echo ================================================
echo.
pause
