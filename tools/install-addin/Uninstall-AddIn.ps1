# Tool Oumijia — EPLAN add-in uninstaller
# =======================================
#   powershell -NoProfile -ExecutionPolicy Bypass -File Uninstall-AddIn.ps1

$ErrorActionPreference = "Stop"

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$registerScript = Join-Path $here "OumijiaRegister.cs"

$addinDir = Join-Path $env:APPDATA "Tool Oumijia\AddIn"
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
Write-Host "  TOOL OUMIJIA — GO ADD-IN KHOI EPLAN"
Write-Host "=============================================="
Write-Host ""

if (-not (Test-Path $registerScript)) {
    Write-Host "[LOI] Khong tim thay OumijiaRegister.cs canh script nay."
    exit 1
}

$w3u = Find-W3u
if (-not $w3u) {
    Write-Host "[LOI] Khong tim thay EPLAN (W3u.exe) tren may nay."
    exit 1
}

# Tạo cờ để script (chạy trong EPLAN) thực hiện GỠ đăng ký.
Set-Content -Path $flagFile -Value "unregister" -Encoding ASCII
Write-Host "[*] Dang go dang ky (EPLAN khoi dong an)..."
$before = if (Test-Path $logFile) { (Get-Item $logFile).Length } else { 0 }

Start-Process -FilePath $w3u -ArgumentList @(
    '/Auto', '/Quiet', '/NoSplash', '/NoLicenseDialog', '/NoUserRightsDialog',
    '/NoLoadWorkspace', 'ExecuteScript', ('/ScriptFile:"' + $registerScript + '"')
) | Out-Null

$deadline = (Get-Date).AddSeconds(300)
$ok = $false
while ((Get-Date) -lt $deadline) {
    if ((Test-Path $logFile) -and ((Get-Item $logFile).Length -gt $before)) {
        $tail = Get-Content $logFile -ErrorAction SilentlyContinue | Select-Object -Last 5
        if ($tail -match 'OK: unregistered') { $ok = $true; break }
    }
    Start-Sleep -Seconds 5
}

if (Test-Path $addinDir) {
    Remove-Item $addinDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "[+] Da xoa thu muc add-in."
}

$portalHome = Join-Path $env:APPDATA "Tool Oumijia\Portal"
if (Test-Path $portalHome) {
    Remove-Item $portalHome -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "[+] Da xoa shared runtime."
}

if ($ok) {
    Write-Host "[OK] Da go dang ky add-in."
} else {
    Write-Host "[!] Chua xac nhan duoc tu dong. Kiem tra log: $logFile"
}
