# AGENTS.md — hướng dẫn cho AI agent / lập trình viên tiếp tục dự án

> File này dành cho **người kế nhiệm**: bất kỳ AI agent hoặc lập trình viên nào tiếp tục
> phát triển *Tool Oumijia — EPLAN Edition*. Đọc hết file này trước khi viết code.

---

## 1. Dự án là gì

**Tool Oumijia (EPLAN Edition)** gồm hai edition cùng giao diện:

1. **Nhập EDZ…** — preview EDZ cục bộ rồi batch import nhiều gói `.edz` vào Parts Database (API chính thức:
   `MDPartsManagement.ImportParts`, converter `IXPartsImportExportEdz`)
2. **Xuất EDZ…** — chọn part từ cây nhóm navigator (checkbox native + tìm kiếm) rồi export
   ra 1 file hoặc mỗi part 1 file (`MDPartsDatabase.ExportParts`, `DataFormat.EDZ`)
3. **Data Portal** — yêu cầu EPLAN mở **Data Portal native** (tab Eplan Cloud);
   phiên đăng nhập do EPLAN quản lý, không có WebView2 của Tool. Xem R19 về lỗi v0.6.0.

Bối cảnh gốc: người dùng có EPLAN bản Education — tài khoản Data Portal không tải được EDZ trực tiếp
(điều khoản service agreement), nên quy trình là: *import part qua Portal trong EPLAN → backup/chia sẻ
bằng Tool*. v0.6.0 không bypass license/điều khoản; thao tác được phép phải do EPLAN cấp cho tài khoản.

**Quyết định phạm vi quan trọng:**
- **v0.3.0: bỏ hoàn toàn app standalone (exe)** theo yêu cầu người dùng — repo chỉ còn Tool EPLAN.
- Tool và nội dung factual; EMA/EMP/EMS không nằm trong phạm vi.
- Kế hoạch xa hơn (EDZ writer, QElectroTech, AutoCAD) — xem `docs/ROADMAP.md`.

## 2. Trạng thái hiện tại (v0.6.0-rc.1 pre-release candidate; không phải bản ổn định)

> **Đọc `docs/AGENT-HANDOFF-2026-11-07.md`** trước khi deploy/cài đặt: có kế hoạch, lịch sử, backup/hash, GUI UAT và rollback mới. ZIP v0.6.0 cũ không chứa bản sửa nhóm EDZ ngày 07-11. RC.1 chỉ dùng thử trên Education 2026.0.3; Add-in/installer headless và các bản EPLAN khác chưa UAT.

- v0.6.0 bỏ hoàn toàn PortalHost/WebView2; UAT phát hiện nút **Data Portal** gọi
  `XDataPortalNavigatorToggle` nhưng action này không được đăng ký, nên nút báo lỗi.
- **Nhập EDZ** có UI hai cột đồng bộ với Xuất EDZ và lưới **Tên | Hãng | Mã hàng**.
- Nhập EDZ dùng cây gói→part và bảng Chi tiết. Bản vá Script Edition đã deploy để UAT
  trên Education 2026.0.3: Data Portal và 7 nút Cloud sao chép command native của EPLAN
  bằng ID tìm theo tên lúc chạy; không mở riêng `DataPortalTabView`. Add-in source đã sửa
  và compile-check, **chưa deploy Add-in** (máy không có API license; thiếu .NET SDK để build package).
- `Oumijia.EplanUi` (UI dùng chung) và `Oumijia.EdzReader` (local EDZ parser) là dependency chủ đích;
  installer copy chúng vào shared runtime `%APPDATA%\Tool Oumijia\Portal`.
