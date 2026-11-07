# Tool Oumijia — go dang ky Add-in/Script trong MOT ban EPLAN duoc chon.
# =======================================================
#   powershell -NoProfile -ExecutionPolicy Bypass -File Uninstall-Tool-Oumijia.ps1 -ListInstalled
#   powershell -NoProfile -ExecutionPolicy Bypass -File Uninstall-Tool-Oumijia.ps1 -TargetW3u '...\W3u.exe'

[CmdletBinding()]
param([switch]$ListInstalled, [string]$TargetW3u)

$ErrorActionPreference = "Stop"

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$targetsHelper = Join-Path $here 'EplanTargets.ps1'
if (-not (Test-Path -LiteralPath $targetsHelper -PathType Leaf)) { throw "Thieu helper: $targetsHelper" }
. $targetsHelper
$installed = @(Get-OumijiaEplanInstallations)
if ($ListInstalled) {
    if ($installed.Count -eq 0) { Write-Host 'Khong tim thay EPLAN.' }
    foreach ($item in $installed) { Write-Host ("{0} {1} | {2}" -f $item.Variant, $item.Version, $item.W3u) }
    return
}
$target = Select-OumijiaEplanInstallation -Installations $installed -TargetW3u $TargetW3u
$w3u = $target.W3u
$activeEplan = @(Get-CimInstance Win32_Process -ErrorAction Stop | Where-Object { $_.Name -match '^(EPLAN|W3u)\.exe$' })
if ($activeEplan.Count -gt 0) { throw 'EPLAN/W3u dang chay; hay luu cong viec va dong tat ca phien EPLAN truoc khi go.' }

$addinDir = Join-Path $here "addin"
if (-not (Test-Path (Join-Path $addinDir "OumijiaRegister.cs"))) {
    $addinDir = Join-Path $here "..\install-addin"
}
$scriptDir = Join-Path $here "script"
if (-not (Test-Path (Join-Path $scriptDir "OumijiaRegisterScript.cs"))) {
    $scriptDir = Join-Path $here "..\script-edition"
}

$appData = Join-Path $env:APPDATA "Tool Oumijia"
$registerLog = Join-Path $appData "logs\register-script.log"
$scriptHome = Join-Path $appData "Script"
$addinUnregisterFlag = Join-Path $appData "unregister.flag"
$scriptUnregisterFlag = Join-Path $appData "script-unregister.flag"

function Get-LogLength {
    param([string]$File)
    if (Test-Path $File) { return (Get-Item $File).Length }
    return 0
}

function Wait-ForFreshLogMatch {
    param([string]$File, [long]$Before, [string]$Pattern, [int]$TimeoutSeconds)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if ((Test-Path -LiteralPath $File) -and ((Get-Item -LiteralPath $File).Length -gt $Before)) {
            $stream = [IO.File]::Open($File, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
            try {
                [void]$stream.Seek($Before, [IO.SeekOrigin]::Begin)
                $reader = New-Object IO.StreamReader($stream, [Text.Encoding]::UTF8)
                $content = $reader.ReadToEnd()
                if ($content -match $Pattern) { return $true }
                if ($content -match '(\[SCRIPT\] ERROR|\bERROR: )') { return $false }
            } finally { $stream.Dispose() }
        }
        Start-Sleep -Seconds 5
    }
    return $false
}

function Wait-ForEplanExit {
    param([int]$TimeoutSeconds = 120)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (@(Get-CimInstance Win32_Process | Where-Object { $_.Name -match '^(EPLAN|W3u)\.exe$' }).Count -eq 0) { return }
        Start-Sleep -Seconds 2
    }
    throw 'EPLAN/W3u chua thoat sau go dang ky; dung de tranh chay hai phien dong thoi.'
}

