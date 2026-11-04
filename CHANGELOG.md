# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned
- EPLAN add-in v0.2: batch EDZ import into the parts database (official EPLAN API), ribbon integration
- EDZ writer: create/repack packages (fill macros, keep unknown records)
- Optional English UI localization

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

[Unreleased]: https://github.com/wuwenhou/Tool-Oumijia-EPLAN/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/tag/v0.1.0
