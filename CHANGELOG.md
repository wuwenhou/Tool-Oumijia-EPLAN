# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.6.0-rc.1] - 2026-11-07 — Pre-release Education 2026 (chưa đủ UAT ổn định)

### Added
- `docs/AGENT-HANDOFF-2026-11-07.md`: kế hoạch, công nghệ, nguồn chứng cứ, lịch sử sửa đổi, ma trận UAT và cách rollback cho AI Agent tiếp tục.
- Smart installer/uninstaller có `EplanTargets.ps1`, `-ListInstalled` (read-only), `-TargetW3u`; test fixture hai variant trong `tools/install-all/Test-EplanTargets.ps1`.

### Changed
- Ribbon Script/Add-in: nhóm **Quản lý EDZ** là nhóm đầu, đúng ba nút **Nhập EDZ → Xuất EDZ → Menu bar** xếp dọc; dọn nhóm cũ **Nhập EDZ** và Portal trùng trong EDZ, giữ bảy nút native ở nhóm **Eplan Cloud** cùng tab Cloud gốc.
- Bộ cài candidate từ chối target khác Education 2026.0.3 (Script Edition), từ chối chọn W3u đầu tiên khi có nhiều bản, chặn cài khi EPLAN đang chạy và sao lưu settings/runtime có SHA-256 trước khi ghi. Không dùng ngầm DLL cũ ở `bin\Release` khi cài từ repo. Uninstaller chỉ gỡ đăng ký trong target và **giữ runtime chung**. `pack.ps1` kèm helper và từ chối Add-in DLL cũ hơn source.
- Build RC.1 chuyển cả ba project sang `net481` khi truyền `-p:EplanTargetFramework=net481` cho API EPLAN 2026.0.3; đồng bộ assembly dependencies SharpCompress để hết cảnh báo xung đột. Gói RC.1 buộc chọn output `net481`.

### Verification / chưa kiểm thử
- Script + Add-in compile-check C# 5 PASS với API EPLAN 2026.0.3. Script runtime đã sao lưu/deploy; GUI UAT Education 2026.0.3 đạt ba nút, một Data Portal trong tab Tool, Portal native kéo/đóng/mở lại, Menu bar báo giới hạn và hai form mở/đóng không ghi dữ liệu. Backup 60 file/hash và kết quả chi tiết trong `docs/AGENT-HANDOFF-2026-11-07.md`.
- Target discovery/selection fixture PASS, list thật chỉ Education 2026.0.3; installer từ chối ghi khi EPLAN đang mở, hash runtime không đổi. **Chưa chạy installer/uninstaller headless thực tế**, chưa deploy/UAT Add-in mới, chưa UAT phiên bản EPLAN khác, sáu nút Cloud khác hay nhập EDZ vào DB test. RC này chỉ dành cho thử nghiệm, không gắn nhãn bản ổn định.
- Add-in/shared DLL build `net481` với API 2026.0.3 PASS (0 warning/error), parser đọc EDZ ZIP giả lập PASS; 45 file trong gói RC.1 so hash SHA-256 với stage đều khớp. Add-in và bytes Script RC vẫn chưa GUI UAT.

## [0.6.0] - 2026-11-06 — “Bỏ WebView2, dùng Data Portal native của EPLAN”

### Added
- Wrapper native cho nút **Data Portal** ở cả hai edition: tìm `XDataPortalNavigatorToggle` bằng `ActionManager.FindAction` rồi `Execute` — dùng đúng phiên EPLAN đã đăng nhập.
- Fallback rõ ràng khi action native không khả dụng: hộp thoại hướng dẫn mở **Eplan Cloud → Data Portal**, ghi chi tiết vào `addin.log`.
- `EnsurePortalCommand` tự dọn command ribbon cũ (`Data Portal Web`, command bind trực tiếp native) khi nâng cấp.

### Changed
- Ribbon còn đúng bốn lệnh: `Nhập EDZ | Xuất EDZ | Data Portal | Menu bar`.
- Script Edition lên `0.6.0-script.1`; Add-in lên `0.6.0`.
- `%APPDATA%\Tool Oumijia\Portal` tiếp tục là shared runtime nhưng chỉ còn `Oumijia.EplanUi`, `Oumijia.EdzReader`, SharpCompress và dependency .NET.
- Installer Add-in/Script/Tool tự lọc và xóa DLL PortalHost/WebView2 còn sót.
- Installer Script/Tool chuyển script 0.5.0 `Scripts\ToolOumijia.cs` vào backups; register script loại mục cũ khỏi danh sách tự nạp để EPLAN chỉ chạy một bản Tool Oumijia.
- `pack.ps1` mặc định `0.6.0`, bỏ yêu cầu WebView2Loader và yêu cầu `docs/releases/v0.6.0.md`.

