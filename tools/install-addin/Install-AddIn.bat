@echo off
chcp 65001 >nul
echo ==============================================
echo   TOOL OUMIJIA - CAI DAT ADD-IN CHO EPLAN
echo ==============================================
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-AddIn.ps1"
echo.
pause
