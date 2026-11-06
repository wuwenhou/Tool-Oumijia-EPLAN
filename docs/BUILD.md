# Build & Development — Tool Oumijia (EPLAN Edition)

> v0.5.2: cả Add-in và Script Edition dùng chung `Oumijia.EplanUi`; đọc `.edz` được build bằng `Oumijia.EdzReader`; Portal dùng WebView2 được đóng gói cùng release.

## Yêu cầu

| | |
|---|---|
| Hệ điều hành | Windows 10/11 |
| .NET SDK | 8.0 (build dự án net48) |
| EPLAN Platform | **Bắt buộc** để build add-in — đã kiểm chứng với 2022.0.3 |
| WebView2 Runtime | Microsoft Edge WebView2 Runtime (fallback mở trình duyệt) |

## Build source

```powershell
# Add-in + component dùng chung (UI, EDZ reader, PortalHost)
dotnet build src/Oumijia.EplanAddIn/Oumijia.EplanAddIn.csproj -c Release `
  -p:EplanApiDir="C:\Program Files\EPLAN\Platform\2022.0.3\Bin"
```

Output chính:

```text
src\Oumijia.EplanAddIn\bin\Release\net48\
  Oumijia.EplAddin.EDZ.dll
  Oumijia.EplanUi.dll
  Oumijia.EdzReader.dll
  Oumijia.PortalHost.dll
  SharpCompress.dll + dependency WebView2/System.*
```

> `Oumijia.EplanUi` và `Oumijia.PortalHost` là dependency có chủ đích. Installer copy chúng vào`%APPDATA%\Tool Oumijia\AddIn\` (bản add-in) và `%APPDATA%\Tool Oumijia\Portal\` (script/path shared). Script nạp qua reflection.

## Kiến trúc nguồn

```text
src/Oumijia.EplanAddIn/       ribbon + actions của Add-in
src/Oumijia.EplanUi/          UI chung: ImportEdzForm, ExportEdzForm, mô hình PartTreeModel
src/Oumijia.EdzReader/        đọc cục bộ tên/hãng/mã hàng trong EDZ
src/Oumijia.PortalHost/       form Data Portal nhúng WebView2
```

## Compile-check Script Edition

Luôn kiểm tra vì EPLAN biên dịch script lúc chạy:

```powershell
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$bin = "C:\Program Files\EPLAN\Platform\2022.0.3\Bin"
$out = Join-Path $env:TEMP "Oumijia-script-check.dll"
& $csc /nologo /target:library /langversion:5 /out:$out `
  /reference:"$bin\Eplan.EplApi.Baseu.dll" `
  /reference:"$bin\Eplan.EplApi.AFu.dll" `
  /reference:"$bin\Eplan.EplApi.Guiu.dll" `
  /reference:"System.Windows.Forms.dll" `
  /reference:"System.Drawing.dll" `
  "tools\script-edition\ToolOumijiaScript.cs"
```

## Kiểm thử parser EDZ

Reader phải:

- xử lý `.edz` dạng ZIP/7z;
- giới hạn số byte mỗi entry;
- đọc tối đa 500 part từ mỗi package;
- không giải nén hàng loạt vào thư mục người dùng;
- lỗi package phải trả về trạng thái preview, không làm crash EPLAN.

## Đóng gói release

```powershell
# Build Add-in/component trước
dotnet build src/Oumijia.EplanAddIn/Oumijia.EplanAddIn.csproj -c Release `
  -p:EplanApiDir="C:\Program Files\EPLAN\Platform\2022.0.3\Bin"

# Package v0.5.2
powershell -ExecutionPolicy Bypass -File tools/pack.ps1 -Version 0.5.2
```

Output:

```text
output\Tool-Oumijia-v0.5.2.zip
output\Tool-Oumijia-v0.5.2\
```

`pack.ps1` yêu cầu có đủ:

```text
Oumijia.EplAddin.EDZ.dll
Oumijia.EplanUi.dll
Oumijia.EdzReader.dll
Oumijia.PortalHost.dll
SharpCompress.dll
Microsoft.Web.WebView2.Core.dll
Microsoft.Web.WebView2.WinForms.dll
WebView2Loader.dll
```

## Kiểm thử máy thật trước phát hành

### Máy EPLAN 2022 được phép

1. Đóng EPLAN.
2. Chạy installer trong `addin\Install-AddIn.bat`.
3. Mở EPLAN và kiểm tra:
   - ribbon **Tool Oumijia**;
   - Nhập EDZ preview một package EDZ;
   - Xuất EDZ giữ hành vi cũ;
   - Data Portal mở/tự đăng nhập; tải tài liệu (nếu có quyền).
   - EDZ tải được nhập lại vào Parts Database.
4. Gỡ cài đặt bằng `Uninstall-Tool-Oumijia.bat`.

### Máy EPLAN công ty

- Chỉ thử khi IT cho phép và license hợp lệ.
- Không sử dụng kết nối để lách kiểm tra license.
- Nếu portal không mở với lỗi mạng/cert, yêu cầu IT/EPLAN cấp quyền thay vì sửa hosts/proxy hoặc SSL.

## Troubleshooting

| Vấn đề | Cách xử lý |
|---|---|
| Add-in không nạp | Đóng EPLAN, chạy lại installer; kiểm tra `register-script.log` |
| Thiếu component lúc chạy | Chạy lại installer; pack.ps1 phải copy mọi DLL |
| EDZ preview lỗi | File vẫn vào hàng đợi; xem thông báo trong grid/log |
| Portal màn hình đen | Dùng `Mở ngoài`; kiểm tra WebView2 Runtime và proxy/cert được IT cho phép |
| Script lỗi compile | Xem `EplLog.eldb`; dùng Add-in nếu bản EPLAN thiếu reference |
| File `.ps1` hiển thị lỗi ký tự | Bảo đảm UTF-8 có BOM |

## Phát hành

Xem [RELEASING.md](RELEASING.md) và [PLAN-v0.5.2.md](PLAN-v0.5.2.md). Không tạo tag/push Release trước khi kiểm thử máy thật đạt.
