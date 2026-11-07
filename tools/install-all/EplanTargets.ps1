# Read-only EPLAN installation discovery shared by the smart installer/uninstaller.
# Keep this file UTF-8 with BOM for Windows PowerShell 5.1.

function Get-OumijiaEplanInstallations {
    param([string[]]$Roots = @(
        (Join-Path $env:ProgramFiles 'EPLAN'),
        $(if (${env:ProgramFiles(x86)}) { Join-Path ${env:ProgramFiles(x86)} 'EPLAN' })
    ))

    $found = @{}
    foreach ($root in $Roots) {
        if (-not $root -or -not (Test-Path -LiteralPath $root -PathType Container)) { continue }
        foreach ($variant in (Get-ChildItem -LiteralPath $root -Directory -ErrorAction SilentlyContinue)) {
            foreach ($version in (Get-ChildItem -LiteralPath $variant.FullName -Directory -ErrorAction SilentlyContinue)) {
                $candidate = Join-Path $version.FullName 'Bin\W3u.exe'
                if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) { continue }
                $full = [IO.Path]::GetFullPath($candidate)
                if (-not $found.ContainsKey($full)) {
                    $found[$full] = [pscustomobject]@{
                        Variant = $variant.Name
                        Version = $version.Name
                        W3u = $full
                    }
                }
            }
        }
    }
    @($found.Values | Sort-Object Variant, Version, W3u)
}

function Select-OumijiaEplanInstallation {
    param([object[]]$Installations, [string]$TargetW3u)

    if ($Installations.Count -eq 0) { throw 'Khong tim thay EPLAN (W3u.exe) tren may nay.' }
    if ($TargetW3u) {
        $full = [IO.Path]::GetFullPath($TargetW3u)
        $match = @($Installations | Where-Object { [String]::Equals($_.W3u, $full, [StringComparison]::OrdinalIgnoreCase) })
        if ($match.Count -ne 1) { throw "TargetW3u khong nam trong danh sach EPLAN da phat hien: $TargetW3u" }
        return $match[0]
    }
    if ($Installations.Count -eq 1) { return $Installations[0] }
    $choices = ($Installations | ForEach-Object { '  ' + $_.Variant + ' ' + $_.Version + ': ' + $_.W3u }) -join [Environment]::NewLine
    throw ('Co nhieu ban EPLAN. Khong tu chon ban dau tien. Chay -ListInstalled roi chi dinh -TargetW3u:' +
        [Environment]::NewLine + $choices)
}
