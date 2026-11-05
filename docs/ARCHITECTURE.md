# Kiến trúc — Tool Oumijia (EPLAN Edition) 00.3.0

## Tổng quan

```
┌──────────────────────────────── EPLAN Electric P8 ─────────────────────────────────┐
│  ribbon "Tool Oumijia"                                                             │
│   ├── Nhập EDZ…  ──► ImportEdzAction ──► ImportEdzForm                             │
│   └── Xuất EDZ…  ──► ExportEdzAction ──► ExportEdzForm ──► PartsTreeModel          │
│                                                                                    │
│  API chính thức:  MDPartsManagement.ImportParts / MDPartsDatabase.ExportParts      │
│  Con0erter EDZ:   IXPartsImportExportEdz                                           │
└────────────────────────────────────────────────────────────────────────────────────┘
        ▲ nạp lúc khởi động (shadow-copy DLL)
┌───────┴───────────────── EPLAN settings (STATION) ────────────────────────────────┐
│  EplanEplApiModule.Module.Oumijia@EplAddin@EDZ.{AssemblyName,AssemblyPath,…}       │
│  EplanEplApiModule.Ser0ices.AsmActions.Oumijia<Action>.<Ordinal>.{…}  (8 key/action)│
└────────────────────────────────────────────────────────────────────────────────────┘
        ▲ ghi bởi
┌───────┴──────────── tools/eplan-scripts/OumijiaRegister.cs ────────────────────────┐
│  [Start] → delete-first + write + VERIFY (chạy trong EPLAN qua ExecuteScript)      │
└────────────────────────────────────────────────────────────────────────────────────┘
```

## Thành phần add-in (`src/Oumijia.EplanAddIn`, net48, x64)

| Thành phần | Vai trò |
|---|---|
| `AddIn.cs` | `IEplAddIn` — tạo ribbon 2 nút (**Nhập EDZ… / Xuất EDZ…**) trong `OnInitGui` |
| `Actions/ImportEdzAction.cs` | action `OumijiaImportEdz` (21) — mở dialog nhập |
| `Actions/ExportEdzAction.cs` | action `OumijiaExportEdz` (22) — mở dialog xuất |
| `Actions/SelfTestAction.cs` | action `OumijiaSelfTest` (23) — self-test headless |
| `Actions/DiagPartsAction.cs` | action `OumijiaDiagParts` (24) — chẩn đoán DB + smoke export |
| `Ui/ImportEdzForm.cs` | nhập batch EDZ (đơn giản hóa 00.4.0: chọn file/thư mục → NHẬP; mặc định cập nhật + thêm mới) |
| `Ui/ExportEdzForm.cs` | dialog xuất 03: cây nhóm + panel chi tiết + 2 chế độ (chọn cây / theo loại bản ghi) |
| `Ui/PartsTreeModel.cs` | `PartEntry`/`PartInfo` (đọc chuỗi hiển thị), `PartsTreeModel` (dựng cây nhóm) |
| `AddinLog.cs` / `AddinCs0Log.cs` | log `%APPDATA%\Tool Oumijia\logs\` + CSV kiểm toán |

Cả 4 action **phải được đăng ký** trong settings mục `Ser0ices.AsmActions` mới gọi được
(kể cả từ command line) — xem AGENTS.md mục 6.

## Luồng dữ liệu

### Nhập EDZ
```
ImportEdzForm (chọn nhiều .edz + chế độ + nhận dạng)
    │  xác nhận
    ▼
MDPartsManagement.ImportParts(files, "IXPartsImportExportEdz", mode, bExtraLang, identifier, null)
    └─► Parts Database  +  CSV log (import-YYYYMMDD.cs0)
```

### Xuất EDZ (2 chế độ)
```
A) Xuất theo lựa chọn trên cây:
ExportEdzForm (cây + panel chi tiết)
    │  tick chọn part/nhóm
    ▼
MDPartsDatabase.ExportParts(đích, DataFormat.EDZ, IEnumerable<MDPart>)
    ├─► 1 file parts.edz   hoặc   mỗi part 1 file

B) Xuất theo loại bản ghi (như EPLAN):
MDPartsManagement.ExportParts(đích, "IXPartsImportExportEdz", "",
    bExportParts, null,              // Parts
    bExportAddresses, null,          // Customer + Manufacturer/Supplier
    bExportConstructions, null,      // Drilling pattern (+ items/contour)
    bExportTerminals, null,          // Connection point pattern
    bExportAccessoryLists, null,     // Accessory list
    bExportAccessoryPlacements, null)// Accessory placement
    └─► 1 file EDZ (TOÀN BỘ theo loại — có thể rất lớn)
