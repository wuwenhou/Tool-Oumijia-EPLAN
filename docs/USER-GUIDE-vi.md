# Hướng dẫn sử dụng — Tool Oumijia (v0.7.0)

> **Phiên bản hiện tại: v0.7.0 (pre-release).** Script Edition và shared runtime mới **chưa GUI UAT trên Education 2026.0.3**; Add-in 2026 không có. Add-in 2022.0.3 đã GUI UAT giao diện; **nhập thật vào Parts Database chưa được test** — luôn dùng database test. Xem [INSTALL.md](INSTALL.md).

Ribbon **Tool Oumijia** gồm:

```text
Quản lý EDZ: Nhập EDZ → Xuất EDZ → Menu bar (theo cột)
Eplan Cloud: Eplan Cloud dashboard, Data Portal, Project generation (Free),
             User management, Parts management (deprecated), Eplan eLearning,
             Guided Installation (command native EPLAN)
```

- **Data Portal**: trong nhóm **Eplan Cloud** của Tool, sao chép command native của EPLAN theo ID tìm lúc chạy; cửa sổ native đã kiểm tra kéo/đóng X/mở lại. Tab Cloud gốc vẫn có Portal.
- **Menu bar**: nếu EPLAN còn `MfToggleMainMenuAction` thì bật/tắt classic menu; trên EPLAN 2026 action này không còn nên nút sẽ báo rõ thay vì chết lặng (xem §4).
- Không còn WebView2, không còn nút **Data Portal Web**, không còn profile trình duyệt riêng của Tool.

Cả Add-in và Script Edition dùng cùng giao diện. Cách cài: [INSTALL.md](INSTALL.md).

---

## 1. Nhập EDZ

1. Ribbon **Tool Oumijia → Nhập EDZ**
2. **Thêm tệp…** hoặc **Thêm thư mục…** để gom các gói `.edz`.
3. Cây bên trái: **tick gói = chọn hết part** trong gói; bỏ tick một part = loại part đó. **Chọn hết / Bỏ chọn** chọn/bỏ toàn bộ; status hiển thị `Chọn: X/Y part`. Nếu cần xóa, **Xóa gói chọn** xóa cả gói chứa part đang chọn.
4. Bên phải chỉ có **Chi tiết** với bảng `Thuộc tính | Giá trị`: gói hiện đường dẫn/kích thước/trạng thái; part hiện metadata theo các nhãn giống Export. File EDZ lỗi vẫn được giữ trong hàng đợi và có dòng lỗi preview.
5. Nháy đúp giá trị để copy. Xem trước không thay đổi database; nút nhập chỉ xử lý **các part đã tick** (gói không tick part nào sẽ bị bỏ qua).
   - Nếu chỉ chọn một phần part trong một gói, Tool tự tạo **EDZ tạm** (chỉ chứa `manifest.xml` đã lọc và các file được package tham chiếu, writer 7z nội bộ), nhập tệp tạm đó, rồi tự xóa.
6. Bấm **NHẬP VÀO PARTS DATABASE ▸** và xác nhận.

Mặc định: **cập nhật part đã có + thêm part mới**, nhận dạng theo **mã hàng**, tự dò field-mapping scheme. Không có tùy chọn mode trong UI để tránh nhập sai.

Log:

- `%APPDATA%\Tool Oumijia\logs\addin.log`
- `%APPDATA%\Tool Oumijia\logs\import-YYYYMMDD.csv`

---

## 2. Xuất EDZ

1. Đảm bảo Parts Database đang mở trong EPLAN.
2. Ribbon **Tool Oumijia → Xuất EDZ**
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

## 3. Data Portal

1. Ribbon **Tool Oumijia → Eplan Cloud → Data Portal** (hoặc tab **Eplan Cloud** gốc → Data Portal). Chỉ coi là đạt khi thấy cửa sổ native có title/X.
2. Dùng phiên EPLAN đã đăng nhập để tìm part, xem chi tiết, Insert Item và tải theo Download list của EPLAN.
3. Tải `.edz` theo quyền tài khoản.
4. Mở **Tool Oumijia → Nhập EDZ**, chọn các file `.edz` vừa tải, xem preview rồi nhập vào **database test**.

Nếu nút **Data Portal** báo không mở được:

- mở trực tiếp **Eplan Cloud → Data Portal** trong EPLAN;
- kiểm tra máy được IT cho phép kết nối Data Portal (firewall/proxy/antivirus có thể chặn `dataportal.eplan.com`, `login.eplan.com`, `apps.eplan.com`);
- xem log `%APPDATA%\Tool Oumijia\logs\addin.log` (probe `Native Cloud ribbon`/retry); không chạy riêng `DataPortalTabView`.

Không sửa proxy/hosts/chứng chỉ để lách kiểm tra license. Không bypass captcha/điều khoản; chỉ tải file bạn có quyền.

---

## 4. Menu bar

- Nút **Menu bar** dò action `MfToggleMainMenuAction` của EPLAN khi tạo ribbon và ghi kết quả vào `addin.log` (dòng `Ribbon probe`).
- Nếu EPLAN còn action này (các bản cũ), nút gọi trực tiếp action để bật/tắt classic menu.
- Trên **EPLAN 2026**, action này không còn trong Bin (đã kiểm chứng bằng tìm chuỗi trong `Eplan.EplApi.*.dll`/`*.erx` của 2026.0.3) nên nút chuyển sang action `OumijiaScriptToggleMainMenu` / `OumijiaToggleMainMenu` để **báo rõ** và hướng dẫn dùng ribbon EPLAN (File, Home, Insert, Edit, View, Devices, Connections, Tools, Pre-planning, Master data, Eplan Cloud) thay thế. Tool không giả lập menu của EPLAN.

---

## 5. Mẹo sử dụng

- Luôn thử trên **database test** trước khi nhập database chính.
- EDZ tải từ Data Portal của EPLAN không nên sao chép thủ công vào Master Data; hãy dùng **Nhập EDZ**.
- File `.edz` không đọc được vẫn nhập được — Tool hiển thị trạng thái lỗi riêng.
- Đừng xin dùng tài khoản công ty ngoài phạm vi được IT cho phép.

## 6. Ghi / hỗ trợ

- Log: `%APPDATA%\Tool Oumijia\logs\`
- CSV `import-*.csv` / `export-*.csv`: thao tác của Tool; việc tải qua Portal native do EPLAN quản lý.
- Log không lưu password/cookie; không gửi log công khai nếu chứa dữ liệu nội bộ.
