# Lộ trình — Tool Oumijia (EPLAN Edition)

## v0.1.0 — App standalone: phân loại thư viện EDZ ✅ (đã bỏ ở v0.3.0)
- WinForms: quét `.edz` → phân loại Hãng/Nhóm → xếp thư viện; log CSV
- *(Đã xóa khỏi repo ở v0.3.0 theo quyết định chỉ giữ add-in — xem git history)*

## v0.2.0 — Add-in EPLAN đầu tiên ✅ (2026-11-04)
- Ribbon "Tool Oumijia": Nhập EDZ hàng loạt vào Parts Database; Xuất EDZ từ Parts DB
- Registration script đã kiểm chứng end-to-end trên EPLAN 2022.0.3
- Installer 1 click

## v0.3.0 — "Add-in duy nhất" ✅ (2026-11-04)
- [x] Bỏ hoàn toàn app exe — chỉ còn add-in (ribbon 2 nút)
- [x] **Dialog Xuất EDZ v2**: chọn part bằng cây nhóm kiểu navigator EPLAN
      (checkbox 3 trạng thái, tìm kiếm, "Lấy từ Parts Management", đếm "Đã chọn: N")
- [x] Fix registration bị giá trị lịch sử đè (delete-first + VERIFY)
- [x] Fix hiển thị tên nhóm/hãng/part (GetDisplayString theo ngôn ngữ)
- [x] Action chẩn đoán `OumijiaDiagParts` + script chẩn đoán ribbon/registration
- [x] Gói release add-in-only (~26 KB)
- [ ] **UAT người dùng** trên database thật → tinh chỉnh (đang chờ phản hồi)

## v0.4.0 — "Hoàn thiện theo UAT" ✅ (2026-11-04)
- [x] Fix tick chọn không hiện dấu ✓ (buộc vẽ lại + dòng chọn in đậm)
- [x] Fix nút "XUẤT RA EDZ ▸" / "Chọn…" bị khuất (TableLayoutPanel + Dock, an toàn mọi DPI)
- [x] Panel chi tiết kiểu EPLAN Overview (đầy đủ thuộc tính + copy giá trị)
- [x] Chế độ xuất "Theo loại bản ghi (như EPLAN)" — cờ API đã kiểm chứng thực tế
- [x] Nhập EDZ đơn giản hóa (bỏ hàng tùy chọn; mặc định cập nhật + thêm mới)
- [x] Construct-test UI offscreen trong DiagParts (bắt được 1 lỗi crash mở dialog — đã sửa)
- [ ] UAT v0.4.0 trên máy người dùng → tinh chỉnh tiếp nếu cần

## v0.5 — Tinh chỉnh & mở rộng
- [ ] Icon cho 2 nút ribbon (SVG)
- [ ] Chọn ngôn ngữ hiển thị tên part trong dialog Xuất (hiện en_US)
- [ ] Tiến trình (progress) cho import/export lô lớn
- [ ] Sắp xếp/bôi đậm kết quả tìm kiếm trong cây
- [ ] EDZ writer/editor: tạo gói từ CSV; gắn macro/PDF/STEP; giữ record lạ khi sửa gói
- [ ] Nghiên cứu tab "Parts Management Extension" (API không chính thức — rủi ro tương thích)

## Nguyên tắc xuyên suốt
- Chỉ API chính thức + dữ liệu factual; không bẻ license, không tái phân phối dữ liệu hãng
- Mọi thay đổi registration/dữ liệu phải có log + có recipe kiểm chứng headless (xem AGENTS.md mục 5)