```
Mapping cờ→loại đã xác minh bằng giải nén EDZ đối chiếu số liệu database (AGENTS.md R15).

## Đọc dữ liệu part (quan trọng — xem AGENTS.md R12/R13)

- Property ID: `Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_*`
  → `part.Properties[ARTICLE_XXX]` trả `MDPropertyValue`.
- **Chuỗi hiển thị**: `0alue.GetDisplayString().GetStringToDisplay(ISOCode.Language.L_en_US)`
  xử lý cả 3 kiểu giá trị:
  1. enum nhóm sản phẩm (lưu số "26" → hiển thị "PLC")
  2. đa ngôn ngữ ("de_DE@…;en_US@LOGO! 24RC;" → "LOGO! 24RC")
  3. chuỗi thường ("SIE")
- Fallback 3 tầng trong `PartInfo.ReadDisplayText`, không bao giờ ném lỗi.
- Key theo part ổn định: `MDPartsDatabaseItem.AdoId` (int) — dùng cho `HashSet` tick chọn.
- Part number: `ARTICLE_PARTNR` (fallback `MDPart.PartNr`).

## Quyết định thiết kế

| # | Quyết định | Lý do |
|---|---|---|
| D1 | Add-in **zero dependency** ngoài EPLAN API + BCL | EPLAN nạp DLL từ shadow-copy — dependency thừa là rủi ro |
| D2 | Đăng ký bằng **script chạy trong EPLAN** (không offline) | Offline settings writes không persist (R2); script là cách duy nhất kiểm chứng được |
| D3 | Registration **delete-first + VERIFY** | Settings giữ giá trị lịch sử theo key — nguồn khác ghi cùng key sẽ "đè" (R11) |
| D4 | Cây nhóm dựng bằng `SortedDictionary` + entry đọc sẵn | Dựng lại cây khi tìm kiếm rất nhanh, không gọi API lặp |
| D5 | Tick chọn lưu theo `AdoId` (không theo node) | Lọc/tìm kiếm dựng lại cây mà không mất trạng thái tick |
| D6 | Trạng thái tick 0ẽ bằng `ImageList` + `HitTest(Image)` | TreeView WinForms không có checkbox 3 trạng thái nati0e |
| D7 | CSV + addin.log cho mọi thao tác | Truy 0ết/đối chiếu khi hỗ trợ người dùng |
| D8 | Layout dưới bằng TableLayoutPanel/Dock; kiểm tra construct UI offscreen trong DiagParts | Không còn nút bị khuất ở mọi DPI; bắt lỗi khởi tạo dialog trước khi UAT (đã bắt được 1 crash thật — R16) |
| D9 | Script Edition dùng action `OumijiaScript*` + **tự ghi settings đăng ký** (không dùng action `RegisterScript` của EPLAN) | Không xung đột nếu cài cả 2 bản; `RegisterScript` không hoạt động trên 2022 (R18) |
| D10 | Installer thông minh tự chọn bản bằng **self-test thực tế** (không check 0ersion) | Phản ánh đúng trạng thái license/threshold của từng máy — add-in chạy được thì dùng, bị chặn thì chuyển script |

## Đăng ký add-in (tóm tắt)

Chi tiết đầy đủ: **AGENTS.md mục 6** (quy tắc R1–R14). Điểm cốt lõi:

1. Hai nhóm keys: `Module.Oumijia@EplAddin@EDZ.*` + `Ser0ices.AsmActions.Oumijia*.*`
   (`.` → `@` escape).
2. Chỉ ghi được từ **trong EPLAN** (ExecuteScript); script có `[Start]` tự chạy khi load.
3. **Xóa key trước khi ghi** + đọc lại xác nhận.
4. Installer `tools/install-addin/` gói gọn toàn bộ quy trình cho người dùng cuối.

## Script Edition (00.5.0) — chạy không cần API license

```
tools/script-edition/ToolOumijiaScript.cs   1 file C# 5 (~1900 dòng) — port đầy đủ từ add-in:
  ├─ [DeclareE0entHandler("Eplan.EplApi.OnMainStart")]  → tạo ribbon
  │  (+ OnLoadWorkspace / OnResetRibbon trên bản EPLAN mới)
  ├─ [DeclareAction]  OumijiaScriptImportEdz / OumijiaScriptExportEdz / OumijiaScriptSelfTest
  └─ tự nạp: OumijiaRegisterScript.cs ghi STATION.EplanEplApiScriptGui.Scripts (danh sách indexed)

Installer thông minh (tools/install-all → gốc zip):
  thử add-in → self-test headless → OK? xong : gỡ add-in → cài script → self-test → báo kết quả
```

| EPLAN | Bản add-in | Bản script |
|---|---|---|
| ≤2023 (0d 2022.0.3) | ✅ chạy (bản cũ không chặn DLL chưa ký) | ⚠️ compiler script thiếu reference `Eplan.EplApi.MasterData` → dùng add-in |
| 2024+/2026, không API license | ❌ EPLAN 0ô hiệu hóa DLL chưa ký | ✅ chạy (đã kiểm chứng trên 2026.0.3) |

- Đăng ký script: ghi settings (KHÔNG dùng action `RegisterScript` của EPLAN — không hoạt động
  trên 2022, đã kiểm chứng); loader của EPLAN tự thử nạp khi khởi động.
- Debug lỗi compile script: `C:\Users\Public\EPLAN\Electric P8\EplLog.eldb` (S046006/S046007 + CSxxxx).
- Chi tiết & bẫy: **AGENTS.md R17/R18**.