### Removed
- Toàn bộ `src/Oumijia.PortalHost` (WebView2 Data Portal) khỏi source và gói phát hành.
- Nút **Data Portal Web**, code precheck WebView2/mạng, đọc file tải về và tự mở hộp Nhập.
- Profile WebView2 của Tool và log `data-portal-downloads-*.csv`.
- Yêu cầu WebView2 Runtime trong installer/docs.

### Verification
- Script C# 5 compile-check PASS với EPLAN 2026.0.3 Bin (`csc /langversion:5`, EXIT=0).
- Add-in compile-check PASS với EPLAN 2026.0.3 Bin + shared runtime (`csc`, DLL 19.968 bytes).
- Chưa UAT runtime nút Data Portal native sau khi restart EPLAN; wrapper đã xử lý trường hợp action không tồn tại.

## [0.5.3] - 2026-11-06 — “Data Portal native-first, Menu bar rõ ràng, ribbon gọn”

### Added
- Nút **Data Portal** (native) trỏ vào `XDataPortalNavigatorToggle` khi EPLAN có action này (đã kiểm chứng trên EPLAN Education 2026.0.3: Data Portal native tab Eplan Cloud mở được, đã đăng nhập, tìm được 2.445.594 parts); nút **Data Portal Web** giữ WebView2 `dataportal.eplan.com` để tải EDZ về thư mục Tool.
- Script/Add-in dò ribbon khi tạo (`Ribbon probe` trong `addin.log`) cho `XDataPortalNavigatorToggle` và `MfToggleMainMenuAction`; có thì bind trực tiếp, không có thì fallback có thông báo.
- PortalHost: log `portal.log`, timeout 30 giây phát hiện kẹt `Loading...`, nút **Đăng nhập EPLAN ID** (`login.eplan.com`) và **Thử lại** trong fallback, giữ SSO (`*.eplan.com`, `*.microsoftonline.com`, `*.live.com`) cùng cửa sổ.

### Changed
- Nhóm ribbon đổi tên **“Quản lý EDZ” → “Nhập EDZ”** (theo yêu cầu người dùng), bỏ dấu `...` ở nhãn nút (Nhập EDZ, Xuất EDZ, Data Portal, Data Portal Web, Menu bar); tự dọn nhóm cũ khi nâng cấp.
- Script Edition lên `0.5.3-script.1`; Add-in thêm action `OumijiaToggleMainMenu` (ordinal 26) và cập nhật `OumijiaRegister.cs`.

### Fixed
- **Data Portal Web kẹt `Loading...`**: đã rõ nguyên nhân chính là WebView2 dùng profile riêng chưa đăng nhập EPLAN ID (khác phiên native đã đăng nhập KL), cộng popup SSO có thể bị ẩn sau cửa sổ modal. Nay có timeout + fallback + log để chẩn đoán thay vì treo không rõ.
- **Menu bar chết lặng trên EPLAN 2026**: đã kiểm chứng `MfToggleMainMenuAction` không còn chuỗi trong Bin 2026.0.3; nút nay báo rõ + hướng dẫn dùng ribbon EPLAN thay vì im lặng.

### Verification
- Script C# 5 compile-check PASS với EPLAN 2026.0.3 Bin (`csc /langversion:5`, EXIT=0).
- Chưa build lại DLL Add-in/PortalHost trên máy này (không có .NET SDK; máy chạy Script Edition do không có API license) — cần build/pack trên máy có SDK + EPLAN 2022/2026 rồi UAT trước phát hành.

## [0.5.2] - 2026-11-06 — “Data Portal và Nhập EDZ mới”

### Added
- Nút **Data Portal…** trong ribbon Tool Oumijia cho Add-in và Script Edition.
- Cửa sổ Data Portal chính thức bằng WebView2; người dùng tự đăng nhập, không lưu credential.
- Chọn và ghi nhớ thư mục tải; có thể chuyển các `.edz` tải xong sang hộp Nhập EDZ.
- Fallback mở Data Portal trong trình duyệt khi WebView2 lỗi/thiếu.
- Reader cục bộ cho `.edz`: hiển thị tên thiết bị, hãng và mã hàng trong dialog Nhập.
- Giao diện Nhập EDZ hai cột; lưới part chọn dòng; file lỗi vẫn nằm trong danh sách nhập.

### Changed
- Ribbon có đúng 4 lệnh: Nhập EDZ, Xuất EDZ, Data Portal, Menu bar.
- `Oumijia.EplanUi` dùng chung cho Add-in và Script Edition để cả hai editions chạy đúng cùng UI.
- `Oumijia.EdzReader` dùng chung để đọc metadata EDZ; package bổ sung SharpCompress với giấy phép mã nguồn mở.
- Installer cài cả component dùng chung và dọn chúng khi gỡ cài.

