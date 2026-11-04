# Tool Oumijia — Script Edition uninstaller
# ==========================================
#   powershell -NoProfile -ExecutionPolicy Bypass -File Uninstall-Script.ps1

$ErrorActionPreference = "Stop"

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$registerScript = Join-Path $here "OumijiaRegisterScript.cs"
$destDir = Join-Path $env:APPDATA "Tool Oumijia\Script"
$destRegister = Join-Path $destDir "OumijiaRegisterScript.cs"
$logFile = Join-Path $env:APPDATA "Tool Oumijia\logs\register-script.log"
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
Write-Host "  TOOL OUMIJIA - GO SCRIPT EDITION"
Write-Host "=============================================="
Write-Host ""

# Ưu tiên dùng bản đã cài trong %APPDATA%; nếu chưa có thì dùng bản cạnh file này
if (-not (Test-Path $destRegister)) { $destRegister = $registerScript }
if (-not (Test-Path $destRegister)) {
    Write-Host "[LOI] Khong tim thay OumijiaRegisterScript.cs."
    exit 1
}

$w3u = Find-W3u
if (-not $w3u) {
    Write-Host "[LOI] Khong tim thay EPLAN (W3u.exe) tren may nay."
    exit 1
}

Set-Content -Path $flagFile -Value "unregister" -Encoding ASCII
Write-Host "[*] Dang go dang ky (EPLAN khoi dong an)..."
$before = if (Test-Path $logFile) { (Get-Item $logFile).Length } else { 0 }

Start-Process -FilePath $w3u -ArgumentList @(
    '/Auto', '/Quiet', '/NoSplash', '/NoLicenseDialog', '/NoUserRightsDialog',
    '/NoLoadWorkspace', 'ExecuteScript', ('/ScriptFile:"' + $destRegister + '"')
) | Out-Null

$deadline = (Get-Date).AddSeconds(300)
$ok = $false
while ((Get-Date) -lt $deadline) {
    if ((Test-Path $logFile) -and ((Get-Item $logFile).Length -gt $before)) {
        $tail = Get-Content $logFile -ErrorAction SilentlyContinue | Select-Object -Last 8
        if ($tail -match '\[SCRIPT\] OK: da go dang ky') { $ok = $true; break }
    }
    Start-Sleep -Seconds 5
}

if (Test-Path $destDir) {
    Remove-Item $destDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "[+] Da xoa thu muc script."
}

if ($ok) {
    Write-Host "[OK] Da go dang ky Script Edition."
} else {
    Write-Host "[!] Chua xac nhan duoc tu dong. Kiem tra log: $logFile"
}