- Script Edition pre-release `0.6.0-rc.1-script.1`; Add-in pre-release `0.6.0-rc.1`. GUI UAT trước khi đổi chuỗi version RC; chỉ thay metadata, chưa UAT lại đúng bytes RC.
- Script C# 5 compile-check thành công; Add-in compile-check thành công với EPLAN 2026.0.3.
- Source ngày 07-11 đã chuyển nhóm chính thành **Quản lý EDZ**, đúng ba nút Nhập → Xuất → Menu bar;
  Data Portal chỉ còn ở nhóm **Eplan Cloud** của Tool, tab Eplan Cloud gốc giữ nguyên.
  Script mới đã deploy trên Education 2026.0.3 và GUI UAT: đóng X, kéo cửa sổ, mở lại Portal native;
  Nhập/Xuất mở/đóng (không nhập/xuất dữ liệu), Menu bar báo giới hạn. Chưa test sáu nút Cloud còn lại.
- Smart installer/uninstaller source có `-ListInstalled`/`-TargetW3u`, discovery fixture PASS;
  chỉ Education 2026.0.3 Script candidate có payload được chọn. **Chưa UAT cài/gỡ headless**;
  Add-in vẫn compile-check, chưa build/deploy mới. Installer riêng từng edition còn chọn W3u đầu tiên.

✅ **Đã có trên source:**
- Nhập EDZ preview EDZ cục bộ; lỗi file không block nhập.
- Xuất EDZ giữ nguyên tree checkbox native và loại bản ghi.
- Nút Data Portal bản vá dùng chính command native; action tương thích cũ chỉ báo hướng dẫn,
  không còn mở nội dung riêng lẻ với `GfDialogManagerShow /DialogName:DataPortalTabView`.
- ZIP/local build có đủ dependencies; installer thông minh giữ Add-in→Script fallback.
- Installer/pack tự dọn DLL PortalHost/WebView2 còn sót.

## 3. Bản đồ repo

