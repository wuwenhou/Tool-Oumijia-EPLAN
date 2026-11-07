# Build & Development — Tool Oumijia (EPLAN Edition)

> Source candidate sau v0.6.0 đã GUI UAT Script Edition trên Education 2026.0.3 bằng `RibbonCommandGroup.AddCommandWithId` tìm ID từ tab Eplan Cloud gốc; **không** mở riêng `DataPortalTabView` (thiếu cửa sổ dockable), không có WebView2 của Tool. Add-in source chỉ compile-check. Xem `docs/AGENT-HANDOFF-2026-11-07.md` trước khi build/deploy.

## Yêu cầu

| | |
|---|---|
| Hệ điều hành | Windows 10/11 x64 |
| .NET SDK | 8.0 (build dự án net48) — hoặc dùng csc thủ công, xem bên dưới |
| EPLAN Platform | **Bắt buộc** để build add-in — đã kiểm chứng với 2022.0.3 và 2026.0.3 |
| WebView2 Runtime | Tool không đóng gói/điều khiển; EPLAN native tự quản lý dependency của nó |

## Build source

```powershell
# Add-in + component dùng chung (UI, EDZ reader)
dotnet build src/Oumijia.EplanAddIn/Oumijia.EplanAddIn.csproj -c Release `
  -p:EplanApiDir="C:\Program Files\EPLAN\Platform\2022.0.3\Bin"
```

Output chính:

```text
src\Oumijia.EplanAddIn\bin\Release\net48\
  Oumijia.EplAddin.EDZ.dll
  Oumijia.EplanUi.dll
  Oumijia.EdzReader.dll
  SharpCompress.dll + System.* dependencies
```

> `Oumijia.EplanUi` và `Oumijia.EdzReader` là dependency có chủ đích. Installer copy chúng vào `%APPDATA%\Tool Oumijia\AddIn\` (bản add-in) và `%APPDATA%\Tool Oumijia\Portal\` (shared runtime cho Script Edition). Script nạp qua reflection.

### Build thủ công bằng csc (khi không có .NET SDK)

Dùng khi chỉ cần kiểm tra compile; **không** coi một DLL csc-check với shared runtime cũ là build release. Tạo file `AssemblyInfo.cs` tạm cạnh source (không commit):

```csharp
using System.Reflection;
[assembly: AssemblyVersion("0.6.0.0")]
[assembly: AssemblyFileVersion("0.6.0.0")]
```

```powershell
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$bin = "C:\Program Files\EPLAN\Platform\2026.0.3\Bin"
$shared = "$env:APPDATA\Tool Oumijia\Portal"
$root = "src\Oumijia.EplanAddIn"

& $csc /nologo /langversion:5 /target:library /platform:x64 `
  /out:"$root\bin\Release\net48\Oumijia.EplAddin.EDZ.dll" `
  /reference:"$bin\Eplan.EplApi.AFu.dll" `
  /reference:"$bin\Eplan.EplApi.Baseu.dll" `
  /reference:"$bin\Eplan.EplApi.Guiu.dll" `
  /reference:"$bin\Eplan.EplApi.MasterDatau.dll" `
  /reference:"$shared\Oumijia.EplanUi.dll" `
  /reference:"$shared\Oumijia.EdzReader.dll" `
  /reference:"$shared\SharpCompress.dll" `
  /reference:"System.Windows.Forms.dll" /reference:"System.Drawing.dll" `
  /reference:"System.dll" /reference:"System.Core.dll" `
  "$root\AssemblyInfo.cs" "$root\AddIn.cs" "$root\Actions\*.cs"
```

> Build chuẩn vẫn là `dotnet build`. Máy hiện tại chỉ có .NET runtime (không có SDK), nhưng có Roslyn `csc.exe` của Visual Studio 2019 tại `MSBuild\Current\Bin\Roslyn\csc.exe`; dùng nó với `/langversion:7.3` để build `Oumijia.EplanUi` vào staging. `csc.exe` của .NET Framework chỉ hỗ trợ C# 5, phù hợp compile-check Script Edition. Không ghi đè DLL đang deploy trước UAT.

## Kiến trúc nguồn

```text
src/Oumijia.EplanAddIn/       ribbon + actions của Add-in
src/Oumijia.EplanUi/          UI chung: ImportEdzForm, ExportEdzForm, mô hình PartTreeModel
src/Oumijia.EdzReader/        đọc cục bộ tên/hãng/mã hàng trong EDZ
```

