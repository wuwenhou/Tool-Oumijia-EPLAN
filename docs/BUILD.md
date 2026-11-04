# Build & Development — Tool Oumijia (EPLAN Edition)

## Yêu cầu

| | |
|---|---|
| Hệ điều hành | Windows 10/11 |
| .NET SDK | 8.0 (để build; chạy bản release **không cần** cài gì) |
| (tùy chọn) EPLAN Platform | Chỉ cần khi build add-in |
| (tùy chọn) 7-Zip | Chỉ cần để chạy test 7z thật (test tự bỏ qua nếu không có) |

## Build & test cơ bản

```powershell
git clone https://github.com/wuwenhou/Tool-Oumijia-EPLAN.git
cd Tool-Oumijia-EPLAN

dotnet build Tool-Oumijia-EPLAN.sln -c Release
dotnet test  Tool-Oumijia-EPLAN.sln -c Release
```

Chạy app ở chế độ dev:

```powershell
dotnet run --project src/Oumijia.Tool -c Release
# hoặc chạy trực tiếp file build:
# src\Oumijia.Tool\bin\Release\net8.0-windows\Tool-Oumijia.exe
```

> Lưu ý: solution build cả target **net48** (dùng cho add-in EPLAN). Target này cần package
> `Microsoft.NETFramework.ReferenceAssemblies` — được restore tự động từ NuGet, không cần cài
> .NET Framework Developer Pack.

## Tạo gói phát hành

```powershell
powershell -ExecutionPolicy Bypass -File tools/pack.ps1 -Version 0.2.0
# -> output\Tool-Oumijia-EPLAN-v0.2.0-win-x64.zip  (self-contained, kèm VERSION.txt + README.txt + addin\)
```

Gói zip kèm thư mục `addin\` (DLL + script + installer) để người dùng EPLAN cài add-in một bước.
Nếu chưa build add-in (`src\Oumijia.EplanAddIn\bin\Release\net48\Oumijia.EplAddin.EDZ.dll` không tồn tại),
zip vẫn tạo được nhưng thiếu `addin\` — pack.ps1 sẽ cảnh báo.

Tùy chọn framework-dependent (zip nhỏ hơn, yêu cầu .NET 8 Desktop Runtime trên máy đích):

```powershell
powershell -ExecutionPolicy Bypass -File tools/pack.ps1 -Version 0.2.0 -FrameworkDependent
```

## Build EPLAN Add-in (cần máy có EPLAN) — v0.2.0

`src/Oumijia.EplanAddIn` **không** nằm trong solution mặc định vì cần assembly của EPLAN:

```powershell
dotnet build src/Oumijia.EplanAddIn/Oumijia.EplanAddIn.csproj -c Release `
  -p:EplanApiDir="C:\Program Files\EPLAN\Platform\2022.0.3\Bin"
# hoặc đặt biến môi trường EPLAN_API_DIR thay cho tham số
# output: src\Oumijia.EplanAddIn\bin\Release\net48\Oumijia.EplAddin.EDZ.dll
```

> **Lưu ý API**: build với đúng bản EPLAN đang cài. Đã kiểm chứng đầy đủ trên **EPLAN Platform 2022.0.3**
> (`Eplan.EplApi.AFu/Baseu/Guiu/MasterDatau.dll`). Interface `IEplAddIn` / `IEplAction` có thể khác
> chút giữa các bản — đối chiếu **EPLAN Help → Eplan Platform API → Add-ins** khi nâng cấp platform.

Cũng build được tiện ích đăng ký (`Oumijia.Register.exe`):

```powershell
dotnet build src/Oumijia.Register/Oumijia.Register.csproj -c Release
```

### Đăng ký add-in (cách chuẩn — đã kiểm chứng end-to-end)

EPLAN **không** load add-in chỉ bằng cách copy DLL; phải ghi registration vào EPLAN settings.
Cách chính thức & duy nhất đã kiểm chứng hoạt động: **chạy registration script trong EPLAN**.

```powershell
# 1. Copy script + DLL
Copy-Item src\Oumijia.EplanAddIn\bin\Release\net48\Oumijia.EplAddin.EDZ.dll "$env:APPDATA\Tool Oumijia\AddIn\" -Force
Copy-Item tools\eplan-scripts\OumijiaRegister.cs "C:\Users\Public\EPLAN\Data\Scripts\Company name\" -Force

# 2. Chạy EPLAN headless: tự khởi động → chạy script → tự thoát
& "C:\Program Files\EPLAN\Electric P8\2022.0.3\Bin\W3u.exe" /Auto /Quiet /NoSplash /NoLicenseDialog `
  /NoUserRightsDialog /NoLoadWorkspace ExecuteScript `
  /ScriptFile:"C:\Users\Public\EPLAN\Data\Scripts\Company name\OumijiaRegister.cs"
```

