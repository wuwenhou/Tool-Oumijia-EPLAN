# Hướng dẫn sử dụng — Tool Oumijia (EPLAN Edition) v0.3.0

**Add-in EPLAN** với ribbon "Tool Oumijia" gồm 2 nút: **Nhập EDZ…** và **Xuất EDZ…**
(toàn bộ thao tác diễn ra trong EPLAN — không có ứng dụng ngoài).

---

## 1. Cài đặt & gỡ cài đặt

Xem [INSTALL.md](INSTALL.md). Tóm tắt:

1. Giải nén `Tool-Oumijia-AddIn-v0.3.0.zip` → chạy **`Install-AddIn.bat`**
2. Mở EPLAN → ribbon **"Tool Oumijia"** xuất hiện
3. Gỡ cài đặt: chạy `Uninstall-AddIn.bat`

---

## 2. Nhập EDZ vào Parts Database

### 2.1 Thao tác

1. Trong EPLAN, bấm ribbon **Tool Oumijia → Nhập EDZ…**
2. **Thêm tệp…** / **Thêm thư mục…** (quét cả thư mục con) để gom các gói `.edz` cần nhập
3. Chọn tùy chọn:

   | Tùy chọn | Ý nghĩa |
   |---|---|
   | **Chế độ nhập** | *Chỉ thêm mới* — bỏ qua part đã tồn tại; *Chỉ cập nhật* — ghi đè part đã có; *Cập nhật + thêm mới* — kết hợp (dùng phổ biến nhất khi sync thư viện) |
   | **Nhận dạng part** | Cách EPLAN xác định part "trùng": *Theo mã hàng* (mặc định) / *Theo UniqueId* / *UniqueId + mã hàng* |
   | **Nhập thêm ngôn ngữ phụ** | Tick nếu gói EDZ có đa ngôn ngữ |

4. Bấm **NHẬP VÀO PARTS DATABASE ▸** → xác nhận
5. Kiểm tra kết quả trong **Parts Management**

### 2.2 Lưu ý

- ⚠️ **Thử trên database test trước** khi nhập lô lớn vào database chính
  (Parts Management → chọn/tạo database test).
- Chế độ *Chỉ thêm mới* sẽ **bỏ qua** part đã tồn tại — không phải lỗi.
- Log kiểm toán: `%APPDATA%\Tool Oumijia\logs\import-YYYYMMDD.csv`

---

## 3. Xuất EDZ — chọn part bằng cây nhóm (kiểu EPLAN)

### 3.1 Giao diện

```
┌─ Tool Oumijia — Xuất EDZ (chọn part cần xuất) ────────────────────────────┐
│ Database: ESS_part001 — 647 part                                          │
│ Tìm: [____________]  [Chọn tất cả] [Bỏ chọn] [Lấy từ Parts Management ▸]   │
│ Mẹo: tick ô vuông trước mỗi mục để chọn; tick vào nhóm = chọn cả nhánh.    │
│ ┌─ Cây part (như navigator EPLAN) ──────────────────────────────────────┐ │
│ │ ☑ Electrical engineering                                              │ │
│ │   ▣ PLC                                                               │ │
│ │     ☑ General         └─ SIE.6ED1052-1HB00-0BA6 (LOGO! 24RC)          │ │
│ │     ☐ ...                                                             │ │
│ │ ☐ Mechanics                                                           │ │
│ └───────────────────────────────────────────────────────────────────────┘ │
│ Cách xuất:  (•) Một file EDZ:   [parts.edz____________] [Chọn…]           │
│             ( ) Mỗi part 1 file:[D:\Export\EDZ________] [Chọn…]           │
│ Đã chọn: 12 part                              [XUẤT RA EDZ ▸]             │
│ ┌ Nhật ký ──────────────────────────────────────────────────────────────┐ │
└───────────────────────────────────────────────────────────────────────────┘
   ☑ = đã tick   ☐ = chưa tick   ▣ = tick một phần (nhóm)
```

