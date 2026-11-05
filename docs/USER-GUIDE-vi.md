# Hướng dẫn sử dụng — Tool Oumijia (EPLAN Edition) v0.6.2

Tool Oumijia có **2 phiên bản cùng giao diện**:

- **Add-in** — cho EPLAN 2022/2023 hoặc máy có API license.
- **Script Edition** — cho EPLAN mới/không có API license.

Cả hai hiện ribbon **Tool Oumijia** với 3 nút:

- **Nhập EDZ…**
- **Xuất EDZ…**
- **Menu bar** — bật/tắt thanh menu chính của EPLAN (`MfToggleMainMenuAction`)

Bản phát hành cũng kèm 2 add-in của **VIET EPLAN & ELECTRIC**, được tác giả cho phép phân phối:

- **VEE.EplTableMaker** — tạo bảng kỹ thuật trực tiếp.
- **VEE.ExportConnections** — xuất dữ liệu kết nối ra CSV/Excel.

---

## 1. Cài đặt & gỡ cài đặt

Xem [INSTALL.md](INSTALL.md). Tóm tắt:

1. Giải nén `Tool-Oumijia-v0.6.2.zip`.
2. Chạy **`Install-Tool-Oumijia.bat`**.
   - Installer tự chọn Add-in hay Script tùy phiên bản EPLAN.
   - Tự copy và đăng ký 2 add-in VEE.
3. Mở EPLAN.

Gỡ cài đặt hoàn toàn: **`Uninstall-Tool-Oumijia.bat`**.

---

## 2. Nhập EDZ vào Parts Database

1. Bấm ribbon **Tool Oumijia → Nhập EDZ…**
2. Chọn:
   - **Thêm tệp…** — chọn một hoặc nhiều `.edz`;
   - **Thêm thư mục…** — quét toàn bộ `.edz` kể cả thư mục con.
3. Bấm **NHẬP VÀO PARTS DATABASE ▸** và xác nhận.

Mặc định:

- Cập nhật part đã có và thêm part mới.
- Nhận dạng part theo **mã hàng**.
- Tự dò field-mapping scheme (không còn lỗi `S030060: Invalid scheme`).

> Khuyến nghị: thử trên database test trước khi nhập vào database chính.

---

## 3. Xuất EDZ — chọn part như EPLAN

### 3.1 Giao diện

```text
Tool Oumijia — Xuất EDZ
├─ Database: …                              [Tìm…] [Chọn tất cả] [Bỏ chọn] [Lấy từ Parts Mgmt]
├─ Cây part (nhóm sản phẩm → nhóm → hãng → part) │ Chi tiết thuộc tính
└─ Cách xuất / đích xuất / nhật ký
```

### 3.2 Chọn part

- **Tick ô vuông** trước mỗi part để chọn.
- **Tick nhóm** để chọn cả nhánh.
- **Bấm dòng** để xem chi tiết.
- **Double-click giá trị** trong bảng Chi tiết để copy.
- Tìm kiếm lọc nhanh cây.
- **Đã chọn: N part** luôn cập nhật.

Các part có tick được in đậm.

### 3.3 Đích xuất

Chọn một trong hai:

1. **Một file EDZ vào thư mục**
   - 1 part đã chọn → tên file = **mã sản phẩm**.
   - Nhiều part → `parts.edz`.
   - Dòng xem trước cho biết tên file sẽ tạo.
2. **Mỗi part một file**
   - Mỗi part một `.edz`.
   - Tên file = mã sản phẩm.

Thư mục được ghi nhớ cho lần sau. Nếu file đã tồn tại, Tool sẽ hỏi **Ghi đè?**.

### 3.4 Chế độ theo loại bản ghi

Không dùng tick trên cây:

1. Chọn **Theo loại bản ghi (như EPLAN)**.
2. Tick loại bản ghi cần xuất:
   - Parts
   - Accessory list / Accessory placement
   - Drilling pattern
   - Connection point pattern
   - Customer / Manufacturer / Supplier
3. Bấm **XUẤT RA EDZ ▸**.

Chế độ này xuất **toàn bộ dữ liệu theo loại đã chọn** và có thể tạo file rất lớn.

---

## 4. Menu bar

Nút **Menu bar** trong ribbon `Tool Oumijia` bật/tắt thanh menu chính của EPLAN bằng action chính thức `MfToggleMainMenuAction`.

- Không tải/ghi đè layout tùy chỉnh của người dùng.
- Nếu EPLAN không có action này trong phiên hiện tại, nút sẽ không tạo được hoặc không có hiệu quả—khung tin/log sẽ phản ánh trạng thái.

---

## 5. VEE add-ins

Nếu 2 add-in đăng ký thành công, ribbon sẽ hiện tab **VIET EPLAN ELECTRIC**:

- **VEE Export connections…**
  - Xuất dữ liệu kết nối ra CSV/Excel.
  - Hỗ trợ công việc in ống/đi dây.
- **VEE Table Maker…**
  - Tạo technical tables trực tiếp trên trang EPLAN.

Nếu tab không xuất hiện:

1. Chạy lại `Install-Tool-Oumijia.bat`; hoặc
2. Xem log `%APPDATA%\Tool Oumijia\logs\register-third-party.log`; hoặc
3. Đóng/mở lại EPLAN.

---

## 6. Nhật ký & kiểm toán

Thư mục log:

```text
%APPDATA%\Tool Oumijia\logs\
```

Các file chính:

- `addin.log` — add-in/script hoạt động;
- `register-script.log` — đăng ký/gỡ add-in;
- `register-third-party.log` — đăng ký/gỡ 2 VEE add-ins;
- `import-*.csv`, `export-*.csv` — thao tác nhập/xuất.

---

## 7. Câu hỏi thường gặp

**Không thấy tab Tool Oumijia?**
→ Khởi động lại EPLAN. Nếu vẫn không: chạy lại `Install-Tool-Oumijia.bat` và kiểm tra log.

**Không thấy VIET EPLAN ELECTRIC?**
→ 2 add-in VEE có thể đăng ký thất bại. Xem `register-third-party.log`.

**Nhập EDZ báo `S030060: Invalid scheme`?**
→ Đã được sửa từ v0.5.0. Cập nhật lên v0.6.2.

**Tick chọn không thấy dấu ✓?**
→ Đã được sửa từ v0.5.1 (checkbox native của Windows).

**Tool có tự phân phối dữ liệu hãng không?**
→ Không. Tool chỉ xử lý file `.edz` người dùng đã có quyền sử dụng.

**Có cần license EPLAN không?**
→ Không dùng license bypass. Bản add-in chạy bằng API chính thức / bản script chạy bằng Scripting miễn phí của EPLAN.
