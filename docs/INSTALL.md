# Cài đặt — Tool Oumijia (EPLAN Edition)

## Cách 1 — Bản phát hành sẵn (khuyến nghị)

1. Vào **[Releases](https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/latest)**, tải
   `Tool-Oumijia-EPLAN-v<version>-win-x64.zip`
2. **Giải nén toàn bộ** zip ra một thư mục (vd `D:\Tool Oumijia\`)
3. Chạy **`Tool-Oumijia.exe`**
   - Bản self-contained: **không cần cài .NET**.
   - Windows SmartScreen có thể cảnh báo lần đầu (file tải từ Internet, chưa ký số):
     bấm **More info → Run anyway**.
4. Xong! Cấu hình được tạo tự động tại `%APPDATA%\Tool Oumijia\`.

## Cách 2 — Build từ source

Xem [BUILD.md](BUILD.md).

## Cách 3 — Cài EPLAN Add-in (v0.2+)

Add-in hiển thị ribbon **"Tool Oumijia"** trong EPLAN với 3 nút **Nhập EDZ… / Xuất EDZ… / Cửa sổ Tool Oumijia**.

**Cài 1 bước** (khuyến nghị) — nếu bạn tải bản release zip:

1. Trong thư mục giải nén, mở `addin\`
2. Chạy **`Install-AddIn.bat`** — không cần quyền Administrator, không đụng license EPLAN
   - Installer copy DLL vào `%APPDATA%\Tool Oumijia\AddIn\` rồi đăng ký add-in bằng cách
     chạy EPLAN ẩn một lần (script `OumijiaRegister.cs` + action `ExecuteScript`)
3. Mở EPLAN → ribbon **"Tool Oumijia"** xuất hiện

**Gỡ cài đặt**: chạy `addin\Uninstall-AddIn.bat`.

**Phương án dự phòng** (nếu installer báo lỗi) — đăng ký thủ công trong 30 giây:

1. Mở EPLAN → **File → Extras → Interfaces → Scripts → Run…**
2. Chọn file `addin\OumijiaRegister.cs`
3. Khởi động lại EPLAN

> **Yêu cầu**: EPLAN Platform 2022 trở lên (x64). Đã kiểm chứng đầy đủ trên EPLAN Platform 2022.0.3.
> Build add-in từ source: xem [BUILD.md](BUILD.md).
> Nhật ký: `%APPDATA%\Tool Oumijia\logs\` (`register-script.log` = đăng ký, `addin.log` = khi chạy).

## Gỡ cài đặt

- Xóa thư mục cài đặt tool (bản giải nén là portable, không ghi vào hệ thống).
- Add-in EPLAN: chạy `addin\Uninstall-AddIn.bat` (hoặc xóa registration — xem BUILD.md).
- (Tùy chọn) xóa cấu hình + log: `%APPDATA%\Tool Oumijia\`.

## Yêu cầu hệ thống

| | |
|---|---|
| OS | Windows 10 / 11 (x64) |
| RAM | ~100 MB khi chạy |
| Quyền | Không cần Administrator |
| EPLAN | Không bắt buộc — chỉ cần khi muốn cài macro / import vào Parts DB |
| .NET | Không cần (bản self-contained) |

## Kiểm tra tính toàn vẹn của bản tải

Mỗi release có kèm mã **SHA-256** của file ZIP. Đối chiếu nhanh:

```powershell
Get-FileHash .\Tool-Oumijia-EPLAN-v0.2.0-win-x64.zip -Algorithm SHA256
```

(và so với giá trị ghi trong Release notes / `VERSION.txt` sau khi giải nén — hash của `Tool-Oumijia.exe`).

## Sử dụng với EPLAN Education

Tool hoạt động độc lập với license EPLAN (không đụng bản quyền). Gợi ý quy trình:

1. Trong EPLAN: **Data Portal → Import part** vào Parts Database (theo tài khoản của bạn)
2. Backup/phân loại: dùng Tool Oumijia quét các `.edz` bạn có (từ website hãng hoặc tự tạo)
3. Muốn lấy EDZ từ Parts DB của chính bạn: **Parts Management → [Extras] → Export → EDZ**
   rồi đưa vào tool để phân loại thư viện.
4. (v0.2) Ngược lại — **Nhập EDZ…** bằng ribbon "Tool Oumijia" trong EPLAN để batch import
   vào Parts Database; **Xuất EDZ…** để backup parts DB ra EDZ.

> Tuân thủ điều khoản sử dụng của EPLAN/Miền dữ liệu hãng: chỉ dùng cá nhân/nội bộ,
> không tái phân phối dữ liệu linh kiện.
