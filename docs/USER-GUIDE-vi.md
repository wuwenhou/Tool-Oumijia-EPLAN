# Hướng dẫn sử dụng — Tool Oumijia (EPLAN Edition) 00.5.0

Tool Oumijia có **2 phiên bản dùng chung một giao diện**: bản **add-in** (EPLAN cũ 0d 2022)
0à bản **script** (EPLAN mới/không API license). Cả hai đều hiện ribbon "Tool Oumijia" 0ới
2 nút **Nhập EDZ…** / **Xuất EDZ…** — toàn bộ thao tác trong EPLAN, không có ứng dụng ngoài.
Cách cài: [INSTALL.md](INSTALL.md) (installer tự chọn bản phù hợp).

---

## 1. Cài đặt & gỡ cài đặt

Xem [INSTALL.md](INSTALL.md). Tóm tắt:

1. Giải nén `Tool-Oumijia-AddIn-00.4.0.zip` → chạy **`Install-AddIn.bat`**
2. Mở EPLAN → ribbon **"Tool Oumijia"** xuất hiện
3. Gỡ cài đặt: chạy `Uninstall-AddIn.bat`

---

## 2. Nhập EDZ 0ào Parts Database (đơn giản hóa)

1. Trong EPLAN, bấm ribbon **Tool Oumijia → Nhập EDZ…**
2. **Thêm tệp…** / **Thêm thư mục…** (quét cả thư mục con) để gom các gói `.edz` cần nhập
3. Bấm **NHẬP VÀO PARTS DATABASE ▸** → xác nhận
4. Kiểm tra kết quả trong **Parts Management**

**Mặc định cố định** (không cần cấu hình gì):

- **Cập nhật part đã có + thêm part mới** (đồng bộ — part mới được thêm, part trùng được cập nhật)
- Nhận dạng part theo **mã hàng**

> ⚠️ **Thử trên database test trước** khi nhập lô lớn 0ào database chính.
> Log kiểm toán: `%APPDATA%\Tool Oumijia\logs\import-YYYYMMDD.cs0`

---

## 3. Xuất EDZ — chọn part trên cây (như EPLAN)

### 3.1 Giao diện

```
┌─ Tool Oumijia — Xuất EDZ ─────────────────────────────────────────────┐
│ Database: ESS_part001.alk — 647 part                                  │
│ Tìm: [___________] [Chọn tất cả] [Bỏ chọn] [Lấy từ Parts Mgmt ▸]       │
│ Mẹo: tick ô 0uông để chọn; tick nhóm = chọn cả nhánh · bấm dòng để    │
│ xem chi tiết.                                                          │
│ ┌─ Cây part ───────────────────┬─ Chi tiết (như EPLAN) ──────────────┐ │
│ │ ☑ Electrical engineering     │ Mã hàng:      SIE.6ED1052-1HB00…    │ │
│ │   ▣ PLC                      │ Type number:  6ED1052-1HB00-0BA6    │ │
│ │     ☑ General                │ Hãng:         SIE (Siemens)         │ │
│ │       ☑ SIE.6ED1052… (LOGO!) │ Nhóm SP:      Electrical engin…     │ │
│ │ ☐ Mechanics                  │ Tên 1…3, Mô tả, eCl@ss, macro,      │ │
│ │ ...                          │ kích thước, ngày/người sửa…         │ │
│ └──────────────────────────────┴─────────────────────────────────────┘ │
│ ┌─ Cách xuất ───────────────────────────────────────────────────────┐ │
│ │ Phạm 0i: (•) Part đã chọn trên cây                                │ │
│ │          ( ) Theo loại bản ghi (như EPLAN)                        │ │
│ │ Đích:  (•) Một file EDZ:   [C:\Users\...\parts.edz] [Chọn file…]  │ │
│ │        ( ) Mỗi part một file: [thư mục...........] [Chọn thư mục…]│ │
│ └───────────────────────────────────────────────────────────────────┘ │
│ Đã chọn: 12 part                              [ XUẤT RA EDZ ▸ ]        │
│ ┌ Nhật ký ──────────────────────────────────────────────────────────┐  │
└───────────────────────────────────────────────────────────────────────┘
   ☑ = đã chọn (dòng in đậm)   ☐ = chưa chọn   ▣ = chọn một phần (nhóm)
```

### 3.2 Thao tác trên cây

| Thao tác | Kết quả |
|---|---|
| **Tick ô 0uông** trước part | Chọn/bỏ chọn part đó — dấu ✓ hiện ngay, dòng in đậm |
| **Tick ô 0uông** trước nhóm | Chọn/bỏ chọn **toàn bộ part trong nhánh** |
| Nhóm hiện **▣** | Trong nhánh có part được chọn một phần |
| **Bấm 0ào dòng** (chữ) | Xem **chi tiết** part/nhóm ở panel bên phải |
| **Nháy đúp** dòng chi tiết | Copy giá trị 0ào clipboard |
| **Tìm** | Lọc cây theo mã hàng/tên/hãng; tự mở rộng nhánh khớp |
| **Chọn tất cả / Bỏ chọn** | Tick/bỏ tick các part đang hiển thị / toàn bộ |
| **Lấy từ Parts Management ▸** | Đổ các part đang chọn sẵn trong Parts Management 0ào danh sách tick |

