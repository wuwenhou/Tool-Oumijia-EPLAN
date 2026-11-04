# AGENTS.md — hướng dẫn cho AI agent / lập trình viên tiếp tục dự án

> File này dành cho **người kế nhiệm**: bất kỳ AI agent hoặc lập trình viên nào tiếp tục
> phát triển *Tool Oumijia — EPLAN Edition*. Đọc hết file này trước khi viết code.

---

## 1. Dự án là gì

**Một add-in EPLAN duy nhất** (ribbon "Tool Oumijia") làm việc với **gói dữ liệu linh kiện EPLAN (`.edz`)**:

1. **Nhập EDZ…** — batch import nhiều gói `.edz` vào Parts Database (API chính thức:
   `MDPartsManagement.ImportParts`, converter `IXPartsImportExportEdz`)
2. **Xuất EDZ…** — chọn part từ **cây nhóm kiểu navigator** (checkbox + tìm kiếm) rồi export
   ra 1 file hoặc mỗi part 1 file (`MDPartsDatabase.ExportParts`, `DataFormat.EDZ`)

Bối cảnh gốc: người dùng có EPLAN bản Education — tài khoản Data Portal không tải được EDZ trực tiếp
(điều khoản service agreement), nên quy trình là: *import part qua Portal trong EPLAN → backup/chia sẻ
bằng add-in này*.

**Quyết định phạm vi quan trọng:**
- **v0.3.0: bỏ hoàn toàn app standalone (exe)** theo yêu cầu người dùng — repo chỉ còn add-in.
  (Lịch sử: v0.1.0–v0.2.0 có app WinForms + Core library — đã xóa, xem git history nếu cần.)
- Tool **chỉ xử lý `.edz`** — EMA/EMP/EMS không còn trong phạm vi.
- Kế hoạch xa hơn (EDZ writer, QElectroTech, AutoCAD) — xem `docs/ROADMAP.md`.

## 2. Trạng thái hiện tại (v0.3.0)

✅ **Chạy được & đã kiểm chứng trên EPLAN Platform 2022.0.3:**
- Add-in nạp ổn định (`OnInit` → `OnInitGui` ribbon 2 nút → `OnExit` sạch với `/Auto`)
- Đăng ký headless 1 click (`tools/install-addin/` + `tools/eplan-scripts/OumijiaRegister.cs`)
- Nhập EDZ: dialog + pipeline API (đã UAT gián tiếp qua smoke test)
- Xuất EDZ: dialog cây nhóm đầy đủ + smoke-export headless (`OumijiaDiagParts`: 647 part → 25 part EDZ OK)
- Đọc dữ liệu part nhanh: 647 part trong **~80 ms** (enum + đọc property)

⚠️ **Cần làm:**
- UAT trực quan với người dùng: mở dialog Xuất, tick chọn, export thật, import lại kiểm tra
- Icon cho 2 nút ribbon; chọn ngôn ngữ hiển thị tên part (hiện hardcode `en_US`)

## 3. Bản đồ repo

```
src/Oumijia.EplanAddIn/           net48, x64 — ZERO dependency ngoài EPLAN API + BCL
  AddIn.cs                        IEplAddIn: OnRegister/OnUnregister/OnInit/OnInitGui/OnExit
  Actions/
    ImportEdzAction.cs            "OumijiaImportEdz"  (21) — mở ImportEdzForm
    ExportEdzAction.cs            "OumijiaExportEdz"  (22) — mở ExportEdzForm
    SelfTestAction.cs             "OumijiaSelfTest"   (23) — ghi SELFTEST OK vào addin.log
    DiagPartsAction.cs            "OumijiaDiagParts"  (24) — chẩn đoán headless DB + smoke export
  Ui/
    ImportEdzForm.cs              chọn file/thư mục + chế độ nhập + log
    ExportEdzForm.cs              dialog xuất v2: cây nhóm + tick + tìm kiếm + xuất
    PartsTreeModel.cs             PartEntry/PartInfo (đọc chuỗi hiển thị) + PartsTreeModel (dựng cây)
  AddinLog.cs / AddinCsvLog.cs    log %APPDATA%\Tool Oumijia\logs\
tools/
  eplan-scripts/
    OumijiaRegister.cs            ⭐ script đăng ký (chạy TRONG EPLAN qua ExecuteScript)
    OumijiaReadReg.cs             chẩn đoán: đọc lại giá trị registration thực tế
    OumijiaRibbonDiag.cs          chẩn đoán: liệt kê ribbon tabs + commands
  install-addin/                  Install/Uninstall ps1+bat + README (nguồn cho gói zip)
  pack.ps1                        đóng gói zip add-in-only
docs/                             PLAN-v0.3.0, BUILD, INSTALL, USER-GUIDE-vi, ARCHITECTURE, ROADMAP, RELEASING
```

