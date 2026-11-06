# Builds the Tool Oumijia release zip (both editions):
#   output\Tool-Oumijia-v<Version>.zip
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File tools/pack.ps1 -Version 0.5.2
#
# Noi dung zip (2 editions + installer thong minh):
#   README.txt + VERSION.txt
#   Install-Tool-Oumijia.bat/.ps1 + Uninstall-Tool-Oumijia.bat/.ps1   (tu chon ban)
#   addin\  : add-in + shared Oumijia.EplanUi/EdzReader/PortalHost + WebView2 + installer
#   script\ : ToolOumijiaScript.cs + OumijiaRegisterScript.cs + Ui\Portal dependencies + installers
#
# Yeu cau: add-in da duoc build (src\Oumijia.EplanAddIn\bin\Release\net48\Oumijia.EplAddin.EDZ.dll).

[CmdletBinding()]
param(
    [string]$Version = "0.5.2"
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
$addinOutput = Split-Path -Parent $dll
$addinStage = Join-Path $stageDir "addin"
New-Item -ItemType Directory -Path $addinStage -Force | Out-Null
# Copy every managed runtime dependency emitted next to the add-in (all shared Oumijia DLLs).
Get-ChildItem -LiteralPath $addinOutput -Filter '*.dll' -File |
    Copy-Item -Destination $addinStage -Force
$addinLoader = Join-Path $addinOutput 'runtimes\win-x64\native\WebView2Loader.dll'
if (-not (Test-Path $addinLoader)) { throw "Thieu WebView2Loader.dll x64 trong Add-in output: $addinLoader" }
Copy-Item $addinLoader (Join-Path $addinStage 'WebView2Loader.dll') -Force
$requiredAddInDlls = @('Oumijia.EplAddin.EDZ.dll', 'Oumijia.EplanUi.dll', 'Oumijia.EdzReader.dll', 'Oumijia.PortalHost.dll', 'SharpCompress.dll', 'Microsoft.Web.WebView2.Core.dll', 'Microsoft.Web.WebView2.WinForms.dll')
foreach ($required in $requiredAddInDlls) {
    if (-not (Test-Path -LiteralPath (Join-Path $addinStage $required))) {
        throw "Thieu dependency bat buoc: $required"
    }
}
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
# Script Edition never compiles EPLAN UI/Portal against WebView2; both load as net48 assemblies.
$scriptSharedStage = Join-Path $scriptStage "portal"
New-Item -ItemType Directory -Path $scriptSharedStage -Force | Out-Null
Get-ChildItem -LiteralPath $addinStage -Filter '*.dll' -File |
    Where-Object { $_.Name -ne 'Oumijia.EplAddin.EDZ.dll' } |
    Copy-Item -Destination $scriptSharedStage -Force

# --- docs\ ---
$docsStage = Join-Path $stageDir "docs"
New-Item -ItemType Directory -Path $docsStage -Force | Out-Null
Copy-Item (Join-Path $root "docs\USER-GUIDE-vi.md") $docsStage -Force
Copy-Item (Join-Path $root "docs\releases\v0.5.2.md") $docsStage -Force

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