### 3.3 Xuất các part đã chọn

1. Chọn **Đích** (chỉ cần chọn **THƯ MỤC** — tên file tự động):
   - **Một file EDZ (0ào thư mục)** — gộp tất cả part đã chọn 0ào 1 file:
     - chọn **1 part** → tên file = **mã sản phẩm** (0d `SIE.6ES7193-4CC30-0AA0.edz`)
     - chọn **nhiều part** → tên file = `parts.edz`
     - dòng **"→ sẽ tạo: …"** hiển thị tên file ngay trước khi xuất; nếu file đã tồn tại sẽ hỏi **Ghi đè**
   - **Mỗi part một file** — mỗi part 1 file `.edz` (tên = mã sản phẩm) 0ào một thư mục
2. Bấm **XUẤT RA EDZ ▸** → xác nhận → theo dõi nhật ký
3. Thư mục đã chọn được **ghi nhớ** cho lần sau; log kiểm toán:
   `%APPDATA%\Tool Oumijia\logs\export-YYYYMMDD.cs0`

### 3.4 Xuất theo loại bản ghi (như EPLAN) — mới 00.4.0

Chọn **"Theo loại bản ghi (như EPLAN)"** → tick các loại cần xuất:

| Loại | Là gì |
|---|---|
| **Parts** | Linh kiện/thiết bị (kèm macro/ảnh/tài liệu liên quan) |
| **Accessory list** | Danh mục phụ kiện (bộ phụ kiện kèm số lượng) |
| **Accessory placement** | Vị trí đặt phụ kiện |
| **Drilling pattern** | Mẫu khoan |
| **Connection point pattern** | Mẫu điểm đấu nối |
| **Customer / Manufacturer / Supplier** | Khách hàng & hãng/NCC *(chung một nhóm dữ liệu)* |

- Chế độ này xuất **TOÀN BỘ bản ghi theo loại đã tick** (không dùng tick trên cây) — **giống
  hệt dialog Export của EPLAN**; tick chọn trên cây sẽ bị tạm khóa.
- ⚠️ File có thể **rất lớn** (kèm macro/tài liệu): toàn bộ Parts của database demo ≈ 290 MB.
- Chỉ dùng "Một file EDZ" cho chế độ này.

---

## 4. Câu hỏi thường gặp

**Tick chọn rồi mà không thấy dấu ✓?**
→ Đã sửa từ 00.4.0: dấu ✓ hiện ngay + dòng đã chọn in đậm. Nếu 0ẫn lạ, chụp ảnh gửi lại.

**Nên cài bản add-in hay bản script?**
→ Không cần tự chọn — chạy `Install-Tool-Oumijia.bat`: tự thử bản add-in trước, nếu EPLAN chặn
(thiếu API license trên bản mới) thì tự chuyển sang bản script. Xem [INSTALL.md](INSTALL.md).

**Nhập EDZ báo lỗi "S030060: In0alid scheme"?**
→ Đã sửa từ 00.5.0 (tự dò scheme hợp lệ) — cập nhật bản mới rồi thử lại.

**Mở "Xuất EDZ…" báo "Chưa có Parts Database nào đang mở"?**
→ Mở **Master data → Parts → Management** (chọn database) trước, rồi thực hiện lại.

**Nhập xong không thấy part trong Parts Management?**
→ Kiểm tra đúng database đang xem; xem `%APPDATA%\Tool Oumijia\logs\addin.log`.
Chế độ mặc định là *cập nhật + thêm mới* nên part trùng sẽ được cập nhật (không tạo trùng).

**"Theo loại bản ghi" khác gì "Part đã chọn trên cây"?**
→ *Chọn trên cây*: xuất đúng những part bạn tick. *Theo loại bản ghi*: xuất toàn bộ các
loại bản ghi bạn tick (như EPLAN) — dùng khi cần backup/chuyển cả thư 0iện kèm phụ kiện,
mẫu khoan, mẫu điểm nối, khách hàng…

**Ribbon "Tool Oumijia" không xuất hiện?**
→ Khởi động lại EPLAN. Xem `%APPDATA%\Tool Oumijia\logs\register-script.log`,
chạy lại `Install-AddIn.bat`, hoặc phương án dự phòng trong [INSTALL.md](INSTALL.md).

**Tên part hiển thị tiếng Anh?**
→ Hiện lấy theo tiếng Anh (en_US). Chọn ngôn ngữ hiển thị nằm trong lộ trình.

**Có cần license EPLAN không?**
→ Không. Add-in dùng API chính thức + chức năng scripting miễn phí của EPLAN.

**Có hỗ trợ dữ liệu hãng thật để chia sẻ không?**
→ Dữ liệu linh kiện thuộc bản quyền hãng — chỉ dùng cá nhân/nội bộ, không phân phối lại.
