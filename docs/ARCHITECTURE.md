# Kiến trúc — Tool Oumijia (EPLAN Edition)

## Tổng quan

```
┌──────────────────────────── Oumijia.Tool (WinForms) ───────────────────────────┐
│  ExportPanel            ImportPanel               SettingsPanel                │
│  (quét → bảng chọn      (EDZ queue +             (library root, inbox,        │
│   → xuất thư viện)       cài macro EMA)           policy, macro dirs)          │
└───────────────┬─────────────────────────────────────────────────────────────────┘
                │ dùng chung
┌───────────────▼──────────────────────── Oumijia.Core (netstandard2.0) ─────────┐
│                                                                                │
│  Edz/                                                                          │
│    EdzArchive        đọc container ZIP/7z (SharpCompress), magic-byte detect   │
│    EdzPackageReader  đọc manifest + inventory + part XML → EdzPackageInfo       │
│    PartXmlParser     harvest XML khoan dung (element name / id / name / key)    │
│                                                                                │
│  Classification/                                                               │
│    ClassifierRules   rule mặc định: 25+ nhóm + alias hãng                      │
│    Classifier        Hãng: alias → bỏ hậu tố công ty → sanitize                │
│                      Nhóm: keyword (product group/designation/partnr/tên file) │
│                            + eCl@ss prefix (tùy chỉnh)                          │
│                                                                                │
│  Library/                                                                      │
│    LibraryWriter     <root>/<relative>/<file>, policy Rename/Skip/Overwrite,   │
│                      phát hiện trùng bằng SHA-256, move hoặc copy               │
│    CsvLog            log UTF-8 BOM (mở Excel chuẩn)                            │
│    OverrideStore     overrides.json (nhớ đích đến sửa tay theo tên file)        │
│                                                                                │
│  Macros/                                                                       │
│    MacroPathDetector dò %PUBLIC%\EPLAN\Data\Macros… + thư mục hãng con          │
│    MacroInstaller    copy .ema/.emp/.ems vào thư mục đích (dùng LibraryWriter)  │
│                                                                                │
│  Config/                                                                       │
│    OumijiaConfig     config.json + rules.json (+ ghi mặc định lần đầu)          │
│    AppPaths          %APPDATA%\Tool Oumijia\{config,rules,overrides}.json, logs │
└─────────────────────────────────────────────────────────────────────────────────┘
        ▲                                            
        │ ProjectReference (net48)                   
┌───────┴──────────────── Oumijia.EplanAddIn (net48, x64) ──────────────────────┐
│  AddIn          IEplAddIn — OnRegister/OnInit/OnInitGui (ribbon) /OnExit       │
│  Actions/       OumijiaOpenTool / OumijiaImportEdz / OumijiaExportEdz /        │
│                 OumijiaSelfTest (IEplAction)                                    │
│  Ui/            ImportEdzForm (ImportParts batch) · ExportEdzForm (ExportParts)│
│  AddinLog       %APPDATA%\Tool Oumijia\logs\addin.log + CSV audit              │
└────────────────────────────────────────────────────────────────────────────────┘
        ▲
        │ ghi registration vào EPLAN settings (chạy bên trong EPLAN)
┌───────┴──────────── tools/eplan-scripts/OumijiaRegister.cs ────────────────────┐
│  [Start] → Register/Unregister: Module keys + Services.AsmActions per action   │
│  (chạy qua W3u.exe … ExecuteScript; installer: tools/install-addin/)           │
└────────────────────────────────────────────────────────────────────────────────┘
```

## Luồng dữ liệu chính

### Export (phân loại thư viện)
```
inbox folders ──► ScanFiles() ──► với mỗi .edz: EdzPackageReader.Read()
                                        │
                                        ▼
                              Classifier.Classify(file, info)
                                ├─ Hãng:  info.PrimaryManufacturer → alias/suffix → "Siemens"
                                └─ Nhóm:  haystack keyword → "MCB" (fallback "_Unsorted")
                                        │
                                        ▼
                              ExportRow { Đích đến gợi ý }  ── sửa tay ──► overrides.json
                                        │
                                        ▼  (bấm XUẤT)
                              LibraryWriter.Write(file, relativeFolder)
                                        └─► <root>/<Hãng>/<Nhóm>/<file>  + CSV log
```

