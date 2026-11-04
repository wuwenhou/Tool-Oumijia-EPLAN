# AGENTS.md — hướng dẫn cho AI agent / lập trình viên tiếp tục dự án

> File này dành cho **người kế nhiệm**: bất kỳ AI agent hoặc lập trình viên nào tiếp tục
> phát triển *Tool Oumijia — EPLAN Edition*. Đọc hết file này trước khi viết code.

---

## 1. Dự án là gì

Công cụ Windows hỗ trợ kỹ sư điện làm việc với **gói dữ liệu linh kiện EPLAN (`.edz`)**:

1. **App standalone**: quét file `.edz` → phân loại tự động (Hãng/Nhóm) → xếp vào thư viện `Import/<Hãng>/<Nhóm>/`
2. **Add-in EPLAN** (ribbon "Tool Oumijia"): **Nhập EDZ…** (batch vào Parts Database) và **Xuất EDZ…** (parts DB → EDZ) — cả hai qua API chính thức của EPLAN

Bối cảnh gốc: người dùng có EPLAN bản Education — tài khoản Data Portal không tải được EDZ trực tiếp
(điều khoản service agreement), nên quy trình làm việc là: *import part qua Portal trong EPLAN →
backup/phân loại bằng tool này (EDZ từ website hãng hoặc export từ Parts DB)*.

**Quyết định phạm vi quan trọng (v0.2)**: tool **chỉ xử lý `.edz`** — EMA/EMP/EMS đã bị gỡ khỏi UI
theo yêu cầu người dùng. Core vẫn còn `Macros/MacroInstaller` (không expose) — có thể bỏ hẳn hoặc
tái dùng sau, **đừng xóa vội** khi chưa hỏi.

**Kế hoạch tổng thể** (các giai đoạn xa hơn: editor EDZ, QElectroTech, AutoCAD) nằm ở các file
plan bên ngoài repo; trong repo này xem `docs/ROADMAP.md`.

## 2. Trạng thái hiện tại (v0.2.0)

✅ **Chạy được & đã kiểm chứng:**
- `Oumijia.Core` — đọc EDZ (ZIP + 7z), trích metadata, classifier, library writer (25+ tests pass)
- `Oumijia.Tool` — app WinForms 3 tab (Xuất / Nhập / Cài đặt), EDZ-only, build `net8.0-windows` + `net48`
- `src/Oumijia.EplanAddIn` — **add-in thật, verified end-to-end trên EPLAN Platform 2022.0.3**:
  - `OnInit` → `OnInitGui` (tạo ribbon "Tool Oumijia") → action gọi được từ command line → `OnExit` sạch với `/Auto`
  - Import: `MDPartsManagement.ImportParts(files, "IXPartsImportExportEdz", mode, bExtraLang, identifier, null)`
  - Export: `MDPartsDatabase.ExportParts(path, MDPartsDatabase.DataFormat.EDZ, IEnumerable<MDPart>)`
- Đăng ký add-in: **headless registration script** (xem mục 6) — đã kiểm chứng thực tế
- `tools/install-addin/` — installer 1 click (`Install-AddIn.bat` / `Uninstall-AddIn.bat`)

⚠️ **Chưa có / cần làm:**
- UAT thật với người dùng (import trên database test, xác nhận kết quả Parts Management)
- `Oumijia.Register.exe` — console utility: `status` hoạt động; `install` chỉ mang tính **thí nghiệm**
  (writes offline không persist — đừng trông cậy; đường chuẩn là script)
- Tab "Parts Management Extension" (nhúng UI vào Parts Management — API không chính thức, xem mục 8)

## 3. Bản đồ repo

```
src/Oumijia.Core/                 netstandard2.0 (BẮT BUỘC giữ — add-in net48 tham chiếu)
  Edz/           EdzArchive (ZIP/7z qua SharpCompress), EdzPackageReader, PartXmlParser
  Classification/Classifier + ClassifierRules (25+ nhóm, alias hãng)
  Library/       LibraryWriter (policy, SHA-256), CsvLog, OverrideStore
  Macros/        MacroPathDetector + MacroInstaller  (hiện KHÔNG dùng trong UI)
  Config/        OumijiaConfig + AppPaths (%APPDATA%\Tool Oumijia)
src/Oumijia.Tool/                 WinForms, multi-target net8.0-windows + net48
  Program.cs     entry + ShowMainWindow()
  Ui/            ExportPanel, ImportPanel (EDZ queue), SettingsPanel
src/Oumijia.EplanAddIn/           net48, x64 — output: Oumijia.EplAddin.EDZ.dll
  AddIn.cs       IEplAddIn: OnRegister/OnUnregister/OnInit/OnInitGui/OnExit
  Actions/       OpenTool, ImportEdz, ExportEdz, SelfTest (+ IEplAction.OnRegister(ref string, ref int))
  Ui/            ImportEdzForm, ExportEdzForm (WinForms dựng bằng code)
  AddinLog.cs / AddinCsvLog.cs / ToolLocator.cs
src/Oumijia.Register/             console: install/uninstall/status/selftest (experimental — xem mục 6)
tools/eplan-scripts/OumijiaRegister.cs   ⭐ registration script — trái tim của cơ chế đăng ký
tools/install-addin/              Install/Uninstall ps1+bat, README.txt (bản copy khi pack)
tools/pack.ps1 / make-samples.ps1
tests/Oumijia.Core.Tests/         25 tests; examples/ = EDZ tổng hợp (KHÔNG commit dữ liệu hãng!)
```