## Compile-check Script Edition

Luôn kiểm tra vì EPLAN biên dịch script lúc chạy:

```powershell
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$bin = "C:\Program Files\EPLAN\Platform\2026.0.3\Bin"
$out = Join-Path $env:TEMP "Oumijia-script-check.dll"
& $csc /nologo /target:library /langversion:5 /out:$out `
  /reference:"$bin\Eplan.EplApi.Baseu.dll" `
  /reference:"$bin\Eplan.EplApi.AFu.dll" `
  /reference:"$bin\Eplan.EplApi.Guiu.dll" `
  /reference:"$bin\Eplan.EplApi.MasterDatau.dll" `
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

# Chỉ sau khi build, tài liệu release cho version mới và UAT phù hợp:
powershell -ExecutionPolicy Bypass -File tools/pack.ps1 -Version X.Y.Z
```

Output:

```text
output\Tool-Oumijia-vX.Y.Z.zip
output\Tool-Oumijia-vX.Y.Z\
```

`pack.ps1` yêu cầu có đủ:

```text
Oumijia.EplAddin.EDZ.dll
Oumijia.EplanUi.dll
Oumijia.EdzReader.dll
SharpCompress.dll
```

Nó cũng lọc bỏ mọi DLL `Oumijia.PortalHost*`, `Microsoft.Web.WebView2*`, `WebView2Loader.dll` còn sót trong output, kèm `EplanTargets.ps1` và từ chối Add-in DLL cũ hơn source. Trên máy hiện tại UI DLL `bin\Release` khác runtime đã UAT: **build mới trước khi pack**.

## Kiểm thử máy thật trước phát hành

### Máy EPLAN 2022 được phép

1. Đóng EPLAN.
2. Build payload Add-in đúng API target; chọn target bằng `Install-Tool-Oumijia.bat -ListInstalled/-TargetW3u` sau khi manifest hỗ trợ target này. Installer riêng edition hiện là legacy, không chọn đúng target trên máy nhiều bản.
3. Mở EPLAN và kiểm tra:
   - ribbon **Tool Oumijia** có nhóm `Quản lý EDZ` ba nút Nhập/Xuất/Menu bar và nhóm `Eplan Cloud` bảy nút native, không có Portal trùng trong EDZ;
   - Nhập EDZ preview một package EDZ;
   - Xuất EDZ giữ hành vi cũ;
   - Data Portal mở Data Portal native (tab Eplan Cloud); tải tài liệu nếu tài khoản có quyền;
   - EDZ tải được nhập lại vào Parts Database.
4. Gỡ đăng ký đúng target bằng `Uninstall-Tool-Oumijia.bat -TargetW3u "<W3u.exe>"`; shared runtime được giữ vì có thể còn EPLAN khác dùng.

### Máy EPLAN công ty / không có API license

- Candidate hỗ trợ cài **Script Edition Education 2026.0.3** sau khi đóng gói và UAT installer headless; không dùng installer riêng cũ trên máy nhiều bản.
- Chỉ thử khi IT cho phép và tài khoản EPLAN hợp lệ.
- Không sử dụng kết nối để lách kiểm tra license.
- Nếu Data Portal không mở với lỗi mạng/cert, yêu cầu IT/EPLAN cấp quyền thay vì sửa hosts/proxy hoặc SSL.

## Troubleshooting

| Vấn đề | Cách xử lý |
|---|---|
| Add-in không nạp | Đóng EPLAN, chạy lại installer; kiểm tra `register-script.log` |
| Thiếu component lúc chạy | Chạy lại installer; pack.ps1 phải copy `Oumijia.EplanUi`, `Oumijia.EdzReader`, `SharpCompress` |
| EDZ preview lỗi | File vẫn vào hàng đợi; xem thông báo trong grid/log |
| Data Portal không mở | Mở trực tiếp **Eplan Cloud → Data Portal**; kiểm tra `addin.log` dòng `Native Cloud ribbon`/retry. Không gọi `XDataPortalNavigatorToggle` hay `DataPortalTabView` riêng; liên hệ IT nếu chính Portal native gặp lỗi mạng/quyền. |
| Script lỗi compile | Xem `EplLog.eldb`; dùng Add-in nếu bản EPLAN thiếu reference |
| File `.ps1` hiển thị lỗi ký tự | Bảo đảm UTF-8 có BOM |

## Phát hành

Xem [RELEASING.md](RELEASING.md). Không tạo tag/push Release trước khi kiểm thử máy thật đạt.