function Backup-OumijiaTargetSettings {
    param([object]$Target, [string]$AppData)
    $cfg = Join-Path (Join-Path (Join-Path (Join-Path (Join-Path $env:PUBLIC 'EPLAN\Settings') $Target.Variant) $Target.Version) 'x64') 'Cfg'
    $backup = Join-Path (Join-Path $AppData 'backups') ('preuninstall-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
    New-Item -ItemType Directory -Path $backup -Force | Out-Null
    $manifest = @()
    if (Test-Path -LiteralPath $cfg -PathType Container) {
        foreach ($entry in (Get-ChildItem -LiteralPath $cfg -Directory | Where-Object { $_.Name -match '^(USER|STATION)\.' -and $_.Name -match '\.xdb$' })) {
            $dest = Join-Path $backup $entry.Name
            Copy-Item -LiteralPath $entry.FullName -Destination $dest -Recurse -Force
            foreach ($file in (Get-ChildItem -LiteralPath $entry.FullName -Recurse -File -Force)) {
                $copy = Join-Path $dest ($file.FullName.Substring($entry.FullName.Length).TrimStart('\'))
                $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
                if ($hash -ne (Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash) { throw "Backup hash khong khop: $($file.FullName)" }
                $manifest += [pscustomobject]@{ Original = $file.FullName; Backup = $copy; SHA256 = $hash }
            }
        }
    }
    $manifest | Export-Csv -LiteralPath (Join-Path $backup 'SHA256.csv') -NoTypeInformation -Encoding UTF8
    Write-Host "[+] Backup settings truoc go (SHA-256 verified): $backup"
}

Write-Host "=============================================="
Write-Host "  TOOL OUMIJIA - GO CAI DAT (ca hai ban)"
Write-Host "=============================================="
Write-Host ""

Write-Host "[+] EPLAN: $($target.Variant) $($target.Version) ($w3u)"
Backup-OumijiaTargetSettings -Target $target -AppData $appData

# ------------------------------------------------------------------ 1) go add-in
$partial = $false
$addinRegister = Join-Path $addinDir "OumijiaRegister.cs"
if (-not (Test-Path -LiteralPath $addinRegister)) {
    $addinRegister = Join-Path $here '..\eplan-scripts\OumijiaRegister.cs'
}
if (Test-Path -LiteralPath $addinRegister) {
    Write-Host "[1/2] Go dang ky add-in..."
    New-Item -ItemType Directory -Path $appData -Force | Out-Null
    Set-Content -Path $addinUnregisterFlag -Value "unregister" -Encoding ASCII
    $before = Get-LogLength $registerLog
    Start-Process -FilePath $w3u -ArgumentList @(
        '/Auto','/Quiet','/NoSplash','/NoLicenseDialog','/NoUserRightsDialog',
        '/NoLoadWorkspace','ExecuteScript', ('/ScriptFile:"' + $addinRegister + '"')
    ) | Out-Null

    if (-not (Wait-ForFreshLogMatch -File $registerLog -Before $before -Pattern 'OK: unregistered' -TimeoutSeconds 300)) {
        throw "Khong xac nhan duoc go dang ky add-in tren $w3u. Runtime van duoc giu. Kiem tra $registerLog"
    }
    Wait-ForEplanExit
} else {
    Write-Host '[1/2] Khong co script go Add-in; khong the xac nhan Add-in da go.'
    $partial = $true
}

# ------------------------------------------------------------------ 2) go script edition
$destRegister = Join-Path $scriptHome "OumijiaRegisterScript.cs"
if (-not (Test-Path $destRegister)) { $destRegister = Join-Path $scriptDir "OumijiaRegisterScript.cs" }

if (Test-Path -LiteralPath $destRegister) {
    Write-Host "[2/2] Go dang ky script..."
    Set-Content -Path $scriptUnregisterFlag -Value "unregister" -Encoding ASCII
    $before2 = Get-LogLength $registerLog
    Start-Process -FilePath $w3u -ArgumentList @(
        '/Auto','/Quiet','/NoSplash','/NoLicenseDialog','/NoUserRightsDialog',
        '/NoLoadWorkspace','ExecuteScript', ('/ScriptFile:"' + $destRegister + '"')
    ) | Out-Null

    if (-not (Wait-ForFreshLogMatch -File $registerLog -Before $before2 -Pattern '\[SCRIPT\] OK: da go dang ky' -TimeoutSeconds 300)) {
        throw "Khong xac nhan duoc go dang ky Script tren $w3u. Runtime van duoc giu. Kiem tra $registerLog"
    }
    Wait-ForEplanExit
} else {
    Write-Host '[2/2] Khong co script go Script Edition; khong the xac nhan Script da go.'
    $partial = $true
}

Write-Host ""
if ($partial) { throw 'Chi go duoc mot phan do thieu script dang ky. Runtime duoc giu; kiem tra settings target.' }
Write-Host "[OK] Da go dang ky trong $($target.Variant) $($target.Version)."
Write-Host "     Giu nguyen shared runtime/Script/AddIn tai $appData vi ban EPLAN khac co the dang dung."
