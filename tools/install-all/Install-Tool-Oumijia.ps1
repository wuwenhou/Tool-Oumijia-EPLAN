# Tool Oumijia — Smart installer (tu chon ban phu hop)
# =====================================================
# 1) Thu cai ban ADD-IN  -> self-test -> neu OK: xong.
# 2) Neu add-in bi EPLAN vo hieu hoa (thuong do thieu API license tren EPLAN moi),
#    tu dong go add-in va chuyen sang cai SCRIPT EDITION.
#
# Chay: powershell -NoProfile -ExecutionPolicy Bypass -File Install-Tool-Oumijia.ps1
#
# Ho tro ca hai kieu thu muc:
#   - Zip phat hanh : <root>\Install-Tool-Oumijia.ps1 + addin\ + script\
#   - Repo          : tools\install-all\ + ..\install-addin\ + ..\script-edition\

$ErrorActionPreference = "Stop"

$here = Split-Path -Parent $MyInvocation.MyCommand.Path

$addinDir = Join-Path $here "addin"
if (-not (Test-Path (Join-Path $addinDir "Oumijia.EplAddin.EDZ.dll"))) {
    $addinDir = Join-Path $here "..\install-addin"
}
$scriptDir = Join-Path $here "script"
if (-not (Test-Path (Join-Path $scriptDir "ToolOumijiaScript.cs"))) {
    $scriptDir = Join-Path $here "..\script-edition"
}
$thirdPartyDir = Join-Path $here "third-party"
if (-not (Test-Path (Join-Path $thirdPartyDir "THIRD-PARTY-NOTICES.txt"))) {
    $thirdPartyDir = Join-Path $here "..\..\third-party"
}

$appData = Join-Path $env:APPDATA "Tool Oumijia"
$registerLog = Join-Path $appData "logs\register-script.log"
$thirdPartyLog = Join-Path $appData "logs\register-third-party.log"
$addinLog = Join-Path $appData "logs\addin.log"
$addinHome = Join-Path $appData "AddIn"
$scriptHome = Join-Path $appData "Script"
$thirdPartyHome = Join-Path $appData "ThirdParty"
$addinUnregisterFlag = Join-Path $appData "unregister.flag"
$scriptUnregisterFlag = Join-Path $appData "script-unregister.flag"
$thirdPartyUnregisterFlag = Join-Path $appData "third-party-unregister.flag"

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

function Invoke-W3u {
    param([string]$W3u, [string[]]$Arguments)
    Start-Process -FilePath $W3u -ArgumentList $Arguments | Out-Null
}

