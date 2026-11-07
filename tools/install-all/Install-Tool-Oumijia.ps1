# Tool Oumijia — target-aware installer (one EPLAN installation per invocation)
# =====================================================
# 1) Thu cai ban ADD-IN  -> self-test -> neu OK: xong.
# 2) Neu add-in bi EPLAN vo hieu hoa (thuong do thieu API license tren EPLAN moi),
#    tu dong go add-in va chuyen sang cai SCRIPT EDITION.
#
# Chay: powershell -NoProfile -ExecutionPolicy Bypass -File Install-Tool-Oumijia.ps1 -ListInstalled
#       powershell -NoProfile -ExecutionPolicy Bypass -File Install-Tool-Oumijia.ps1 -TargetW3u '...\W3u.exe'
#
# Ho tro ca hai kieu thu muc:
#   - Zip phat hanh : <root>\Install-Tool-Oumijia.ps1 + addin\ + script\
#   - Repo          : tools\install-all\ + ..\install-addin\ + ..\script-edition\

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
    foreach ($item in $installed) {
        $status = if ($item.Variant -eq 'Education' -and $item.Version -eq '2026.0.3') { 'Script GUI UAT local; installer chua UAT headless' } else { 'Chua co payload da UAT' }
        Write-Host ("{0} {1} | {2} | {3}" -f $item.Variant, $item.Version, $item.W3u, $status)
    }
    return
}
$target = Select-OumijiaEplanInstallation -Installations $installed -TargetW3u $TargetW3u
if ($target.Variant -ne 'Education' -or $target.Version -ne '2026.0.3') {
    throw "Chua co payload da kiem chung cho $($target.Variant) $($target.Version). Khong cai thu DLL/Script 2026 sang ban nay."
}
$w3u = $target.W3u
$edition = 'Script'
$activeEplan = @(Get-CimInstance Win32_Process -ErrorAction Stop | Where-Object { $_.Name -match '^(EPLAN|W3u)\.exe$' })
if ($activeEplan.Count -gt 0) {
    throw 'EPLAN/W3u dang chay; hay luu cong viec va dong tat ca phien EPLAN truoc khi cai.'
}

$addinDir = Join-Path $here "addin"
if (-not (Test-Path (Join-Path $addinDir "Oumijia.EplAddin.EDZ.dll"))) {
    $addinDir = Join-Path $here "..\install-addin"
}
$scriptDir = Join-Path $here "script"
if (-not (Test-Path (Join-Path $scriptDir "ToolOumijiaScript.cs"))) {
    $scriptDir = Join-Path $here "..\script-edition"
}

$appData = Join-Path $env:APPDATA "Tool Oumijia"
$registerLog = Join-Path $appData "logs\register-script.log"
$addinLog = Join-Path $appData "logs\addin.log"
$addinHome = Join-Path $appData "AddIn"
$scriptHome = Join-Path $appData "Script"
$portalHome = Join-Path $appData "Portal"
$addinUnregisterFlag = Join-Path $appData "unregister.flag"
$scriptUnregisterFlag = Join-Path $appData "script-unregister.flag"

