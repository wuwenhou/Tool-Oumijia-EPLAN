# Electric P8 2022.0.3 only. Stage -> GUI UAT -> Finalize, or Rollback.
# Never copy into the shared AddIn/Portal folders used by other EPLAN versions.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$TargetW3u,
    [ValidateSet('Stage','Finalize','Rollback')][string]$Phase = 'Stage'
)

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $here 'EplanTargets.ps1')
$target = Select-OumijiaEplanInstallation -Installations @(Get-OumijiaEplanInstallations) -TargetW3u $TargetW3u
if ($target.Variant -ne 'Electric P8' -or $target.Version -ne '2022.0.3') {
    throw 'Refusing: 2022 payload is only for Electric P8 2022.0.3.'
}
$platform = Join-Path (Split-Path (Split-Path (Split-Path $target.W3u -Parent) -Parent) -Parent) 'Platform\2022.0.3\Bin\EPLAN.exe'
if (-not (Test-Path -LiteralPath $platform -PathType Leaf)) {
    # W3u is in <root>\Electric P8\2022.0.3\Bin, EPLAN.exe is in <root>\Platform\2022.0.3\Bin.
    $platform = Join-Path (Split-Path (Split-Path (Split-Path (Split-Path $target.W3u -Parent) -Parent) -Parent) -Parent) 'Platform\2022.0.3\Bin\EPLAN.exe'
}
if (-not (Test-Path -LiteralPath $platform -PathType Leaf)) { throw 'Missing Platform/2022.0.3 EPLAN.exe.' }
$payload = Join-Path $here 'payload\2022'
$manifestPath = Join-Path $payload 'manifest.sha256.csv'
$registerScript = Join-Path $payload 'OumijiaRegister2022.cs'
$appData = Join-Path $env:APPDATA 'Tool Oumijia'
$addinRoot = Join-Path $appData 'AddIn'
$stageDir = Join-Path $addinRoot '2022.staging'
$finalDir = Join-Path $addinRoot '2022'
$dllName = 'Oumijia.EplAddin.EDZ.dll'
$stateFile = Join-Path $appData '2022-install-state.json'
$registrationLog = Join-Path $appData 'logs\register-2022.log'
$addinLog = Join-Path $appData 'logs\addin.log'
$cfg = Join-Path (Join-Path (Join-Path (Join-Path $env:PUBLIC 'EPLAN\Settings\Electric P8') '2022.0.3') 'x64') 'Cfg'
$stationName = 'STATION.Basic.' + $env:COMPUTERNAME + '.xdb'

function Assert-Closed {
    if (@(Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -match '^(EPLAN|W3u)$' }).Count -gt 0) {
        throw 'EPLAN/W3u is running. Save work and close all EPLAN instances before continuing.'
    }
}

function Assert-Payload {
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf) -or
        -not (Test-Path -LiteralPath $registerScript -PathType Leaf)) { throw '2022 payload/registration script missing.' }
    $entries = @(Import-Csv -LiteralPath $manifestPath)
    $actual = @(Get-ChildItem -LiteralPath $payload -Filter '*.dll' -File)
    if ($entries.Count -lt 4 -or $actual.Count -ne $entries.Count) { throw '2022 manifest/DLL count mismatch.' }
    foreach ($required in @($dllName,'Oumijia.EplanUi.dll','Oumijia.EdzReader.dll','SharpCompress.dll')) {
        if (@($entries | Where-Object { $_.Name -eq $required }).Count -ne 1) { throw "Missing required DLL: $required" }
    }
    foreach ($entry in $entries) {
        if ($entry.Name -notmatch '^[a-zA-Z0-9_.-]+\.dll$' -or
            $entry.Name -match '^(Oumijia\.PortalHost|Microsoft\.Web\.WebView2|WebView2Loader)' -or
            @($entries | Where-Object { $_.Name -eq $entry.Name }).Count -ne 1) { throw 'Invalid or duplicate DLL in manifest.' }
        $file = Join-Path $payload $entry.Name
        if (-not (Test-Path -LiteralPath $file -PathType Leaf) -or
            (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $entry.SHA256) { throw "Payload hash mismatch: $file" }
    }
    return $entries
}

