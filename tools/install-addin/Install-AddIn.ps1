# Tool Oumijia — EPLAN add-in installer
# =====================================
# Copies the add-in DLL to its per-user home and registers it in EPLAN by running
# the registration script headlessly (EPLAN's ExecuteScript action).
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File Install-AddIn.ps1
#
# The script needs: Oumijia.EplAddin.EDZ.dll + OumijiaRegister.cs next to it.

$ErrorActionPreference = "Stop"

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$dll = Join-Path $here "Oumijia.EplAddin.EDZ.dll"
$registerScript = Join-Path $here "OumijiaRegister.cs"

$destDir = Join-Path $env:APPDATA "Tool Oumijia\AddIn"
$destDll = Join-Path $destDir "Oumijia.EplAddin.EDZ.dll"
$logFile = Join-Path $env:APPDATA "Tool Oumijia\logs\register-script.log"
$flagFile = Join-Path $env:APPDATA "Tool Oumijia\unregister.flag"

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
Write-Host "  TOOL OUMIJIA — CAI DAT ADD-IN CHO EPLAN"
Write-Host "=============================================="
Write-Host ""

if (-not (Test-Path $dll)) {
    Write-Host "[LOI] Khong tim thay Oumijia.EplAddin.EDZ.dll canh script nay."
    exit 1
}
if (-not (Test-Path $registerScript)) {
    Write-Host "[LOI] Khong tim thay OumijiaRegister.cs canh script nay."
    exit 1
}

$w3u = Find-W3u
if (-not $w3u) {
    Write-Host "[LOI] Khong tim thay EPLAN (W3u.exe) tren may nay."
    exit 1
}
Write-Host "[+] EPLAN: $w3u"

New-Item -ItemType Directory -Path $destDir -Force | Out-Null
Copy-Item $dll $destDll -Force
Write-Host "[+] Add-in dll: $destDll"

Remove-Item $flagFile -ErrorAction SilentlyContinue

$before = if (Test-Path $logFile) { (Get-Item $logFile).Length } else { 0 }

Write-Host "[*] Dang dang ky add-in (EPLAN se khoi dong an, chay script, roi tu thoat)..."
Start-Process -FilePath $w3u -ArgumentList @(
    '/Auto', '/Quiet', '/NoSplash', '/NoLicenseDialog', '/NoUserRightsDialog',
    '/NoLoadWorkspace', 'ExecuteScript', ('/ScriptFile:"' + $registerScript + '"')
) | Out-Null

$deadline = (Get-Date).AddSeconds(300)
$ok = $false
while ((Get-Date) -lt $deadline) {
    if ((Test-Path $logFile) -and ((Get-Item $logFile).Length -gt $before)) {
        $tail = Get-Content $logFile -ErrorAction SilentlyContinue | Select-Object -Last 5
        if ($tail -match 'OK: registered') { $ok = $true; break }
        if ($tail -match 'ERROR') { break }
    }
    Start-Sleep -Seconds 5
}

if ($ok) {
    Write-Host ""
    Write-Host "[OK] Da dang ky add-in thanh cong!"
    Write-Host "     Mo EPLAN lai -> ribbon 'Tool Oumijia' se xuat hien."
} else {
    Write-Host ""
    Write-Host "[!] Khong xac nhan duoc ket qua tu dong. Kiem tra log:"
    Write-Host "    $logFile"
    Write-Host ""
    Write-Host "    Phuong an du phong (30 giay, thu cong):"
    Write-Host "    1. Mo EPLAN -> File -> Extras -> Interfaces -> Scripts -> Run..."
    Write-Host "    2. Chon file: $registerScript"
}
