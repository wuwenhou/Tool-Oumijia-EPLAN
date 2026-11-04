# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned
- EDZ writer: create/repack packages (fill macros, keep unknown records)
- Parts Management tab (official EPLAN "Parts Management Extension" API) — xem docs/ROADMAP.md
- Optional English UI localization

## [0.2.0] - 2026-11-04

### Added — EPLAN add-in (`Oumijia.EplAddin.EDZ`)
- Real EPLAN add-in (net48, x64) with the ribbon tab **"Tool Oumijia"**:
  - **Nhập EDZ…** — batch import of EDZ packages into the parts database via the official
    `MDPartsManagement.ImportParts` API (converter `IXPartsImportExportEdz`) with import modes
    (append only / update only / update + append), identifier options, and a CSV audit log.
  - **Xuất EDZ…** — export parts to EDZ via `MDPartsDatabase.ExportParts(..., DataFormat.EDZ)`,
    whole database or current selection, single file or one file per part.
  - **Cửa sổ Tool Oumijia** — opens the standalone tool.
- Headless add-in installer (`addin/Install-AddIn.bat`): copies the DLL and registers the add-in by
  running a small EPLAN script through `W3u.exe … ExecuteScript` — verified end-to-end on EPLAN
  Platform 2022.0.3 (load → ribbon → command-line action → clean exit). Uninstall included.
- Registration reference script (`tools/eplan-scripts/OumijiaRegister.cs`) documenting the complete
  EPLAN registration surface (module + per-action `Services.AsmActions` entries, `@` escape rule).
- `Oumijia.Register` console utility (status/read + experimental offline registration; note: EPLAN
  does not persist offline settings writes — the script installer is the supported path).

### Changed
- **EDZ-only focus**: Export scan and Import queue now handle only `.edz` files; the EMA/EMP/EMS
  handling was removed from the UI (per user decision).
- Version bumped to 0.2.0; release packaging bundles the `addin/` folder with an integrity note.

### Fixed
- (add-in) `IEplAction.OnRegister(ref string, ref int)` signature corrected for EPLAN 2022.

## [0.1.0] - 2026-11-04

### Added
- **Export workflow**: scan inbox folders for `.edz` / `.ema` files, show a selection table
  (manufacturer, category, part number, editable destination), and file the selection into a
  managed library tree (`<root>/<Manufacturer>/<Category>/<file>`).
- **EDZ reader** (`Oumijia.Core.Edz`): container detection (ZIP + 7z), tolerant part-XML
  metadata harvesting (manufacturer, part/type/order number, product group, eCl@ss),
  attachment inventory (macros, documents, DXF, images).
- **Rule-based classifier** with 25+ built-in categories and manufacturer alias/suffix
  normalization; fully configurable through `rules.json` (auto-generated on first run).
- **Safe library writer**: conflict policies `Rename` (default), `Skip`, `Overwrite`;
  identical-content detection (SHA-256); move or copy; CSV audit log.
- **Macro installer**: copy `.ema/.emp/.ems` into the EPLAN master-data macro folder
  (auto-detection of common locations + manual override).
- **Manual override memory** (`overrides.json`): edited destinations are remembered per file name.
- **WinForms application** (Vietnamese UI) with Export / Import / Settings tabs; multi-target
  `net8.0-windows` (standalone) + `net48` (for the EPLAN add-in to reuse the same UI).
- **EPLAN add-in scaffold** (`src/Oumijia.EplanAddIn`): `IEplAddIn` entry point + `OumijiaOpenTool`
  action; buildable on machines with an EPLAN installation (see docs/BUILD.md).
- **Test suite**: 25+ xUnit tests covering the EDZ reader (ZIP + real 7z), classifier,
  library writer and the committed synthetic example packages.
- **Tooling**: `tools/pack.ps1` (self-contained release zip + hashes),
  `tools/make-samples.ps1` (synthetic EDZ examples), GitHub Actions CI.

### Notes
- The tool parses only factual part data and never reproduces EPLAN proprietary
  macro geometry (`.ema` is treated as an opaque attachment).
- Vendor data is never committed to this repository; `examples/` contains synthetic files only.

[Unreleased]: https://github.com/wuwenhou/Tool-Oumijia-EPLAN/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/tag/v0.2.0
[0.1.0]: https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/tag/v0.1.0
