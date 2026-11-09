# Build the EPLAN 2026 Script Edition package (no Add-in DLL).
# Payload: Script Edition (.cs) + net48 shared UI/EDZ runtime taken from the
# same build used for the 2022 package, plus the smart installer.
# First run creates a candidate directory. Only -CreateZip after the release
# notes exist; the 2026 payload is NOT GUI UAT'd by this script, so the
# release notes must state the verification status honestly.
[CmdletBinding()]
param(
    [ValidatePattern('^0\.\d+\.\d+$')][string]$Version = '0.7.0',
    [string]$OutputDirectory = '',
    [ValidatePattern('^(-[a-zA-Z0-9]+)?$')][string]$CandidateSuffix = '',
    [switch]$CreateZip
)
$ErrorActionPreference = 'Stop'
$ScriptRoot = $PSScriptRoot
if (-not $ScriptRoot) { $ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path }
if (-not $OutputDirectory) { $OutputDirectory = Join-Path (Split-Path -Parent $ScriptRoot) 'output' }
$root = Split-Path -Parent $ScriptRoot

$name = "Tool-Oumijia-v$Version-2026"
$notes = Join-Path $root "docs\releases\v$Version-2026.md"
if (-not (Test-Path -LiteralPath $notes -PathType Leaf)) {
    throw "Missing 2026 release notes: $notes"
}
$stage = Join-Path $OutputDirectory ($name + '.candidate' + $CandidateSuffix)
$zip = Join-Path $OutputDirectory ($name + '.zip')
$runtimeDir = Join-Path $root 'src\Oumijia.EplanAddIn\bin\Release\net48'