## 4. Lệnh thường dùng

```powershell
# Build add-in (cần máy có EPLAN; tự dò 2022.0.3 hoặc truyền EplanApiDir / EPLAN_API_DIR)
dotnet build src/Oumijia.EplanAddIn/Oumijia.EplanAddIn.csproj -c Release `
  -p:EplanApiDir="C:\Program Files\EPLAN\Platform\2022.0.3\Bin"

# Kiểm tra compile script đăng ký TRƯỚC khi deploy (rất quan trọng — xem R5)
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$bin = "C:\Program Files\EPLAN\Platform\2022.0.3\Bin"
& $csc /nologo /target:library /out:"$env:TEMP\check.dll" `
  /reference:"$bin\Eplan.EplApi.Baseu.dll" /reference:"$bin\Eplan.EplApi.AFu.dll" `
  /reference:"$bin\Eplan.EplApi.Guiu.dll" /reference:"$bin\Eplan.EplApi.MasterDatau.dll" `
  /reference:"System.Windows.Forms.dll" /reference:"System.Drawing.dll" `
  "tools\eplan-scripts\OumijiaRegister.cs"

# Đóng gói release (yêu cầu add-in đã build)
powershell -ExecutionPolicy Bypass -File tools/pack.ps1 -Version 0.3.0
```

## 5. Kiểm thử headless trên máy có EPLAN (recipes đã kiểm chứng)

```powershell
$w3u = "C:\Program Files\EPLAN\Electric P8\2022.0.3\Bin\W3u.exe"
$sdir = "C:\Users\Public\EPLAN\Data\Scripts\Company name"   # thư mục script của EPLAN

# 1) Deploy script + DLL
Copy-Item tools\eplan-scripts\OumijiaRegister.cs "$sdir\OumijiaRegister.cs" -Force
New-Item -ItemType Directory -Path "$env:APPDATA\Tool Oumijia\AddIn" -Force | Out-Null
Copy-Item src\Oumijia.EplanAddIn\bin\Release\net48\Oumijia.EplAddin.EDZ.dll "$env:APPDATA\Tool Oumijia\AddIn\" -Force

# 2) Đăng ký (EPLAN tự mở ẩn → chạy script → tự thoát; chờ log register-script.log có "OK: registered")
& $w3u /Auto /Quiet /NoSplash /NoLicenseDialog /NoUserRightsDialog /NoLoadWorkspace ExecuteScript "/ScriptFile:$sdir\OumijiaRegister.cs"

# 3) Kiểm tra add-in nạp + action gọi được
& $w3u /Auto /Quiet /NoSplash /NoLicenseDialog /NoUserRightsDialog /NoLoadWorkspace OumijiaSelfTest
#    → %APPDATA%\Tool Oumijia\logs\addin.log: "OnInit (version 0.3.0.0)" + "SELFTEST OK"

# 4) Chẩn đoán dữ liệu part (mở DB, đếm, smoke export 25 part ra %TEMP%\OumijiaDiag)
& $w3u /Auto /Quiet /NoSplash /NoLicenseDialog /NoUserRightsDialog /NoLoadWorkspace OumijiaDiagParts

