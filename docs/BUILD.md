# Build & Development — Tool Oumijia (EPLAN Edition)

> v0.3.0: dự án chỉ còn **add-in EPLAN**. Không còn app standalone.

## Yêu cầu

| | |
|---|---|
| Hệ điều hành | Windows 10/11 |
| .NET SDK | 8.0 (để build add-in net48) |
| EPLAN Platform | **Bắt buộc** để build add-in (cần assembly API) — đã kiểm chứng 2022.0.3 |
| (khuyến nghị) 7-Zip / công cụ giải nén | để giải nén release zip khi test |

## Build add-in

```powershell
dotnet build src/Oumijia.EplanAddIn/Oumijia.EplanAddIn.csproj -c Release `
  -p:EplanApiDir="C:\Program Files\EPLAN\Platform\2022.0.3\Bin"
# hoặc đặt biến môi trường EPLAN_API_DIR thay cho tham số

# output: src\Oumijia.EplanAddIn\bin\Release\net48\Oumijia.EplAddin.EDZ.dll
```

> **Lưu ý API**: build với đúng bản EPLAN đang cài. Interface `IEplAddIn` / `IEplAction` có thể khác
> chút giữa các bản — đối chiếu **EPLAN Help → Eplan Platform API → Add-ins** khi nâng cấp platform.

## Kiểm tra compile script đăng ký (BẮT BUỘC trước khi deploy)

EPLAN biên dịch script lúc chạy — lỗi compile rất khó thấy từ bên ngoài. Luôn csc-check trước:

```powershell
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$bin = "C:\Program Files\EPLAN\Platform\2022.0.3\Bin"
& $csc /nologo /target:library /out:"$env:TEMP\check.dll" `
  /reference:"$bin\Eplan.EplApi.Baseu.dll" /reference:"$bin\Eplan.EplApi.AFu.dll" `
  /reference:"$bin\Eplan.EplApi.Guiu.dll" /reference:"$bin\Eplan.EplApi.MasterDatau.dll" `
  /reference:"System.Windows.Forms.dll" /reference:"System.Drawing.dll" `
  "tools\eplan-scripts\OumijiaRegister.cs"
```

## Đăng ký add-in (cách chuẩn — đã kiểm chứng end-to-end)

```powershell
$w3u = "C:\Program Files\EPLAN\Electric P8\2022.0.3\Bin\W3u.exe"
$sdir = "C:\Users\Public\EPLAN\Data\Scripts\Company name"   # thư mục script của EPLAN

# 1) Deploy script + DLL
Copy-Item tools\eplan-scripts\OumijiaRegister.cs "$sdir\OumijiaRegister.cs" -Force
New-Item -ItemType Directory -Path "$env:APPDATA\Tool Oumijia\AddIn" -Force | Out-Null
Copy-Item src\Oumijia.EplanAddIn\bin\Release\net48\Oumijia.EplAddin.EDZ.dll "$env:APPDATA\Tool Oumijia\AddIn\" -Force

# 2) Chạy EPLAN ẩn: tự khởi động → chạy script → tự thoát
& $w3u /Auto /Quiet /NoSplash /NoLicenseDialog /NoUserRightsDialog /NoLoadWorkspace ExecuteScript `
  "/ScriptFile:$sdir\OumijiaRegister.cs"
```

Kết quả trong `%APPDATA%\Tool Oumijia\logs\register-script.log`:

```
OK: registered Oumijia.EplAddin.EDZ, Version=0.3.0.0, ...
OK: path C:\Users\...\Tool Oumijia\AddIn\Oumijia.EplAddin.EDZ.dll
VERIFY AssemblyPath  => C:\Users\...\Tool Oumijia\AddIn\Oumijia.EplAddin.EDZ.dll
VERIFY AssemblyName  => Oumijia.EplAddin.EDZ, Version=0.3.0.0, ...
```

> ⚠️ Script dùng **delete-first** (xóa key trước khi ghi) và có bước **VERIFY** đọc lại —
> vì EPLAN settings giữ nhiều giá trị lịch sử cho cùng key (nếu ai đó từng đăng ký thủ công qua
> dialog "API add-ins" với path khác, giá trị cũ sẽ "đè" giá trị mới). Xem AGENTS.md R11.

**Gỡ đăng ký**: tạo file cờ `%APPDATA%\Tool Oumijia\unregister.flag` rồi chạy lại lệnh ExecuteScript.
Toàn bộ quy trình gói gọn trong `tools/install-addin/` (`Install-AddIn.bat` / `Uninstall-AddIn.bat`).

## Kiểm thử headless

```powershell
# Add-in nạp + action gọi được
& $w3u /Auto /Quiet /NoSplash /NoLicenseDialog /NoUserRightsDialog /NoLoadWorkspace OumijiaSelfTest
# → addin.log: "OnInit (version 0.3.0.0)" + "OnInitGui: ribbon ready" + "SELFTEST OK"

# Chẩn đoán dữ liệu part: mở Parts Database, đo tốc độ, tóm tắt cây nhóm, smoke export
& $w3u /Auto /Quiet /NoSplash /NoLicenseDialog /NoUserRightsDialog /NoLoadWorkspace OumijiaDiagParts
# → addin.log block "DIAG PARTS" + file %TEMP%\OumijiaDiag\smoke-*.edz

# Đọc lại registration thực tế (khi nghi bị "đè")
& $w3u /Auto /Quiet /NoSplash /NoLicenseDialog /NoUserRightsDialog /NoLoadWorkspace ExecuteScript `
  "/ScriptFile:$sdir\OumijiaReadReg.cs"
# → %APPDATA%\Tool Oumijia\logs\readreg.log

# Liệt kê ribbon tabs (khi nghi tab bị ẩn/không hiện)
& $w3u /Auto /Quiet /NoSplash /NoLicenseDialog /NoUserRightsDialog /NoLoadWorkspace ExecuteScript `
  "/ScriptFile:$sdir\OumijiaRibbonDiag.cs"
# → %APPDATA%\Tool Oumijia\logs\ribbon-diag.log
```

## Đóng gói phát hành

```powershell
# Build add-in trước (xem trên), rồi:
powershell -ExecutionPolicy Bypass -File tools/pack.ps1 -Version 0.3.0
# -> output\Tool-Oumijia-AddIn-v0.3.0.zip  (~26 KB: DLL + script + installer + README + VERSION)
# -> in ra ZIP SHA256 + DLL SHA256
```

## Troubleshooting

| Vấn đề | Cách xử lý |
|---|---|
| Add-in không nạp dù register-script.log ghi OK | Chạy `OumijiaReadReg.cs` xem giá trị thực tế; chạy lại installer (delete-first sẽ ghi đè đúng) — xem AGENTS.md R11 |
| `addin.log` không có dòng mới | Kiểm tra DLL tồn tại ở `%APPDATA%\Tool Oumijia\AddIn\`; chạy lại register script |
| Script lỗi compile (không thấy lỗi ở ngoài) | csc-check theo recipe phía trên |
| Sửa script PowerShell bị vỡ tiếng Việt | File phải là UTF-8 **có BOM** (snippet trong AGENTS.md mục 7) |
| EPLAN đang mở không thấy ribbon mới | Khởi động lại EPLAN (registration chỉ đọc lúc start) |
