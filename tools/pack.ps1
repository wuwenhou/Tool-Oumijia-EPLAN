# Builds the Tool Oumijia release zip:
#   output\Tool-Oumijia-EPLAN-v<Version>-win-x64.zip
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File tools/pack.ps1 -Version 0.1.0
#
# The archive is self-contained (no .NET runtime installation required on the
# target machine) and includes README.txt + VERSION.txt for end users.

[CmdletBinding()]
param(
    [string]$Version = "0.1.0",
    [string]$Runtime = "win-x64",
    [switch]$FrameworkDependent
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$outputDir = Join-Path $root "output"
$stageName = "Tool-Oumijia-EPLAN-v$Version-$Runtime"
$stageDir = Join-Path $outputDir $stageName
$publishDir = Join-Path $stageDir "_publish"

if (Test-Path $stageDir) { Remove-Item $stageDir -Recurse -Force }
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

$project = Join-Path $root "src\Oumijia.Tool\Oumijia.Tool.csproj"

$publishArgs = @(
    "publish", $project,
    "-c", "Release",
    "-f", "net8.0-windows",
    "-r", $Runtime,
    "-o", $publishDir,
    "-p:Version=$Version",
    "-p:DebugType=none"
)
if ($FrameworkDependent) {
    $publishArgs += @("--self-contained", "false")
} else {
    $publishArgs += @("--self-contained", "true")
}

Write-Host "[*] dotnet publish ..." -ForegroundColor Cyan
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

# Move published app to the root of the stage folder.
Get-ChildItem -Path $publishDir -Force | Move-Item -Destination $stageDir -Force
Remove-Item $publishDir -Recurse -Force

# VERSION.txt
$exePath = Join-Path $stageDir "Tool-Oumijia.exe"
$exeHash = (Get-FileHash $exePath -Algorithm SHA256).Hash
@(
    "Tool Oumijia (EPLAN Edition) v$Version"
    "Build date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    "Runtime: $Runtime (self-contained: $(-not $FrameworkDependent))"
    ""
    "Tool-Oumijia.exe SHA256:"
    $exeHash
) | Set-Content -Path (Join-Path $stageDir "VERSION.txt") -Encoding UTF8

# End-user README
Copy-Item (Join-Path $PSScriptRoot "release-extras\README.txt") (Join-Path $stageDir "README.txt") -Force

# Zip
$zipPath = Join-Path $outputDir "$stageName.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Write-Host "[*] Compress-Archive ..." -ForegroundColor Cyan
Compress-Archive -Path (Join-Path $stageDir "*") -DestinationPath $zipPath -CompressionLevel Optimal

$zipHash = (Get-FileHash $zipPath -Algorithm SHA256).Hash

Write-Host ""
Write-Host "[OK] Package : $zipPath" -ForegroundColor Green
Write-Host "[OK] ZIP SHA256 : $zipHash" -ForegroundColor Green
Write-Host "[OK] EXE SHA256 : $exeHash" -ForegroundColor Green
