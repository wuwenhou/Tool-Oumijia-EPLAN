# Tool Oumijia — EPLAN Edition

**Add-in / Script cho EPLAN: nhập/xuất `.edz`, mở EPLAN Data Portal chính thức và quản lý file tải về ngay trong EPLAN.**

> **Mốc phát triển hiện tại: v0.5.2.** Phiên bản này thêm Data Portal và làm lại giao diện Nhập EDZ. Nguồn và gói local được build từ `main`; người dùng kiểm tra trên database test trước khi phát hành/UAT.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-informational)

## Hai edition — Add-in hay Script?

| | **Add-in** | **Script Edition** |
|---|---|---|
| Công nghệ | DLL add-in (net48) | EPLAN Scripting (C# 5) |
| Yêu cầu | EPLAN có API license hoặc cho phép DLL chưa ký (vd 2022) | Không cần API license |
| Phù hợp | EPLAN 2022–2023 / có quyền IT | EPLAN mới chặn Add-in (check IT trước khi chạy) |
| Tính năng | Như nhau | Như nhau |

Chạy **`Install-Tool-Oumijia.bat`**; installer tự thử Add-in và chuyển sang Script nếu EPLAN chặn DLL.

## Tính năng (v0.5.2)

### Nhập EDZ — đồng bộ với Xuất
- Chọn nhiều tệp/thư mục, quét thư mục con.
- Bề mặt hai cột giống hộp Xuất:
  - bên trái danh sách EDZ;
  - bên phải chi tiết file và **lưới part trong EDZ**.
- Lưới gồm đúng **Tên thiết bị | Hãng | Mã hàng**; chọn dòng để focus part.
- EDZ lỗi không làm hỏng danh sách; Tool hiển thị lý do và vẫn dùng API nhập.
- Mặc định: cập nhật + thêm mới; nhận dạng theo mã hàng; tự dò field-mapping scheme.

### Xuất EDZ — giữ nguyên v0.5.1
- Cây nhóm kiểu navigator EPLAN; checkbox native; dòng đã chọn in đậm.
- Panel chi tiết như EPLAN Overview; nháy đúp để copy.
- Xuất một file EDZ hoặc mỗi part một file; tên file tự động theo mã sản phẩm.
- Chế độ **Theo loại bản ghi (như EPLAN)** cho Parts, Accessory list/placement, Drilling pattern, Connection point pattern, Customer/Manufacturer/Supplier.

### Data Portal
- Nút **Data Portal…** trong ribbon Tool Oumijia.
- Mở EPLAN Data Portal **chính thức** trong WebView2.
- Người dùng tự đăng nhập bằng tài khoản hợp lệ; Tool không lưu password hoặc tự đăng nhập.
- Chọn/ghi nhớ thư mục tải; file `.edz` tải xong có thể đưa sang hộp Nhập.
- Nút **Mở ngoài** dùng khi WebView2 thiếu/lỗi; không bypass license/captcha/điều khoản.

### Ribbon
```text
Nhập EDZ… | Xuất EDZ… | Data Portal… | Menu bar
```

## Bắt đầu nhanh

1. Tải `Tool-Oumijia-v0.5.2.zip` khi bản phát hành được duyệt; hoặc build theo [docs/BUILD.md](docs/BUILD.md).
2. Đóng EPLAN → chạy `Install-Tool-Oumijia.bat`.
3. Mở EPLAN → ribbon **Tool Oumijia**.
4. Test trên database test trước khi nhập database chính.

Chi tiết: [docs/INSTALL.md](docs/INSTALL.md) · [docs/USER-GUIDE-vi.md](docs/USER-GUIDE-vi.md) · [docs/PLAN-v0.5.2.md](docs/PLAN-v0.5.2.md)

## Build từ source

```powershell
dotnet build src/Oumijia.EplanAddIn/Oumijia.EplanAddIn.csproj -c Release `
  -p:EplanApiDir="C:\Program Files\EPLAN\Platform\2022.0.3\Bin"

powershell -ExecutionPolicy Bypass -File tools/pack.ps1 -Version 0.5.2
```

Packaging gồm Add-in, Script Edition, shared Portal/UI/EDZ reader, Playbook installers và docs. Xem [docs/BUILD.md](docs/BUILD.md).

## Cấu trúc repo

```text
src/Oumijia.EplanAddIn/   ribbon + actions (net48, x64)
src/Oumijia.EplanUi/      UI dùng chung cho cả hai editions
src/Oumijia.PortalHost/   Data Portal WebView2 dùng chung
src/Oumijia.EdzReader/     đọc metadata EDZ cục bộ
tools/                    script-edition, installers, registration, pack.ps1
docs/                     thiết kế, build, cài đặt, hướng dẫn, release notes
```

## Nguyên tắc

- Chỉ API chính thức và dữ liệu factual.
- Không bypass license/captcha/điều khoản.
- Không tự đăng nhập hoặc lưu credential.
- Không upload nội dung `.edz` lên dịch vụ bên ngoài.
- Tính năng Data Portal cần IT cho phép và tài khoản hợp lệ.

## Đóng góp & AI agents

Đọc [AGENTS.md](AGENTS.md) trước khi sửa code. Mọi thay đổi phải build/compile kiểm tra và cập nhật changelog/docs.

## License

[MIT](LICENSE) © 2026 wuwenhou (Oumijia).
