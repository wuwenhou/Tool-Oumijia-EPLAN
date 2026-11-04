# Tool Oumijia — EPLAN Edition

**Add-in EPLAN: nhập/xuất gói dữ liệu linh kiện (`.edz`) trực tiếp trong EPLAN Electric P8 — batch import vào Parts Database, và export theo cây nhóm như navigator.**

[![Release](https://img.shields.io/github/v/release/wuwenhou/Tool-Oumijia-EPLAN)](https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-informational)

> 🇬🇧 **English overview** — *Tool Oumijia (EPLAN Edition)* is an EPLAN add-in (ribbon **"Tool Oumijia"**) that batch-imports EDZ part packages into the parts database and exports selected parts back to EDZ — both through the official EPLAN API (`MDPartsManagement.ImportParts` / `MDPartsDatabase.ExportParts`, converter `IXPartsImportExportEdz`). The export dialog lets you pick parts from a product-group tree (like the EPLAN navigator) with tri-state checkboxes and search. One-click installer, no external application. Not affiliated with EPLAN. AI agents continuing this project should read [AGENTS.md](AGENTS.md).

---

## Tính năng (v0.4.0)

### 📥 Nhập EDZ… (Import) — đơn giản
- Chọn **nhiều file hoặc cả thư mục** `.edz` (quét cả thư mục con) → bấm **NHẬP**
- Mặc định hợp lý (cố định, không cần cấu hình): *cập nhật part đã có + thêm part mới*,
  nhận dạng theo *mã hàng*
- Batch import qua API chính thức của EPLAN (`IXPartsImportExportEdz`) + log CSV kiểm toán

### 📤 Xuất EDZ… (Export) — chọn part như EPLAN
- **Cây nhóm kiểu navigator EPLAN**: Nhóm sản phẩm → nhóm → phân nhóm → hãng → part
- **Tick chọn từng part hoặc cả nhóm** — dấu ✓ hiện ngay, dòng đã chọn **in đậm**,
  nhóm chọn một phần hiện ▣
- **Panel CHI TIẾT** bên phải (như EPLAN Overview): bấm part → xem đầy đủ thuộc tính
  (mã hàng, type/order/ERP, hãng, nhóm, tên 1-3, mô tả, eCl@ss, macro, kích thước, ngày sửa…);
  nháy đúp = copy giá trị
- **Tìm kiếm** nhanh; nút *Chọn tất cả / Bỏ chọn / Lấy từ Parts Management*; đếm **"Đã chọn: N part"**
- Xuất **1 file `parts.edz`** (tự điền sẵn đích) hoặc **mỗi part 1 file** vào thư mục
- 🆕 **Chế độ "Theo loại bản ghi (như EPLAN)"**: xuất cả **Parts / Accessory list /
  Accessory placement / Drilling pattern / Connection point pattern /
  Customer + Manufacturer/Supplier** — cờ API đã kiểm chứng thực tế
- Log CSV từng part / từng đợt xuất

### ⚙️ Khác
- **Cài đặt 1 click** (`Install-AddIn.bat`) — không cần Administrator, không đụng license EPLAN;
  cài bằng chức năng scripting miễn phí của EPLAN (action `ExecuteScript`)
- Gỡ cài đặt 1 click (`Uninstall-AddIn.bat`)
- Mọi hoạt động có log tại `%APPDATA%\Tool Oumijia\logs\`
- Đã kiểm chứng end-to-end trên **EPLAN Platform 2022.0.3** (nạp, ribbon, import/export, thoát sạch)

## 🚀 Bắt đầu nhanh

1. Tải **`Tool-Oumijia-AddIn-v0.4.0.zip`** (~26 KB) tại [Releases](https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/latest)
2. Giải nén ra một thư mục bất kỳ → chạy **`Install-AddIn.bat`**
3. Mở EPLAN → ribbon **"Tool Oumijia"** xuất hiện với 2 nút: **Nhập EDZ… / Xuất EDZ…**

> 💡 Khuyến nghị: thử nhập trên **database test** trước khi nhập vào database chính
> (Parts Management → chọn/tạo database test).

Chi tiết: [docs/INSTALL.md](docs/INSTALL.md) · [docs/USER-GUIDE-vi.md](docs/USER-GUIDE-vi.md)

## 🔨 Build từ source

```powershell
# Cần: .NET SDK 8 + máy có cài EPLAN Platform 2022+
dotnet build src/Oumijia.EplanAddIn/Oumijia.EplanAddIn.csproj -c Release `
  -p:EplanApiDir="C:\Program Files\EPLAN\Platform\2022.0.3\Bin"

# Đóng gói zip phát hành (add-in only)
powershell -ExecutionPolicy Bypass -File tools/pack.ps1 -Version 0.3.0
```

Hướng dẫn đầy đủ (đăng ký add-in, test headless): [docs/BUILD.md](docs/BUILD.md).

## Cấu trúc repo

```
Tool-Oumijia-EPLAN/
├─ src/Oumijia.EplanAddIn/       # add-in duy nhất (net48, x64)
│  ├─ AddIn.cs                   # IEplAddIn — ribbon "Tool Oumijia"
│  ├─ Actions/                   # Nhập/Xuất EDZ + SelfTest + DiagParts
│  └─ Ui/                        # ImportEdzForm, ExportEdzForm (cây nhóm), PartsTreeModel
├─ tools/
│  ├─ eplan-scripts/             # OumijiaRegister.cs (+ script chẩn đoán ribbon/registration)
│  ├─ install-addin/             # Install/Uninstall 1 click + README
│  └─ pack.ps1                   # đóng gói zip add-in-only
└─ docs/                         # kiến trúc, build, cài đặt, hướng dẫn, lộ trình
```

## 🗺️ Định hướng (Roadmap)

- **v0.3.0** ✅ — Add-in duy nhất: nhập/xuất EDZ với cây chọn part như EPLAN; bỏ app exe
- **v0.4.0** ✅ — Panel chi tiết + xuất theo loại bản ghi (như EPLAN) + fix toàn bộ lỗi UAT
- **v0.5** — Icon ribbon; chọn ngôn ngữ hiển thị part; tiến trình lô lớn; EDZ writer — xem [docs/ROADMAP.md](docs/ROADMAP.md)

## ⚖️ Ghi chú pháp lý

- Dự án **không liên kết** với EPLAN GmbH & Co. KG; chỉ dùng định dạng file trao đổi và API chính thức.
- Dữ liệu linh kiện trong các gói `.edz` thuộc bản quyền của các hãng sản xuất / EPLAN —
  **chỉ dùng cho mục đích cá nhân/nội bộ**, không phân phối lại.
- Công cụ chỉ phân tích **dữ liệu factual** (mã hàng, tên, nhóm sản phẩm…) và **không tái tạo**
  hình vẽ/macro độc quyền.

## 🤝 Đóng góp & AI agents

Mọi đóng góp đều hoan nghênh — mở issue/PR. Nếu bạn là **AI agent** tiếp tục dự án này,
hãy đọc trước [**AGENTS.md**](AGENTS.md): nó chứa bối cảnh, kiến thức đăng ký add-in đã kiểm chứng,
quy ước, ranh giới và danh sách việc tiếp theo.

## License

[MIT](LICENSE) © 2026 wuwenhou (Oumijia).
