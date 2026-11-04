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

## v0.5.0 — "Script Edition" ✅ (2026-11-04)
- [x] **Bản Script (C#5) chạy không cần API license** — cho EPLAN mới (2024+/2026) chặn add-in chưa ký
- [x] Đăng ký "script tự nạp" hoạt động (ghi `EplanEplApiScriptGui.Scripts`; loader tự thử nạp khi khởi động — kiểm chứng trên 2022)
- [x] **Installer thông minh** tự chọn bản add-in/script + gỡ cài 1 lệnh
- [x] Fix **nhập EDZ** (`S030060` — tự dò field-mapping scheme; smoke test `OK scheme='Standardvorgabe EPLAN'`)
- [x] Fix **đích xuất** (chỉ chọn thư mục + tên tự động theo mã sản phẩm + xem trước + ghi đè + nhớ thư mục)
- [ ] UAT v0.5.0: máy EPLAN 2026.0.3 (bản script) + máy 2022 (bản add-in)

## v0.6 — Tinh chỉnh & mở rộng
- [ ] Icon cho 2 nút ribbon (SVG)
- [ ] Chọn ngôn ngữ hiển thị tên part trong dialog Xuất (hiện en_US)
- [ ] Tiến trình (progress) cho import/export lô lớn
- [ ] Sắp xếp/bôi đậm kết quả tìm kiếm trong cây
- [ ] EDZ writer/editor: tạo gói từ CSV; gắn macro/PDF/STEP; giữ record lạ khi sửa gói
- [ ] Nghiên cứu tab "Parts Management Extension" (API không chính thức — rủi ro tương thích)

## Nguyên tắc xuyên suốt
- Chỉ API chính thức + dữ liệu factual; không bẻ license, không tái phân phối dữ liệu hãng
- Mọi thay đổi registration/dữ liệu phải có log + có recipe kiểm chứng headless (xem AGENTS.md mục 5)