function Backup-OumijiaInstallState {
    param([object]$Target, [string]$AppData)
    $backup = Join-Path (Join-Path $AppData 'backups') ('preinstall-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
    New-Item -ItemType Directory -Path $backup -Force | Out-Null
    $sources = @()
    foreach ($name in @('Script', 'Portal', 'AddIn', 'Scripts')) {
        $path = Join-Path $AppData $name
        if (Test-Path -LiteralPath $path -PathType Container) {
            $sources += [pscustomobject]@{ Path = $path; Destination = (Join-Path (Join-Path $backup 'runtime') $name) }
        }
    }
    $cfg = Join-Path (Join-Path (Join-Path (Join-Path (Join-Path $env:PUBLIC 'EPLAN\Settings') $Target.Variant) $Target.Version) 'x64') 'Cfg'
    if (Test-Path -LiteralPath $cfg -PathType Container) {
        foreach ($entry in (Get-ChildItem -LiteralPath $cfg -Directory | Where-Object { $_.Name -match '^(USER|STATION)\.' -and $_.Name -match '\.xdb$' })) {
            $sources += [pscustomobject]@{ Path = $entry.FullName; Destination = (Join-Path (Join-Path $backup 'settings') $entry.Name) }
        }
    }
    $manifest = @()
    foreach ($source in $sources) {
        New-Item -ItemType Directory -Path (Split-Path -Parent $source.Destination) -Force | Out-Null
        Copy-Item -LiteralPath $source.Path -Destination $source.Destination -Recurse -Force
        foreach ($file in (Get-ChildItem -LiteralPath $source.Path -Recurse -File -Force)) {
            $relative = $file.FullName.Substring($source.Path.Length).TrimStart('\')
            $copy = Join-Path $source.Destination $relative
            $originalHash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
            $copyHash = (Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash
            if ($originalHash -ne $copyHash) { throw "Backup hash khong khop: $($file.FullName)" }
            $manifest += [pscustomobject]@{ Original = $file.FullName; Backup = $copy; SHA256 = $copyHash }
        }
    }
    $manifest | Export-Csv -LiteralPath (Join-Path $backup 'SHA256.csv') -NoTypeInformation -Encoding UTF8
    Write-Host "[+] Backup truoc cai (SHA-256 verified): $backup"
}

function Invoke-W3u {
    param([string]$W3u, [string[]]$Arguments)
    Start-Process -FilePath $W3u -ArgumentList $Arguments | Out-Null
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

function Get-LogLength {
    param([string]$File)
    if (Test-Path $File) { return (Get-Item $File).Length }
    return 0
}

Write-Host "=================================================="
Write-Host "  TOOL OUMIJIA - CAI DAT (tu chon ban phu hop)"
Write-Host "=================================================="
Write-Host ""

Write-Host "[+] EPLAN: $($target.Variant) $($target.Version) ($w3u); edition: $edition"
Write-Host ""

$scriptSource = Join-Path $scriptDir "ToolOumijiaScript.cs"
$scriptRegisterSource = Join-Path $scriptDir "OumijiaRegisterScript.cs"
if (-not (Test-Path -LiteralPath $scriptSource) -or -not (Test-Path -LiteralPath $scriptRegisterSource)) {
    throw 'Thieu Script Edition trong goi. Khong thay doi runtime.'
}

function Wait-ForEplanExit {
    param([int]$TimeoutSeconds = 120)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (@(Get-CimInstance Win32_Process | Where-Object { $_.Name -match '^(EPLAN|W3u)\.exe$' }).Count -eq 0) { return }
        Start-Sleep -Seconds 2
    }
    throw 'EPLAN/W3u chua thoat sau thao tac headless; dung de tranh chay hai phien dong thoi.'
}
$portalSource = Join-Path $scriptDir 'portal'
if (-not (Test-Path -LiteralPath (Join-Path $portalSource 'Oumijia.EplanUi.dll'))) {
    throw 'Thieu script\portal\Oumijia.EplanUi.dll. Khong dung ngầm DLL bin\Release cu trong repo; build lai va dong goi truoc.'
}
foreach ($name in @('Oumijia.EplanUi.dll', 'Oumijia.EdzReader.dll', 'SharpCompress.dll')) {
    if (-not (Test-Path -LiteralPath (Join-Path $portalSource $name))) { throw "Thieu shared runtime $name; khong thay doi runtime." }
}

Backup-OumijiaInstallState -Target $target -AppData $appData

# v0.6.0: di chuyen script cu sau khi da backup, khong ghi de ban backup khac.
$legacyDir = Join-Path $appData 'Scripts'
$legacyScript = Join-Path $legacyDir 'ToolOumijia.cs'
if (Test-Path -LiteralPath $legacyScript) {
    $legacyBackupDir = Join-Path $appData 'backups'
    Move-Item -LiteralPath $legacyScript -Destination (Join-Path $legacyBackupDir ('ToolOumijia.legacy-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.cs')) -ErrorAction Stop
    Write-Host '[+] Da luu script cu Scripts\ToolOumijia.cs'
}

# ------------------------------------------------------------------ BUOC 1: thu ban ADD-IN
$addinDll = Join-Path $addinDir "Oumijia.EplAddin.EDZ.dll"
$addinRegister = Join-Path $addinDir "OumijiaRegister.cs"

if ($edition -eq 'AddIn' -and (Test-Path $addinDll) -and (Test-Path $addinRegister)) {
    Write-Host "[BUOC 1/2] Thu cai ban ADD-IN..."
    New-Item -ItemType Directory -Path $addinHome -Force | Out-Null
    Get-ChildItem -LiteralPath $addinDir -Filter '*.dll' -File |
        Where-Object { $_.Name -notmatch '^(Oumijia\.PortalHost|Microsoft\.Web\.WebView2|WebView2Loader)' } |
        Copy-Item -Destination $addinHome -Force

    New-Item -ItemType Directory -Path $portalHome -Force | Out-Null
    Get-ChildItem -LiteralPath $addinDir -Filter '*.dll' -File |
        Where-Object { $_.Name -notmatch '^(Oumijia\.PortalHost|Microsoft\.Web\.WebView2|WebView2Loader)' } |
        Copy-Item -Destination $portalHome -Force
    $stale = @('Oumijia.PortalHost.dll', 'Microsoft.Web.WebView2.Core.dll', 'Microsoft.Web.WebView2.WinForms.dll', 'Microsoft.Web.WebView2.Wpf.dll', 'WebView2Loader.dll')
    foreach ($name in $stale) {
        foreach ($home in @($addinHome, $portalHome)) {
            $path = Join-Path $home $name
            if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue }
        }
    }

    Remove-Item $addinUnregisterFlag -ErrorAction SilentlyContinue
    $before = Get-LogLength $registerLog
    Invoke-W3u $w3u @('/Auto','/Quiet','/NoSplash','/NoLicenseDialog','/NoUserRightsDialog','/NoLoadWorkspace','ExecuteScript', ('/ScriptFile:"' + $addinRegister + '"'))
    $registered = Wait-ForFreshLogMatch -File $registerLog -Before $before -Pattern 'OK: registered' -TimeoutSeconds 300
    Wait-ForEplanExit

    if ($registered) {
        Write-Host "[*] Da dang ky add-in. Kiem tra add-in co chay duoc khong..."
        $beforeAddin = Get-LogLength $addinLog
        Invoke-W3u $w3u @('/Auto','/Quiet','/NoSplash','/NoLicenseDialog','/NoUserRightsDialog','/NoLoadWorkspace','OumijiaSelfTest')
        $addinWorks = Wait-ForFreshLogMatch -File $addinLog -Before $beforeAddin -Pattern 'add-in loaded' -TimeoutSeconds 300
        Wait-ForEplanExit

        if ($addinWorks) {
            Write-Host ""
            Write-Host "[OK] BAN ADD-IN DA HOAT DONG!"
            Write-Host "     Mo EPLAN lai -> ribbon 'Tool Oumijia' se xuat hien."
            exit 0
        }
    }

    Write-Host ""
    Write-Host "[!] Add-in khong chay duoc (EPLAN co the da vo hieu hoa DLL chua ky"
    Write-Host "    do thieu API license - thuong gap tren EPLAN ban moi)."
    Write-Host "    -> Tu dong chuyen sang ban SCRIPT..."
    Write-Host ""

    # go dang ky add-in cho sach
    Set-Content -Path $addinUnregisterFlag -Value "unregister" -Encoding ASCII
    $beforeClean = Get-LogLength $registerLog
    Invoke-W3u $w3u @('/Auto','/Quiet','/NoSplash','/NoLicenseDialog','/NoUserRightsDialog','/NoLoadWorkspace','ExecuteScript', ('/ScriptFile:"' + $addinRegister + '"'))
    Wait-ForFreshLogMatch -File $registerLog -Before $beforeClean -Pattern 'OK: unregistered' -TimeoutSeconds 240 | Out-Null
    Wait-ForEplanExit
} else {
    if ($edition -eq 'Script') { Write-Host '[1/2] Target nay dung Script Edition; khong thu Add-in chua duoc UAT.' }
    else { Write-Host '[!] Khong tim thay bo add-in trong goi - bo qua buoc 1.' }
}

# ------------------------------------------------------------------ BUOC 2: cai SCRIPT EDITION
if (-not (Test-Path $scriptSource) -or -not (Test-Path $scriptRegisterSource)) {
    Write-Host "[LOI] Khong tim thay bo Script Edition trong goi."
    exit 1
}

Write-Host "[BUOC 2/2] Cai ban SCRIPT EDITION..."
New-Item -ItemType Directory -Path $scriptHome -Force | Out-Null
$destScript = Join-Path $scriptHome "ToolOumijiaScript.cs"
$destRegister = Join-Path $scriptHome "OumijiaRegisterScript.cs"
Copy-Item $scriptSource $destScript -Force
Copy-Item $scriptRegisterSource $destRegister -Force

if (Test-Path -LiteralPath (Join-Path $portalSource "Oumijia.EplanUi.dll")) {
    New-Item -ItemType Directory -Path $portalHome -Force | Out-Null
    Get-ChildItem -LiteralPath $portalSource -Filter '*.dll' -File |
        Where-Object { $_.Name -notmatch '^(Oumijia\.PortalHost|Microsoft\.Web\.WebView2|WebView2Loader)' } |
        Copy-Item -Destination $portalHome -Force
    $stale = @('Oumijia.PortalHost.dll', 'Microsoft.Web.WebView2.Core.dll', 'Microsoft.Web.WebView2.WinForms.dll', 'Microsoft.Web.WebView2.Wpf.dll', 'WebView2Loader.dll')
    foreach ($name in $stale) {
        $path = Join-Path $portalHome $name
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue }
    }
    Write-Host "[+] Shared runtime: $portalHome"
} else {
    throw "Thieu shared runtime (Oumijia.EplanUi.dll). Hay build lai Add-in va dong goi."
}

Remove-Item $scriptUnregisterFlag -ErrorAction SilentlyContinue
$before2 = Get-LogLength $registerLog
Invoke-W3u $w3u @('/Auto','/Quiet','/NoSplash','/NoLicenseDialog','/NoUserRightsDialog','/NoLoadWorkspace','ExecuteScript', ('/ScriptFile:"' + $destRegister + '"'))
$regScript = Wait-ForFreshLogMatch -File $registerLog -Before $before2 -Pattern '\[SCRIPT\] OK: da dang ky' -TimeoutSeconds 300
Wait-ForEplanExit

if (-not $regScript) {
    Write-Host ""
    Write-Host "[!] Khong xac nhan duoc dang ky script. Kiem tra log: $registerLog"
    exit 1
}

Write-Host "[*] Da dang ky script. Kiem chung script chay duoc..."
$beforeScript = Get-LogLength $addinLog
Invoke-W3u $w3u @('/Auto','/Quiet','/NoSplash','/NoLicenseDialog','/NoUserRightsDialog','/NoLoadWorkspace','OumijiaScriptSelfTest')
$scriptWorks = Wait-ForFreshLogMatch -File $addinLog -Before $beforeScript -Pattern 'script loaded' -TimeoutSeconds 300
Wait-ForEplanExit

Write-Host ""
if ($scriptWorks) {
    Write-Host "[OK] BAN SCRIPT EDITION DA HOAT DONG!"
    Write-Host "     Mo EPLAN lai -> ribbon 'Tool Oumijia' se xuat hien."
} else {
    Write-Host "[!] Da dang ky nhung chua xac nhan duoc script chay tren ban EPLAN nay."
    Write-Host "    Kiem tra log: $addinLog"
    Write-Host "    (Mot so ban EPLAN cu thieu reference script - xem script\README.txt)"
    exit 1
}
