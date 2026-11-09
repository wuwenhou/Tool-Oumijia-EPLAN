# Cài đặt — Tool Oumijia (EPLAN Edition, pre-release v0.7.1)

> **Pre-release, không phải bản ổn định.** v0.7.1 (2026-11-09): Add-in 2022.0.3 đã GUI UAT giao diện (v0.7.1 dọn 2 nút Eplan Cloud chết); **nhập thật vào Parts Database chưa được test**; Script Edition 2026 được đóng gói lại nhưng **chưa GUI UAT trên Education 2026.0.3**; installer chưa UAT cài/gỡ headless. Bản v0.6.0 giữ nguyên như đã phát hành.

## Cài đặt nhanh

1. Chỉ dùng gói pre-release v0.7.1 trên **máy thử nghiệm** chấp nhận rủi ro; bản v0.6.0 giữ nguyên như đã phát hành.
2. Giải nén vào một thư mục bất kỳ.
3. Đóng mọi cửa sổ EPLAN.
4. Chạy `Install-Tool-Oumijia.bat -ListInstalled` để xem các đích. Nếu có nhiều bản EPLAN, gọi `Install-Tool-Oumijia.bat -TargetW3u "C:\Program Files\EPLAN\Education\2026.0.3\Bin\W3u.exe"` (thay bằng đường dẫn được liệt kê).
5. Installer candidate chỉ cho **Education 2026.0.3 / Script Edition**; bản khác bị từ chối cho đến khi có payload riêng và UAT. Không tự chọn `W3u.exe` đầu tiên. Bản này không thử Add-in trên Education.
6. Mở EPLAN và kiểm tra ribbon **Tool Oumijia**. Đừng coi cài/headless PASS là GUI PASS.

Ribbon v0.7.1 (giống v0.6.0 về ribbon):

```text
Quản lý EDZ: Nhập EDZ → Xuất EDZ → Menu bar (xếp dọc)
Eplan Cloud: bảy nút native, gồm một Data Portal; tab Cloud gốc giữ nguyên
```

## Hai phiên bản

| | **Add-in** | **Script Edition** |
|---|---|---|
| Công nghệ | DLL add-in (.NET Framework 4.8.1 cho API 2026) | EPLAN Scripting (C# 5) |
| Yêu cầu | API license hoặc EPLAN cho phép DLL chưa ký | Có scripting EPLAN; không cần API license |
| Phạm vi pre-release | Build với API 2026 đạt, chưa deploy/UAT runtime | GUI UAT Education 2026.0.3 trước khi đổi version RC; phiên bản khác chưa UAT |
| Payload của installer candidate | Chưa bật cho target nào | Education 2026.0.3, vẫn cần UAT cài/gỡ headless |

## Component trong gói

- `addin\`: Add-in + shared UI/EDZ reader + SharpCompress.
- `script\`: Script Edition + folder `portal\` (shared runtime).
- `EplanTargets.ps1`, `Install-Tool-Oumijia.bat/.ps1`, `Uninstall-Tool-Oumijia.bat/.ps1` ở gốc gói; các installer riêng từng edition là legacy, không tự chọn target.
- `docs\`: hướng dẫn người dùng và release note.

## Yêu cầu hệ thống

| | |
|---|---|
| OS | Windows 10/11 x64 |
| EPLAN đã GUI UAT source mới | Education 2026.0.3 / Script Edition; không khẳng định 2022–2025 |
| Quyền | Không cần Administrator |
| EPLAN Data Portal | Máy và tài khoản EPLAN hợp lệ, được IT cho phép |

Không cần WebView2 Runtime. Không copy `.edz` vào Master Data bằng tay; file thiết bị phải được nhập vào Parts Database bằng chức năng Nhập EDZ.

## Data Portal

- Nút **Data Portal** mở **Data Portal native của EPLAN** (tab **Eplan Cloud**), dùng đúng phiên đã đăng nhập.
- Không còn WebView2, không còn đăng nhập riêng, không còn profile trình duyệt của Tool.
- Tải `.edz` theo quyền tài khoản EPLAN.
- Sau khi tải, mở **Nhập EDZ** của Tool Oumijia và chọn file `.edz` vừa tải.
- Tool sao chép **command ID native** từ tab Eplan Cloud ở thời điểm chạy, không gọi `XDataPortalNavigatorToggle` hay mở `DataPortalTabView` riêng. Nếu Cloud không sẵn sàng, dùng tab gốc **Eplan Cloud → Data Portal**.

Không tắt/proxy, hosts, SSL hay license để lách Data Portal.

## Gỡ cài đặt

Chạy **`Uninstall-Tool-Oumijia.bat -ListInstalled`**, rồi **`Uninstall-Tool-Oumijia.bat -TargetW3u "<đường dẫn đã liệt kê>"`** (có thể bỏ `-TargetW3u` khi chỉ có một đích). Gỡ đăng ký **chỉ đích được chọn**, sao lưu USER/STATION settings có SHA-256; **giữ nguyên** `Script`, `AddIn`, `Portal` và log trong `%APPDATA%\Tool Oumijia` vì bản EPLAN khác có thể đang dùng. Chưa có garbage collector xác minh reference count; dọn tay các file cũ chỉ sau khi xác nhận không còn bản nào dùng.

## Nhật ký

- `%APPDATA%\Tool Oumijia\logs\addin.log`
- `%APPDATA%\Tool Oumijia\logs\register-script.log`
- `%APPDATA%\Tool Oumijia\logs\import-*.csv`
- `%APPDATA%\Tool Oumijia\logs\export-*.csv`

Không đính kèm log công khai nếu có dữ liệu nội bộ; che token/đường dẫn trước khi gửi issue.