### Import macro (kiểu A)
```
.ema/.emp/.ems ──► MacroInstaller.Install(file, targetDir)
                    ├─ validate extension
                    └─ LibraryWriter (copy, policy trùng tên)
                          └─► %PUBLIC%\EPLAN\Data\Macros\<Company>\...  → khởi động lại EPLAN
```
*(Core vẫn còn nhưng UI đã gỡ — xem quyết định phạm vi EDZ-only trong AGENTS.md mục 1)*

### Add-in: Import EDZ (bên trong EPLAN)
```
ribbon "Nhập EDZ…" ──► ImportEdzForm (chọn file/folder, mode, identifier)
                          │  xác nhận
                          ▼
        MDPartsManagement.ImportParts(files, "IXPartsImportExportEdz",
                                      mode, bExtraLang, identifier, null)
                          └─► Parts Database (EPLAN xử lý)  + CSV log
```

### Add-in: Export EDZ (bên trong EPLAN)
```
ribbon "Xuất EDZ…" ──► ExportEdzForm (phạm vi: all / selected; 1 file / mỗi part 1 file)
                          │  xác nhận
                          ▼
        MDPartsDatabase.ExportParts(path, DataFormat.EDZ, IEnumerable<MDPart>)
                          └─► file .edz  + CSV log
```

## Quyết định thiết kế quan trọng

| # | Quyết định | Lý do |
|---|---|---|
| D1 | Core = **netstandard2.0** | EPLAN API chạy trên .NET Framework 4.8.1 → add-in net48 phải tham chiếu được core. Đã trả giá 1 lần: tránh `string.Contains(char)`, `TryAdd`, `Range`… |
| D2 | UI multi-target **net8.0-windows + net48** | Cùng một UI dùng cho cả app độc lập lẫn trong EPLAN |
| D3 | `.ema` là **file đục** (opaque) | PXF tokenized độc quyền — chỉ copy/đính kèm, không phân tích geometry |
| D4 | Chỉ xử lý **dữ liệu factual** từ EDZ | Tránh vấn đề bản quyền/ToU; giống cách QElectroTech làm với EDZ importer |
| D5 | Trùng tên mặc định **không ghi đè** (Rename) | An toàn dữ liệu thư viện; người dùng có thể đổi sang Skip/Overwrite |
| D6 | Mọi thao tác có **log CSV + SHA-256** | Truy vết/đối chiếu về sau |
| D7 | PowerShell 5.1 tương thích, **UTF-8 BOM** cho .ps1 | Máy kỹ sư thường chỉ có PowerShell 5.1 mặc định |

## Định dạng EDZ — ghi chú thực nghiệm

- Container: **ZIP** hoặc **7z** (phát hiện bằng magic bytes: `PK\x03\x04` / `7z¼¯'\x1C`).
- Cấu trúc điển hình: `manifest.xml` (v2.0), `items/partxml/*.part.xml`, `macros/*.ema`,
  ảnh, tài liệu, (tùy gói) DXF/STEP.
- Part XML **không có schema thống nhất** giữa các nhà sản xuất → `PartXmlParser` harvest
  mọi cặp key/value từ: tên element, thuộc tính `id/name/key`, cặp `<name>/<value>`,
  và regex fallback cho `P_ARTICLE_*`.
- Khóa tra cứu: `MANUFACTURER`, `PARTNR/PARTNUMBER`, `TYPENR`, `ORDERNR`, `DESCR1`,
  `PRODUCTGROUP/PRODUCTTOPGROUP`, `ECLASS` (so khớp chính xác trước, "chứa" sau; biến thể
  bỏ ký tự đặc biệt được chuẩn hóa).

## Mở rộng (dành cho người kế nhiệm)

- **Thêm nhóm phân loại**: sửa `ClassifierRules.CreateDefault()` hoặc để người dùng sửa
  `rules.json` (file tự sinh trong `%APPDATA%`).
- **Thêm nguồn metadata**: `EdzPackageReader.Read()` — thêm bước đọc nếu có định dạng mới.
- **Sửa/thêm action add-in**: `src/Oumijia.EplanAddIn/Actions/` + đăng ký action mới trong
  `tools/eplan-scripts/OumijiaRegister.cs` (section `Services.AsmActions` — xem AGENTS.md mục 6).
- **EDZ writer**: tạo `EdzArchiveWriter` mới trong `Edz/`, dùng SharpCompress để ghi
  (ZIP chắc chắn được; 7z-ghi cần kiểm chứng với SharpCompress hoặc LZMA SDK).