### 3.2 Cấu trúc cây

Giống hệt navigator Parts Management của EPLAN:

**Nhóm sản phẩm** → **nhóm** → **(phân nhóm)** → **hãng** → **part**

Ví dụ thực tế: `Electrical engineering → PLC → General → SIE → SIE.6ED1052-1HB00-0BA6 (LOGO! 24RC)`

### 3.3 Thao tác

| Thao tác | Kết quả |
|---|---|
| **Tick ô vuông** trước part | Chọn/bỏ chọn part đó |
| **Tick ô vuông** trước nhóm | Chọn/bỏ chọn **toàn bộ part trong nhánh** |
| Nhóm hiện **▣** | Trong nhánh có part được chọn một phần |
| **Tìm** (gõ vào ô) | Lọc cây theo mã hàng/tên/hãng; tự mở rộng các nhánh khớp |
| **Chọn tất cả** | Tick mọi part đang hiển thị (theo bộ lọc hiện tại) |
| **Bỏ chọn** | Bỏ tick toàn bộ |
| **Lấy từ Parts Management ▸** | Đổ các part bạn đang chọn sẵn trong cửa sổ Parts Management vào danh sách tick |
| **Đã chọn: N part** | Luôn hiển thị chính xác số part sẽ xuất |

### 3.4 Xuất

1. Chọn **Cách xuất**:
   - **Một file EDZ duy nhất** — tất cả part đã chọn vào 1 file (đặt tên, mặc định `parts.edz`)
   - **Mỗi part một file** — mỗi part 1 file `.edz` (đặt tên theo mã hàng) vào một thư mục
2. Chọn đích bằng nút **Chọn…**
3. Bấm **XUẤT RA EDZ ▸** → xác nhận
4. Theo dõi tiến trình trong ô nhật ký; kết quả tổng kết hiện cuối
5. Log kiểm toán: `%APPDATA%\Tool Oumijia\logs\export-YYYYMMDD.csv`

> 💡 Gói EDZ sinh ra bằng converter chính thức của EPLAN (`IXPartsImportExportEdz`) —
> có thể import lại trên máy khác bằng nút **Nhập EDZ…**, hoặc mở trong Parts Management.

---

## 4. Câu hỏi thường gặp

**Mở "Xuất EDZ…" báo "Chưa có Parts Database nào đang mở"?**
→ Mở **Master data → Parts → Management** (chọn database) trước, rồi thực hiện lại.

**Cây part trống hoặc thiếu part?**
→ Kiểm tra đúng database đang mở; gõ ô *Tìm* xoá trắng để bỏ lọc.

**Nhập xong không thấy part trong Parts Management?**
→ Kiểm tra đúng database đang xem; với chế độ *Chỉ thêm mới*, part đã tồn tại bị bỏ qua.
Xem `%APPDATA%\Tool Oumijia\logs\addin.log` để biết chi tiết.

**Ribbon "Tool Oumijia" không xuất hiện?**
→ Khởi động lại EPLAN. Nếu vẫn không có: xem `%APPDATA%\Tool Oumijia\logs\register-script.log`,
chạy lại `Install-AddIn.bat`, hoặc phương án dự phòng trong [INSTALL.md](INSTALL.md).

**Tên part hiển thị tiếng Anh?**
→ Hiện tại tên nhóm/tên part lấy theo tiếng Anh (en_US). Việc chọn ngôn ngữ hiển thị nằm
trong lộ trình.

**Có cần license EPLAN không?**
→ Không. Add-in dùng API chính thức + chức năng scripting miễn phí của EPLAN.

**Có hỗ trợ dữ liệu hãng thật để chia sẻ không?**
→ Dữ liệu linh kiện thuộc bản quyền hãng — chỉ dùng cá nhân/nội bộ, không phân phối lại.
