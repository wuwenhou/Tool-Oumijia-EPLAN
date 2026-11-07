# Lộ trình — Tool Oumijia (EPLAN Edition)

## Mốc hiện tại: source candidate sau v0.6.0, chưa phát hành

Data Portal native dùng `AddCommandWithId`; Script Edition đã GUI UAT trên Education 2026.0.3. Chưa UAT installer/headless, Add-in runtime, bản EPLAN khác hoặc nhập EDZ trên DB test. Xem `docs/AGENT-HANDOFF-2026-11-07.md`.

## Candidate 2026-11-07 — ribbon EDZ + target selection
- [x] Nhóm `Quản lý EDZ` ba nút theo cột; Portal chỉ trong nhóm Eplan Cloud của Tool, tab Cloud gốc giữ nguyên.
- [x] Script Education 2026.0.3: GUI UAT Portal kéo/đóng X/mở lại, hai form mở/đóng không ghi dữ liệu.
- [x] `-ListInstalled`/`-TargetW3u`, fail khi nhiều target/unsupported; fixture target selection PASS.
- [ ] Build lại shared UI/Add-in DLL và UAT installer/gỡ theo target; không phát hành trước đó.
- [ ] Build/test payload khác cho từng Variant/Version trước khi bật cài toàn bộ.

## v0.6.0 — Data Portal native, bỏ WebView2
- [x] UAT xác định `XDataPortalNavigatorToggle` không phải action; wrapper v0.6.0 chỉ hiện fallback.
- [x] `GfDialogManagerShow /DialogName:DataPortalTabView` trả true nhưng thiếu cửa sổ; loại bỏ đường này. `AddCommandWithId` của tab Cloud gốc đã GUI UAT native.
- [x] Bỏ `Oumijia.PortalHost`, WebView2, profile và precheck mạng khỏi source/gói cài.
- [x] Ribbon 4 lệnh: Nhập EDZ | Xuất EDZ | Data Portal | Menu bar.
- [x] Installer/pack dọn DLL WebView2/PortalHost còn sót.
- [x] UAT mở/đóng form Import/Export và Portal native trên EPLAN 2026 sau deploy/restart; **chưa** import thực hay test đủ workflow EDZ.

## v0.5.2 — Data Portal + preview EDZ (đã thay thế bởi v0.6.0)
- Ribbon 4 lệnh: Nhập EDZ… / Xuất EDZ… / Data Portal… / Menu bar.
- Data Portal nhúng WebView2; không lưu credential, không bypass license/captcha/điều khoản.
- Nhập EDZ hiển thị Tên thiết bị/Hãng/Mã hàng và file lỗi vẫn nằm trong hàng đợi.
- WebView2 bị bỏ ở v0.6.0 do cần đăng nhập riêng và kẹt `Loading...`.

## v0.5.1 — Checkbox native
- Cây chọn part trong dialog Xuất EDZ dùng checkbox native của Windows.
- Áp dụng cho Add-in và Script Edition; xuất phát điểm cho v0.5.2.

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



## Nguyên tắc xuyên suốt
- Chỉ API chính thức + dữ liệu factual; không bẻ license, không tái phân phối dữ liệu hãng
- Mọi thay đổi registration/dữ liệu phải có log + có recipe kiểm chứng headless (xem AGENTS.md mục 5)
