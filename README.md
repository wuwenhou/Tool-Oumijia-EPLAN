# Tool Oumijia — EPLAN Edition

**Add-in EPLAN: nhập/xuất gói dữ liệu linh kiện (`.edz`) trực tiếp trong EPLAN Electric P8 — batch import vào Parts Database, và export theo cây nhóm như navigator.**

[![Release](https://img.shields.io/github/v/release/wuwenhou/Tool-Oumijia-EPLAN)](https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-informational)

> 🇬🇧 **English overview** — *Tool Oumijia (EPLAN Edition)* imports/exports EPLAN part packages (`.edz`) straight from EPLAN (ribbon **"Tool Oumijia"**): batch import into the parts database and export selected parts back to EDZ — through the official EPLAN API. Ships in **two editions**: an **add-in** (DLL; needs an API license or an EPLAN version that allows unsigned DLLs) and a **Script Edition** (EPLAN Scripting, **no API license required** — for newer EPLAN releases that block unsigned add-ins). A smart installer picks the right one automatically. Not affiliated with EPLAN. AI agents continuing this project should read [AGENTS.md](AGENTS.md).

## 🧩 Hai phiên bản — cài cái nào?

| | **Bản ADD-IN** (`addin\`) | **Bản SCRIPT** (`script\`) |
|---|---|---|
| Công nghệ | DLL add-in (net48) | EPLAN Scripting (C# 5) |
| Yêu cầu | EPLAN có **API license**, HOẶC bản EPLAN cho phép DLL chưa ký (vd 2022) | **Không cần license** — dùng scripting miễn phí của EPLAN |
| Phù hợp | EPLAN 2022–2023 | **EPLAN mới (2024+/2026)** chặn add-in chưa ký |
| Tính năng | Như nhau: Nhập EDZ (batch) + Xuất EDZ (cây nhóm, chi tiết, loại bản ghi) | Như nhau |

**Không cần tự chọn** — chạy **`Install-Tool-Oumijia.bat`**: tự thử bản add-in trước, nếu EPLAN
vô hiệu hóa DLL (thiếu license) thì tự chuyển sang cài bản script và kiểm chứng lại.

---

## Tính năng (v0.6.0)

### 📥 Nhập EDZ… (Import) — đơn giản
- Chọn **nhiều file hoặc cả thư mục** `.edz` (quét cả thư mục con) → bấm **NHẬP**
- Mặc định hợp lý (cố định): *cập nhật part đã có + thêm part mới*, theo *mã hàng*;
  **tự dò field-mapping scheme** (không còn lỗi `S030060`)
- Batch import qua API chính thức của EPLAN (`IXPartsImportExportEdz`) + log CSV kiểm toán

### 📤 Xuất EDZ… (Export) — chọn part như EPLAN
- **Cây nhóm kiểu navigator EPLAN**: Nhóm sản phẩm → nhóm → phân nhóm → hãng → part
- **Tick chọn từng part hoặc cả nhóm** — dấu ✓ hiện ngay, dòng đã chọn **in đậm**,
  nhóm chọn một phần hiện ▣
- **Panel CHI TIẾT** bên phải (như EPLAN Overview): bấm part → xem đầy đủ thuộc tính
  (mã hàng, type/order/ERP, hãng, nhóm, tên 1-3, mô tả, eCl@ss, macro, kích thước, ngày sửa…);
  nháy đúp = copy giá trị
- **Tìm kiếm** nhanh; nút *Chọn tất cả / Bỏ chọn / Lấy từ Parts Management*; đếm **"Đã chọn: N part"**
- Xuất **1 file EDZ** (chỉ chọn **thư mục** — tên file **tự động theo mã sản phẩm** khi chọn 1 part,
  `parts.edz` khi nhiều part; có dòng xem trước + hỏi ghi đè + nhớ thư mục) hoặc
  **mỗi part 1 file** (tên = mã sản phẩm)
- 🆕 **Chế độ "Theo loại bản ghi (như EPLAN)"**: xuất cả **Parts / Accessory list /
  Accessory placement / Drilling pattern / Connection point pattern /
  Customer + Manufacturer/Supplier** — cờ API đã kiểm chứng thực tế
- Log CSV từng part / từng đợt xuất

### ⚙️ Khác
- **Ribbon Tool Oumijia v0.6.0**: bốn nhóm **Quản lý EDZ | EPLAN Classic | VEE | GIRD**;
  cửa sổ **EPLAN Classic** tìm/chia nhóm lệnh menu cũ và chỉ chạy các action đã có;
  nút **Menu bar** gọi `MfToggleMainMenuAction` chính thức của EPLAN
- **VEE launcher (tự cài riêng)**: gọi `VEE Export connections` / `VEE Table Maker` nếu đã cài;
  nếu chưa cài sẽ mở hướng dẫn tải chính thức của tác giả. **Tool không phân phối DLL/tài nguyên bên thứ ba.**
- **Cài đặt 1 click** với **installer thông minh** — `Install-Tool-Oumijia.bat` tự chọn bản
  add-in hay script phù hợp với máy; không cần Administrator, không đụng license EPLAN
- Gỡ cài đặt 1 click (`Uninstall-Tool-Oumijia.bat`)
- Mọi hoạt động có log tại `%APPDATA%\Tool Oumijia\logs\`
- Đã kiểm chứng end-to-end trên **EPLAN Platform 2022.0.3** (add-in) và **2026.0.3** (script)

## 🚀 Bắt đầu nhanh

1. Tải **`Tool-Oumijia-v0.6.0.zip`** tại [Releases](https://github.com/wuwenhou/Tool-Oumijia-EPLAN/releases/latest)
2. Giải nén ra một thư mục bất kỳ → chạy **`Install-Tool-Oumijia.bat`** (tự chọn bản phù hợp)
3. Mở EPLAN → ribbon **“Tool Oumijia”**: nhánh **Quản lý EDZ** với **Nhập EDZ… / Xuất EDZ…**;
   **EPLAN Classic…** để tìm kiếm nhóm lệnh menu cũ; **VEE** để gọi/add-in hướng dẫn của tác giả;
   **GIRD** cho grid/snap (nếu phiên EPLAN cho phép).

> 💡 Khuyến nghị: thử nhập trên **database test** trước khi nhập vào database chính
> (Parts Management → chọn/tạo database test).

Chi tiết: [docs/INSTALL.md](docs/INSTALL.md) · [docs/USER-GUIDE-vi.md](docs/USER-GUIDE-vi.md)

## 🔨 Build từ source

```powershell
# Cần: .NET SDK 8 + máy có cài EPLAN Platform 2022+
dotnet build src/Oumijia.EplanAddIn/Oumijia.EplanAddIn.csproj -c Release `
  -p:EplanApiDir="C:\Program Files\EPLAN\Platform\2022.0.3\Bin"

# Đóng gói zip phát hành (2 editions)
powershell -ExecutionPolicy Bypass -File tools/pack.ps1 -Version 0.6.0
```

Hướng dẫn đầy đủ (đăng ký add-in, test headless): [docs/BUILD.md](docs/BUILD.md).

## Cấu trúc repo

```
Tool-Oumijia-EPLAN/
├─ src/Oumijia.EplanAddIn/       # bản add-in (net48, x64)
│  ├─ AddIn.cs                   # IEplAddIn — ribbon "Tool Oumijia"
│  ├─ Actions/                   # Nhập/Xuất EDZ, Classic, VEE/menu, SelfTest + DiagParts
│  └─ Ui/                        # ImportEdzForm, ExportEdzForm, EplanClassicForm, VeeIntegration, PartsTreeModel
├─ tools/
│  ├─ script-edition/            # bản SCRIPT (ToolOumijiaScript.cs, C#5) + installer + probe
│  ├─ install-all/               # installer thông minh (tự chọn bản)
│  ├─ install-addin/             # installer bản add-in
│  ├─ eplan-scripts/             # OumijiaRegister.cs (+ script chẩn đoán)
│  └─ pack.ps1                   # đóng gói zip 2 bản
└─ docs/                         # kế hoạch, kiến trúc, build, cài đặt, hướng dẫn, lộ trình
```

## 🗺️ Định hướng (Roadmap)

- **v0.3.0** ✅ — Add-in duy nhất: nhập/xuất EDZ với cây chọn part như EPLAN; bỏ app exe
- **v0.4.0** ✅ — Panel chi tiết + xuất theo loại bản ghi (như EPLAN) + fix toàn bộ lỗi UAT
- **v0.6.0** ✅ — **Script Edition** (không cần API license) + installer thông minh + fix scheme nhập & đích xuất
- **v0.6** — Icon ribbon; chọn ngôn ngữ hiển thị part; tiến trình lô lớn; EDZ writer — xem [docs/ROADMAP.md](docs/ROADMAP.md)

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
