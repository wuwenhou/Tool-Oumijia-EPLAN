# Build an isolated Electric P8 2022.0.3 package from an API-2022/net48 build.
# First run creates a candidate directory. Only -CreateZip after GUI UAT succeeds.
[CmdletBinding()]
param(
    [string]$OutputDirectory = (Join-Path (Split-Path -Parent $PSScriptRoot) 'output'),
    [ValidatePattern('^(-[a-zA-Z0-9]+)?$')][string]$CandidateSuffix = '',
    [switch]$CreateZip
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$name = 'Tool-Oumijia-v0.6.0-2022'
$stage = Join-Path $OutputDirectory ($name + '.candidate' + $CandidateSuffix)
$zip = Join-Path $OutputDirectory ($name + '.zip')
$dllDir = Join-Path $root 'src\Oumijia.EplanAddIn\bin\Release\net48'
$addinDll = Join-Path $dllDir 'Oumijia.EplAddin.EDZ.dll'

if ($CreateZip) {
    if (-not (Test-Path -LiteralPath $stage -PathType Container) -or (Test-Path -LiteralPath $zip)) {
        throw 'Candidate missing or release ZIP already exists; refusing overwrite.'
    }
    $list = @(Import-Csv -LiteralPath (Join-Path $stage 'SHA256.csv'))
    if ($list.Count -lt 8) { throw 'Candidate manifest missing or incomplete.' }
    foreach ($row in $list) {
        $file = Join-Path $stage $row.Path
        if (-not (Test-Path -LiteralPath $file -PathType Leaf) -or
            (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $row.SHA256) {
            throw "Candidate changed since UAT: $($row.Path)"
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
if (-not (Test-Path -LiteralPath $addinDll -PathType Leaf)) { throw 'Build net48 API 2022 before packaging.' }
$newer = @(Get-ChildItem (Join-Path $root 'src') -Recurse -File -Include '*.cs','*.csproj' |
    Where-Object { $_.FullName -notmatch '[\\/]bin[\\/]|[\\/]obj[\\/]' -and $_.LastWriteTimeUtc -gt (Get-Item $addinDll).LastWriteTimeUtc })
if ($newer.Count -gt 0) { throw "Add-in build older than source: $($newer[0].FullName)" }
$required = @('Oumijia.EplAddin.EDZ.dll','Oumijia.EplanUi.dll','Oumijia.EdzReader.dll','SharpCompress.dll')
foreach ($file in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $dllDir $file) -PathType Leaf)) { throw "Missing required DLL: $file" }
}
if ((Get-Item -LiteralPath $addinDll).VersionInfo.FileVersion -ne '0.6.0.0') {
    throw 'Unexpected add-in version; this packer only supports v0.6.0 for 2022.'
}

$payload = Join-Path $stage 'payload\2022'
New-Item -ItemType Directory -Path $payload -Force | Out-Null
$dlls = @(Get-ChildItem -LiteralPath $dllDir -Filter '*.dll' -File | Where-Object {
    $_.Name -notmatch '^(Oumijia\.PortalHost|Microsoft\.Web\.WebView2|WebView2Loader)'
})
foreach ($item in $dlls) { Copy-Item -LiteralPath $item.FullName -Destination (Join-Path $payload $item.Name) }
@($dlls | ForEach-Object { [pscustomobject]@{Name=$_.Name; SHA256=(Get-FileHash -LiteralPath (Join-Path $payload $_.Name) -Algorithm SHA256).Hash} }) |
    Export-Csv -LiteralPath (Join-Path $payload 'manifest.sha256.csv') -Encoding UTF8 -NoTypeInformation
Copy-Item -LiteralPath (Join-Path $root 'tools\eplan-scripts\OumijiaRegister2022.cs') -Destination $payload
foreach ($file in @('Install-Tool-Oumijia.ps1','Install-Tool-Oumijia.bat','Install-2022.ps1','EplanTargets.ps1')) {
    Copy-Item -LiteralPath (Join-Path $root ('tools\install-all\' + $file)) -Destination $stage
}
Copy-Item -LiteralPath (Join-Path $root 'tools\install-all\README-2022.txt') -Destination (Join-Path $stage 'README.txt')
@(
    'Tool Oumijia - Electric P8 2022.0.3 candidate'
    'Payload is net48 built with EPLAN Platform 2022.0.3 API.'
    'Other 2022.0.x, 2023-2025 and 2026 are not supported by THIS ZIP.'
    ('Add-in SHA256: ' + (Get-FileHash -LiteralPath (Join-Path $payload 'Oumijia.EplAddin.EDZ.dll') -Algorithm SHA256).Hash)
) | Set-Content -LiteralPath (Join-Path $stage 'VERSION.txt') -Encoding UTF8

@(Get-ChildItem -LiteralPath $stage -File -Recurse | Where-Object { $_.Name -ne 'SHA256.csv' } |
    ForEach-Object { [pscustomobject]@{ Path=$_.FullName.Substring($stage.Length).TrimStart('\\'); SHA256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash } }) |
    Export-Csv -LiteralPath (Join-Path $stage 'SHA256.csv') -NoTypeInformation -Encoding UTF8
Write-Host "CANDIDATE: $stage"
Write-Host 'Not a release ZIP. Stage and GUI UAT before running -CreateZip.'
