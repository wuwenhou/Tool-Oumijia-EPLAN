@echo off
chcp 65001 >nul
echo ==============================================
echo   TOOL OUMIJIA - GO SCRIPT EDITION
echo ==============================================
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Uninstall-Script.ps1"
echo.
pause
