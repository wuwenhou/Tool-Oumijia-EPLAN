# Tool Oumijia — EPLAN Edition

**Trợ lý quản lý gói dữ liệu linh kiện EPLAN (`.edz`) — quét, phân loại, xếp thư viện, và nhập/xuất Parts Database trực tiếp trong EPLAN qua add-in chính thức.**

[![CI](https://github.com/wuwenhou/Tool-Oumijia-EPLAN/actions/workflows/ci.yml/badge.svg)](https://github.com/wuwenhou/Tool-Oumijia-EPLAN/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/wuwenhou/Tool-Oumijia-EPLAN)](https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-informational)

> 🇬🇧 **English overview** — *Tool Oumijia (EPLAN Edition)* is a Windows toolkit for EPLAN part packages (`.edz`): a standalone app that reads part metadata (manufacturer, part number, product group), classifies packages with editable rules and files them into a clean library tree (`Manufacturer/Category/…`), plus a real EPLAN add-in (ribbon "Tool Oumijia") that batch-imports EDZ packages into the EPLAN parts database and exports parts back to EDZ — both via the official EPLAN API. Not affiliated with EPLAN. See [docs/](docs/) for details; AI agents continuing this project should read [AGENTS.md](AGENTS.md).

---

## Tính năng (v0.2.0)

### 📦 Xuất (Export) — phân loại thư viện
- Quét một hoặc nhiều thư mục "inbox" tìm file `.edz` (chỉ EDZ từ v0.2.0)
- Đọc metadata **không cần EPLAN** (parser riêng, hỗ trợ cả container **ZIP lẫn 7z**)
- **Bảng chọn** trực quan: chọn/bỏ từng file, xem Hãng / Nhóm / Mã hàng / đích đến, sửa đích đến ngay trên bảng
- **Rule engine** (`rules.json`) tự phân loại: `MCB`, `MCCB`, `RCD`, `Contactor`, `Relay`, `MotorProtection`, `PLC`, `HMI`, `PSU`, `Terminal`, `Cable`, `Sensor`, `Drive`, `Enclosure`… hơn 25 nhóm
- Xếp file vào cây `Tool Oumijia/Import/<Hãng>/<Nhóm>/<file>` với chính sách trùng tên an toàn (mặc định: **không ghi đè**)
- **Log CSV** đầy đủ (nguồn, đích, hành động, SHA-256) + ghi nhớ các đích đến bạn sửa tay

### 🧩 Add-in EPLAN — nhập/xuất EDZ trực tiếp trong EPLAN ⭐ (v0.2.0)
- Ribbon **"Tool Oumijia"** với 3 nút:
  - **Nhập EDZ…** — chọn file/folder → **nhập HÀNG LOẠT** vào Parts Database qua API chính thức
    của EPLAN (`MDPartsManagement.ImportParts`, converter `IXPartsImportExportEdz`);
    chọn chế độ (chỉ thêm mới / chỉ cập nhật / cập nhật + thêm mới) và cách nhận dạng part
  - **Xuất EDZ…** — xuất parts ra EDZ qua `MDPartsDatabase.ExportParts`
    (toàn bộ database hoặc phần đang chọn; 1 file duy nhất hoặc mỗi part 1 file)
  - **Cửa sổ Tool Oumijia** — mở app standalone
- Cài đặt 1 click: chạy **`addin\Install-AddIn.bat`** (không cần admin, không cần biên dịch) —
  EPLAN tự nạp add-in và hiển thị ribbon; **đã kiểm chứng end-to-end trên EPLAN Platform 2022.0.3**
  (nạp, tạo ribbon, gọi action từ command line, thoát sạch)

### ⚙️ Khác
- Toàn bộ cấu hình trong `%APPDATA%\Tool Oumijia\` (`config.json`, `rules.json`, `overrides.json`, `logs\`)
- Không cần cài đặt, không cần quyền admin, không đụng gì tới license của EPLAN

```
┌─ Tool Oumijia — Export: tìm thấy 12 file ───────────────────────────────┐
│ [✓] ABB_S201-C16.edz      ABB        MCB    → …/Import/ABB/MCB           │
│ [✓] Siemens_S7-1200…edz   Siemens    PLC    → …/Import/Siemens/PLC       │
│ [✓] Phoenix_QUINT…edz     Phoenix C. PSU    → …/Import/Phoenix Contact/PSU│
└──────────────────────────────────────────────────────────────────────────┘
```

## Bắt đầu nhanh

1. Tải **`Tool-Oumijia-EPLAN-v0.2.0-win-x64.zip`** tại [Releases](https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/latest)
2. Giải nén → chạy **`Tool-Oumijia.exe`** (bản self-contained, không cần cài .NET)
3. Tab **Xuất**: thêm thư mục chứa `.edz` → **Quét ngay** → kiểm tra bảng → **XUẤT ▸**
4. **Add-in EPLAN**: chạy **`addin\Install-AddIn.bat`** → mở EPLAN → ribbon "Tool Oumijia"
   → **Nhập EDZ…** (hàng loạt) / **Xuất EDZ…** / **Cửa sổ Tool**

Chi tiết đầy đủ: [docs/INSTALL.md](docs/INSTALL.md) · [docs/USER-GUIDE-vi.md](docs/USER-GUIDE-vi.md)

## Build từ source

```powershell
git clone https://github.com/wuwenhou/Tool-Oumijia-EPLAN.git
cd Tool-Oumijia-EPLAN
dotnet build Tool-Oumijia-EPLAN.sln -c Release
dotnet test  Tool-Oumijia-EPLAN.sln -c Release

# tạo gói phát hành (self-contained zip; kèm addin\ nếu add-in đã build)
powershell -ExecutionPolicy Bypass -File tools/pack.ps1 -Version 0.2.0
```

**EPLAN Add-in** (`src/Oumijia.EplanAddIn`) cần máy có cài EPLAN để biên dịch — xem [docs/BUILD.md](docs/BUILD.md).

## Cấu trúc repo

```
Tool-Oumijia-EPLAN/
├─ src/
│  ├─ Oumijia.Core/          # thư viện lõi (netstandard2.0): đọc EDZ, classifier, library writer
│  ├─ Oumijia.Tool/          # ứng dụng WinForms (net8.0-windows + net48 cho add-in)
│  ├─ Oumijia.EplanAddIn/    # EPLAN add-in (net48, cần EPLAN API để build)
│  └─ Oumijia.Register/      # tiện ích đăng ký (status/experimental, xem AGENTS.md)
├─ tests/Oumijia.Core.Tests/ # 25+ unit & integration tests
├─ examples/                 # file EDZ mẫu tổng hợp (giả lập) để thử ngay
├─ tools/                    # pack.ps1, make-samples.ps1, eplan-scripts\, install-addin\
└─ docs/                     # kiến trúc, build, hướng dẫn, lộ trình
```

## Định hướng (Roadmap)

- **v0.2.0** ✅ — Add-in EPLAN: batch import/export EDZ qua API chính thức + cài 1 click
- **v0.3** — Editor/creator EDZ (tạo gói từ CSV, gắn macro/PDF/STEP); xuất từ Parts DB nâng cao
- **v0.4+** — Chuyển đổi mở rộng (QElectroTech, DXF, AutoCAD) — xem [docs/ROADMAP.md](docs/ROADMAP.md)

## Ghi chú pháp lý

- Dự án **không liên kết** với EPLAN GmbH & Co. KG; chỉ dùng định dạng file trao đổi và API chính thức.
- Dữ liệu linh kiện trong các gói `.edz` thuộc bản quyền của các hãng sản xuất / EPLAN — **chỉ dùng cho mục đích cá nhân/nội bộ**, không phân phối lại.
- Công cụ chỉ phân tích **dữ liệu factual** (mã hàng, tên, nhóm sản phẩm…) và **không tái tạo** hình vẽ/macro độc quyền.

## Đóng góp & AI agents

Mọi đóng góp đều hoan nghênh — mở issue/PR. Nếu bạn là **AI agent** tiếp tục dự án này, hãy đọc trước [**AGENTS.md**](AGENTS.md): nó chứa bối cảnh, quy ước, ranh giới và danh sách việc tiếp theo.

## License

[MIT](LICENSE) © 2026 wuwenhou (Oumijia). Đi kèm [SharpCompress](https://github.com/adamhathcock/sharpcompress) (MIT).
