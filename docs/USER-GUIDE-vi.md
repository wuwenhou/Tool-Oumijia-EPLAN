# Hướng dẫn sử dụng — Tool Oumijia (EPLAN Edition) v0.2.0

Có **2 cách dùng**, dùng chung một bộ xử lý:

| | Cách dùng | Phù hợp với |
|---|---|---|
| **A** | **App standalone** (`Tool-Oumijia.exe`) — quét & phân loại thư viện EDZ | Làm việc ngoài EPLAN, dọn kho file |
| **B** | **Add-in trong EPLAN** (ribbon "Tool Oumijia") — Nhập/Xuất EDZ trực tiếp | Batch import vào Parts Database, export parts ra EDZ |

> **v0.2.0 xử lý DUY NHẤT file `.edz`** (theo yêu cầu dự án). Các định dạng macro `.ema/.emp/.ems`
> tạm thời không còn xuất hiện trong giao diện.

---

## Phần A — App standalone

App gồm 3 tab: **Xuất (Export)** · **Nhập (Import)** · **Cài đặt**.

### A1. Tab Xuất — quét & phân loại thư viện

1. Thêm thư mục "inbox" chứa file `.edz` (có thể thêm nhiều thư mục; tick *Gồm thư mục con* nếu cần)
2. Bấm **🔍 Quét ngay** — app đọc từng gói, trích Hãng / Mã hàng / Nhóm
3. Kiểm tra bảng:

| Cột | Ý nghĩa |
|---|---|
| **Chọn** | Tick để xuất (mặc định chọn hết) |
| **Tệp** | Tên file nguồn (`.edz`) |
| **Hãng** | Đọc từ metadata |
| **Nhóm** | Kết quả phân loại tự động |
| **Mã hàng** | Part / Type / Order number |
| **Đến thư mục** | **Sửa được** — đường dẫn tương đối trong thư viện |
| **Trạng thái** | Tóm tắt gói (số part/macro/ảnh) hoặc lỗi |

4. Muốn giữ nguyên các đích đã sửa cho lần sau → bấm **Lưu đích đã sửa** (ghi vào `overrides.json`)
5. Bấm **XUẤT ▸** — app sao chép (hoặc di chuyển) vào:

```
<Thư viện>\<Hãng>\<Nhóm>\<tên file>
Ví dụ:     D:\Tool Oumijia\Import\Siemens\PLC\siemens_6es7214.edz
```

6. Log CSV chi tiết: `%APPDATA%\Tool Oumijia\logs\export-YYYYMMDD-HHMMSS.csv`
   (nguồn, đích, hành động, kích thước, SHA-256)

#### Quy tắc phân loại (cách hoạt động)

- **Hãng**: lấy từ metadata EDZ → áp bảng alias (vd `Siemens AG` → `Siemens`) → nếu không có
  thì bỏ các hậu tố công ty (GmbH, Ltd, …) → sanitize tên thư mục.
- **Nhóm**: tìm từ khóa (không phân biệt hoa thường) trong **product group + designation + mã hàng + tên file**.
  Quy tắc đầu tiên khớp sẽ thắng. Không khớp → rơi vào `_Unsorted`.

#### Trùng tên file (chính sách)

Đặt trong tab Cài đặt:

| Chính sách | Hành vi |
|---|---|
| **Rename** (mặc định) | Giữ cả hai — file mới thành `tên (2).edz` |
| **Skip** | Bỏ qua file mới nếu đã tồn tại file khác nội dung |
| **Overwrite** | Ghi đè file cũ |

Nếu file tồn tại **giống hệt nội dung** (SHA-256), luôn được bỏ qua — không tạo bản sao.

#### Di chuyển hay sao chép?

- Mặc định: **sao chép** (an toàn — file gốc còn nguyên).
- Tick *Di chuyển (move)* trong Cài đặt nếu muốn dọn inbox sau khi xếp thư viện.

### A2. Tab Nhập — trợ lý danh sách EDZ

Tab này giúp **gom danh sách** các gói EDZ cần nhập vào EPLAN Parts Database:

1. **Thêm tệp…** / **Thêm thư mục…** (quét cả thư mục con) — chỉ nhận `.edz`
2. **Copy danh sách** — dán vào ghi chú, email, tài liệu… để theo dõi
3. Cách nhập vào EPLAN:
   - **Khuyến nghị (v0.2)**: dùng **add-in** — ribbon "Tool Oumijia" → **Nhập EDZ…** (batch, xem Phần B)
   - Hoặc thủ công: **Master data → Parts → Management → [Extras] → Import** → chọn từng file `.edz`
4. Khuyến nghị: thao tác trên **database test** trước với tùy chọn *"chỉ thêm mới"*, đối chiếu kết quả
   rồi mới nhập vào database chính.

### A3. Tab Cài đặt

| Mục | Ý nghĩa |
|---|---|
| **Thư mục gốc thư viện** | Gốc cây `Import/<Hãng>/<Nhóm>` (mặc định `Documents\Tool Oumijia\Import`) |
| **Thư mục quét (inbox)** | Danh sách thư mục nguồn |
| **Chính sách trùng tên** | Rename / Skip / Overwrite |
| **Di chuyển thay vì sao chép** | Như trên |
| **Thư mục không phân loại** | Tên thư mục fallback (mặc định `_Unsorted`) |
| **Mở rules.json** | Tùy chỉnh quy tắc phân loại |

