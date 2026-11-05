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

## 2. Trạng thái hiện tại (v0.6.2)

✅ **Chạy được & đã kiểm chứng:**
- **Bản ADD-IN** (cho EPLAN ≤2023 / máy có API license): verified trên EPLAN Platform 2022.0.3 —
  nạp, ribbon, import/export, self-test, diag đầy đủ (bao gồm import smoke test với scheme thật)
- **Bản SCRIPT** (không cần API license — cho EPLAN mới chặn add-in chưa ký): người dùng đã kiểm chứng
  trên EPLAN 2026.0.3 (đọc 552 part, xuất EDZ OK); cơ chế đăng ký + loader tự nạp đã kiểm chứng trên 2022 (R18)
- Fix **nhập EDZ** (field-mapping scheme) — `IMPORT SMOKE OK (scheme='Standardvorgabe EPLAN')`
- Fix **đích xuất** (chỉ chọn thư mục + tên tự động theo mã sản phẩm + xem trước + ghi đè + nhớ thư mục)
- **Native checkbox** cho cây chọn part (v0.5.1) — tick hiển thị ổn định trên Windows TreeView
- **v0.6.2**: Tool quay về tối giản (`Nhập EDZ…` · `Xuất EDZ…` · `Menu bar`);
  **2 add-in VIET EPLAN** (`VEE.EplTableMaker`, `VEE.ExportConnections`) được phép phân phối và
  được smart installer tự copy/đăng ký/gỡ.
- **Installer thông minh** tự chọn bản phù hợp + tự cài 2 add-in VEE + gỡ cài 1 lệnh

⚠️ **Cần làm:**
- UAT v0.6.2: máy EPLAN 2026.0.3 (script + VIET EPLAN ELECTRIC tab) và máy 2022 (add-in + 2 VEE add-ins)
- Icon ribbon; tiến trình lô lớn; EDZ writer

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
    ExportEdzForm.cs              dialog xuất: cây nhóm + tick native + tìm kiếm + xuất
    PartsTreeModel.cs             PartEntry/PartInfo (đọc chuỗi hiển thị) + PartsTreeModel (dựng cây)
  AddinLog.cs / AddinCsvLog.cs    log %APPDATA%\Tool Oumijia\logs\
