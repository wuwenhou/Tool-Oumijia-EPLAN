# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned
- Icon cho 2 nút ribbon; chọn ngôn ngữ hiển thị tên part (hiện dùng en_US)
- Thanh tiến trình cho import/export lô lớn
- EDZ writer: create/repack packages
- Nghiên cứu tab "Parts Management Extension" (API không chính thức)

## [0.4.0] - 2026-11-04 — "Hoàn thiện theo UAT"

### Added
- **Panel chi tiết** trong dialog Xuất (như EPLAN Overview): bấm part/nhóm → bảng
  Thuộc tính/Giá trị (mã hàng, type/order/ERP, hãng, nhà cung cấp, nhóm 3 cấp,
  tên 1/2/3, mô tả, eCl@ss, EAN, macro, kích thước, ngày/người tạo & sửa); nháy đúp = copy
- **Chế độ xuất "Theo loại bản ghi (như EPLAN)"**: tick chọn Parts / Accessory list /
  Accessory placement / Drilling pattern / Connection point pattern /
  Customer+Manufacturer+Supplier — dùng `MDPartsManagement.ExportParts` (cờ đã kiểm chứng
  thực tế bằng giải nén EDZ đối chiếu số liệu database)
- Ô đích xuất **tự điền sẵn** `Documents\parts.edz`; nhãn database gọn (`ESS_part001.alk — 647 part`)
- Kiểm tra **construct UI offscreen** trong `OumijiaDiagParts` (bắt lỗi khởi tạo/layout)

### Changed
- **Dialog Nhập đơn giản hóa**: bỏ hàng tùy chọn (Chế độ nhập / Nhận dạng part / Ngôn ngữ phụ);
  cố định mặc định hợp lý — cập nhật + thêm mới, theo mã hàng
- Dialog Xuất: layout làm lại bằng TableLayoutPanel + Dock (không còn toạ độ tuyệt đối/neo phải
  dễ khuất); dòng đã chọn **in đậm**; đếm "Đã chọn: N part" luôn hiển thị

### Fixed
- **Tick chọn không hiện dấu ✓** (dù đã đếm đúng): buộc vẽ lại TreeView (`Invalidate/Update`)
  sau mỗi thay đổi trạng thái + in đậm dòng đã chọn làm chỉ báo phụ
- **Nút "XUẤT RA EDZ ▸" và "Chọn…" bị khuất** khỏi vùng nhìn (layout neo toạ độ cũ) — nay
  luôn ở đúng vị trí (nút Xuất neo phải hàng "Đã chọn" bằng bảng layout)
- **Crash khi mở dialog Xuất** (`SplitterDistance must be between Panel1MinSize and…`) —
  bắt được nhờ construct-test, sửa bằng cách áp dụng min-size trong `OnShown` có guard

## [0.3.0] - 2026-11-04 — "Add-in duy nhất"

### Removed (BREAKING)
- **Bỏ hoàn toàn app standalone (`.exe`)** theo yêu cầu người dùng — chỉ còn add-in trong EPLAN:
  - Xóa `src/Oumijia.Tool`, `src/Oumijia.Core`, `tests/`, `examples/`, `src/Oumijia.Register`,
    `tools/make-samples.ps1`, solution file và CI workflow
  - Bỏ nút "Cửa sổ Tool Oumijia" + action `OumijiaOpenTool` khỏi ribbon

### Added
- **Dialog Xuất EDZ v2**: chọn part bằng **cây nhóm kiểu navigator EPLAN**
  (Nhóm sản phẩm → nhóm → phân nhóm → hãng → part), checkbox 3 trạng thái (chọn/một phần/bỏ),
  ô tìm kiếm theo mã hàng/tên/hãng, nút *Chọn tất cả / Bỏ chọn / Lấy từ Parts Management*,
  đếm "Đã chọn: N part"; xuất **1 file `parts.edz`** hoặc **mỗi part 1 file** — giống EPLAN
- `OumijiaDiagParts` — action chẩn đoán headless: mở Parts Database, đo tốc độ đọc part
  (647 part ~80 ms), tóm tắt cây nhóm, smoke-export 25 part ra EDZ
- Script chẩn đoán: `tools/eplan-scripts/OumijiaRibbonDiag.cs` (liệt kê ribbon tabs)
  và `OumijiaReadReg.cs` (đọc lại giá trị registration thực tế)
- Gói release **add-in-only** `Tool-Oumijia-AddIn-vX.zip` (~26 KB thay vì 67 MB)

### Fixed
- **Registration bị giá trị lịch sử "đè"**: script đăng ký giờ **xóa key trước khi ghi**
  (delete-first) + đọc lại xác nhận (VERIFY) — khắc phục triệt để việc EPLAN vẫn nạp DLL cũ
  từ đường dẫn khác (vd đăng ký thủ công qua dialog "API add-ins" với path bản giải nén)
- Hiển thị tên nhóm/hãng/tên part: dùng `MDPropertyValue.GetDisplayString().GetStringToDisplay()`
  — nhóm hiện đúng tên ("Electrical engineering / PLC / General") thay vì mã số,
  tên part hiện đúng một ngôn ngữ ("LOGO! 24RC") thay vì chuỗi đa ngôn ngữ trộn

### Notes
- Script đăng ký tự dọn key `OumijiaOpenTool` cũ khi chạy lại; chạy lại installer là đủ để nâng cấp.

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

[Unreleased]: https://github.com/wuwenhou/Tool-Oumijia-EPLAN/compare/v0.4.0...HEAD
[0.4.0]: https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/tag/v0.4.0
[0.3.0]: https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/tag/v0.3.0
[0.2.0]: https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/tag/v0.2.0
[0.1.0]: https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/tag/v0.1.0
