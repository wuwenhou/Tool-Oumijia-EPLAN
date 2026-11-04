# Hướng dẫn sử dụng — Tool Oumijia (EPLAN Edition) v0.1.0

## 1. Màn hình chính

App gồm 3 tab: **Xuất (Export)** · **Nhập (Import)** · **Cài đặt**.

---

## 2. Tab Xuất (Export) — quét & phân loại thư viện

### Các bước

1. Thêm thư mục "inbox" chứa file `.edz` / `.ema` (có thể thêm nhiều thư mục; tick *Gồm thư mục con* nếu cần)
2. Bấm **🔍 Quét ngay** — app đọc từng gói, trích Hãng / Mã hàng / Nhóm
3. Kiểm tra bảng:

| Cột | Ý nghĩa |
|---|---|
| **Chọn** | Tick để xuất (mặc định chọn hết) |
| **Tệp** | Tên file nguồn |
| **Loại** | `edz` / `ema` / `emp` / `ems` |
| **Hãng** | Đọc từ metadata (hoặc `-` với file .ema) |
| **Nhóm** | Kết quả phân loại tự động |
| **Mã hàng** | Part / Type / Order number |
| **Đến thư mục** | **Sửa được** — đường dẫn tương đối trong thư viện |
| **Trạng thái** | Tóm tắt gói (số part/macro/ảnh) hoặc lỗi |

4. Muốn giữ nguyên các đích đã sửa cho lần sau → bấm **Lưu đích đã sửa** (ghi vào `overrides.json`)
5. Bấm **XUẤT ▸** — app sao chép (hoặc di chuyển) vào:

```
<Thư viện>\<Hãng>\<Nhóm>\<tên file>
Ví dụ:     D:\Tool Oumijia\Import\Siemens\PLC\Siemens_6ES7214-1AG40-0XB0.edz
```

6. Log CSV chi tiết: `%APPDATA%\Tool Oumijia\logs\export-YYYYMMDD-HHMMSS.csv`
   (nguồn, đích, hành động, kích thước, SHA-256)

### Quy tắc phân loại (cách hoạt động)

- **Hãng**: lấy từ metadata EDZ → áp bảng alias (vd `Siemens AG` → `Siemens`) → nếu không có
  thì bỏ các hậu tố công ty (GmbH, Ltd, …) → sanitize tên thư mục.
- **Nhóm**: tìm từ khóa (không phân biệt hoa thường) trong **product group + designation + mã hàng + tên file**.
  Quy tắc đầu tiên khớp sẽ thắng. Không khớp → rơi vào `_Unsorted`.
- File `.ema` đơn lẻ không có metadata → mặc định vào `_Macros` (bạn tự đổi nếu muốn).

### Trùng tên file (chính sách)

Đặt trong tab Cài đặt:

| Chính sách | Hành vi |
|---|---|
| **Rename** (mặc định) | Giữ cả hai — file mới thành `tên (2).edz` |
| **Skip** | Bỏ qua file mới nếu đã tồn tại file khác nội dung |
| **Overwrite** | Ghi đè file cũ |

Nếu file tồn tại **giống hệt nội dung** (SHA-256), luôn được bỏ qua — không tạo bản sao.

### Di chuyển hay sao chép?

- Mặc định: **sao chép** (an toàn — file gốc còn nguyên).
- Tick *Di chuyển (move)* trong Cài đặt nếu muốn dọn inbox sau khi xếp thư viện.

---

## 3. Tab Nhập (Import)

### 3.1 Cài macro EPLAN (`.ema` / `.emp` / `.ems`)

1. **Thêm tệp…** hoặc **Thêm thư mục…** (quét cả thư mục con)
2. Chọn **Thư mục macro đích**:
   - Bấm **Dò tìm** để tự dò các vị trí thường gặp (`C:\Users\Public\EPLAN\Data\Macros\…`)
   - Hoặc copy đường dẫn chính xác từ EPLAN: **Options → Settings → User → Management → Directories**
     (mục master-data / macros)
3. Bấm **CÀI VÀO EPLAN ▸**
4. **Khởi động lại EPLAN** → mở Macro Navigator để thấy macro

Log: `%APPDATA%\Tool Oumijia\logs\import-*.csv`

### 3.2 Nhập EDZ vào Parts Database

**v0.1.0**: nhập thủ công (có trợ giúp gom danh sách):

1. Gom các `.edz` cần nhập vào danh sách (Thêm tệp / Thêm thư mục / **Copy danh sách**)
2. Trong EPLAN: **Master data → Parts → Management → [Extras] → Import** → chọn file type **EDZ**
3. Khuyến nghị: thao tác trên **database test** trước với tùy chọn *"chỉ thêm mới"*, đối chiếu kết quả
   rồi mới nhập vào database chính.

**v0.2 (đang phát triển)**: add-in EPLAN sẽ tự batch import cả danh sách + báo cáo từng gói.

---

## 4. Tab Cài đặt

| Mục | Ý nghĩa |
|---|---|
| **Thư mục gốc thư viện** | Gốc cây `Import/<Hãng>/<Nhóm>` (mặc định `Documents\Tool Oumijia\Import`) |
| **Thư mục quét (inbox)** | Danh sách thư mục nguồn |
| **Chính sách trùng tên** | Rename / Skip / Overwrite |
| **Di chuyển thay vì sao chép** | Như trên |
| **Thư mục không phân loại** | Tên thư mục fallback (mặc định `_Unsorted`) |
| **Thư mục macro EPLAN** | Bổ sung cho phần dò tự động |
| **Mở rules.json** | Tùy chỉnh quy tắc phân loại |

### Tùy chỉnh `rules.json`

File tự sinh tại `%APPDATA%\Tool Oumijia\rules.json`. Ví dụ thêm nhóm mới:

```json
{
  "FallbackCategory": "_Unsorted",
  "ManufacturerAliases": {
    "schneider electric": "Schneider Electric"
  },
  "Categories": [
    {
      "Folder": "ServoMotor",
      "Keywords": ["servo motor", "servomotor", "ac servo"],
      "EClassPrefixes": []
    }
  ]
}
```

- `Categories` xét **theo thứ tự** — nhóm cụ thể hơn nên đặt trước.
- `EClassPrefixes`: so khớp đầu mã eCl@ss (vd `"27-14"` khớp `27-14-05-11`) — hiện để trống mặc định,
  thêm khi bạn có dữ liệu kiểm chứng.
- Sau khi sửa: mở lại app hoặc quét lại; bấm **Áp dụng lại quy tắc** để tính lại bảng.

---

## 5. Câu hỏi thường gặp

**Gói EDZ đọc lỗi "Lỗi đọc: …"?**
→ File có thể hỏng hoặc dùng container ngoài ZIP/7z. Gửi issue kèm thông báo lỗi.

**Hãng/Nhóm phân loại sai?**
→ Sửa trực tiếp cột *Đến thư mục* và bấm *Lưu đích đã sửa*; hoặc cập nhật `rules.json`.

**Cài macro xong EPLAN không thấy?**
→ Khởi động lại EPLAN. Kiểm tra đã trỏ đúng thư mục master data (Options → Settings xem đường dẫn chuẩn).

**Tool có cần license EPLAN không?**
→ Không. Tool chạy độc lập, không can thiệp bản quyền; chỉ đọc/ghi file và copy vào thư mục dữ liệu.

**Có hỗ trợ dữ liệu hãng thật để chia sẻ không?**
→ Không. Dữ liệu linh kiện thuộc bản quyền hãng — chỉ dùng cá nhân/nội bộ, không phân phối lại.