tools/
  eplan-scripts/
    OumijiaRegister.cs            ⭐ script đăng ký ADD-IN (chạy TRONG EPLAN qua ExecuteScript)
    OumijiaReadReg.cs             chẩn đoán: đọc lại giá trị registration thực tế
    OumijiaRibbonDiag.cs          chẩn đoán: liệt kê ribbon tabs + commands
  script-edition/                 ⭐ bản SCRIPT (C#5): ToolOumijiaScript.cs + OumijiaRegisterScript.cs
    probe/                        script chẩn đoán (load test / đọc settings / dọn danh sách)
  install-addin/                  installer bản add-in (nguồn cho gói zip)
  install-all/                    installer THÔNG MINH (tự chọn bản) — entry point của zip
  third-party/                    ⭐ 2 add-in VEE (tác giả cho phép) + OumijiaRegisterThirdParty.cs
  pack.ps1                        đóng gói zip 2 bản + third-party
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
| **R15** | ⭐ **Xuất theo loại bản ghi**: `MDPartsManagement.ExportParts` 15 tham số = (file, converter `"IXPartsImportExportEdz"`, filterScheme `""`) + 6 cặp `{bool, MDObjectFilter}` theo thứ tự: **Parts / Addresses (Customer+Manufacturer/Supplier) / Constructions (= Drilling pattern + `items/contour`) / Terminals (= Connection point pattern) / AccessoryLists / AccessoryPlacements**; filter `null` chấp nhận được. Mapping ĐÃ xác minh bằng giải nén EDZ (EDZ thực chất là **7z container** — `tar`/bsdtar đọc được) đối chiếu số liệu DB. Cảnh báo: chế độ này xuất TOÀN BỘ theo loại (toàn bộ parts của DB demo ≈ 290 MB, accessory lists ≈ 110 MB). |
| **R16** | ⭐ **Bẫy construct WinForms**: `SplitContainer.Panel2MinSize` / `SplitterDistance` ném `InvalidOperationException` nếu đặt khi control chưa có kích thước thật (trong ctor) → luôn đặt trong `OnShown` + try/catch. Tổng quát: **kiểm tra construct form offscreen** (DiagParts đã làm) để bắt lỗi khởi tạo UI trước khi UAT — cách này đã bắt được 1 lỗi crash mở dialog thật. |
| **R17** | ⭐ **Script Edition & license**: EPLAN mới (2024+/2026) **vô hiệu hóa DLL add-in chưa ký** khi máy không có API license ("The assembly … is deactivated") — không phải lỗi code. Bản SCRIPT (EPLAN Scripting) **không cần license**. Compiler script của EPLAN reference **theo phiên bản**: 2022.0.3 thiếu `Eplan.EplApi.MasterData` (script dùng MasterData không compile được → dùng add-in); 2026.0.3 có (đã kiểm chứng bởi người dùng). Coverage chuẩn: EPLAN cũ → add-in; EPLAN mới + no license → script. |
| **R18** | ⭐ **Đăng ký "script tự nạp"**: action `RegisterScript` của EPLAN 2022 chạy không lỗi nhưng **KHÔNG ghi gì** (bẫy!). Cách đã kiểm chứng: script chạy trong EPLAN tự ghi danh sách indexed `STATION.EplanEplApiScriptGui.Scripts` (`SetStringSetting(path, value, index)`; gỡ = đọc lại toàn bộ + `RemoveAllIndexedSetting` + ghi lại các entry giữ). EPLAN **tự thử nạp** các script trong danh sách ở mỗi lần khởi động (xác nhận qua log). Script phải compile được bằng **compiler của EPLAN** (khác csc ngoài!) — lỗi chi tiết nằm trong **`C:\Users\Public\EPLAN\Electric P8\EplLog.eldb`** (mã S046006/S046007 + chi tiết CSxxxx). Attributes `[DeclareRegister]`/`[DeclareUnregister]`/`[DeclareEventHandler]` đều có trên 2022. |

| **R19** | ⭐ **Third-party add-ins are shipped only with the author's permission.** VIET EPLAN & ELECTRIC DLLs are distributed unmodified with THIRD-PARTY-NOTICES + hashes; never alter/recompile/patch them or claim ownership. Registration is done by our `OumijiaRegisterThirdParty.cs`; uninstall must remove their module/action settings too. |
| **R20** | ⭐ **Command mapping must be verified against EPLAN configuration** (e.g. `MfToggleMainMenuAction`, GIRD actions, or `regPlatform.xml` action lists) and checked with `ActionManager`/headless e2e tests. Do not add buttons for undocumented or guessed IDs. |
| **R21** | ⭐ **Đồng bộ hai editions:** mọi thay đổi UI/logic chung phải áp dụng cho cả add-in C# 7+ và Script C#5. Khi sửa giao diện/shared actions, giữ `tools/script-edition/ToolOumijiaScript.cs` tương ứng và luôn chạy csc C#5 sau cùng. |

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

**P1 — UAT v0.6.2 với người dùng:**
- Máy **EPLAN 2026.0.3** (không API license): chạy `Install-Tool-Oumijia.bat` → kiểm tra:
  ribbon Tool gồm 3 nút (Nhập/Xuất/Menu bar), Việt EPLAN tab có VEE actions, nhập/xuất EDZ OK.
- Máy **2022**: installer chọn bản add-in; kiểm tra hai add-in VEE được đăng ký và gọi được;
  Menu bar bật/tắt từ nút; nhập/xuất EDZ OK.

**P2 — chất lượng & usability:**
- Icon chuyên nghiệp cho các nút ribbon; thêm dòng/tooltip và kiểm tra DPI ở nhiều mức;
- bổ sung trạng thái/đăng ký cho 2 add-in VEE (ví dụ nút kiểm tra hoặc bài viết hướng dẫn).

**P3 — mở rộng (xem docs/ROADMAP.md):**
- EDZ writer/editor; nghiên cứu tab "Parts Management Extension".

## 10. Phát hành

Xem `docs/RELEASING.md`. Tóm tắt: bump version csproj + CHANGELOG → build add-in + csc C#5 script →
`tools/pack.ps1 -Version X.Y.Z` (zip 2 editions + third-party theo license) → `git tag vX.Y.Z` → push →
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
