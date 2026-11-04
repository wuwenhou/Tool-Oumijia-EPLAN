@echo off
chcp 65001 >nul
echo ==============================================
echo   TOOL OUMIJIA - GO ADD-IN KHOI EPLAN
echo ==============================================
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Uninstall-AddIn.ps1"
echo.
pause