function Wait-ForFreshLogMatch {
    param([string]$File, [long]$Before, [string]$Pattern, [int]$TimeoutSeconds)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if ((Test-Path $File) -and ((Get-Item $File).Length -gt $Before)) {
            $content = Get-Content $File -Raw -ErrorAction SilentlyContinue
            if ($content.Substring($Before) -match $Pattern) { return $true }
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

function Wait-ForThirdPartySuccess {
    param([string]$File, [long]$Before, [int]$TimeoutSeconds)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if ((Test-Path $File) -and ((Get-Item $File).Length -gt $Before)) {
            $content = Get-Content $File -Raw -ErrorAction SilentlyContinue
            $fresh = $content.Substring($Before)
            if ($fresh -match 'registered VEE@EplTableMaker' -and
                $fresh -match 'registered VE_E_ConnectionExport') {
                return $true
            }
        }
        Start-Sleep -Seconds 5
    }
    return $false
}

function Install-ThirdPartyPlugins {
    param([string]$W3u)

    $tableDll = Join-Path $thirdPartyDir 'VEE.EplTableMaker\VEE.EplTableMaker.dll'
    $exportDll = Join-Path $thirdPartyDir 'VEE.ExportConnections\VEE.ExportConnections.dll'
    $registerScript = Join-Path $thirdPartyDir 'OumijiaRegisterThirdParty.cs'
    $notice = Join-Path $thirdPartyDir 'THIRD-PARTY-NOTICES.txt'

    if (-not (Test-Path $tableDll) -or -not (Test-Path $exportDll) -or
        -not (Test-Path $registerScript) -or -not (Test-Path $notice)) {
        Write-Host "[!] Khong tim thay day du bo add-in VEE trong goi - bo qua."
        return $false
    }

    Write-Host "[*] Dang cai 2 add-in VEE (cho phep phan phoi boi tac gia)..."
    $tableHome = Join-Path $thirdPartyHome 'VEE.EplTableMaker'
    $exportHome = Join-Path $thirdPartyHome 'VEE.ExportConnections'
    New-Item -ItemType Directory -Path $tableHome -Force | Out-Null
    New-Item -ItemType Directory -Path $exportHome -Force | Out-Null

    Copy-Item $tableDll (Join-Path $tableHome 'VEE.EplTableMaker.dll') -Force
    Copy-Item $exportDll (Join-Path $exportHome 'VEE.ExportConnections.dll') -Force
    Copy-Item $registerScript (Join-Path $thirdPartyHome 'OumijiaRegisterThirdParty.cs') -Force
    Copy-Item $notice (Join-Path $thirdPartyHome 'THIRD-PARTY-NOTICES.txt') -Force

    Remove-Item $thirdPartyUnregisterFlag -ErrorAction SilentlyContinue
    $destRegister = Join-Path $thirdPartyHome 'OumijiaRegisterThirdParty.cs'
    $before = Get-LogLength $thirdPartyLog

    Start-Process -FilePath $W3u -ArgumentList @(
        '/Auto','/Quiet','/NoSplash','/NoLicenseDialog','/NoUserRightsDialog',
        '/NoLoadWorkspace','ExecuteScript', ('/ScriptFile:"' + $destRegister + '"')
    ) | Out-Null

    if (Wait-ForThirdPartySuccess -File $thirdPartyLog -Before $before -TimeoutSeconds 300) {
        Write-Host "[+] Da dang ky VEE Table Maker va VEE Export Connections."
        return $true
    }

    Write-Host ""
    Write-Host "[!] Khong xac nhan duoc dang ky 2 add-in VEE."
    Write-Host "    Kiem tra log: $thirdPartyLog"
    return $false
}

Write-Host "=================================================="
Write-Host "  TOOL OUMIJIA - CAI DAT (tu chon ban phu hop)"
Write-Host "=================================================="
Write-Host ""

$w3u = Find-W3u
if (-not $w3u) {
    Write-Host "[LOI] Khong tim thay EPLAN (W3u.exe) tren may nay."
    exit 1
}
Write-Host "[+] EPLAN: $w3u"
Write-Host ""

$thirdPartyOk = Install-ThirdPartyPlugins $w3u
Write-Host ""

# ------------------------------------------------------------------ BUOC 1: thu ban ADD-IN
$addinDll = Join-Path $addinDir "Oumijia.EplAddin.EDZ.dll"
$addinRegister = Join-Path $addinDir "OumijiaRegister.cs"

if ((Test-Path $addinDll) -and (Test-Path $addinRegister)) {
    Write-Host "[BUOC 1/2] Thu cai ban ADD-IN..."
    New-Item -ItemType Directory -Path $addinHome -Force | Out-Null
    Copy-Item $addinDll (Join-Path $addinHome "Oumijia.EplAddin.EDZ.dll") -Force

    Remove-Item $addinUnregisterFlag -ErrorAction SilentlyContinue
    $before = Get-LogLength $registerLog
    Invoke-W3u $w3u @('/Auto','/Quiet','/NoSplash','/NoLicenseDialog','/NoUserRightsDialog','/NoLoadWorkspace','ExecuteScript', ('/ScriptFile:"' + $addinRegister + '"'))
    $registered = Wait-ForFreshLogMatch -File $registerLog -Before $before -Pattern 'OK: registered' -TimeoutSeconds 300

    if ($registered) {
        Write-Host "[*] Da dang ky add-in. Kiem tra add-in co chay duoc khong..."
        $beforeAddin = Get-LogLength $addinLog
        Invoke-W3u $w3u @('/Auto','/Quiet','/NoSplash','/NoLicenseDialog','/NoUserRightsDialog','/NoLoadWorkspace','OumijiaSelfTest')
        $addinWorks = Wait-ForFreshLogMatch -File $addinLog -Before $beforeAddin -Pattern 'add-in loaded' -TimeoutSeconds 300

        if ($addinWorks) {
            Write-Host ""
            Write-Host "[OK] BAN ADD-IN DA HOAT DONG!"
            if ($thirdPartyOk) { Write-Host "[OK] Add-ins VEE da cai va dang ky." }
            Write-Host "     Mo EPLAN lai -> ribbon 'Tool Oumijia' va 'VIET EPLAN ELECTRIC' se xuat hien."
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
} else {
    Write-Host "[!] Khong tim thay bo add-in trong goi - bo qua buoc 1."
}

# ------------------------------------------------------------------ BUOC 2: cai SCRIPT EDITION
$scriptSource = Join-Path $scriptDir "ToolOumijiaScript.cs"
$scriptRegisterSource = Join-Path $scriptDir "OumijiaRegisterScript.cs"

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

Remove-Item $scriptUnregisterFlag -ErrorAction SilentlyContinue
$before2 = Get-LogLength $registerLog
Invoke-W3u $w3u @('/Auto','/Quiet','/NoSplash','/NoLicenseDialog','/NoUserRightsDialog','/NoLoadWorkspace','ExecuteScript', ('/ScriptFile:"' + $destRegister + '"'))
$regScript = Wait-ForFreshLogMatch -File $registerLog -Before $before2 -Pattern '\[SCRIPT\] OK: da dang ky' -TimeoutSeconds 300

if (-not $regScript) {
    Write-Host ""
    Write-Host "[!] Khong xac nhan duoc dang ky script. Kiem tra log: $registerLog"
    exit 1
}

Write-Host "[*] Da dang ky script. Kiem chung script chay duoc..."
$beforeScript = Get-LogLength $addinLog
Invoke-W3u $w3u @('/Auto','/Quiet','/NoSplash','/NoLicenseDialog','/NoUserRightsDialog','/NoLoadWorkspace','OumijiaScriptSelfTest')
$scriptWorks = Wait-ForFreshLogMatch -File $addinLog -Before $beforeScript -Pattern 'script loaded' -TimeoutSeconds 300

Write-Host ""
if ($scriptWorks) {
    Write-Host "[OK] BAN SCRIPT EDITION DA HOAT DONG!"
    if ($thirdPartyOk) { Write-Host "[OK] Add-ins VEE da cai va dang ky." }
    Write-Host "     Mo EPLAN lai -> ribbon 'Tool Oumijia' va 'VIET EPLAN ELECTRIC' se xuat hien."
} else {
    Write-Host "[!] Da dang ky nhung chua xac nhan duoc script chay tren ban EPLAN nay."
    Write-Host "    Kiem tra log: $addinLog"
    Write-Host "    (Mot so ban EPLAN cu thieu reference script - xem script\README.txt)"
    exit 1
}
