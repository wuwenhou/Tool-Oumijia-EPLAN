# Cài đặt — Tool Oumijia (EPLAN Edition) v0.3.0

> v0.3.0 là **add-in EPLAN** — cài một lần, dùng ngay trong EPLAN. Không còn app exe.

## Cách cài (1 bước — khuyến nghị)

1. Vào **[Releases](https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/latest)**, tải
   `Tool-Oumijia-AddIn-v0.3.0.zip` (~26 KB)
2. **Giải nén** ra một thư mục bất kỳ (vd `D:\Tool Oumijia\`)
3. **Tắt EPLAN** (khuyến nghị) rồi chạy **`Install-AddIn.bat`**
   - Installer copy DLL vào `%APPDATA%\Tool Oumijia\AddIn\` và đăng ký add-in trong EPLAN
     bằng cách chạy EPLAN ẩn một lần (script `OumijiaRegister.cs` + action `ExecuteScript`)
   - **Không cần quyền Administrator**; **không đụng gì tới license EPLAN**
     (dùng chức năng scripting miễn phí)
4. Mở EPLAN → ribbon **"Tool Oumijia"** xuất hiện với 2 nút: **Nhập EDZ… / Xuất EDZ…**

## Yêu cầu hệ thống

| | |
|---|---|
| OS | Windows 10 / 11 (x64) |
| EPLAN | **Platform 2022 trở lên** — đã kiểm chứng đầy đủ trên 2022.0.3 |
| Quyền | Không cần Administrator |
| .NET | Không cần cài (add-in chạy trên .NET Framework có sẵn của EPLAN) |

## Kiểm tra tính toàn vẹn

Mỗi release kèm mã **SHA-256** của file ZIP. Đối chiếu nhanh:

```powershell
Get-FileHash .\Tool-Oumijia-AddIn-v0.3.0.zip -Algorithm SHA256
```

(và so với giá trị ghi trong Release notes / `VERSION.txt` sau khi giải nén — hash của DLL).

## Gỡ cài đặt

Chạy **`Uninstall-AddIn.bat`** (trong thư mục đã giải nén) — gỡ registration khỏi EPLAN
và xóa DLL trong `%APPDATA%\Tool Oumijia\AddIn\`.

(Tùy chọn) xóa log/cấu hình: `%APPDATA%\Tool Oumijia\`.

## Phương án dự phòng (nếu Install-AddIn.bat báo lỗi)

Đăng ký thủ công trong 30 giây:

1. Trong thư mục giải nén: copy `Oumijia.EplAddin.EDZ.dll` vào `%APPDATA%\Tool Oumijia\AddIn\`
   (tạo thư mục nếu chưa có)
2. Mở EPLAN → **File → Extras → Interfaces → Scripts → Run…** → chọn file **`OumijiaRegister.cs`**
   (nằm cạnh file này)
3. Khởi động lại EPLAN

## Sử dụng với EPLAN Education

Add-in hoạt động độc lập với license EPLAN (không đụng bản quyền). Gợi ý quy trình:

1. Trong EPLAN: **Data Portal → Import part** vào Parts Database (theo tài khoản của bạn)
2. Muốn chia sẻ/backup: dùng add-in **Xuất EDZ…** để lấy parts ra `.edz` (chọn theo cây nhóm)
3. Trên máy khác: dùng add-in **Nhập EDZ…** để nhập lô `.edz` vào Parts Database
   (thử trên **database test** trước với chế độ *Chỉ thêm mới*)

> Tuân thủ điều khoản sử dụng của EPLAN/Miền dữ liệu hãng: chỉ dùng cá nhân/nội bộ,
> không tái phân phối dữ liệu linh kiện.

## Nhật ký & hỗ trợ

- Log: `%APPDATA%\Tool Oumijia\logs\`
  (`register-script.log` = đăng ký; `addin.log` = hoạt động; `import-*.csv`/`export-*.csv` = kiểm toán)
- Gặp lỗi: mở issue kèm nội dung `addin.log` (và `register-script.log` nếu lỗi cài đặt).
