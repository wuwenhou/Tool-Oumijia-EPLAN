# Tool Oumijia — EPLAN Edition

**Add-in / Script cho EPLAN: nhập/xuất `.edz`, mở Data Portal native của EPLAN và quản lý dữ liệu linh kiện ngay trong EPLAN.**

> **v0.7.0 là pre-release (2026-11-09).** Nhập EDZ trên EPLAN Electric P8 2022.0.3 đã GUI UAT (checkbox từng part, layout đồng bộ); **nhập thật vào Parts Database chưa được test** — chỉ dùng database test. Script Edition 2026 được build lại nhưng **chưa GUI UAT trên Education 2026.0.3**; nếu bạn cần ổn định trên 2026, dùng bản v0.6.0-2026. Chi tiết: [docs/releases/v0.7.0.md](docs/releases/v0.7.0.md) · [docs/PLAN-v0.7.0.md](docs/PLAN-v0.7.0.md).

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-informational)

## Hai edition — Add-in hay Script?

| | **Add-in** | **Script Edition** |
|---|---|---|
| Công nghệ | DLL add-in (net48) | EPLAN Scripting (C# 5) |
| Yêu cầu | EPLAN có API license hoặc cho phép DLL chưa ký (vd 2022) | Không cần API license |
| Trạng thái source mới | Compile-check 2026, chưa deploy/UAT | GUI UAT Education 2026.0.3; bản khác chưa UAT |

Installer candidate cho `-ListInstalled` và `-TargetW3u`; chỉ chọn Script Education 2026.0.3, **chưa UAT cài headless**, không tự dùng DLL 2026 cho bản khác. Xem [cài đặt](docs/INSTALL.md).

## Tính năng (pre-release v0.7.0; khác v0.6.0)

### Nhập EDZ — đồng bộ với Xuất
- Chọn nhiều tệp/thư mục, quét thư mục con.
- **Tick/untick từng part** ngay trên cây: tick gói = chọn hết part trong gói; bỏ tick 1 part = loại nó; nút **Chọn hết / Bỏ chọn**; status `Chọn: X/Y part`; chỉ các part đã tick mới được gửi cho `ImportParts`.
- Khi chỉ chọn một phần gói, Tool tự sinh EDZ tạm (`manifest` đã lọc + các file được tham chiếu, writer 7z nội bộ) rồi tự xóa sau khi nạp.
- Bề mặt hai cột giống hộp Xuất:
  - bên trái cây **gói EDZ → part**;
  - bên phải chỉ có **Chi tiết** và bảng **Thuộc tính | Giá trị**.
- Tên part hiển thị `PARTNR (Tên thiết bị)` (dọn chuỗi đa ngôn ngữ); tooltip giữ nguyên văn bản.
- Chọn node gói/part để xem metadata đọc cục bộ; double-click giá trị để copy.
- EDZ lỗi không làm hỏng danh sách; Tool hiển thị lý do và vẫn dùng API nhập.
- Mặc định: cập nhật + thêm mới; nhận dạng theo mã hàng; tự dò field-mapping scheme.

### Xuất EDZ — giữ nguyên v0.5.1
- Cây nhóm kiểu navigator EPLAN; checkbox native; dòng đã chọn in đậm.
- Panel chi tiết như EPLAN Overview; nháy đúp để copy.
- Xuất một file EDZ hoặc mỗi part một file; tên file tự động theo mã sản phẩm.
- Chế độ **Theo loại bản ghi (như EPLAN)** cho Parts, Accessory list/placement, Drilling pattern, Connection point pattern, Customer/Manufacturer/Supplier.

### Data Portal
- Nút **Data Portal** trong nhóm **Eplan Cloud** của tab Tool Oumijia; nhóm **Quản lý EDZ** không còn nút Portal trùng.
- Clone command native bằng ID tìm từ tab Eplan Cloud gốc lúc chạy; GUI UAT kéo/đóng/mở lại PASS trên Education 2026. Phiên do EPLAN quản lý.
- Không có WebView2/profile riêng của Tool; EPLAN native có thể dùng WebView2 nội bộ.
- Tải `.edz` theo quyền tài khoản, sau đó dùng **Nhập EDZ** của Tool để nhập vào Parts Database.
- Nếu lệnh Cloud chưa sẵn sàng, mở trực tiếp **Eplan Cloud → Data Portal** ở tab gốc.

### Ribbon
```text
Quản lý EDZ: Nhập EDZ → Xuất EDZ → Menu bar
Eplan Cloud: bảy lệnh native, gồm một Data Portal
```

## Bắt đầu nhanh

1. Chỉ dùng gói **pre-release v0.7.0** trên máy thử nghiệm chấp nhận rủi ro; bản v0.6.0 giữ nguyên như lần phát hành trước. Build theo [docs/BUILD.md](docs/BUILD.md).
2. Đóng EPLAN → `Install-Tool-Oumijia.bat -ListInstalled`, chọn target qua `-TargetW3u` khi có nhiều bản.
3. Mở EPLAN → ribbon **Tool Oumijia**.
4. Test trên database test trước khi nhập database chính.

Chi tiết: [docs/INSTALL.md](docs/INSTALL.md) · [docs/USER-GUIDE-vi.md](docs/USER-GUIDE-vi.md) · [CHANGELOG.md](CHANGELOG.md)

## Build từ source

```powershell
dotnet build src/Oumijia.EplanAddIn/Oumijia.EplanAddIn.csproj -c Release `
  -p:EplanApiDir="C:\Program Files\EPLAN\Platform\2022.0.3\Bin"

# Chỉ sau khi build mới, bổ sung release notes và đủ UAT:
powershell -ExecutionPolicy Bypass -File tools/pack.ps1 -Version X.Y.Z
```

Packaging gồm Add-in, Script Edition, shared UI/EDZ reader, installer và docs. Không còn PortalHost/WebView2. Xem [docs/BUILD.md](docs/BUILD.md).

## Cấu trúc repo

```text
src/Oumijia.EplanAddIn/   ribbon + actions (net48, x64)
src/Oumijia.EplanUi/      UI dùng chung cho cả hai editions
src/Oumijia.EdzReader/     đọc metadata EDZ cục bộ
tools/                    script-edition, installers, registration, pack.ps1
docs/                     thiết kế, build, cài đặt, hướng dẫn, release notes
```

## Nguyên tắc

- Chỉ API chính thức và dữ liệu factual.
- Không bypass license/captcha/điều khoản.
- Không tự đăng nhập hoặc lưu credential.
- Không upload nội dung `.edz` lên dịch vụ bên ngoài.
- Data Portal dùng tài khoản EPLAN hợp lệ và quyền do EPLAN/IT cấp.

## Đóng góp & AI agents

Đọc [AGENTS.md](AGENTS.md) và [bàn giao chi tiết](docs/AGENT-HANDOFF-2026-11-07.md) trước khi sửa code. Mọi thay đổi phải build/compile kiểm tra và cập nhật changelog/docs.

## License

[MIT](LICENSE) © 2026 wuwenhou (Oumijia).
