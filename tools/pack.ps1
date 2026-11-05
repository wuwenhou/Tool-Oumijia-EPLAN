# Packs the Tool Oumijia release zip:
#   output\Tool-Oumijia-v<Version>.zip
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File tools/pack.ps1 -Version 0.6.2
#
# Noi dung zip (2 editions + installer thong minh):
#   README.txt + VERSION.txt
#   Install-Tool-Oumijia.bat/.ps1 + Uninstall-Tool-Oumijia.bat/.ps1
#   addin\        : Oumijia.EplAddin.EDZ.dll + OumijiaRegister.cs + Install/Uninstall + README
#   script\       : ToolOumijiaScript.cs + OumijiaRegisterScript.cs + Install/Uninstall + README
#   third-party\  : VEE.EplTableMaker.dll + VEE.ExportConnections.dll + metadata + notice
#
# Yeu cau: add-in da duoc build (src\Oumijia.EplanAddIn\bin\Release\net48\Oumijia.EplAddin.EDZ.dll).

[CmdletBinding()]
param(
    [string]$Version = "0.6.2"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$outputDir = Join-Path $root "output"
$stageName = "Tool-Oumijia-v$Version"
$stageDir = Join-Path $outputDir $stageName

$dll = Join-Path $root "src\Oumijia.EplanAddIn\bin\Release\net48\Oumijia.EplAddin.EDZ.dll"
if (-not (Test-Path $dll)) {
    throw "Add-in chua duoc build: $dll`nChay: dotnet build src/Oumijia.EplanAddIn/Oumijia.EplanAddIn.csproj -c Release -p:EplanApiDir=`"...`" (xem docs/BUILD.md)"
}

if (Test-Path $stageDir) { Remove-Item $stageDir -Recurse -Force }
New-Item -ItemType Directory -Path $stageDir -Force | Out-Null

# --- addin\ ---
$addinStage = Join-Path $stageDir "addin"
New-Item -ItemType Directory -Path $addinStage -Force | Out-Null
Copy-Item $dll (Join-Path $addinStage "Oumijia.EplAddin.EDZ.dll") -Force
Copy-Item (Join-Path $root "tools\eplan-scripts\OumijiaRegister.cs") $addinStage -Force
foreach ($f in @("Install-AddIn.ps1", "Install-AddIn.bat", "Uninstall-AddIn.ps1", "Uninstall-AddIn.bat", "README.txt")) {
    Copy-Item (Join-Path $root "tools\install-addin\$f") $addinStage -Force
}

# --- script\ ---
$scriptStage = Join-Path $stageDir "script"
New-Item -ItemType Directory -Path $scriptStage -Force | Out-Null
foreach ($f in @("ToolOumijiaScript.cs", "OumijiaRegisterScript.cs",
                 "Install-Script.ps1", "Install-Script.bat",
                 "Uninstall-Script.ps1", "Uninstall-Script.bat", "README.txt")) {
    Copy-Item (Join-Path $root "tools\script-edition\$f") $scriptStage -Force
}

# --- third-party\ ---
$thirdPartyStage = Join-Path $stageDir "third-party"
New-Item -ItemType Directory -Path $thirdPartyStage -Force | Out-Null
foreach ($folder in @("VEE.EplTableMaker", "VEE.ExportConnections")) {
    $source = Join-Path $root "third-party\$folder"
    if (-not (Test-Path $source)) {
        throw "Thieu bo add-in ben thu ba: $source"
    }
    $destination = Join-Path $thirdPartyStage $folder
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Copy-Item (Join-Path $source "*.dll") $destination -Force
}
foreach ($file in @("THIRD-PARTY-NOTICES.txt", "README.txt")) {
    Copy-Item (Join-Path $root "third-party\$file") $thirdPartyStage -Force
}
Copy-Item (Join-Path $root "tools\third-party\OumijiaRegisterThirdParty.cs") $thirdPartyStage -Force

# --- root: installer thong minh + README ---
foreach ($f in @("Install-Tool-Oumijia.ps1", "Install-Tool-Oumijia.bat",
                 "Uninstall-Tool-Oumijia.ps1", "Uninstall-Tool-Oumijia.bat", "README.txt")) {
    Copy-Item (Join-Path $root "tools\install-all\$f") $stageDir -Force
}

# --- VERSION.txt ---
$stageDll = Join-Path $addinStage "Oumijia.EplAddin.EDZ.dll"
$dllHash = (Get-FileHash $stageDll -Algorithm SHA256).Hash
$dllVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($stageDll).FileVersion
$scriptHash = (Get-FileHash (Join-Path $scriptStage "ToolOumijiaScript.cs") -Algorithm SHA256).Hash
$tableDllHash = (Get-FileHash (Join-Path $thirdPartyStage "VEE.EplTableMaker\VEE.EplTableMaker.dll") -Algorithm SHA256).Hash
$exportDllHash = (Get-FileHash (Join-Path $thirdPartyStage "VEE.ExportConnections\VEE.ExportConnections.dll") -Algorithm SHA256).Hash
$thirdPartyRegisterHash = (Get-FileHash (Join-Path $thirdPartyStage "OumijiaRegisterThirdParty.cs") -Algorithm SHA256).Hash
@(
    "Tool Oumijia (EPLAN Edition) v$Version"
    "Build date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    "Add-in DLL version: $dllVersion"
    ""
    "Oumijia.EplAddin.EDZ.dll SHA256:"
    $dllHash
    ""
    "ToolOumijiaScript.cs SHA256:"
    $scriptHash
    ""
    "VEE.EplTableMaker.dll SHA256:"
    $tableDllHash
    ""
    "VEE.ExportConnections.dll SHA256:"
    $exportDllHash
    ""
    "OumijiaRegisterThirdParty.cs SHA256:"
    $thirdPartyRegisterHash
) | Set-Content -Path (Join-Path $stageDir "VERSION.txt") -Encoding UTF8

# --- zip ---
$zipPath = Join-Path $outputDir "$stageName.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Write-Host "[*] Compress-Archive ..." -ForegroundColor Cyan
Compress-Archive -Path (Join-Path $stageDir "*") -DestinationPath $zipPath -CompressionLevel Optimal

$zipHash = (Get-FileHash $zipPath -Algorithm SHA256).Hash

Write-Host ""
Write-Host "[OK] Package    : $zipPath" -ForegroundColor Green
Write-Host "[OK] ZIP SHA256 : $zipHash" -ForegroundColor Green
Write-Host "[OK] DLL SHA256 : $dllHash" -ForegroundColor Green
Write-Host "[OK] CS  SHA256 : $scriptHash" -ForegroundColor Green
Write-Host "[OK] VEE Table  : $tableDllHash" -ForegroundColor Green
Write-Host "[OK] VEE Export : $exportDllHash" -ForegroundColor Green
Write-Host "[OK] TP Reg CS : $thirdPartyRegisterHash" -ForegroundColor Green
