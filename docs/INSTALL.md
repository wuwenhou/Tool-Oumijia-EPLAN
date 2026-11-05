# Cài đặt — Tool Oumijia (EPLAN Edition) v0.6.2

> Gói phát hành chứa **2 phiên bản Tool**, **Installer thông minh** và **2 add-in VEE của tác giả** (đã được cấp phép phân phối).

## Cách cài (1 bước — khuyến nghị)

1. Vào **[Releases](https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/latest)**, tải
   `Tool-Oumijia-v0.6.2.zip`
2. **Giải nén** ra một thư mục bất kỳ → chạy **`Install-Tool-Oumijia.bat`**
   - Installer tự thử bản **add-in** trước (đăng ký + self-test headless)
   - Nếu EPLAN **vô hiệu hóa add-in** (máy không có API license trên EPLAN bản mới):
     tự gỡ add-in → **chuyển cài bản script** → kiểm chứng lại rồi mới báo thành công
   - Đồng thời tự cài và đăng ký **VEE.EplTableMaker** + **VEE.ExportConnections**;
     không cần dùng hộp "API modules (assemblies) > Load"
3. Mở EPLAN → ribbon **"Tool Oumijia"** xuất hiện với 3 nút: **Nhập EDZ… / Xuất EDZ… / Menu bar**.
   Sẽ có thêm tab **VIET EPLAN ELECTRIC** nếu hai add-in VEE đăng ký thành công.

## Hai phiên bản — khi nào dùng bản nào

| | **Bản ADD-IN** (`addin\`) | **Bản SCRIPT** (`script\`) |
|---|---|---|
| Công nghệ | DLL add-in (net48) | EPLAN Scripting (C# 5) |
| Yêu cầu | EPLAN có **API license**, HOẶC bản EPLAN cho phép DLL chưa ký (vd 2022) | **Không cần license** (scripting miễn phí) |
| Phù hợp | EPLAN 2022–2023 | **EPLAN mới (2024+/2026)** chặn add-in chưa ký |
| Tính năng | Như nhau (Nhập/Xuất EDZ + Menu bar) | Như nhau |

**Cài thủ công** (nếu không dùng installer thông minh):

- Bản add-in: chạy `addin\Install-AddIn.bat`
- Bản script: chạy `script\Install-Script.bat`

## Yêu cầu hệ thống

| | |
|---|---|
| OS | Windows 10 / 11 (x64) |
| EPLAN | **Platform 2022 trở lên** — add-in đã kiểm chứng trên 2022.0.3; script đã kiểm chứng trên 2026.0.3 |
| Quyền | Không cần Administrator |
| .NET | Không cần cài (add-in chạy trên .NET Framework của EPLAN; script do EPLAN tự biên dịch) |

## Kiểm tra tính toàn vẹn

Mỗi release kèm mã **SHA-256** của file ZIP. Đối chiếu nhanh:

```powershell
Get-FileHash .\Tool-Oumijia-v0.6.2.zip -Algorithm SHA256
```

(`VERSION.txt` trong gói ghi hash của DLL add-in và file script chính.)

## Gỡ cài đặt

Chạy **`Uninstall-Tool-Oumijia.bat`** — gỡ cả 2 bản (nếu có), xóa các thư mục
`%APPDATA%\Tool Oumijia\AddIn` và `%APPDATA%\Tool Oumijia\Script`.

(Tùy chọn) xóa log/cấu hình: `%APPDATA%\Tool Oumijia\`.

## Phương án dự phòng (nếu installer thông minh báo lỗi)

- **Bản add-in**: copy `addin\Oumijia.EplAddin.EDZ.dll` vào `%APPDATA%\Tool Oumijia\AddIn\`,
  rồi trong EPLAN: **File → Extras → Interfaces → Scripts → Run…** chọn `addin\OumijiaRegister.cs`.
- **Bản script**: copy `script\ToolOumijiaScript.cs` + `script\OumijiaRegisterScript.cs` vào
  `%APPDATA%\Tool Oumijia\Script\`, rồi chạy `OumijiaRegisterScript.cs` qua Scripts → Run… như trên.
- **2 add-in VEE**: chạy installer ở gốc, hoặc chạy `third-party\OumijiaRegisterThirdParty.cs`
  qua Scripts → Run… như trên.
- Khởi động lại EPLAN sau khi đăng ký.

## Sử dụng với EPLAN Education

Tool hoạt động độc lập với license EPLAN:
- Máy EPLAN cũ (2022): dùng bản **add-in**.
- Máy EPLAN mới + không API license: dùng bản **script** (EPLAN Scripting không cần license).

Gợi ý quy trình:

1. Trong EPLAN: **Data Portal → Import part** vào Parts Database (theo tài khoản của bạn)
2. Backup/chia sẻ: **Xuất EDZ…** (chọn part trên cây) hoặc xuất theo loại bản ghi
3. Máy khác: **Nhập EDZ…** lô `.edz` vào Parts Database (thử trên **database test** trước)

> Tuân thủ điều khoản sử dụng của EPLAN/Miền dữ liệu hãng: chỉ dùng cá nhân/nội bộ,
> không tái phân phối dữ liệu linh kiện.

## Nhật ký & hỗ trợ

- Log: `%APPDATA%\Tool Oumijia\logs\`
  (`register-script.log` = đăng ký — dòng `[SCRIPT]` là bản script; `addin.log` = hoạt động;
  `import-*.csv` / `export-*.csv` = kiểm toán)
- Lỗi compile script của EPLAN (nếu có): `C:\Users\Public\EPLAN\Electric P8\EplLog.eldb`
- Gặp lỗi: mở issue kèm nội dung log tương ứng.