```
src/Oumijia.EplanAddIn/           ribbon + actions Add-in
  AddIn.cs                        IEplAddIn: EDZ Nhập/Xuất/Menu bar + Cloud native
  Actions/
    ImportEdzAction.cs            "OumijiaImportEdz"  (21)
    ExportEdzAction.cs            "OumijiaExportEdz"  (22)
    SelfTestAction.cs             "OumijiaSelfTest"   (23)
    DiagPartsAction.cs            "OumijiaDiagParts"  (24)
    DataPortalAction.cs           "OumijiaDataPortal"  (25) — wrapper native
    ToggleMainMenuAction.cs       "OumijiaToggleMainMenu" (26)
src/Oumijia.EplanUi/              net48/x64 — UI dùng chung
  Ui/
    ImportEdzForm.cs              preview EDZ + nhập batch
    ExportEdzForm.cs              dialog xuất v2
    PartsTreeModel.cs             PartEntry/PartInfo + PartsTreeModel
    ImportPartsSupport.cs         tự dò field-mapping scheme
src/Oumijia.EdzReader/            net48/x64 — đọc cục bộ metadata EDZ
tools/
  eplan-scripts/
    OumijiaRegister.cs            script đăng ký Add-in/ordinal 21–26
    OumijiaReadReg.cs             chẩn đoán read-back
  script-edition/                 Script Edition C#5; shared UI nạp qua reflection
  install-addin/                  installer Add-in (copy dependencies)
  install-all/                    installer thông minh
    EplanTargets.ps1              discovery/selection theo Variant/Version/W3u
    Test-EplanTargets.ps1         fixture không chạy EPLAN thật
  pack.ps1                        đóng gói ZIP 2 editions + component
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

# Đóng gói release (yêu cầu add-in đã build + docs\releases\vX.Y.Z.md)
powershell -ExecutionPolicy Bypass -File tools/pack.ps1 -Version 0.6.0
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
| **R12** | ⭐ **Đọc chuỗi hiển thị property part**: dùng `MDPropertyValue.GetDisplayString().GetStringToDisplay(ISOCode.Language.L_en_US)` — hoạt động cho cả 3 kiểu: enum nhóm ("26" → "PLC"), đa ngôn ngữ ("de_DE@…;en_US@LOGO! 24RC;" → "LOGO! 24RC"), chuỗi thường ("SIE"). `ToString()` trần trả mã số/chuỗi trộn; `ToString(Language)` trả số với thuộc tính enum. Code chuẩn: `src/Oumijia.EplanUi/Ui/PartsTreeModel.cs` (PartInfo.ReadDisplayText). |
| **R13** | Property ID: `Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_*` (field type nested, truyền thẳng vào `part.Properties[...]`). Enum nhóm: `MDPartsDatabaseItem.Enums.ProductTopGroup/ProductGroup/ProductSubGroup` (1=Electric, 2=Fluid, 3=Mechanic, 4=Process). Part number cũng có `part.PartNr`; `AdoId` (int) ổn định để key theo part. |
| **R14** | Đăng ký thủ công qua dialog "API add-ins" của EPLAN (nếu người dùng từng làm) sẽ trỏ vào path người dùng chọn (vd bản giải nén zip) và **đè** lên registration của installer — xử lý bằng R11 (delete-first), không cần đi tìm key. |
| **R15** | ⭐ **Xuất theo loại bản ghi**: `MDPartsManagement.ExportParts` 15 tham số = (file, converter `"IXPartsImportExportEdz"`, filterScheme `""`) + 6 cặp `{bool, MDObjectFilter}` theo thứ tự: **Parts / Addresses (Customer+Manufacturer/Supplier) / Constructions (= Drilling pattern + `items/contour`) / Terminals (= Connection point pattern) / AccessoryLists / AccessoryPlacements**; filter `null` chấp nhận được. Mapping ĐÃ xác minh bằng giải nén EDZ (EDZ thực chất là **7z container** — `tar`/bsdtar đọc được) đối chiếu số liệu DB. Cảnh báo: chế độ này xuất TOÀN BỘ theo loại (toàn bộ parts của DB demo ≈ 290 MB, accessory lists ≈ 110 MB). |
| **R16** | ⭐ **Bẫy construct WinForms**: `SplitContainer.Panel2MinSize` / `SplitterDistance` ném `InvalidOperationException` nếu đặt khi control chưa có kích thước thật (trong ctor) → luôn đặt trong `OnShown` + try/catch. Tổng quát: **kiểm tra construct form offscreen** (DiagParts đã làm) để bắt lỗi khởi tạo UI trước khi UAT — cách này đã bắt được 1 lỗi crash mở dialog thật. |
| **R17** | ⭐ **Script Edition & license**: EPLAN mới (2024+/2026) **vô hiệu hóa DLL add-in chưa ký** khi máy không có API license ("The assembly … is deactivated") — không phải lỗi code. Bản SCRIPT (EPLAN Scripting) **không cần license**. Compiler script của EPLAN reference **theo phiên bản**: 2022.0.3 thiếu `Eplan.EplApi.MasterData` (script dùng MasterData không compile được → dùng add-in); 2026.0.3 có (đã kiểm chứng bởi người dùng). Coverage chuẩn: EPLAN cũ → add-in; EPLAN mới + no license → script. |
| **R18** | ⭐ **Đăng ký "script tự nạp"**: action `RegisterScript` của EPLAN 2022 chạy không lỗi nhưng **KHÔNG ghi gì** (bẫy!). Cách đã kiểm chứng: script chạy trong EPLAN tự ghi danh sách indexed `STATION.EplanEplApiScriptGui.Scripts` (`SetStringSetting(path, value, index)`; gỡ = đọc lại toàn bộ + `RemoveAllIndexedSetting` + ghi lại các entry giữ). EPLAN **tự thử nạp** các script trong danh sách ở mỗi lần khởi động (xác nhận qua log). Script phải compile được bằng **compiler của EPLAN** (khác csc ngoài!) — lỗi chi tiết nằm trong **`C:\Users\Public\EPLAN\Electric P8\EplLog.eldb`** (mã S046006/S046007 + chi tiết CSxxxx). Attributes `[DeclareRegister]`/`[DeclareUnregister]`/`[DeclareEventHandler]` đều có trên 2022. |
| **R19** | ⭐ **Các opener sai đã loại bỏ**: `XDataPortalNavigatorToggle` chỉ là chuỗi hằng, không được đăng ký làm action; `XDataPortalNavigate` và các action `XDataPortalNavigatorInit`/`TriggerInit` không phải opener. `GfDialogManagerShow /DialogName:DataPortalTabView` trả `execute=True` nhưng chỉ mở phần nội dung, thiếu cửa sổ dockable/closable. Gọi trực tiếp `XMVCCloudSolutionAction /Url:https://dataportal.eplan.com/` mở cửa sổ **Eplan Cloud** chung, không phải Data Portal native. Không khôi phục hai đường này. |
| **R20** | ⭐ **Bản vá Cloud UAT 2026-11-06**: `DataPortalTabView` chỉ là nội dung, không phải container dockable. Nút gốc Eplan Cloud chạy `MfEPulseCommandExecuteAction → XMVCCloudSolutionAction → ECloudSolutionsViewBarN → GfDialogManagerToggle`. `RibbonCommandGroup.AddCommandWithId(id)` clone được đúng hành vi native; tìm ID theo tên command trên tab `Eplan Cloud` tại runtime, không hard-code `38901`. Eplan Cloud có thể báo 0 command trong `OnMainStart`; retry qua `System.Windows.Forms.Timer` trên UI thread, lần UAT này sẵn sàng sau 1 lần. Xem `docs/CLOUD_NATIVE_UAT.md` và log UAT; không dùng URL hay token. |

