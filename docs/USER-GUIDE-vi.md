# Hướng dẫn sử dụng — Tool Oumijia (EPLAN Edition) v0.5.2

Ribbon **Tool Oumijia** có bốn lệnh:

```text
Nhập EDZ… | Xuất EDZ… | Data Portal… | Menu bar
```

Cả Add-in và Script Edition dùng cùng giao diện. Cách cài: [INSTALL.md](INSTALL.md).

---

## 1. Nhập EDZ

1. Ribbon **Tool Oumijia → Nhập EDZ…**
2. **Thêm tệp…** hoặc **Thêm thư mục…** để gom các gói `.edz`.
3. Chọn file trong cột trái để xem chi tiết.
4. Cột phải hiển thị:
   - tên tệp;
   - dung lượng và ngày sửa;
   - số part đọc được;
   - lưới **Tên thiết bị | Hãng | Mã hàng**.
   - File EDZ lỗi vẫn được giữ trong hàng đợi; preview bị bỏ qua và lý do được ghi log.
5. Chọn dòng part trong lưới để focus part. Đây là bước xem trước; không thay đổi clipboard/database.
6. Bấm **NHẬP VÀO PARTS DATABASE ▸** và xác nhận.

Mặc định: **cập nhật part đã có + thêm part mới**, nhận dạng theo **mã hàng**, tự dò field-mapping scheme. Không có tùy chọn mode trong UI để tránh nhập sai.

Log:

- `%APPDATA%\Tool Oumijia\logs\addin.log`
- `%APPDATA%\Tool Oumijia\logs\import-YYYYMMDD.csv`

---

## 2. Xuất EDZ

1. Đảm bảo Parts Database đang mở trong EPLAN.
2. Ribbon **Tool Oumijia → Xuất EDZ…**
3. Dùng ô **Tìm** để lọc cây theo mã hàng/tên/hãng.
4. Tick part/nhóm; tick nhóm để chọn cả nhánh.
5. Panel bên phải hiển thị chi tiết part hoặc tổng số part của nhóm.
6. Chọn **Cách xuất**:
   - `Part đã chọn trên cây`, hoặc
   - `Theo loại bản ghi (như EPLAN)`.

### 2.1 Xuất part đã chọn

- `Một file EDZ`: 1 part → tên file theo mã; N part → `parts.edz`.
- `Mỗi part một file`: mỗi part một file `.edz`.
- Thư mục chọn được ghi nhớ.

### 2.2 Theo loại bản ghi

Tick các loại cần xuất:

- Parts;
- Accessory list;
- Accessory placement;
- Drilling pattern;
- Connection point pattern;
- Customer / Manufacturer / Supplier.

Chế độ này xuất toàn bộ bản ghi theo loại đã tick; file có thể rất lớn. Part được tick trên cây không được dùng trong chế độ này.

---

## 3. Data Portal — popup đăng nhập

1. Ribbon **Tool Oumijia → Data Portal…**
2. Đăng nhập bằng tài khoản Data Portal hợp lệ.
3. Trang chính thức mở trong cửa sổ nhúng của Tool.
4. Chọn thư mục tải phía trên trang.
5. Tải tài liệu/thiết bị theo quyền account.
6. File `.edz` tải xong có thể:
   - giữ trong thư mục đã chọn; hoặc
   - bấm **Nhập EDZ vừa tải** để mở hộp Nhập với các file đó.
7. Tài liệu khác (PDF, DOCX, XLSX, ảnh…) được lưu về folder; Tool không tự nhập hoặc sửa.
8. Nút **Xóa dữ liệu phiên** xóa cookie/cache portal.

### Nếu màn hình portal đen/lỗi

- Kiểm tra máy được IT cho phép kết nối Data Portal.
- Dùng **Mở ngoài** để mở portal trong trình duyệt.
- Không sửa proxy/hosts/chứng chỉ để lách kiểm tra license.
- Không bypass captcha/điều khoản; chỉ tải file bạn có quyền.

Cookie/cache phiên ở `%APPDATA%\Tool Oumijia\WebView2\`.

---

## 4. Menu bar

- Nút **Menu bar** gọi action chính thức `MfToggleMainMenuAction` của EPLAN.
- Nếu EPLAN không cung cấp action này, thanh menu không thay đổi và trạng thái/log cho biết action không khả dụng.

---

## 5. Mẹo sử dụng

- Luôn thử trên **database test** trước khi nhập database chính.
- EDZ download không nên sao chép thủ công vào Master Data; hãy dùng **Nhập EDZ…**.
- File `.edz` không đọc được vẫn nhập được — Tool hiển thị trạng thái lỗi riêng.
- Đừng xin dùng tài khoản công ty ngoài phạm vi được IT cho phép.

## 6. Ghi / hỗ trợ

- Log: `%APPDATA%\Tool Oumijia\logs\`
- `data-portal-downloads-*.csv`: lịch sử tải; URL đã bỏ query/fragment.
- Log không lưu password/cookie; không gửi log công khai nếu chứa dữ liệu nội bộ.
