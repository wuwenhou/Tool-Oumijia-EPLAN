@echo off
chcp 65001 >nul
echo ==================================================
echo   TOOL OUMIJIA - GO CAI DAT (ca hai ban)
echo ==================================================
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Uninstall-Tool-Oumijia.ps1"
echo.
pause