## 7. Quy ước kỹ thuật

- Projects dùng net48/x64. Bundled dependencies (`Oumijia.EplanUi`, `Oumijia.EdzReader`,
  SharpCompress + System.*) là chủ đích; installer phải copy chúng vào shared runtime.
- Script Edition không compile-time reference SharpCompress; nó nạp shared assemblies bằng reflection.
- Data Portal chỉ dùng UI/action native EPLAN; không tìm action `XDataPortalNavigatorToggle`,
  không nhúng trình duyệt/WebView2 của Tool.
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
6. ❌ Không sửa proxy/hosts/proxy VPN/port để lách license hoặc kiểm tra truy cập Data Portal.
7. ❌ Không lưu password/cookie/session ra log hoặc vào repo; Data Portal dùng phiên EPLAN do người dùng quản lý.

## 9. Việc tiếp theo

- Ribbon sửa mới đã GUI UAT Script Edition trên Education 2026.0.3; cần test sáu nút Cloud còn lại
  trong môi trường được phép và build/deploy Add-in trên máy có SDK + API license trước phát hành.
- Build lại Add-in/shared runtime (DLL `bin\Release` cũ khác hash runtime UI đã UAT), đóng gói rồi
  test installer/uninstaller headless trên đúng target; đừng dùng installer repo để lấy DLL build cũ.
- Import EDZ thật vẫn cần Parts Database thử nghiệm được người dùng xác nhận; không nhập vào DB hiện tại.
- Xác nhận installer đã dọn sạch PortalHost/WebView2 khỏi `%APPDATA%\Tool Oumijia`.
- Không phát hành **Release ổn định** trước khi đủ UAT; chỉ cho phép RC pre-release có cảnh báo nếu người dùng chấp thuận rõ.

## 10. Phát hành

Mỗi bản cập nhật phải được commit và push lên GitHub. Xem `docs/RELEASING.md`.
Tóm tắt: bump version csproj + CHANGELOG → build add-in + compile-check Script C#5 →
`tools/pack.ps1 -Version X.Y.Z` (zip Add-in + Script Edition) → `git tag vX.Y.Z` → push →
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
