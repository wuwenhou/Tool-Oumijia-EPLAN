# AGENTS.md — hướng dẫn cho AI agent / lập trình viên tiếp tục dự án

> File này dành cho **người kế nhiệm**: bất kỳ AI agent hoặc lập trình viên nào tiếp tục
> phát triển *Tool Oumijia — EPLAN Edition*. Đọc hết file này trước khi viết code.

---

## 1. Dự án là gì

Công cụ Windows hỗ trợ kỹ sư điện làm việc với **gói dữ liệu linh kiện EPLAN (`.edz`)** và
**macro EPLAN (`.ema/.emp/.ems`)**:

1. **Export**: quét file → phân loại tự động (Hãng/Nhóm) → xếp vào thư viện chuẩn `Import/<Hãng>/<Nhóm>/`
2. **Import**: cài macro vào master data của EPLAN; (v0.2) batch import EDZ vào Parts Database qua API chính thức

Bối cảnh gốc: người dùng có EPLAN bản Education — tài khoản Data Portal không tải được EDZ trực tiếp
(điều khoản service agreement), nên quy trình làm việc là: *import part qua Portal trong EPLAN →
backup/phân loại bằng tool này (EDZ từ website hãng hoặc export từ Parts DB)*.

**Kế hoạch tổng thể** (các giai đoạn xa hơn: editor EDZ, QElectroTech, AutoCAD) nằm ở các file
plan bên ngoài repo (thư mục plan của người dùng); trong repo này xem `docs/ROADMAP.md`.

## 2. Trạng thái hiện tại (v0.1.0)

✅ **Chạy được & đã test:**
- `Oumijia.Core` — đọc EDZ (ZIP + 7z), trích metadata, classifier, library writer, macro installer
- `Oumijia.Tool` — app WinForms 3 tab (Xuất / Nhập / Cài đặt), build cả `net8.0-windows` + `net48`
- 25+ tests pass (`dotnet test`), gồm test đọc 7z thật (dùng 7-Zip cài máy) và integration với `examples/`

⚠️ **Scaffold — cần máy có EPLAN để hoàn thiện:**
- `src/Oumijia.EplanAddIn` — khung `IEplAddIn` + action `OumijiaOpenTool`. **Chưa** có batch import.
  Khi build cần `-p:EplanApiDir="...\EPLAN\...\Bin"`. Kiểm tra chữ ký interface theo đúng bản EPLAN
  của bạn (EPLAN Help > API > Add-ins).

## 3. Bản đồ repo

```
src/Oumijia.Core/
  Edz/           EdzArchive (ZIP/7z qua SharpCompress), EdzPackageReader, PartXmlParser (tolerant)
  Classification/Classifier + ClassifierRules (default rules & alias hãng)
  Library/       LibraryWriter (conflict policy, SHA-256), CsvLog, OverrideStore
  Macros/        MacroPathDetector + MacroInstaller
  Config/        OumijiaConfig + AppPaths (%APPDATA%\Tool Oumijia)
src/Oumijia.Tool/
  Program.cs     entry + ShowMainWindow() (dùng được từ add-in, tự spawn STA thread)
  App.cs         state tĩnh (Config/Classifier)
  Ui/            ExportPanel, ImportPanel, SettingsPanel (WinForms dựng bằng code, không designer)
src/Oumijia.EplanAddIn/
  AddIn.cs, OpenToolAction.cs   (net48, refs EPLAN qua EplanApiDir)
tests/Oumijia.Core.Tests/       fixtures + 4 nhóm test
examples/                       EDZ mẫu tổng hợp (KHÔNG commit dữ liệu hãng thật!)
tools/                          make-samples.ps1, pack.ps1
```

## 4. Lệnh thường dùng

```powershell
dotnet build Tool-Oumijia-EPLAN.sln -c Release
dotnet test  Tool-Oumijia-EPLAN.sln -c Release
dotnet test  tests/Oumijia.Core.Tests -c Release --filter "FullyQualifiedName~Classifier"

# tạo lại file mẫu + chạy thử
powershell -ExecutionPolicy Bypass -File tools/make-samples.ps1
powershell -ExecutionPolicy Bypass -File tools/pack.ps1 -Version 0.1.0

# build add-in (cần EPLAN cài máy)
dotnet build src/Oumijia.EplanAddIn/Oumijia.EplanAddIn.csproj -c Release -p:EplanApiDir="C:\Program Files\EPLAN\Platform\2024.0\Bin"
```

## 5. Quy ước kỹ thuật

- **Core phải giữ `netstandard2.0`** (để add-in `net48` dùng được). Tránh API chỉ có ở
  netstandard2.1+/net6+: `string.Contains(char)`, `TryAdd`, `Range/Index`, file-scoped APIs mới…
  (đã có tiền lệ lỗi — xem git history).
