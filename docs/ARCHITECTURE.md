# Kiến trúc — Tool Oumijia (EPLAN Edition) v0.3.0

## Tổng quan

```
┌──────────────────────────────── EPLAN Electric P8 ─────────────────────────────────┐
│  ribbon "Tool Oumijia"                                                             │
│   ├── Nhập EDZ…  ──► ImportEdzAction ──► ImportEdzForm                             │
│   └── Xuất EDZ…  ──► ExportEdzAction ──► ExportEdzForm ──► PartsTreeModel          │
│                                                                                    │
│  API chính thức:  MDPartsManagement.ImportParts / MDPartsDatabase.ExportParts      │
│  Converter EDZ:   IXPartsImportExportEdz                                           │
└────────────────────────────────────────────────────────────────────────────────────┘
        ▲ nạp lúc khởi động (shadow-copy DLL)
┌───────┴───────────────── EPLAN settings (STATION) ────────────────────────────────┐
│  EplanEplApiModule.Module.Oumijia@EplAddin@EDZ.{AssemblyName,AssemblyPath,…}       │
│  EplanEplApiModule.Services.AsmActions.Oumijia<Action>.<Ordinal>.{…}  (8 key/action)│
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
| `Ui/ImportEdzForm.cs` | nhập batch EDZ (đơn giản hóa v0.4.0: chọn file/thư mục → NHẬP; mặc định cập nhật + thêm mới) |
| `Ui/ExportEdzForm.cs` | dialog xuất v3: cây nhóm + panel chi tiết + 2 chế độ (chọn cây / theo loại bản ghi) |
| `Ui/PartsTreeModel.cs` | `PartEntry`/`PartInfo` (đọc chuỗi hiển thị), `PartsTreeModel` (dựng cây nhóm) |
| `AddinLog.cs` / `AddinCsvLog.cs` | log `%APPDATA%\Tool Oumijia\logs\` + CSV kiểm toán |

Cả 4 action **phải được đăng ký** trong settings mục `Services.AsmActions` mới gọi được
(kể cả từ command line) — xem AGENTS.md mục 6.

## Luồng dữ liệu

### Nhập EDZ
```
ImportEdzForm (chọn nhiều .edz + chế độ + nhận dạng)
    │  xác nhận
    ▼
MDPartsManagement.ImportParts(files, "IXPartsImportExportEdz", mode, bExtraLang, identifier, null)
    └─► Parts Database  +  CSV log (import-YYYYMMDD.csv)
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
- **Chuỗi hiển thị**: `value.GetDisplayString().GetStringToDisplay(ISOCode.Language.L_en_US)`
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
| D6 | Trạng thái tick vẽ bằng `ImageList` + `HitTest(Image)` | TreeView WinForms không có checkbox 3 trạng thái native |
| D7 | CSV + addin.log cho mọi thao tác | Truy vết/đối chiếu khi hỗ trợ người dùng |
| D8 | Layout dưới bằng TableLayoutPanel/Dock; kiểm tra construct UI offscreen trong DiagParts | Không còn nút bị khuất ở mọi DPI; bắt lỗi khởi tạo dialog trước khi UAT (đã bắt được 1 crash thật — R16) |

## Đăng ký add-in (tóm tắt)

Chi tiết đầy đủ: **AGENTS.md mục 6** (quy tắc R1–R14). Điểm cốt lõi:

1. Hai nhóm keys: `Module.Oumijia@EplAddin@EDZ.*` + `Services.AsmActions.Oumijia*.*`
   (`.` → `@` escape).
2. Chỉ ghi được từ **trong EPLAN** (ExecuteScript); script có `[Start]` tự chạy khi load.
3. **Xóa key trước khi ghi** + đọc lại xác nhận.
4. Installer `tools/install-addin/` gói gọn toàn bộ quy trình cho người dùng cuối.