# 5) Đọc lại registration thực tế (nếu nghi bị "đè" — xem R11)
& $w3u /Auto /Quiet /NoSplash /NoLicenseDialog /NoUserRightsDialog /NoLoadWorkspace ExecuteScript "/ScriptFile:$sdir\OumijiaReadReg.cs"
#    → %APPDATA%\Tool Oumijia\logs\readreg.log
```

**Gỡ đăng ký**: tạo file cờ `%APPDATA%\Tool Oumijia\unregister.flag` rồi chạy lại lệnh ExecuteScript
(script tự phát hiện cờ → xóa registration → xóa cờ).

## 6. ⭐ Cơ chế đăng ký add-in (toàn bộ tri thức đã kiểm chứng)

Phần tốn nhiều thời gian nhất để tìm ra — **đọc kỹ trước khi sửa**.

### Bức tranh tổng thể

EPLAN nạp add-in từ **settings store**: `C:\Users\Public\EPLAN\Settings\<Variant>\<Version>\x64\Cfg\<STATION|USER>.Basic.<user>.xdb\Setting.eod`.
Ghi đúng keys → EPLAN load DLL lúc khởi động, **shadow-copy vào `%APPDATA%\EPLAN\ShadowCopyAssemblies\<pid>\<asm-name>\bin\`**
(lưu ý: EPLAN copy **cả folder chứa DLL**, không chỉ DLL).

### Hai nhóm keys BẮT BUỘC

**① Module:**

```
STATION.EplanEplApiModule.Module.Oumijia@EplAddin@EDZ.AssemblyName  = "Oumijia.EplAddin.EDZ, Version=..., Culture=neutral, PublicKeyToken=null"
STATION.EplanEplApiModule.Module.Oumijia@EplAddin@EDZ.AssemblyPath  = %APPDATA%\Tool Oumijia\AddIn\Oumijia.EplAddin.EDZ.dll
STATION.EplanEplApiModule.Module.Oumijia@EplAddin@EDZ.TypeName      = Oumijia.EplanAddIn.AddIn
STATION.EplanEplApiModule.Module.Oumijia@EplAddin@EDZ.LoadOnStart   = true
```

**② AsmActions (mỗi action một nhóm 8 key):**

```
STATION.EplanEplApiModule.Services.AsmActions.<ActionName>.<Ordinal>.{AssemblyName,AssemblyPath,TypeName,ModuleName,EnableInterface,CheckInterface,EnableCondition,ActionLicense}
```

### Quy tắc & bẫy (R = rule, đã trả giá)

| # | Sự thật đã xác minh |
|---|---|
| R1 | `.` trong tên module escape thành `@`: `Oumijia.EplAddin.EDZ` → `Oumijia@EplAddin@EDZ`. |
| R2 | **Offline writes không persist** — `new Settings()` từ process ngoài chỉ giữ trong bộ nhớ. Luôn dùng ExecuteScript. |
| R3 | Cách chuẩn để ghi: **script chạy TRONG EPLAN** qua action `ExecuteScript` + `/ScriptFile:"..."`. Script có `[Start]` tự chạy khi load. Scripting miễn phí. |
| R4 | Khi EPLAN đang mở: `Setting.eod` bị khóa — đọc bằng `[IO.File]::Open(path, Open, Read, ReadWrite)`; giá trị mới có thể bị **nén** trong file (không đọc raw ASCII được) → dùng `OumijiaReadReg.cs` để hỏi EPLAN. |
| R5 | Script phải **compile được** — luôn csc-check trước khi deploy (recipe mục 4). Lỗi compile rất khó thấy từ ngoài. |
| R6 | `IEplAction.OnRegister(ref string Name, ref int Ordinal)`; `IEplAddIn` = `OnRegister(ref bool)/OnUnregister/OnInit/OnInitGui/OnExit`. |
| R7 | `GetActionProperties(ref ActionProperties)` — không có member public dùng được trong 2022; để trống. |
| R8 | `/Auto` vẫn nạp add-in đầy đủ (OnInit/OnInitGui) rồi gọi action command-line và thoát — dùng cho test. |
| R9 | EPLAN instance đang mở không nhận ribbon mới giữa chừng — cần khởi động lại. |
| R10 | Debug khi add-in "có vẻ đúng mà không load": so `addin.log`, chạy `OumijiaRibbonDiag.cs` + `OumijiaReadReg.cs`, đối chiếu với entry tham chiếu `VEE@ExportConnections` trong `Setting.eod`. |
| **R11** | ⭐ **Settings giữ NHIỀU GIÁ TRỊ LỊCH SỬ cho cùng key.** Nếu key đã được ghi bởi nguồn khác (vd dialog "API add-ins" của EPLAN với path khác), `SetStringSetting` KHÔNG đổi giá trị mà loader đọc — add-in vẫn nạp bản cũ từ path cũ dù script "OK". **FIX: luôn `DeleteSetting` key trước rồi mới Add+Set (delete-first)** — script hiện tại đã làm vậy + `VERIFY` đọc lại xác nhận. |
| **R12** | ⭐ **Đọc chuỗi hiển thị property part**: dùng `MDPropertyValue.GetDisplayString().GetStringToDisplay(ISOCode.Language.L_en_US)` — hoạt động cho cả 3 kiểu: enum nhóm ("26" → "PLC"), đa ngôn ngữ ("de_DE@…;en_US@LOGO! 24RC;" → "LOGO! 24RC"), chuỗi thường ("SIE"). `ToString()` trần trả mã số/chuỗi trộn; `ToString(Language)` trả số với thuộc tính enum. Code chuẩn: `Ui/PartsTreeModel.cs` (PartInfo.ReadDisplayText). |
| **R13** | Property ID: `Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_*` (field type nested, truyền thẳng vào `part.Properties[...]`). Enum nhóm: `MDPartsDatabaseItem.Enums.ProductTopGroup/ProductGroup/ProductSubGroup` (1=Electric, 2=Fluid, 3=Mechanic, 4=Process). Part number cũng có `part.PartNr`; `AdoId` (int) ổn định để key theo part. |
| **R14** | Đăng ký thủ công qua dialog "API add-ins" của EPLAN (nếu người dùng từng làm) sẽ trỏ vào path người dùng chọn (vd bản giải nén zip) và **đè** lên registration của installer — xử lý bằng R11 (delete-first), không cần đi tìm key. |

## 7. Quy ước kỹ thuật

- Add-in: **net48, x64, ZERO dependency ngoài EPLAN API + BCL** (EPLAN load từ shadow-copy —
  mọi dependency thừa là rủi ro). **Không thêm PackageReference** vào project add-in.
- **PowerShell scripts trong `tools/` phải là UTF-8 CÓ BOM** (PowerShell 5.1 đọc không BOM theo ANSI
  → vỡ tiếng Việt). Sau khi sửa script, re-apply BOM:
  ```powershell
  $p="tools\pack.ps1"; $t=[IO.File]::ReadAllText($p,[Text.Encoding]::UTF8)
  [IO.File]::WriteAllText($p,$t,(New-Object Text.UTF8Encoding($true)))
  ```
- UI strings: **tiếng Việt**. Docs: VI + EN overview. Commit: **Conventional Commits**. Version: SemVer.
- Add-in chạy trên **UI thread của EPLAN** — không dùng thread con cho EPLAN API.
- Log: `%APPDATA%\Tool Oumijia\logs\` (`addin.log`, `register-script.log`, `readreg.log`, `import-*.csv`, `export-*.csv`).

## 8. Ranh giới — KHÔNG làm

1. ❌ **Không** phá/bẻ license, không vượt cơ chế tải EDZ của EPLAN Data Portal, không giả mạo
   token/session. Chỉ dùng **API chính thức** + file người dùng tự có hợp lệ.
2. ❌ **Không** commit dữ liệu hãng thật (EDZ/EMA/ảnh/macro) vào repo.
3. ❌ **Không** tái tạo/copy hình vẽ, geometry `.ema` (PXF độc quyền) — chỉ xử lý **dữ liệu factual**.
4. ❌ Không tự ý đổi namespace/thương hiệu (giữ "Tool Oumijia", `Oumijia.*`).
5. ⚠️ "Parts Management Extension" (nhúng tab vào Parts Management) — API **không chính thức**,
   chỉ làm khi người dùng yêu cầu rõ và chấp nhận rủi ro tương thích.

## 9. Việc tiếp theo (ưu tiên gợi ý)

**P1 — UAT v0.3.0** với người dùng: mở Cây part, tick chọn (từng part + cả nhóm), tìm kiếm,
xuất 1 file + mỗi part 1 file, import lại database test kiểm tra; thu phản hồi.

**P2 — chất lượng & usability:**
- Icon cho 2 nút ribbon (SVG qua `AddCommand(..., svgIcon)` — cần thử nghiệm semantics).
- Chọn ngôn ngữ hiển thị tên part (hiện hardcode en_US trong `PartInfo.ReadDisplayText`).
- Thanh tiến trình cho import/export lô lớn (nhớ: EPLAN API trên UI thread).
- Sắp xếp/tô đậm phần trùng khi tìm kiếm trong cây.

**P3 — mở rộng (xem docs/ROADMAP.md):**
- EDZ writer/editor; export nâng cao (filter scheme); nghiên cứu Parts Management Extension.

## 10. Phát hành

Xem `docs/RELEASING.md`. Tóm tắt: bump version csproj + CHANGELOG → build add-in →
`tools/pack.ps1 -Version X.Y.Z` (zip add-in-only) → `git tag vX.Y.Z` → push →
`gh release create vX.Y.Z <zip> --notes-file docs/releases/vX.Y.Z.md` → dán SHA-256 vào notes.

## 11. Tham chiếu kỹ thuật chính

| Chủ đề | Nguồn |
|---|---|
| EPLAN API (add-in, actions) | https://www.eplan.help/en-us/Infoportal/Content/api/2022/AddIns.html |
| Import/Export parts | `MDPartsManagement.ImportParts` / `MDPartsDatabase.ExportParts` (eplan.help) |
| EDZ converter | `IXPartsImportExportEdz` (eplan.help → API → XML Converters → XPamExport) |
| Định dạng EDZ (bối cảnh, importer QET) | https://github.com/qelectrotech/qelectrotech-source-mirror/tree/master/sources/import/edz |
| Tool tham chiếu (editor EDZ tiếng Trung) | https://github.com/zetajing/EplanPartBuilder |
| Data Portal API (endpoint, PAT) | https://dataportal.eplan.com/api/doc |