- UI chạy **cả net8.0-windows lẫn net48** — không dùng API WinForms mới hơn net48.
  Dùng `#if NET8_0_OR_GREATER` khi buộc phải khác nhau (xem `Program.cs`).
- **PowerShell scripts trong `tools/` phải là UTF-8 CÓ BOM** (PowerShell 5.1 đọc file không BOM
  theo ANSI → vỡ tiếng Việt). Sau khi sửa script, chạy:
  ```powershell
  $p="tools\make-samples.ps1"; $t=[IO.File]::ReadAllText($p,[Text.Encoding]::UTF8)
  [IO.File]::WriteAllText($p,$t,(New-Object Text.UTF8Encoding($true)))
  ```
- UI strings: **tiếng Việt** (đối tượng người dùng chính). Docs: VI + EN overview.
- Commit style: **Conventional Commits** (`feat:`, `fix:`, `docs:`, `chore:`…). Version: SemVer.

## 6. Ranh giới — KHÔNG làm

1. ❌ **Không** phá/bẻ license, không vượt cơ chế tải EDZ của EPLAN Data Portal, không giả mạo
   token/session. Chỉ dùng **API chính thức** + file người dùng tự có hợp lệ.
2. ❌ **Không** commit dữ liệu hãng thật (EDZ/EMA/ảnh/macro) vào repo — chỉ dùng file tổng hợp
   từ `tools/make-samples.ps1`.
3. ❌ **Không** tái tạo/copy hình vẽ, geometry `.ema` (PXF độc quyền) — chỉ xử lý **dữ liệu factual**
   (mã hàng, tên, nhóm, thông số) và coi `.ema` như file đính kèm đục.
4. ❌ Không tự ý đổi namespace/thương hiệu (giữ "Tool Oumijia", `Oumijia.*`).

## 7. Việc tiếp theo (ưu tiên gợi ý)

**P1 — v0.2: batch import EDZ vào EPLAN (giá trị lớn nhất)**
- Trong `Oumijia.EplanAddIn`: implement action `OumijiaImportEdz` + ribbon/toolbar button.
- Nghiên cứu trước trên máy có EPLAN: `Eplan.EplApi.MasterData` (MDPartsDatabase import),
  interface `IXPartsImportExportEdz` (XPamExport) — dùng Parts Management Import/Export làm chuẩn.
- Luồng: chọn nhiều EDZ → chọn chế độ (chỉ thêm mới / cập nhật / thay thế) → chạy trên
  **database test** trước → log từng gói. UI dùng lại `ImportPanel` (hiện là hướng dẫn thủ công).
- **Cẩn thận**: chạy import phải trên UI thread của EPLAN; đọc khuyến nghị trong EPLAN Help.

**P2 — chất lượng dữ liệu & usability**
- Rule eCl@ss thực tế cho classifier (hiện đang để trống có chủ đích — cần dữ liệu kiểm chứng).
- Test cho `MacroInstaller` (hiện chưa có; chỉ mock thư mục tạm).
- Localization EN cho UI (tách resource). High-DPI polish. Icon ứng dụng.
- Xử lý EDZ nhiều part (chọn part chính để phân loại; cảnh báo trong UI).

**P3 — mở rộng (xem docs/ROADMAP.md)**
- EDZ writer/editor (tạo gói từ CSV; gắn macro vào `P_ARTICLE_GROUPSYMBOLMACRO`/`P_ARTICLE_MACRO`).
- Export từ Parts DB ra EDZ (backup thư viện).
- Các bridge chuyển đổi: QElectroTech `.elmt`, DXF/AutoCAD (từng giai đoạn, đúng ranh giới mục 6).

## 8. Phát hành

Xem `docs/RELEASING.md`. Tóm tắt: bump version trong 3 csproj + CHANGELOG → chạy `tools/pack.ps1`
→ `git tag vX.Y.Z` → `gh release create vX.Y.Z <zip> --title "..." --notes-file ...`
→ dán SHA-256 của ZIP vào release notes.

## 9. Tham chiếu kỹ thuật chính

| Chủ đề | Nguồn |
|---|---|
| EPLAN API (add-in, actions, parts) | https://www.eplan.help/en-us/Infoportal/Content/api/2025/EplanApiDotNet.html |
| Import/Export DXF/DWG qua API | `Eplan.EplApi.HEServices.Import/Export` (eplan.help API) |
| Định dạng EDZ (bối cảnh, importer QET) | https://github.com/qelectrotech/qelectrotech-source-mirror/tree/master/sources/import/edz |
| Tool tham chiếu (editor EDZ tiếng Trung) | https://github.com/zetajing/EplanPartBuilder |
| Data Portal API (endpoint, PAT) | https://dataportal.eplan.com/api/doc |
| SharpCompress (ZIP/7z) | https://github.com/adamhathcock/sharpcompress |
