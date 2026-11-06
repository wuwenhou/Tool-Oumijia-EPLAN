# Tool Oumijia — go cai dat CA HAI ban (add-in + script)
# =======================================================
#   powershell -NoProfile -ExecutionPolicy Bypass -File Uninstall-Tool-Oumijia.ps1

$ErrorActionPreference = "Stop"

$here = Split-Path -Parent $MyInvocation.MyCommand.Path

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
$addinHome = Join-Path $appData "AddIn"
$scriptHome = Join-Path $appData "Script"
$portalHome = Join-Path $appData "Portal"
$addinUnregisterFlag = Join-Path $appData "unregister.flag"
$scriptUnregisterFlag = Join-Path $appData "script-unregister.flag"

function Find-W3u {
    $roots = @(
        (Join-Path $env:ProgramFiles "EPLAN"),
        $(if (${env:ProgramFiles(x86)}) { Join-Path ${env:ProgramFiles(x86)} "EPLAN" })
    )
    foreach ($root in $roots) {
        if (-not $root -or -not (Test-Path $root)) { continue }
        foreach ($variant in (Get-ChildItem $root -Directory -ErrorAction SilentlyContinue)) {
            foreach ($version in (Get-ChildItem $variant.FullName -Directory -ErrorAction SilentlyContinue)) {
                $candidate = Join-Path $version.FullName "Bin\W3u.exe"
                if (Test-Path $candidate) { return $candidate }
            }
        }
    }
    return $null
}

function Get-LogLength {
    param([string]$File)
    if (Test-Path $File) { return (Get-Item $File).Length }
    return 0
}

Write-Host "=============================================="
Write-Host "  TOOL OUMIJIA - GO CAI DAT (ca hai ban)"
Write-Host "=============================================="
Write-Host ""

$w3u = Find-W3u

# ------------------------------------------------------------------ 1) go add-in
$addinRegister = Join-Path $addinDir "OumijiaRegister.cs"
if ((Test-Path $addinRegister) -and $w3u) {
    Write-Host "[1/2] Go dang ky add-in..."
    Set-Content -Path $addinUnregisterFlag -Value "unregister" -Encoding ASCII
    $before = Get-LogLength $registerLog
    Start-Process -FilePath $w3u -ArgumentList @(
        '/Auto','/Quiet','/NoSplash','/NoLicenseDialog','/NoUserRightsDialog',
        '/NoLoadWorkspace','ExecuteScript', ('/ScriptFile:"' + $addinRegister + '"')
    ) | Out-Null

    $deadline = (Get-Date).AddSeconds(300)
    while ((Get-Date) -lt $deadline) {
        if ((Test-Path $registerLog) -and ((Get-Item $registerLog).Length -gt $before)) {
            $content = Get-Content $registerLog -Raw -ErrorAction SilentlyContinue
            if ($content.Substring($before) -match 'OK: unregistered') { break }
        }
        Start-Sleep -Seconds 5
    }
    if (Test-Path $addinHome) {
        Remove-Item $addinHome -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "[+] Da xoa thu muc add-in."
    }
} else {
    Write-Host "[1/2] (khong thay bo add-in / EPLAN - bo qua)"
}

# ------------------------------------------------------------------ 2) go script edition
$destRegister = Join-Path $scriptHome "OumijiaRegisterScript.cs"
if (-not (Test-Path $destRegister)) { $destRegister = Join-Path $scriptDir "OumijiaRegisterScript.cs" }

if ((Test-Path $destRegister) -and $w3u) {
    Write-Host "[2/2] Go dang ky script..."
    Set-Content -Path $scriptUnregisterFlag -Value "unregister" -Encoding ASCII
    $before2 = Get-LogLength $registerLog
    Start-Process -FilePath $w3u -ArgumentList @(
        '/Auto','/Quiet','/NoSplash','/NoLicenseDialog','/NoUserRightsDialog',
        '/NoLoadWorkspace','ExecuteScript', ('/ScriptFile:"' + $destRegister + '"')
    ) | Out-Null

    $deadline2 = (Get-Date).AddSeconds(300)
    while ((Get-Date) -lt $deadline2) {
        if ((Test-Path $registerLog) -and ((Get-Item $registerLog).Length -gt $before2)) {
            $content = Get-Content $registerLog -Raw -ErrorAction SilentlyContinue
            if ($content.Substring($before2) -match '\[SCRIPT\] OK: da go dang ky') { break }
        }
        Start-Sleep -Seconds 5
    }
    if (Test-Path $scriptHome) {
        Remove-Item $scriptHome -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "[+] Da xoa thu muc script."
    }
} else {
    Write-Host "[2/2] (khong thay bo script / EPLAN - bo qua)"
}

if (Test-Path $portalHome) {
    Remove-Item $portalHome -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "[+] Da xoa shared runtime."
}

Write-Host ""
Write-Host "[OK] Da go cai dat. (Cau hinh/log giu lai tai: $appData)"