### Verification
- Build Add-in với EPLAN Platform 2022.0.3; C# 5 compile-check Script.
- EDZ reader unit smoke test trên các package tổng hợp cũ: tên/hãng/mã hàng đọc thành công.
- Chưa UAT thực tế Data Portal hoặc nhập tệp tải về trong EPLAN; chưa tạo tag/GitHub Release.


## [0.5.1] - 2026-11-05 — "Native tick chọn part"

### Fixed
- **Ô tick trong dialog Xuất EDZ không hiện dấu ✓** trên các máy người dùng.
  Tick tự vẽ bị TreeView Windows bỏ qua/cập nhật không ổn định; cây giờ dùng
  **checkbox gốc của TreeView** (`CheckBoxes = true`), luôn hiển thị rõ.
- Chọn/bỏ chọn theo nhóm vẫn áp dụng cho cả nhánh; dòng đã chọn in đậm.
- Áp dụng cho cả add-in và Script Edition.

## [0.5.0] - 2026-11-04 — "Script Edition (cho máy không có API license)"

### Added
- **Script Edition** (`script\ToolOumijiaScript.cs`): bản port đầy đủ của add-in sang EPLAN Scripting
  (C# 5) — chạy **KHÔNG cần EPLAN API license**, không cần ký assembly:
  - Đủ 2 dialog (nhập EDZ + xuất EDZ với cây nhóm, panel chi tiết, loại bản ghi)
  - Ribbon tạo ở `Eplan.EplApi.OnMainStart` (+ `OnLoadWorkspace`/`OnResetRibbon` trên bản mới)
  - Actions riêng: `OumijiaScriptImportEdz` / `OumijiaScriptExportEdz` / `OumijiaScriptSelfTest`
- **Đăng ký script tự nạp** (`OumijiaRegisterScript.cs`): tự ghi `STATION.EplanEplApiScriptGui.Scripts`
  (đã kiểm chứng persist + loader tự nạp; action `RegisterScript` của EPLAN 2022 không ghi gì — bẫy đã tránh);
  gỡ đăng ký bằng cùng cơ chế + file cờ
- **Installer thông minh** (`Install-Tool-Oumijia.bat`): thử bản add-in → self-test headless →
  nếu bị EPLAN vô hiệu hóa (thiếu license) → tự gỡ add-in + chuyển cài Script Edition + kiểm chứng;
  gỡ cả hai bản bằng `Uninstall-Tool-Oumijia.bat`
- Gói release chứa **CẢ HAI bản**: `addin\` + `script\` + installer thông minh + README chọn bản

### Fixed
- **Nhập EDZ lỗi `S030060: Invalid scheme`**: tự dò field-mapping scheme
  (`USER.PartsManagementGui.FieldMapping.LastUsed` → fallback `Standardvorgabe EPLAN` →
  tên đọc từ chính thông báo lỗi). Đã kiểm chứng: re-import 1 part OK — áp dụng cho cả 2 bản
- **Đích xuất "Một file EDZ"**: chỉ chọn THƯ MỤC; tên file **tự động theo mã sản phẩm** (1 part chọn)
  hoặc `parts.edz` (nhiều part); dòng xem trước "→ sẽ tạo: …" cập nhật realtime; hỏi **ghi đè**
  khi file tồn tại; **nhớ thư mục** cho lần sau — áp dụng cho cả 2 bản

### Notes
- Script Edition cần EPLAN có reference `Eplan.EplApi.MasterData` cho script compiler — có trên
  các bản EPLAN mới (đã kiểm chứng trên **2026.0.3**); EPLAN cũ (vd 2022) dùng bản add-in
  (chạy tốt không cần license). Installer thông minh tự chọn bản phù hợp.
- Debug lỗi compile script: xem `C:\Users\Public\EPLAN\Electric P8\EplLog.eldb` (mã S04600x + chi tiết CSxxxx).

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

[Unreleased]: https://github.com/wuwenhou/Tool-Oumijia-EPLAN/compare/v0.6.0...HEAD
[0.6.0]: https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/tag/v0.6.0
[0.5.3]: https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/tag/v0.5.3
[0.5.2]: https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/tag/v0.5.2
[0.5.1]: https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/tag/v0.5.1
[0.5.0]: https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/tag/v0.5.0
[0.4.0]: https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/tag/v0.4.0
[0.3.0]: https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/tag/v0.3.0
[0.2.0]: https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/tag/v0.2.0
[0.1.0]: https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/tag/v0.1.0
