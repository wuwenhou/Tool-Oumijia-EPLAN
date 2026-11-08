# Build the EPLAN Education 2026.0.3 Script Edition package.
# The 2026 Add-in was never runtime UAT'd, so this packer intentionally ships
# the verified Script Edition only (no addin\ folder, no net481 Add-in DLL).
[CmdletBinding()]
param(
    [string]$OutputDirectory = (Join-Path (Split-Path -Parent $PSScriptRoot) 'output'),
    [ValidatePattern('^(-[a-zA-Z0-9]+)?$')][string]$CandidateSuffix = '',
    [switch]$CreateZip
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$name = 'Tool-Oumijia-v0.6.0-2026'
$stage = Join-Path $OutputDirectory ($name + '.candidate' + $CandidateSuffix)
$zip = Join-Path $OutputDirectory ($name + '.zip')
$dllDir = Join-Path $root 'src\Oumijia.EplanAddIn\bin\Release\net48'
$scriptDir = Join-Path $root 'tools\script-edition'
$releaseNotes = Join-Path $root 'docs\releases\v0.6.0-2026.md'

if ($CreateZip) {
    if (-not (Test-Path -LiteralPath $stage -PathType Container) -or (Test-Path -LiteralPath $zip)) {
        throw 'Candidate missing or release ZIP already exists; refusing overwrite.'
    }
    $list = @(Import-Csv -LiteralPath (Join-Path $stage 'SHA256.csv'))
    if ($list.Count -lt 12) { throw 'Candidate manifest missing or incomplete.' }
    foreach ($row in $list) {
        $file = Join-Path $stage $row.Path
        if (-not (Test-Path -LiteralPath $file -PathType Leaf) -or
            (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $row.SHA256) {
            throw "Candidate changed since packaging: $($row.Path)"
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
foreach ($file in @('ToolOumijiaScript.cs','OumijiaRegisterScript.cs','Install-Script.ps1','Install-Script.bat',
                    'Uninstall-Script.ps1','Uninstall-Script.bat','README.txt')) {
    if (-not (Test-Path -LiteralPath (Join-Path $scriptDir $file) -PathType Leaf)) { throw "Missing script file: $file" }
}
if (-not (Test-Path -LiteralPath $releaseNotes -PathType Leaf)) { throw 'Missing docs/releases/v0.6.0-2026.md' }
$requiredShared = @('Oumijia.EplanUi.dll','Oumijia.EdzReader.dll','SharpCompress.dll')
foreach ($file in $requiredShared) {
    if (-not (Test-Path -LiteralPath (Join-Path $dllDir $file) -PathType Leaf)) { throw "Missing shared runtime DLL: $file" }
}
if ((Get-Item -LiteralPath (Join-Path $dllDir 'Oumijia.EplanUi.dll')).VersionInfo.FileVersion -ne '0.6.0.0') {
    throw 'Unexpected Oumijia.EplanUi version; this packer supports v0.6.0.'
}

$scriptStage = Join-Path $stage 'script'
$portalStage = Join-Path $scriptStage 'portal'
$docsStage = Join-Path $stage 'docs'
New-Item -ItemType Directory -Path $portalStage -Force | Out-Null
New-Item -ItemType Directory -Path $docsStage -Force | Out-Null

foreach ($file in @('ToolOumijiaScript.cs','OumijiaRegisterScript.cs',
                    'Install-Script.ps1','Install-Script.bat',
                    'Uninstall-Script.ps1','Uninstall-Script.bat','README.txt')) {
    Copy-Item -LiteralPath (Join-Path $scriptDir $file) -Destination $scriptStage -Force
}
Get-ChildItem -LiteralPath $dllDir -Filter '*.dll' -File |
    Where-Object { $_.Name -notmatch '^(Oumijia\.EplAddin\.EDZ|Oumijia\.PortalHost|Microsoft\.Web\.WebView2|WebView2Loader)' } |
    Copy-Item -Destination $portalStage -Force
foreach ($file in $requiredShared) {
    if (-not (Test-Path -LiteralPath (Join-Path $portalStage $file) -PathType Leaf)) { throw "Shared runtime copy missing: $file" }
}
if (@(Get-ChildItem -LiteralPath $portalStage -Filter '*.dll' -File | Where-Object {
        $_.Name -match '^(Oumijia\.PortalHost|Microsoft\.Web\.WebView2|WebView2Loader)' }).Count -gt 0) {
    throw 'Stale WebView2/PortalHost DLL leaked into the 2026 package.'
}

Copy-Item -LiteralPath (Join-Path $root 'docs\USER-GUIDE-vi.md') -Destination $docsStage -Force
Copy-Item -LiteralPath (Join-Path $root 'docs\INSTALL.md') -Destination $docsStage -Force
Copy-Item -LiteralPath $releaseNotes -Destination $docsStage -Force
foreach ($file in @('Install-Tool-Oumijia.ps1','Install-Tool-Oumijia.bat',
                    'Uninstall-Tool-Oumijia.ps1','Uninstall-Tool-Oumijia.bat','EplanTargets.ps1')) {
    Copy-Item -LiteralPath (Join-Path $root ('tools\install-all\' + $file)) -Destination $stage -Force
}
Copy-Item -LiteralPath (Join-Path $root 'tools\install-all\README-2026.txt') -Destination (Join-Path $stage 'README.txt') -Force

$scriptHash = (Get-FileHash -LiteralPath (Join-Path $scriptStage 'ToolOumijiaScript.cs') -Algorithm SHA256).Hash
$registerHash = (Get-FileHash -LiteralPath (Join-Path $scriptStage 'OumijiaRegisterScript.cs') -Algorithm SHA256).Hash
$uiHash = (Get-FileHash -LiteralPath (Join-Path $portalStage 'Oumijia.EplanUi.dll') -Algorithm SHA256).Hash
$readerHash = (Get-FileHash -LiteralPath (Join-Path $portalStage 'Oumijia.EdzReader.dll') -Algorithm SHA256).Hash
$compressHash = (Get-FileHash -LiteralPath (Join-Path $portalStage 'SharpCompress.dll') -Algorithm SHA256).Hash
@(
    'Tool Oumijia (EPLAN) v0.6.0 - EPLAN Education 2026.0.3 Script Edition'
    ('Build date: ' + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))
    'ScriptVersion: 0.6.0-script.1'
    'Payload: Script Edition only (no Add-in DLL)'
    ''
    'ToolOumijiaScript.cs SHA256:'
    $scriptHash
    'OumijiaRegisterScript.cs SHA256:'
    $registerHash
    'Oumijia.EplanUi.dll SHA256:'
    $uiHash
    'Oumijia.EdzReader.dll SHA256:'
    $readerHash
    'SharpCompress.dll SHA256:'
    $compressHash
) | Set-Content -LiteralPath (Join-Path $stage 'VERSION.txt') -Encoding UTF8

@(Get-ChildItem -LiteralPath $stage -File -Recurse | Where-Object { $_.Name -ne 'SHA256.csv' } |
    ForEach-Object { [pscustomobject]@{ Path=$_.FullName.Substring($stage.Length).TrimStart('\'); SHA256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash } }) |
    Export-Csv -LiteralPath (Join-Path $stage 'SHA256.csv') -NoTypeInformation -Encoding UTF8
Write-Host "CANDIDATE: $stage"
Write-Host 'Not a release ZIP. Run -CreateZip after reviewing the package.'