## 4. Lệnh thường dùng

```powershell
# Build + test app chính
dotnet build Tool-Oumijia-EPLAN.sln -c Release
dotnet test  Tool-Oumijia-EPLAN.sln -c Release

# Build add-in (cần EPLAN cài máy; tự dò 2022.0.3, hoặc truyền EplanApiDir / EPLAN_API_DIR)
dotnet build src/Oumijia.EplanAddIn/Oumijia.EplanAddIn.csproj -c Release `
  -p:EplanApiDir="C:\Program Files\EPLAN\Platform\2022.0.3\Bin"

# Đóng gói release (kèm addin\ nếu add-in đã build)
powershell -ExecutionPolicy Bypass -File tools/pack.ps1 -Version X.Y.Z

# Tạo lại file mẫu
powershell -ExecutionPolicy Bypass -File tools/make-samples.ps1
```

## 5. Kiểm thử add-in trên máy (recipes đã kiểm chứng)

**Đăng ký / cập nhật registration** (script idempotent — chạy lại bất cứ lúc nào):

```powershell
# Deploy script + DLL rồi chạy EPLAN ẩn (tự khởi động → chạy script → tự thoát):
Copy-Item tools\eplan-scripts\OumijiaRegister.cs "C:\Users\Public\EPLAN\Data\Scripts\Company name\" -Force
Copy-Item src\Oumijia.EplanAddIn\bin\Release\net48\Oumijia.EplAddin.EDZ.dll "$env:APPDATA\Tool Oumijia\AddIn\" -Force
& "C:\Program Files\EPLAN\Electric P8\2022.0.3\Bin\W3u.exe" /Auto /Quiet /NoSplash /NoLicenseDialog `
  /NoUserRightsDialog /NoLoadWorkspace ExecuteScript `
  /ScriptFile:"C:\Users\Public\EPLAN\Data\Scripts\Company name\OumijiaRegister.cs"
# Thành công => %APPDATA%\Tool Oumijia\logs\register-script.log có "OK: registered ..."
```

**Kiểm tra add-in load + action gọi được** (headless):

```powershell
& "...\W3u.exe" /Auto /Quiet /NoSplash /NoLicenseDialog /NoUserRightsDialog /NoLoadWorkspace OumijiaSelfTest
# Kỳ vọng %APPDATA%\Tool Oumijia\logs\addin.log: OnInit → OnInitGui: ribbon ready → SELFTEST OK → OnExit
```

**Gỡ đăng ký**: tạo file cờ `%APPDATA%\Tool Oumijia\unregister.flag` rồi chạy lại lệnh ExecuteScript
(script tự phát hiện cờ → xóa registration → xóa cờ).

**Compile-check script nhanh** (bắt lỗi trước khi chạy EPLAN — rất hữu ích):

```powershell
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:library `
  /out:"$env:TEMP\test.dll" /reference:"...\Bin\Eplan.EplApi.Baseu.dll" `
  /reference:"...\Bin\Eplan.EplApi.AFu.dll" /reference:"...\Bin\Eplan.EplApi.Guiu.dll" `
  /reference:"...\Bin\Eplan.EplApi.MasterDatau.dll" /reference:"System.Windows.Forms.dll" `
  "tools\eplan-scripts\OumijiaRegister.cs"
