# Tool Oumijia — EPLAN Edition

**Trợ lý quản lý gói dữ liệu linh kiện EPLAN (`.edz`) & macro (`.ema/.emp/.ems`) — quét, phân loại, xếp thư viện và cài đặt, trong một ứng dụng Windows gọn nhẹ.**

[![CI](https://github.com/wuwenhou/Tool-Oumijia-EPLAN/actions/workflows/ci.yml/badge.svg)](https://github.com/wuwenhou/Tool-Oumijia-EPLAN/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/wuwenhou/Tool-Oumijia-EPLAN)](https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-informational)

> 🇬🇧 **English overview** — *Tool Oumijia (EPLAN Edition)* is a Windows toolkit that scans folders for EPLAN part packages (`.edz`) and macros (`.ema/.emp/.ems`), reads their part metadata (manufacturer, part number, product group), classifies them with editable rules, files them into a clean library tree (`Manufacturer/Category/…`), installs macros into the EPLAN master-data folder, and (roadmap) batch-imports EDZ packages into the EPLAN parts database via the official API. Not affiliated with EPLAN. See [docs/](docs/) for details; AI agents continuing this project should read [AGENTS.md](AGENTS.md).

---

## Tính năng (v0.1.0)

### 📦 Xuất (Export) — phân loại thư viện
- Quét một hoặc nhiều thư mục "inbox" tìm file `.edz` + `.ema`
- Đọc metadata **không cần EPLAN** (parser riêng, hỗ trợ cả container **ZIP lẫn 7z**)
- **Bảng chọn** trực quan: chọn/bỏ từng file, xem Hãng / Nhóm / Mã hàng / đích đến, sửa đích đến ngay trên bảng
- **Rule engine** (`rules.json`) tự phân loại: `MCB`, `MCCB`, `RCD`, `Contactor`, `Relay`, `MotorProtection`, `PLC`, `HMI`, `PSU`, `Terminal`, `Cable`, `Sensor`, `Drive`, `Enclosure`… hơn 25 nhóm
- Xếp file vào cây `Tool Oumijia/Import/<Hãng>/<Nhóm>/<file>` với chính sách trùng tên an toàn (mặc định: **không ghi đè**)
- **Log CSV** đầy đủ (nguồn, đích, hành động, SHA-256) + ghi nhớ các đích đến bạn sửa tay

### 🧩 Nhập (Import)
- **Macro** `.ema/.emp/.ems` → cài vào **thư mục master-data macro của EPLAN** (tự dò đường dẫn, có thể chọn tay), khởi động lại EPLAN là dùng được
- **EDZ → Parts Database**: gom danh sách, kiểm tra, một nút copy để nhập trong EPLAN (`Master data → Parts → Management → [Extras] → Import`) — **batch import tự động qua EPLAN API sẽ có ở v0.2** (add-in đã được scaffold trong repo)

### ⚙️ Khác
- Toàn bộ cấu hình trong `%APPDATA%\Tool Oumijia\` (`config.json`, `rules.json`, `overrides.json`, `logs\`)
- Không cần cài đặt, không cần quyền admin, không đụng gì tới license của EPLAN

```
┌─ Tool Oumijia — Export: tìm thấy 12 file ───────────────────────────────┐
│ [✓] ABB_S201-C16.edz      ABB        MCB    → …/Import/ABB/MCB           │
│ [✓] Siemens_S7-1200…edz   Siemens    PLC    → …/Import/Siemens/PLC       │
│ [✓] Phoenix_QUINT…edz     Phoenix C. PSU    → …/Import/Phoenix Contact/PSU│
│ [ ] motor_3ph.ema         -          -      → …/Import/_Macros           │
└──────────────────────────────────────────────────────────────────────────┘
```

## Bắt đầu nhanh

1. Tải **`Tool-Oumijia-EPLAN-v0.1.0-win-x64.zip`** tại [Releases](https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/latest)
2. Giải nén → chạy **`Tool-Oumijia.exe`** (bản self-contained, không cần cài .NET)
3. Tab **Xuất**: thêm thư mục chứa `.edz` → **Quét ngay** → kiểm tra bảng → **XUẤT ▸**
4. Tab **Nhập**: cài macro `.ema` vào EPLAN, hoặc gom danh sách EDZ để nhập

Chi tiết đầy đủ: [docs/INSTALL.md](docs/INSTALL.md) · [docs/USER-GUIDE-vi.md](docs/USER-GUIDE-vi.md)

## Build từ source

```powershell
git clone https://github.com/wuwenhou/Tool-Oumijia-EPLAN.git
cd Tool-Oumijia-EPLAN
dotnet build Tool-Oumijia-EPLAN.sln -c Release
dotnet test  Tool-Oumijia-EPLAN.sln -c Release

# tạo gói phát hành (self-contained zip)
powershell -ExecutionPolicy Bypass -File tools/pack.ps1 -Version 0.1.0
```

**EPLAN Add-in** (`src/Oumijia.EplanAddIn`) cần máy có cài EPLAN để biên dịch — xem [docs/BUILD.md](docs/BUILD.md).

## Cấu trúc repo

```
Tool-Oumijia-EPLAN/
├─ src/
│  ├─ Oumijia.Core/          # thư viện lõi (netstandard2.0): đọc EDZ, classifier, thư viện, macro
│  ├─ Oumijia.Tool/          # ứng dụng WinForms (net8.0-windows + net48 cho add-in)
│  └─ Oumijia.EplanAddIn/    # EPLAN add-in (net48, cần EPLAN API để build)
├─ tests/Oumijia.Core.Tests/ # 25+ unit & integration tests
├─ examples/                 # file EDZ mẫu tổng hợp (giả lập) để thử ngay
├─ tools/                    # pack.ps1, make-samples.ps1
└─ docs/                     # kiến trúc, build, hướng dẫn, lộ trình
```

## Định hướng (Roadmap)

- **v0.2** — Add-in EPLAN: batch import EDZ vào Parts DB qua API chính thức; nút ribbon; báo cáo kết quả từng gói
- **v0.3** — Editor/creator EDZ (tạo gói từ CSV, gắn macro/PDF/STEP); xuất từ Parts DB
- **v0.4+** — Chuyển đổi mở rộng (QElectroTech, DXF, AutoCAD) — xem [docs/ROADMAP.md](docs/ROADMAP.md)

## Ghi chú pháp lý

- Dự án **không liên kết** với EPLAN GmbH & Co. KG; chỉ dùng định dạng file trao đổi và API chính thức.
- Dữ liệu linh kiện trong các gói `.edz` thuộc bản quyền của các hãng sản xuất / EPLAN — **chỉ dùng cho mục đích cá nhân/nội bộ**, không phân phối lại.
- Công cụ chỉ phân tích **dữ liệu factual** (mã hàng, tên, nhóm sản phẩm…) và **không tái tạo** hình vẽ/macro độc quyền.

## Đóng góp & AI agents

Mọi đóng góp đều hoan nghênh — mở issue/PR. Nếu bạn là **AI agent** tiếp tục dự án này, hãy đọc trước [**AGENTS.md**](AGENTS.md): nó chứa bối cảnh, quy ước, ranh giới và danh sách việc tiếp theo.

## License

[MIT](LICENSE) © 2026 wuwenhou (Oumijia). Đi kèm [SharpCompress](https://github.com/adamhathcock/sharpcompress) (MIT).