#### Tùy chỉnh `rules.json`

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

## Phần B — Add-in trong EPLAN (v0.2)

### B1. Cài đặt

Xem **Cách 3** trong [INSTALL.md](INSTALL.md). Tóm tắt: chạy `addin\Install-AddIn.bat`
(không cần admin) → mở EPLAN → ribbon **"Tool Oumijia"**.

### B2. Nhập EDZ vào Parts Database — thay thế nhập thủ công

1. Trong EPLAN, bấm ribbon **Tool Oumijia → Nhập EDZ…**
2. **Thêm tệp…** / **Thêm thư mục…** (quét cả thư mục con) — gom tất cả gói `.edz` cần nhập
3. Chọn tùy chọn:
   - **Chế độ nhập**:
     | Chế độ | Ý nghĩa |
     |---|---|
     | Chỉ thêm mới | Chỉ thêm part chưa tồn tại; bỏ qua part đã có |
     | Chỉ cập nhật | Chỉ ghi đè part đã tồn tại |
     | Cập nhật + thêm mới | Kết hợp cả hai (dùng phổ biến nhất khi sync thư viện) |
   - **Nhận dạng part**: Theo mã hàng (mặc định) / Theo UniqueId / UniqueId + mã hàng —
     cách EPLAN nhận diện part nào là "trùng" với part nào
   - **Nhập thêm ngôn ngữ phụ**: tick nếu gói EDZ có đa ngôn ngữ
4. Bấm **NHẬP VÀO PARTS DATABASE ▸** → xác nhận
5. Kiểm tra **Parts Management** để xác nhận kết quả.
   Log kiểm toán: `%APPDATA%\Tool Oumijia\logs\import-YYYYMMDD.csv`

> ⚠️ **Khuyến nghị**: thử trên **database test** trước (Parts Management → database → tạo/chọn bản test)
> khi nhập lô lớn.

### B3. Xuất Parts Database ra EDZ (backup / chia sẻ nội bộ)

1. Trong EPLAN, mở **Parts Management** và chọn database (nếu chưa mở)
2. Bấm ribbon **Tool Oumijia → Xuất EDZ…**
3. Chọn:
   - **Phạm vi**: toàn bộ database đang mở, hoặc các part đang chọn trong Parts Management
   - **Cách xuất**: 1 file EDZ duy nhất, hoặc mỗi part 1 file EDZ (vào thư mục)
4. Chọn đích → **XUẤT RA EDZ ▸**
5. Log: `%APPDATA%\Tool Oumijia\logs\export-YYYYMMDD.csv`

Gói EDZ sinh ra bằng converter chính thức của EPLAN (`IXPartsImportExportEdz`) — có thể import lại
trên máy khác, hoặc đưa vào app standalone để phân loại thư viện.

### B4. Cửa sổ Tool Oumijia

Nút thứ 3 mở app standalone ngay từ EPLAN. Lần đầu sẽ hỏi đường dẫn `Tool-Oumijia.exe`
(nếu chưa nằm cạnh add-in hoặc trong `%APPDATA%\Tool Oumijia`) — chọn một lần, app sẽ nhớ.

---

## Phần C — Câu hỏi thường gặp

**Gói EDZ đọc lỗi "Lỗi đọc: …"?**
→ File có thể hỏng hoặc dùng container ngoài ZIP/7z. Gửi issue kèm thông báo lỗi.

**Hãng/Nhóm phân loại sai?**
→ Sửa trực tiếp cột *Đến thư mục* và bấm *Lưu đích đã sửa*; hoặc cập nhật `rules.json`.

**Nhập EDZ xong không thấy part trong Parts Management?**
→ Kiểm tra đúng Parts Database đang mở; xem log `%APPDATA%\Tool Oumijia\logs\addin.log`.
Với chế độ *Chỉ thêm mới*, part đã tồn tại sẽ bị bỏ qua (không phải lỗi).

**Xuất EDZ báo "Chưa có Parts Database nào đang mở"?**
→ Mở **Master data → Parts → Management** trước, chọn database rồi thực hiện lại.

**Ribbon "Tool Oumijia" không xuất hiện sau khi cài?**
→ Khởi động lại EPLAN. Nếu vẫn không có: xem `%APPDATA%\Tool Oumijia\logs\register-script.log`
và thử phương án dự phòng trong [INSTALL.md](INSTALL.md) (Scripts → Run…).

**Tool có cần license EPLAN không?**
→ Không. Tool chạy độc lập, không can thiệp bản quyền; add-in dùng API chính thức của EPLAN.
Script đăng ký add-in chạy qua chức năng scripting miễn phí của EPLAN.

**Có hỗ trợ dữ liệu hãng thật để chia sẻ không?**
→ Không. Dữ liệu linh kiện thuộc bản quyền hãng — chỉ dùng cá nhân/nội bộ, không phân phối lại.
