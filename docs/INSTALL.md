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

## Cách 3 — Cài EPLAN Add-in (tùy chọn, v0.2+)

Add-in cho phép mở tool ngay trong EPLAN và (v0.2) batch import EDZ vào Parts Database.

- Yêu cầu: máy có EPLAN Platform, build từ source với `EplanApiDir` (xem [BUILD.md](BUILD.md)).
- Đăng ký: **EPLAN → Utilities → API Add-ins** → trỏ tới `Oumijia.EplanAddIn.dll`.
- Chạy action: `OumijiaOpenTool`.

## Gỡ cài đặt

- Xóa thư mục cài đặt (bản giải nén là portable, không ghi vào hệ thống).
- (Tùy chọn) xóa cấu hình: `%APPDATA%\Tool Oumijia\`.

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
Get-FileHash .\Tool-Oumijia-EPLAN-v0.1.0-win-x64.zip -Algorithm SHA256
```

(và so với giá trị ghi trong Release notes / `VERSION.txt` sau khi giải nén — hash của `Tool-Oumijia.exe`).

## Sử dụng với EPLAN Education

Tool hoạt động độc lập với license EPLAN (không đụng bản quyền). Gợi ý quy trình:

1. Trong EPLAN: **Data Portal → Import part** vào Parts Database (theo tài khoản của bạn)
2. Backup/phân loại: dùng Tool Oumijia quét các `.edz` bạn có (từ website hãng hoặc tự tạo)
3. Muốn lấy EDZ từ Parts DB của chính bạn: **Parts Management → [Extras] → Export → EDZ**
   rồi đưa vào tool để phân loại thư viện.

> Tuân thủ điều khoản sử dụng của EPLAN/Miền dữ liệu hãng: chỉ dùng cá nhân/nội bộ,
> không tái phân phối dữ liệu linh kiện.
