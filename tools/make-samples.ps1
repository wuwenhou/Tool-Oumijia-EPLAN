# Generates synthetic demo EDZ packages + a fake macro file into .\examples.
# All data is invented for testing — no vendor content is used.
#
#   pwsh -File tools/make-samples.ps1
#   powershell -ExecutionPolicy Bypass -File tools/make-samples.ps1

[CmdletBinding()]
param(
    [string]$OutputDir = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $OutputDir = Join-Path (Split-Path -Parent $scriptDir) "examples"
}
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$samples = @(
    @{ File = "ABB_S201-C16.edz";                   Mfr = "ABB";                Part = "S201-C16";                      Group = "Miniature circuit breaker";          Descr = "Miniature circuit breaker 1P C16 6kA" },
    @{ File = "Siemens_3RT2024-1AL20.edz";          Mfr = "Siemens AG";         Part = "3RT2024-1AL20";                 Group = "Contactor";                          Descr = "SIRIUS contactor 7.5 kW 24 V AC/DC" },
    @{ File = "Schneider_LC1D18BD.edz";             Mfr = "Schneider Electric"; Part = "LC1D18BD";                      Group = "Contactor";                          Descr = "TeSys D contactor 18 A 24 V DC" },
    @{ File = "Phoenix_QUINT-PS-100-240AC-24DC-10.edz"; Mfr = "PHOENIX CONTACT"; Part = "QUINT-PS-100-240AC-24DC-10";   Group = "Power supply";                       Descr = "QUINT power supply 24 V 10 A" },
    @{ File = "Siemens_6ES7214-1AG40-0XB0.edz";     Mfr = "Siemens AG";         Part = "6ES7214-1AG40-0XB0";            Group = "SIMATIC S7-1200 PLC CPU 1214C";      Descr = "CPU 1214C DC/DC/DC" },
    @{ File = "Weidmueller_WDU-2-5.edz";            Mfr = "Weidmüller";         Part = "WDU 2.5";                       Group = "Terminal block";                     Descr = "Feed-through terminal 2.5 mm2" },
    @{ File = "Rittal_AE-1039-500.edz";             Mfr = "Rittal";             Part = "AE 1039.500";                   Group = "Enclosure";                          Descr = "Wall-mounted enclosure AE 300x300x120" }
)

foreach ($s in $samples) {
    $path = Join-Path $OutputDir $s.File
    Remove-Item $path -Force -ErrorAction SilentlyContinue

    $zip = [System.IO.Compression.ZipFile]::Open($path, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        $manifest = $zip.CreateEntry("manifest.xml")
        $w = New-Object System.IO.StreamWriter($manifest.Open())
        $w.Write('<?xml version="1.0" encoding="utf-8"?><manifest version="2.0"><created>SAMPLE</created></manifest>')
        $w.Dispose()

        $xml = @"
<part>
  <property id="P_ARTICLE_MANUFACTURER">$($s.Mfr)</property>
  <property id="P_ARTICLE_PARTNR">$($s.Part)</property>
  <property id="P_ARTICLE_PRODUCTGROUP">$($s.Group)</property>
  <property id="P_ARTICLE_DESCR1">$($s.Descr)</property>
</part>
"@
        $partXml = $zip.CreateEntry("items/partxml/$($s.Part).part.xml")
        $w = New-Object System.IO.StreamWriter($partXml.Open())
        $w.Write($xml)
        $w.Dispose()

        $macro = $zip.CreateEntry("macros/$($s.Part).ema")
        $w = New-Object System.IO.StreamWriter($macro.Open())
        $w.Write("SAMPLE-MACRO-CONTENT (not a real EPLAN macro)")
        $w.Dispose()

        $picture = $zip.CreateEntry("pictures/$($s.Part).png")
        $w = New-Object System.IO.StreamWriter($picture.Open())
        $w.Write("SAMPLE-PICTURE")
        $w.Dispose()
    }
    finally {
        $zip.Dispose()
    }

    Write-Host "[+] $($s.File)"
}

$fakeMacro = Join-Path $OutputDir "demo_macro.ema"
"FAKE EPLAN MACRO (synthetic file for testing Tool Oumijia — NOT a real .ema)" |
    Set-Content -Path $fakeMacro -Encoding UTF8
Write-Host "[+] demo_macro.ema"

$readme = Join-Path $OutputDir "README.md"
$readmeContent = @"
# Sample files (synthetic)

These files are **invented for testing** Tool Oumijia's scan / classify / file
workflow. They contain no vendor data and are not real EPLAN packages.

Regenerate with:

    powershell -ExecutionPolicy Bypass -File tools/make-samples.ps1

Try it:

1. Open Tool-Oumijia.exe
2. In **Xuất (Export)**, add this ``examples`` folder as inbox and press **Quét ngay**
3. Watch how each package is classified (`ABB/MCB`, `Siemens/PLC`, ...)
4. Press **XUẤT** to file them into your library tree
"@
Set-Content -Path $readme -Value $readmeContent -Encoding UTF8

Write-Host ""
Write-Host "Done. Output: $OutputDir"