```

## 6. ⭐ Cơ chế đăng ký add-in (toàn bộ trí thức đã kiểm chứng)

Đây là phần tốn nhiều thời gian nhất để tìm ra — **đọc kỹ trước khi sửa**.

### Bức tranh tổng thể

EPLAN nạp add-in từ **settings store** (không phải registry, không phải file config trong Program Files).
Store: `C:\Users\Public\EPLAN\Settings\<Variant>\<Version>\x64\Cfg\<STATION|USER>.Basic.<user>.xdb\Setting.eod`.
Ghi đúng keys → EPLAN load DLL lúc khởi động (shadow-copy vào `%APPDATA%\EPLAN\ShadowCopyAssemblies\<pid>\`).

### Hai nhóm keys BẮT BUỘC

**① Module (đăng ký add-in):**

```
STATION.EplanEplApiModule.Module.Oumijia@EplAddin@EDZ.AssemblyName  = "Oumijia.EplAddin.EDZ, Version=..., Culture=neutral, PublicKeyToken=null"
STATION.EplanEplApiModule.Module.Oumijia@EplAddin@EDZ.AssemblyPath  = C:\Users\...\Tool Oumijia\AddIn\Oumijia.EplAddin.EDZ.dll
STATION.EplanEplApiModule.Module.Oumijia@EplAddin@EDZ.TypeName      = Oumijia.EplanAddIn.AddIn
STATION.EplanEplApiModule.Module.Oumijia@EplAddin@EDZ.LoadOnStart   = true
```

**② AsmActions (đăng ký từng action class)** — thiếu phần này thì action **không gọi được**
(kể cả khi module load + ribbon hiện):

```
STATION.EplanEplApiModule.Services.AsmActions.<ActionName>.<Ordinal>.AssemblyName
                                                       ... .AssemblyPath
                                                       ... .TypeName       (class của action)
                                                       ... .ModuleName     (tên module, vd "Oumijia.EplAddin.EDZ")
                                                       ... .EnableInterface  (bool)
                                                       ... .CheckInterface   (bool)
                                                       ... .EnableCondition  (string)
                                                       ... .ActionLicense    (bool)