function Backup-Verified {
    if (-not (Test-Path -LiteralPath $cfg -PathType Container) -or
        -not (Test-Path -LiteralPath (Join-Path $cfg $stationName) -PathType Container)) {
        throw "Missing 2022 settings: $cfg\$stationName"
    }
    $base = Join-Path $env:LOCALAPPDATA 'Tool Oumijia\backups'
    $backup = Join-Path $base ('pre-2022-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,8))
    New-Item -ItemType Directory -Path $backup -Force | Out-Null
    $rows = New-Object System.Collections.Generic.List[object]
    foreach ($source in @(
        [pscustomobject]@{Path=$cfg; Target=(Join-Path $backup 'Cfg')},
        [pscustomobject]@{Path=$appData; Target=(Join-Path $backup 'appdata-ToolOumijia')}
    )) {
        if (-not (Test-Path -LiteralPath $source.Path -PathType Container)) { continue }
        Copy-Item -LiteralPath $source.Path -Destination $source.Target -Recurse -Force
        foreach ($item in (Get-ChildItem -LiteralPath $source.Path -File -Recurse -Force)) {
            $rel = $item.FullName.Substring($source.Path.Length).TrimStart('\')
            $copy = Join-Path $source.Target $rel
            if (-not (Test-Path -LiteralPath $copy -PathType Leaf)) { throw "Backup missing: $copy" }
            $hash = (Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash
            if ($hash -ne (Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash) { throw "Backup mismatch: $copy" }
            $rows.Add([pscustomobject]@{Original=$item.FullName;Backup=$copy;SHA256=$hash})
        }
    }
    $rows | Export-Csv -LiteralPath (Join-Path $backup 'SHA256.csv') -NoTypeInformation -Encoding UTF8
    Write-Host "Backup verified: $backup ($($rows.Count) files)"
    return $backup
}

function Restore-Station {
    param([string]$Backup)
    Assert-Closed
    $snapshot = Join-Path (Join-Path $Backup 'Cfg') $stationName
    $dest = Join-Path $cfg $stationName
    if (-not (Test-Path -LiteralPath $snapshot -PathType Container) -or
        -not (Test-Path -LiteralPath (Join-Path $Backup 'SHA256.csv') -PathType Leaf)) { throw 'Backup station snapshot missing.' }
    $expected = @(Import-Csv -LiteralPath (Join-Path $Backup 'SHA256.csv') | Where-Object { $_.Backup.StartsWith($snapshot + '\', [StringComparison]::OrdinalIgnoreCase) })
    if ($expected.Count -eq 0) { throw 'No station hashes in backup.' }
    foreach ($row in $expected) {
        if ((Get-FileHash -LiteralPath $row.Backup -Algorithm SHA256).Hash -ne $row.SHA256) { throw "Corrupt backup: $($row.Backup)" }
    }
    # Replace only this target's STATION database. USER settings/credentials are never restored.
    foreach ($row in $expected) {
        $rel = $row.Backup.Substring($snapshot.Length).TrimStart('\')
        $destination = Join-Path $dest $rel
        New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
        Copy-Item -LiteralPath $row.Backup -Destination $destination -Force
        if ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash -ne $row.SHA256) { throw "Station restore mismatch: $destination" }
    }
    Write-Host "Station settings restored from: $Backup"
}

function Invoke-Headless {
    param([string[]]$Arguments, [string]$Log, [string[]]$Required, [switch]$AllowNonZeroExitWithMarkers)
    Assert-Closed
    $before = if (Test-Path -LiteralPath $Log) { (Get-Item -LiteralPath $Log).Length } else { 0 }
    $p = Start-Process -FilePath $target.W3u -WorkingDirectory (Split-Path -Parent $target.W3u) -ArgumentList $Arguments -PassThru
    if (-not $p.WaitForExit(180000)) { throw 'EPLAN headless timed out; leave it running for inspection.' }
    $limit = (Get-Date).AddSeconds(30)
    while ((Get-Date) -lt $limit) {
        if (@(Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -match '^(EPLAN|W3u)$' }).Count -eq 0) { break }
        Start-Sleep -Seconds 2
    }
    Assert-Closed
    if (-not (Test-Path -LiteralPath $Log) -or (Get-Item -LiteralPath $Log).Length -le $before) {
        throw "No fresh log after EPLAN command (exit $($p.ExitCode)): $Log"
    }
    $fs = [IO.File]::Open($Log,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite)
    try {
        [void]$fs.Seek($before,[IO.SeekOrigin]::Begin)
        $reader = New-Object IO.StreamReader($fs,[Text.Encoding]::UTF8)
        $fresh = $reader.ReadToEnd()
    } finally { $fs.Dispose() }
    if ($fresh -match '(?m)^.*ERROR: ' -or $fresh -match 'OnInitGui.*(error|failed)') {
        throw "EPLAN command failed: $fresh"
    }
    $missing = @($Required | Where-Object { -not $fresh.Contains($_) })
    if ($missing.Count -gt 0) {
        throw "Missing fresh marker(s) '$($missing -join "', '")': $fresh"
    }
    if ($p.ExitCode -ne 0) {
        if (-not $AllowNonZeroExitWithMarkers) {
            throw "EPLAN command failed (exit $($p.ExitCode)): $fresh"
        }
        # Headless EPLAN often exits non-zero after a successful script/registration.
        # The fresh marker plus the following Verify run are the authoritative checks.
        Write-Warning "EPLAN exit code $($p.ExitCode), but all fresh markers are present; continuing."
    }
    Write-Host "Verified fresh markers: $($Required -join ', ')"
}

function Invoke-Registration {
    param([string]$Dll, [string]$Operation)
    Invoke-Headless -Arguments @('/Auto','/Quiet','/NoSplash','/NoLicenseDialog','/NoUserRightsDialog','/NoLoadWorkspace',
        'ExecuteScript',('/ScriptFile:"' + $registerScript + '"'),('/AssemblyPath:"' + $Dll + '"'),('/Operation:' + $Operation)) `
        -Log $registrationLog -Required @( ('OK: ' + $(if($Operation -eq 'Register'){'registered'}elseif($Operation -eq 'Verify'){'verified'}else{'unregistered'}) + ' ' + $Dll) ) `
        -AllowNonZeroExitWithMarkers
}

function Invoke-SelfTest {
    Invoke-Headless -Arguments @('/Auto','/Quiet','/NoSplash','/NoLicenseDialog','/NoUserRightsDialog','/NoLoadWorkspace','OumijiaSelfTest') `
        -Log $addinLog -Required @('SELFTEST OK','SELFTEST ACTION XMVCCloudSolutionAction => found',
            'SELFTEST ACTION GfDialogManagerShow => found','SELFTEST ACTION MfToggleMainMenuAction => found')
}

Assert-Closed
if ($Phase -eq 'Stage') {
    $entries = @(Assert-Payload)
    if ((Test-Path -LiteralPath $stateFile) -or (Test-Path -LiteralPath $stageDir) -or (Test-Path -LiteralPath $finalDir)) {
        throw '2022 installation state/runtime already exists. Do not overwrite it; inspect or rollback first.'
    }
    $backup = Backup-Verified
    try {
        New-Item -ItemType Directory -Path $stageDir -Force | Out-Null
        foreach ($entry in $entries) {
            $dst = Join-Path $stageDir $entry.Name
            Copy-Item -LiteralPath (Join-Path $payload $entry.Name) -Destination $dst
            if ((Get-FileHash -LiteralPath $dst -Algorithm SHA256).Hash -ne $entry.SHA256) { throw "Stage hash mismatch: $dst" }
        }
        $dll = Join-Path $stageDir $dllName
        Invoke-Registration -Dll $dll -Operation Register
        Invoke-Registration -Dll $dll -Operation Verify
        Invoke-SelfTest
        [pscustomobject]@{ Phase='Staged';TargetW3u=$target.W3u;Backup=$backup;DllSHA256=(Get-FileHash $dll -Algorithm SHA256).Hash } |
            ConvertTo-Json | Set-Content -LiteralPath $stateFile -Encoding UTF8
        Write-Host 'STAGED: Start EPLAN 2022 GUI for ribbon/Cloud UAT. Close EPLAN before -Phase2022 Finalize.'
    } catch {
        $failure = $_
        Write-Warning "Stage failed: $failure"
        Assert-Closed
        Restore-Station -Backup $backup
        if (Test-Path -LiteralPath $stageDir) { Remove-Item -LiteralPath $stageDir -Recurse -Force }
        throw $failure
    }
    return
}

if (-not (Test-Path -LiteralPath $stateFile -PathType Leaf)) { throw 'No staged 2022 installation state.' }
$state = Get-Content -LiteralPath $stateFile -Raw | ConvertFrom-Json
if ($state.Phase -ne 'Staged' -or $state.TargetW3u -ne $target.W3u -or
    -not (Test-Path -LiteralPath $state.Backup -PathType Container)) { throw 'State/target mismatch; refusing changes.' }

if ($Phase -eq 'Rollback') {
    $stagedDll = Join-Path $stageDir $dllName
    if (-not (Test-Path -LiteralPath $stagedDll -PathType Leaf) -or
        (Get-FileHash -LiteralPath $stagedDll -Algorithm SHA256).Hash -ne $state.DllSHA256) {
        throw 'Staged DLL changed; refuse automatic rollback. Inspect manually.'
    }
    Invoke-Registration -Dll $stagedDll -Operation Verify
    Restore-Station -Backup $state.Backup
    if (Test-Path -LiteralPath $stageDir) { Remove-Item -LiteralPath $stageDir -Recurse -Force }
    if (Test-Path -LiteralPath $finalDir) { throw 'Final runtime exists; do not delete it automatically during staged rollback.' }
    Remove-Item -LiteralPath $stateFile -Force
    Write-Host 'ROLLED BACK 2022 STATION registration. Shared runtime and 2026 were untouched.'
    return
}

$stagedDll = Join-Path $stageDir $dllName
if (-not (Test-Path -LiteralPath $stagedDll -PathType Leaf) -or
    (Get-FileHash -LiteralPath $stagedDll -Algorithm SHA256).Hash -ne $state.DllSHA256 -or
    (Test-Path -LiteralPath $finalDir)) { throw 'Staged DLL hash/state mismatch or final folder already exists.' }
try {
    Invoke-Registration -Dll $stagedDll -Operation Verify
} catch {
    Write-Warning "Staged DLL is not currently registered; restoring the staged registration before finalize."
    Invoke-Registration -Dll $stagedDll -Operation Register
    Invoke-Registration -Dll $stagedDll -Operation Verify
}

# Keep 2022.staging valid while final registration runs. Moving it first made
# the STATION registration point at a missing path during the headless launch,
# so EPLAN exited non-zero even though the register script had written the new
# path. Copy first, register/verify/self-test the final copy, then remove staging.
$finalDll = Join-Path $finalDir $dllName
try {
    New-Item -ItemType Directory -Path $finalDir -Force | Out-Null
    foreach ($item in (Get-ChildItem -LiteralPath $stageDir -File -Force)) {
        Copy-Item -LiteralPath $item.FullName -Destination (Join-Path $finalDir $item.Name) -Force
    }
    if (-not (Test-Path -LiteralPath $finalDll -PathType Leaf) -or
        (Get-FileHash -LiteralPath $finalDll -Algorithm SHA256).Hash -ne $state.DllSHA256) {
        throw 'Final copy hash mismatch.'
    }
    Invoke-Registration -Dll $finalDll -Operation Register
    Invoke-Registration -Dll $finalDll -Operation Verify
    Invoke-SelfTest
    $state.Phase = 'Finalized'
    $state | ConvertTo-Json | Set-Content -LiteralPath $stateFile -Encoding UTF8
    if (Test-Path -LiteralPath $stageDir) {
        try { Remove-Item -LiteralPath $stageDir -Recurse -Force -ErrorAction Stop }
        catch { Write-Warning "Finalized, but could not remove staging folder: $stageDir" }
    }
    Write-Host "FINALIZED: $finalDll (backup: $($state.Backup))."
} catch {
    $failure = $_
    Write-Warning "Finalize failed: $failure"
    Assert-Closed
    Restore-Station -Backup $state.Backup
    if (Test-Path -LiteralPath $finalDir) { Remove-Item -LiteralPath $finalDir -Recurse -Force }
    if (Test-Path -LiteralPath $stageDir) {
        try {
            Invoke-Registration -Dll $stagedDll -Operation Register
            Invoke-Registration -Dll $stagedDll -Operation Verify
            Write-Host 'Staging restored and registered; state remains Staged.'
        } catch {
            Write-Warning 'Could not re-register the staged DLL. State remains Staged; re-run Stage from a backup if needed.'
        }
    }
    throw $failure
}
