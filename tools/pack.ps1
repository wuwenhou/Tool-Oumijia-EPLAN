# Builds the Tool Oumijia EPLAN add-in release zip:
#   output\Tool-Oumijia-AddIn-v<Version>.zip
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File tools/pack.ps1 -Version 0.3.0
#
# Yêu cầu: add-in đã được build (src\Oumijia.EplanAddIn\bin\Release\net48\Oumijia.EplAddin.EDZ.dll).
# Gói zip chứa (phẳng, không thư mục con):
#   Oumijia.EplAddin.EDZ.dll + OumijiaRegister.cs + Install/Uninstall + README.txt + VERSION.txt

[CmdletBinding()]
param(
    [string]$Version = "0.3.0"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$outputDir = Join-Path $root "output"
$stageName = "Tool-Oumijia-AddIn-v$Version"
$stageDir = Join-Path $outputDir $stageName

$dll = Join-Path $root "src\Oumijia.EplanAddIn\bin\Release\net48\Oumijia.EplAddin.EDZ.dll"
if (-not (Test-Path $dll)) {
    throw "Add-in chua duoc build: $dll`nChay: dotnet build src/Oumijia.EplanAddIn/Oumijia.EplanAddIn.csproj -c Release -p:EplanApiDir=`"...`" (xem docs/BUILD.md)"
}

if (Test-Path $stageDir) { Remove-Item $stageDir -Recurse -Force }
New-Item -ItemType Directory -Path $stageDir -Force | Out-Null

Copy-Item $dll (Join-Path $stageDir "Oumijia.EplAddin.EDZ.dll") -Force
Copy-Item (Join-Path $root "tools\eplan-scripts\OumijiaRegister.cs") $stageDir -Force
foreach ($f in @("Install-AddIn.ps1", "Install-AddIn.bat", "Uninstall-AddIn.ps1", "Uninstall-AddIn.bat", "README.txt")) {
    Copy-Item (Join-Path $root "tools\install-addin\$f") $stageDir -Force
}

# VERSION.txt
$stageDll = Join-Path $stageDir "Oumijia.EplAddin.EDZ.dll"
$dllHash = (Get-FileHash $stageDll -Algorithm SHA256).Hash
$dllVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($stageDll).FileVersion
@(
    "Tool Oumijia (EPLAN Edition) - Add-in v$Version"
    "Build date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    "Add-in DLL version: $dllVersion"
    ""
    "Oumijia.EplAddin.EDZ.dll SHA256:"
    $dllHash
) | Set-Content -Path (Join-Path $stageDir "VERSION.txt") -Encoding UTF8

# Zip (phang: noi dung stage nam o goc zip)
$zipPath = Join-Path $outputDir "$stageName.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Write-Host "[*] Compress-Archive ..." -ForegroundColor Cyan
Compress-Archive -Path (Join-Path $stageDir "*") -DestinationPath $zipPath -CompressionLevel Optimal

$zipHash = (Get-FileHash $zipPath -Algorithm SHA256).Hash

Write-Host ""
Write-Host "[OK] Package    : $zipPath" -ForegroundColor Green
Write-Host "[OK] ZIP SHA256 : $zipHash" -ForegroundColor Green
Write-Host "[OK] DLL SHA256 : $dllHash" -ForegroundColor Green
