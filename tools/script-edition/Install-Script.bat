@echo off
chcp 65001 >nul
echo ==============================================
echo   TOOL OUMIJIA - CAI SCRIPT EDITION CHO EPLAN
echo ==============================================
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-Script.ps1"
echo.
pause
