# Cài đặt — Tool Oumijia (EPLAN Edition) v0.5.2

> Gói gồm Add-in và Script Edition, component dùng chung và installer thông minh tự chọn bản phù hợp.

## Cài đặt nhanh

1. Tải `Tool-Oumijia-v0.5.2.zip` khi bản phát hành được duyệt.
2. Giải nén vào một thư mục bất kỳ.
3. Đóng mọi cửa sổ EPLAN.
4. Chạy **`Install-Tool-Oumijia.bat`**.
5. Installer thử bản Add-in trước; nếu EPLAN chặn DLL thì tự gỡ đăng ký Add-in và chuyển sang Script Edition.
6. Mở EPLAN và kiểm tra ribbon **Tool Oumijia**.

Ribbon v0.5.2 có đúng bốn lệnh:

```text
Nhập EDZ… | Xuất EDZ… | Data Portal… | Menu bar
```

## Hai phiên bản

| | **Add-in** | **Script Edition** |
|---|---|---|
| Công nghệ | DLL add-in (.NET 48) | EPLAN Scripting (C# 5) |
| Yêu cầu | API license hoặc EPLAN cho phép DLL chưa ký | Có scripting EPLAN; không cần API license |
| Phù hợp | EPLAN 2022–2023 hoặc máy IT cho phép | EPLAN mới chặn Add-in |
| Tính năng | Như nhau | Như nhau |

## Component trong gói

- `addin\`: Add-in + shared UI/EDZ reader + PortalHost/WebView2 dependencies.
- `script\`: Script Edition + folder `portal\` (shared runtime).
- `Install-Tool-Oumijia.bat/.ps1`, installer/uninstaller riêng cho từng edition.
- `docs\`: hướng dẫn người dùng và release note.

## Yêu cầu hệ thống

| | |
|---|---|
| OS | Windows 10/11 x64 |
| EPLAN | Platform 2022 trở lên |
| Quyền | Không cần Administrator |
| EPLAN Data Portal | Máy và tài khoản hợp lệ, được IT cho phép |
| WebView2 | Runtime chính thức của Microsoft; nếu lỗi, dùng nút **Mở ngoài** |

Không copy `.edz` vào Master Data bằng tay; file thiết bị phải được nhập vào Parts Database bằng chức năng Nhập EDZ.

## Data Portal

- Nút **Data Portal…** mở portal chính thức trong WebView2.
- Người dùng tự đăng nhập; Tool không lưu password hoặc tự đăng nhập.
- Có thể chọn/ghi nhớ thư mục tải.
- Tài liệu khác chỉ được tải về thư mục đó.
- `.edz` tải xong có thể chuyển sang **Nhập EDZ…**.
- Cookie/cache phiên nằm trong `%APPDATA%\Tool Oumijia\WebView2\`; có thể xóa từ cửa sổ Portal.

Nếu portal/WebView2 không mở:

1. nhấn **Thử lại**; hoặc
2. nhấn **Mở ngoài** để dùng trình duyệt; hoặc
3. gỡ cài/xóa dữ liệu phiên; hoặc
4. liên hệ IT/EPLAN để mở kết nối Data Portal hợp lệ.

Không tắt/proxy, hosts, SSL hay license để lách Data Portal.

## Gỡ cài đặt

Chạy **`Uninstall-Tool-Oumijia.bat`**. Script gỡ đăng ký Add-in/Script, xóa:

- `%APPDATA%\Tool Oumijia\AddIn`
- `%APPDATA%\Tool Oumijia\Script`
- `%APPDATA%\Tool Oumijia\Portal`

Log và `%APPDATA%\Tool Oumijia\WebView2` không bị xóa trong luồng gỡ mặc định nếu cần giữ profile/audit. Để xóa profile hoàn toàn, dùng nút **Xóa dữ liệu phiên** trong Portal hoặc xóa thủ công thư mục sau khi gỡ Tool.

## Nhật ký

- `%APPDATA%\Tool Oumijia\logs\addin.log`
- `%APPDATA%\Tool Oumijia\logs\register-script.log`
- `%APPDATA%\Tool Oumijia\logs\import-*.csv`
- `%APPDATA%\Tool Oumijia\logs\export-*.csv`
- `%APPDATA%\Tool Oumijia\logs\data-portal-downloads-*.csv`

Không đính kèm log công khai nếu có dữ liệu nội bộ; che token/đường dẫn trước khi gửi issue.