Script có `[Start]` → tự chạy khi được load, ghi registration rồi kết thúc:

- `STATION.EplanEplApiModule.Module.Oumijia@EplAddin@EDZ.{AssemblyName,AssemblyPath,TypeName,LoadOnStart}`
- `STATION.EplanEplApiModule.Services.AsmActions.Oumijia<Action>.<Ordinal>.{AssemblyName,AssemblyPath,TypeName,ModuleName,…}` cho **từng action class**

→ Kết quả trong `%APPDATA%\Tool Oumijia\logs\register-script.log`:

```
OK: registered Oumijia.EplAddin.EDZ, Version=0.2.0.0, ...
OK: path C:\Users\OWG\AppData\Roaming\Tool Oumijia\AddIn\Oumijia.EplAddin.EDZ.dll
```

**Gỡ đăng ký**: xóa script như trên nhưng tạo trước file cờ `%APPDATA%\Tool Oumijia\unregister.flag`
(script sẽ xóa registration thay vì ghi). Toàn bộ quy trình gói gọn trong `tools/install-addin/`
(`Install-AddIn.bat` / `Uninstall-AddIn.bat`).

### Kiểm tra add-in tự động (headless)

```powershell
# EPLAN khởi động ẩn, nạp add-in, gọi action OumijiaSelfTest, rồi thoát
& "C:\Program Files\EPLAN\Electric P8\2022.0.3\Bin\W3u.exe" /Auto /Quiet /NoSplash /NoLicenseDialog `
  /NoUserRightsDialog /NoLoadWorkspace OumijiaSelfTest
# Kỳ vọng trong %APPDATA%\Tool Oumijia\logs\addin.log:
#   OnInit (version 0.2.0.0)
#   OnInitGui: ribbon ready
#   SELFTEST OK — add-in loaded, actions callable
#   OnExit
```

### Ghi chú kỹ thuật quan trọng (đã trả giá)

| Chủ đề | Kết luận đã kiểm chứng |
|---|---|
| Offline `Settings` writes | **Không bao giờ persist** — API offline chỉ giữ trong bộ nhớ. Luôn dùng cách ExecuteScript. |
| Escape tên module | Dấu `.` trong tên module → `@` (vd `Oumijia.EplAddin.EDZ` → `Oumijia@EplAddin@EDZ`). |
| Vì sao cần cả AsmActions? | Module đơn thuần làm EPLAN shadow-copy DLL + gọi `OnInit`/`OnInitGui`, nhưng action **chỉ đăng ký được** nhờ section `Services.AsmActions` (mirror entry của VEE.ExportConnections). |
| Shadow copy | EPLAN copy DLL vào `%APPDATA%\EPLAN\ShadowCopyAssemblies\<pid>\<tên-addin>\bin\` — sửa DLL gốc thì restart EPLAN sẽ copy bản mới. |
| Cảnh báo license EPLAN khi chạy script | `ExecuteScript` chạy được **không cần license API** (scripting miễn phí). |
| Dấu hiệu add-in không nạp | Không thấy `addin.log` mới; kiểm tra lại `Setting.eod` (mở bằng `FileShare.ReadWrite` — file thường bị EPLAN khóa). |

## Chạy thử với file mẫu

```powershell
powershell -ExecutionPolicy Bypass -File tools/make-samples.ps1   # tạo examples\*.edz
# mở app → tab Xuất → thêm thư mục examples → Quét ngay
```

## Test bằng dữ liệu thật của bạn

1. Copy vài `.edz` thật (từ website hãng) vào một thư mục.
2. Mở app → quét thư mục đó.
3. Soi bảng kết quả: Hãng/Nhóm có đúng không. Nếu lệch, sửa `rules.json` trong
   `%APPDATA%\Tool Oumijia\` rồi **Áp dụng lại quy tắc**.

> Không bao giờ commit dữ liệu hãng thật vào repo — chỉ dùng cho `examples/` các file tổng hợp.

## Troubleshooting

| Vấn đề | Cách xử lý |
|---|---|
| `NU1202`/restore lỗi lạ | Xóa thư mục package hỏng trong `~\.nuget\packages\<tên>\<version>` rồi restore lại |
| Build lỗi ở `Oumijia.Tool (net48)` | Chạy `dotnet restore` trước; kiểm tra package `Microsoft.NETFramework.ReferenceAssemblies` |
| Script `.ps1` lỗi parser với tiếng Việt | File phải là UTF-8 **có BOM** (PowerShell 5.1). Chuyển bằng snippet trong AGENTS.md mục 5 |
| Test 7z không chạy | Không có 7-Zip ở đường dẫn chuẩn — test tự bỏ qua, không ảnh hưởng CI |
