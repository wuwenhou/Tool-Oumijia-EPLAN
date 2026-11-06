# Tool Oumijia — Script Edition installer
# ========================================
# Copies ToolOumijiaScript.cs into its per-user home and registers it as an
# EPLAN "registered script" (auto-loaded at every EPLAN start) by running a small
# registration script inside EPLAN (ExecuteScript action).
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File Install-Script.ps1
#
# Needs: ToolOumijiaScript.cs + OumijiaRegisterScript.cs next to this file.

$ErrorActionPreference = "Stop"

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$scriptSource = Join-Path $here "ToolOumijiaScript.cs"
$registerScriptSource = Join-Path $here "OumijiaRegisterScript.cs"

$destDir = Join-Path $env:APPDATA "Tool Oumijia\Script"
$destScript = Join-Path $destDir "ToolOumijiaScript.cs"
$destRegister = Join-Path $destDir "OumijiaRegisterScript.cs"
$logFile = Join-Path $env:APPDATA "Tool Oumijia\logs\register-script.log"
$addinLog = Join-Path $env:APPDATA "Tool Oumijia\logs\addin.log"
$flagFile = Join-Path $env:APPDATA "Tool Oumijia\script-unregister.flag"

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

Write-Host "=============================================="
Write-Host "  TOOL OUMIJIA - CAI SCRIPT EDITION CHO EPLAN"
Write-Host "=============================================="
Write-Host ""

if (-not (Test-Path $scriptSource)) {
    Write-Host "[LOI] Khong tim thay ToolOumijiaScript.cs canh script nay."
    exit 1
}
if (-not (Test-Path $registerScriptSource)) {
    Write-Host "[LOI] Khong tim thay OumijiaRegisterScript.cs canh script nay."
    exit 1
}

$w3u = Find-W3u
if (-not $w3u) {
    Write-Host "[LOI] Khong tim thay EPLAN (W3u.exe) tren may nay."
    exit 1
}
Write-Host "[+] EPLAN: $w3u"

New-Item -ItemType Directory -Path $destDir -Force | Out-Null
Copy-Item $scriptSource $destScript -Force
Copy-Item $registerScriptSource $destRegister -Force
Write-Host "[+] Script: $destScript"

# ------------------------------------------------------------------ Shared Portal/UI/EDZ runtime
$portalSource = Join-Path $here "portal"
if (-not (Test-Path -LiteralPath (Join-Path $portalSource "Oumijia.PortalHost.dll"))) {
    $portalSource = Join-Path $here "..\..\src\Oumijia.PortalHost\bin\Release\net48"
}
$portalHome = Join-Path $env:APPDATA "Tool Oumijia\Portal"
if (-not (Test-Path -LiteralPath (Join-Path $portalSource "Oumijia.PortalHost.dll"))) {
    Write-Host "[LOI] Thieu shared runtime (Oumijia.PortalHost.dll). Hay build lai Add-in/PortalHost truoc."
    exit 1
}
New-Item -ItemType Directory -Path $portalHome -Force | Out-Null
Get-ChildItem -LiteralPath $portalSource -Filter '*.dll' -File |
    Copy-Item -Destination $portalHome -Force
if (Test-Path -LiteralPath (Join-Path $portalSource 'WebView2Loader.dll')) {
    Copy-Item -LiteralPath (Join-Path $portalSource 'WebView2Loader.dll') -Destination $portalHome -Force
}
Write-Host "[+] Shared runtime: $portalHome"

Remove-Item $flagFile -ErrorAction SilentlyContinue

# ------------------------------------------------------------------ 1) dang ky
$before = if (Test-Path $logFile) { (Get-Item $logFile).Length } else { 0 }

Write-Host "[*] Dang dang ky script (EPLAN khoi dong an, chay script dang ky, roi tu thoat)..."
Start-Process -FilePath $w3u -ArgumentList @(
    '/Auto', '/Quiet', '/NoSplash', '/NoLicenseDialog', '/NoUserRightsDialog',
    '/NoLoadWorkspace', 'ExecuteScript', ('/ScriptFile:"' + $destRegister + '"')
) | Out-Null

$deadline = (Get-Date).AddSeconds(300)
$registered = $false
while ((Get-Date) -lt $deadline) {
    if ((Test-Path $logFile) -and ((Get-Item $logFile).Length -gt $before)) {
        $tail = Get-Content $logFile -ErrorAction SilentlyContinue | Select-Object -Last 8
        if ($tail -match '\[SCRIPT\] OK: da dang ky') { $registered = $true; break }
        if ($tail -match '\[SCRIPT\] ERROR') { break }
    }
    Start-Sleep -Seconds 5
}

if (-not $registered) {
    Write-Host ""
    Write-Host "[!] Khong xac nhan duoc dang ky tu dong. Kiem tra log: $logFile"
    Write-Host "    Phuong an du phong: mo EPLAN > File > Extras > Interfaces > Scripts"
    Write-Host "    va chay file: $destRegister"
    exit 1
}

Write-Host "[+] Da ghi danh sach script tu nap."
Write-Host ""

# ------------------------------------------------------------------ 2) kiem chung script chay duoc
$beforeAddin = if (Test-Path $addinLog) { (Get-Item $addinLog).Length } else { 0 }

Write-Host "[*] Kiem chung: khoi dong an + goi OumijiaScriptSelfTest..."
Start-Process -FilePath $w3u -ArgumentList @(
    '/Auto', '/Quiet', '/NoSplash', '/NoLicenseDialog', '/NoUserRightsDialog',
    '/NoLoadWorkspace', 'OumijiaScriptSelfTest'
) | Out-Null

$deadline2 = (Get-Date).AddSeconds(300)
$verified = $false
while ((Get-Date) -lt $deadline2) {
    if ((Test-Path $addinLog) -and ((Get-Item $addinLog).Length -gt $beforeAddin)) {
        $content = Get-Content $addinLog -Raw -ErrorAction SilentlyContinue
        if ($content.Substring($beforeAddin) -match 'SELFTEST OK - script loaded') { $verified = $true; break }
    }
    Start-Sleep -Seconds 5
}

Write-Host ""
if ($verified) {
    Write-Host "[OK] Script Edition da hoat dong!"
    Write-Host "     Mo EPLAN lai -> ribbon 'Tool Oumijia' se xuat hien."
} else {
    Write-Host "[!] Da dang ky nhung CHUA xac nhan duoc script chay tren ban EPLAN nay."
    Write-Host "    Mot so ban EPLAN cu thieu reference cho script (Eplan.EplApi.MasterData)."
    Write-Host "    -> Tren cac ban do, hay dung ban ADD-IN (Install-AddIn.bat)."
    Write-Host "    Kiem tra log: $addinLog"
}