```

### Quy tắc & bẫy

| # | Sự thật đã xác minh |
|---|---|
| R1 | Dấu `.` trong **tên module** khi làm key phải escape thành `@`: `Oumijia.EplAddin.EDZ` → `Oumijia@EplAddin@EDZ`. (Không escape ở phần `Module.` prefix.) |
| R2 | **Offline writes không bao giờ persist**: `new Settings()` + `SetStringSetting` từ process ngoài chạy KHÔNG lỗi nhưng không ghi xuống `Setting.eod`. Đừng phí thời gian — dùng ExecuteScript. |
| R3 | Cách chuẩn để ghi: **script chạy TRONG EPLAN** qua action có sẵn `ExecuteScript` + tham số `/ScriptFile:"..."`. Script có `[Start]` tự chạy khi load. Scripting miễn phí (không cần license API). |
| R4 | Khi EPLAN đang mở: `Setting.eod` bị khóa — đọc bằng `[IO.File]::Open(path, Open, Read, ReadWrite)` (FileShare), không dùng `ReadAllBytes`. Ghi chỉ khi EPLAN đã đóng (ExecuteScript tự lo việc này — EPLAN instance chạy script chính là instance ghi store). |
| R5 | Registration script phải **compile được** — EPLAN biên dịch script lúc chạy. Lỗi compile rất khó thấy từ bên ngoài → luôn csc-check trước khi deploy (recipe mục 5). |
| R6 | `IEplAction.OnRegister` signature thật: `bool OnRegister(ref string Name, ref int Ordinal)` — **không có** `ref bool`. `IEplAddIn` = `OnRegister(ref bool)` / `OnUnregister` / `OnInit` / `OnInitGui` / `OnExit`. |
| R7 | `GetActionProperties(ref ActionProperties)` — class này không expose member public nào trong 2022; để trống. |
| R8 | `/Auto` không ngăn add-in load: EPLAN vẫn chạy `OnInit`/`OnInitGui`, gọi action command-line, rồi thoát sạch. Dùng cho CI/self-test. |
| R9 | EPLAN instance đang mở khóa UI sẽ nhận registration mới ở **lần khởi động sau** (ribbon không hiện giữa chừng). |
| R10 | Nếu mọi thứ có vẻ đúng mà add-in không load: kiểm tra `addin.log` có dòng mới không, đối chiếu block keys với entry tham chiếu `VEE@ExportConnections` trong `Setting.eod`. |

### Tham chiếu: entry của EPLAN/VEE trong store (để đối chiếu khi debug)

EPLAN's own entries: `EplanEplApiModule.Module.Eplan@EplCoreAddin@<Name>.*` (113 modules).
Add-in mẫu của bên thứ ba: `VEE@ExportConnections` (VE_E_ConnectionExport.AddInModule) — mirror đúng
các key của nó là cách chắc nhất để thêm module mới.

## 7. Quy ước kỹ thuật

- **Core phải giữ `netstandard2.0`** (để add-in `net48` dùng được). Tránh API mới hơn:
  `string.Contains(char)`, `TryAdd`, `Range/Index`… (đã có tiền lệ lỗi — xem git history).
- Add-in: **net48, x64, ZERO dependency ngoài EPLAN API + BCL** (EPLAN load từ shadow-copy —
  mọi dependency thừa là rủi ro load). Không thêm PackageReference vào project add-in!
- UI chạy **cả net8.0-windows lẫn net48** — không dùng API WinForms mới hơn net48.
- **PowerShell scripts trong `tools/` phải là UTF-8 CÓ BOM** (PowerShell 5.1 đọc không BOM theo ANSI
  → vỡ tiếng Việt). Sau khi sửa script, re-apply BOM:
  ```powershell
  $p="tools\make-samples.ps1"; $t=[IO.File]::ReadAllText($p,[Text.Encoding]::UTF8)
  [IO.File]::WriteAllText($p,$t,(New-Object Text.UTF8Encoding($true)))
  ```
- UI strings: **tiếng Việt**. Docs: VI + EN overview. Commit: **Conventional Commits**. Version: SemVer.
- Log của add-in: `%APPDATA%\Tool Oumijia\logs\addin.log` (mọi thứ) + CSV audit (`import-*.csv`, `export-*.csv`).

## 8. Ranh giới — KHÔNG làm

1. ❌ **Không** phá/bẻ license, không vượt cơ chế tải EDZ của EPLAN Data Portal, không giả mạo
   token/session. Chỉ dùng **API chính thức** + file người dùng tự có hợp lệ.
2. ❌ **Không** commit dữ liệu hãng thật (EDZ/EMA/ảnh/macro) vào repo — chỉ dùng file tổng hợp
   từ `tools/make-samples.ps1`.
3. ❌ **Không** tái tạo/copy hình vẽ, geometry `.ema` (PXF độc quyền) — chỉ xử lý **dữ liệu factual**
   (mã hàng, tên, nhóm, thông số) và coi `.ema` như file đính kèm đục (nếu quay lại hỗ trợ).
4. ❌ Không tự ý đổi namespace/thương hiệu (giữ "Tool Oumijia", `Oumijia.*`).
5. ⚠️ "Parts Management Extension" (nhúng tab vào Parts Management bằng `MDPartsManagement.RegisterAddin/
   RegisterItem/RegisterTabsheet`) — API **không chính thức**, WPF-based; chỉ làm khi người dùng yêu cầu
   rõ và chấp nhận rủi ro tương thích.

## 9. Việc tiếp theo (ưu tiên gợi ý)

**P1 — UAT & hoàn thiện v0.2**
- Người dùng test **Nhập EDZ… / Xuất EDZ…** trên database test; thu phản hồi (chế độ nhập, ngôn ngữ phụ…).
- Kiểm tra kết quả import thực tế trong Parts Management, đối chiếu CSV log.

**P2 — chất lượng & usability**
- Progress bar trong 2 form (import lô lớn có thể chạy lâu) — cẩn thận UI thread (EPLAN API phải chạy trên UI thread của EPLAN).
- Rule eCl@ss thực tế cho classifier (cần dữ liệu kiểm chứng).
- Localization EN cho UI; icon ribbon; high-DPI polish.
- Test cho `MacroInstaller` (nếu tái dùng core).

**P3 — mở rộng (xem docs/ROADMAP.md)**
- EDZ writer/editor (tạo gói từ CSV; gắn macro vào `P_ARTICLE_GROUPSYMBOLMACRO`/`P_ARTICLE_MACRO`).
- Export nâng cao từ Parts DB (filter scheme, chỉ thuộc tính chọn lọc — `ExportParts` có nhiều overload).
- Các bridge chuyển đổi: QElectroTech `.elmt`, DXF/AutoCAD (từng giai đoạn, đúng ranh giới mục 8).

## 10. Phát hành

Xem `docs/RELEASING.md`. Tóm tắt: bump version trong 3 csproj + CHANGELOG → build add-in →
`tools/pack.ps1 -Version X.Y.Z` (kèm `addin/`) → `git tag vX.Y.Z` → push →
`gh release create vX.Y.Z <zip> --notes-file docs/releases/vX.Y.Z.md` → dán SHA-256 của ZIP vào notes.

## 11. Tham chiếu kỹ thuật chính

| Chủ đề | Nguồn |
|---|---|
| EPLAN API (add-in, actions, parts) | https://www.eplan.help/en-us/Infoportal/Content/api/2022/AddIns.html |
| Import/Export parts qua API | `MDPartsManagement.ImportParts` / `MDPartsDatabase.ExportParts` (eplan.help) |
| EDZ converter | `IXPartsImportExportEdz` (eplan.help → API → XML Converters → XPamExport) |
| Định dạng EDZ (bối cảnh, importer QET) | https://github.com/qelectrotech/qelectrotech-source-mirror/tree/master/sources/import/edz |
| Tool tham chiếu (editor EDZ tiếng Trung) | https://github.com/zetajing/EplanPartBuilder |
| Data Portal API (endpoint, PAT) | https://dataportal.eplan.com/api/doc |
| SharpCompress (ZIP/7z) | https://github.com/adamhathcock/sharpcompress |
