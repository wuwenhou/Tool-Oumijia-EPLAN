@echo off
chcp 65001 >nul
echo ==================================================
echo   TOOL OUMIJIA - CAI DAT (tu chon ban phu hop)
echo ==================================================
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-Tool-Oumijia.ps1" %*
set result=%errorlevel%
echo.
pause
exit /b %result%