if ($CreateZip) {
    if (-not (Test-Path -LiteralPath $stage -PathType Container) -or (Test-Path -LiteralPath $zip)) {
        throw 'Candidate missing or release ZIP already exists; refusing overwrite.'
    }
    $list = @(Import-Csv -LiteralPath (Join-Path $stage 'SHA256.csv'))
    if ($list.Count -lt 15) { throw 'Candidate manifest missing or incomplete.' }
    foreach ($row in $list) {
        $file = Join-Path $stage $row.Path
        if (-not (Test-Path -LiteralPath $file -PathType Leaf) -or
            (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $row.SHA256) {
            throw "Candidate changed since verification: $($row.Path)"
        }
    }
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -CompressionLevel Optimal
    Write-Host "ZIP: $zip"
    Write-Host "SHA256: $((Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash)"
    return
}

if ((Test-Path -LiteralPath $stage) -or (Test-Path -LiteralPath $zip)) {
    throw "Existing candidate or ZIP for $name; do not overwrite previous work."
}
if (-not (Test-Path -LiteralPath (Join-Path $runtimeDir 'Oumijia.EplAddin.EDZ.dll') -PathType Leaf)) {
    throw "Shared runtime build missing: build with the 2022 API first (see docs/BUILD.md)."
}

# --- script\ ---
$scriptStage = Join-Path $stage 'script'
New-Item -ItemType Directory -Path $scriptStage -Force | Out-Null
foreach ($f in @('ToolOumijiaScript.cs', 'OumijiaRegisterScript.cs',
                 'Install-Script.ps1', 'Install-Script.bat',
                 'Uninstall-Script.ps1', 'Uninstall-Script.bat', 'README.txt')) {
    Copy-Item -LiteralPath (Join-Path $root ('tools\script-edition\' + $f)) -Destination $scriptStage -Force
}

# --- script\portal\ (net48 shared runtime, no Add-in DLL) ---
$portalStage = Join-Path $scriptStage 'portal'
New-Item -ItemType Directory -Path $portalStage -Force | Out-Null
$shared = Get-ChildItem -LiteralPath $runtimeDir -Filter '*.dll' -File |
    Where-Object { $_.Name -ne 'Oumijia.EplAddin.EDZ.dll' -and
                   $_.Name -notmatch '^(Oumijia\.PortalHost|Microsoft\.Web\.WebView2|WebView2Loader)' }
foreach ($item in $shared) {
    Copy-Item -LiteralPath $item.FullName -Destination (Join-Path $portalStage $item.Name) -Force
}
foreach ($required in @('Oumijia.EplanUi.dll', 'Oumijia.EdzReader.dll', 'SharpCompress.dll')) {
    if (-not (Test-Path -LiteralPath (Join-Path $portalStage $required) -PathType Leaf)) {
        throw "Missing required shared runtime DLL: $required"
    }
}
if (@(Get-ChildItem -LiteralPath $portalStage -Filter '*.dll' -File | Where-Object {
        $_.Name -match '^(Oumijia\.PortalHost|Microsoft\.Web\.WebView2|WebView2Loader)' }).Count -gt 0) {
    throw 'Stale WebView2/PortalHost DLL leaked into the 2026 package.'
}

# --- docs\ ---
$docsStage = Join-Path $stage 'docs'
New-Item -ItemType Directory -Path $docsStage -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'docs\INSTALL.md') -Destination $docsStage -Force
Copy-Item -LiteralPath (Join-Path $root 'docs\USER-GUIDE-vi.md') -Destination $docsStage -Force
Copy-Item -LiteralPath $notes -Destination (Join-Path $docsStage "v$Version-2026.md") -Force

# --- root: smart installer + README ---
foreach ($f in @('Install-Tool-Oumijia.ps1', 'Install-Tool-Oumijia.bat',
                  'Uninstall-Tool-Oumijia.ps1', 'Uninstall-Tool-Oumijia.bat', 'EplanTargets.ps1')) {
    Copy-Item -LiteralPath (Join-Path $root ('tools\install-all\' + $f)) -Destination $stage -Force
}
Copy-Item -LiteralPath (Join-Path $root 'tools\install-all\README-2026.txt') -Destination (Join-Path $stage 'README.txt') -Force

# --- VERSION.txt ---
$scriptText = Get-Content -LiteralPath (Join-Path $scriptStage 'ToolOumijiaScript.cs') -Raw
$versionMatch = [regex]::Match($scriptText, 'ScriptVersion\s*=\s*"([^"]+)"')
if (-not $versionMatch.Success) { throw 'ScriptVersion constant not found in ToolOumijiaScript.cs' }
$scriptVersion = $versionMatch.Groups[1].Value

$now = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
@(
    "Tool Oumijia (EPLAN) v$Version - EPLAN Education 2026.0.3 Script Edition"
    "Build date: $now"
    "ScriptVersion: $scriptVersion"
    'Payload: Script Edition only (no Add-in DLL)'
    ''
    'ToolOumijiaScript.cs SHA256:'
    (Get-FileHash -LiteralPath (Join-Path $scriptStage 'ToolOumijiaScript.cs') -Algorithm SHA256).Hash
    'OumijiaRegisterScript.cs SHA256:'
    (Get-FileHash -LiteralPath (Join-Path $scriptStage 'OumijiaRegisterScript.cs') -Algorithm SHA256).Hash
    'Oumijia.EplanUi.dll SHA256:'
    (Get-FileHash -LiteralPath (Join-Path $portalStage 'Oumijia.EplanUi.dll') -Algorithm SHA256).Hash
    'Oumijia.EdzReader.dll SHA256:'
    (Get-FileHash -LiteralPath (Join-Path $portalStage 'Oumijia.EdzReader.dll') -Algorithm SHA256).Hash
    'SharpCompress.dll SHA256:'
    (Get-FileHash -LiteralPath (Join-Path $portalStage 'SharpCompress.dll') -Algorithm SHA256).Hash
) | Set-Content -LiteralPath (Join-Path $stage 'VERSION.txt') -Encoding UTF8

# --- SHA256.csv (paths sorted ordinally, like the 2022 manifest) ---
$manifest = Get-ChildItem -LiteralPath $stage -File -Recurse |
    Where-Object { $_.Name -ne 'SHA256.csv' } |
    Sort-Object @{ Expression = { $_.FullName.Substring($stage.Length).TrimStart('\') } } |
    ForEach-Object { [pscustomobject]@{ Path = $_.FullName.Substring($stage.Length).TrimStart('\'); SHA256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash } }
$manifest | Export-Csv -LiteralPath (Join-Path $stage 'SHA256.csv') -NoTypeInformation -Encoding UTF8

Write-Host "CANDIDATE: $stage"
Write-Host 'Script Edition is NOT GUI UAT-able on this machine; state the results in'
Write-Host 'docs/releases/v<version>-2026.md honestly, then run -CreateZip.'
