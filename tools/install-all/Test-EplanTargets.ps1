# Read-only discovery/selection tests with synthetic W3u files in a unique temp folder.
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'EplanTargets.ps1')

$fixture = Join-Path ([IO.Path]::GetTempPath()) ('opencode\eplan-target-fixture-' + [guid]::NewGuid().ToString('N'))
try {
    foreach ($folder in @('Education\2026.0.3', 'Electric P8\2022.0.3')) {
        $bin = Join-Path (Join-Path $fixture $folder) 'Bin'
        New-Item -ItemType Directory -Path $bin -Force | Out-Null
        New-Item -ItemType File -Path (Join-Path $bin 'W3u.exe') -Force | Out-Null
    }
    $all = @(Get-OumijiaEplanInstallations -Roots @($fixture, $fixture))
    if ($all.Count -ne 2) { throw "Expected two distinct installations, got $($all.Count)" }

    $ambiguous = $false
    try { Select-OumijiaEplanInstallation -Installations $all | Out-Null }
    catch { $ambiguous = $_.Exception.Message -match 'nhieu ban EPLAN' }
    if (-not $ambiguous) { throw 'Multiple installations were not rejected.' }

    $education = @($all | Where-Object { $_.Variant -eq 'Education' })[0]
    $selected = Select-OumijiaEplanInstallation -Installations $all -TargetW3u $education.W3u
    if ($selected.W3u -ne $education.W3u) { throw 'Explicit target selection failed.' }

    $unknown = $false
    try { Select-OumijiaEplanInstallation -Installations $all -TargetW3u (Join-Path $fixture 'unknown\W3u.exe') | Out-Null }
    catch { $unknown = $_.Exception.Message -match 'khong nam trong danh sach' }
    if (-not $unknown) { throw 'Unknown W3u was not rejected.' }

    if ((Select-OumijiaEplanInstallation -Installations @($education)).W3u -ne $education.W3u) {
        throw 'Single installation auto-selection failed.'
    }
    if (@(Get-OumijiaEplanInstallations -Roots @((Join-Path $fixture 'missing'))).Count -ne 0) {
        throw 'Missing directory discovery failed.'
    }
    Write-Host '[OK] Discovery/deduplication, ambiguous/explicit/single/unknown target tests PASS.'
}
finally {
    if (Test-Path -LiteralPath $fixture) { Remove-Item -LiteralPath $fixture -Recurse -Force }
}
