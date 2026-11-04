# Roadmap — Tool Oumijia (EPLAN Edition)

## Nguyên tắc

- Mỗi phiên bản là một sản phẩm **dùng được thật**, có test và release notes đàng hoàng.
- Core giữ `netstandard2.0`; mọi thứ EPLAN-specific đặt trong add-in.
- Ranh giới (không đổi): chỉ API chính thức + dữ liệu factual, không bypass license/ToU,
  không commit dữ liệu hãng thật.

## v0.1.0 — Nền tảng Export/Import ✅ (đã phát hành)

- Đọc EDZ ZIP + 7z, trích metadata, classifier rule-based, xếp thư viện an toàn, log CSV
- Cài macro vào EPLAN master data
- App WinForms (net8) + khung add-in (net48), 25+ tests, CI, tooling pack/make-samples

## v0.2 — EPLAN Add-in: batch import EDZ (mục tiêu chính)

- [ ] Ribbon/toolbar "Tool Oumijia" trong EPLAN (Import / Export / Settings)
- [ ] Action `OumijiaImportEdz`: import danh sách EDZ vào Parts Database qua EPLAN API
      (nghiên cứu `Eplan.EplApi.MasterData` + `IXPartsImportExportEdz`, đối chiếu Parts Management)
- [ ] Chế độ import: chỉ thêm mới / cập nhật / thay thế + chọn database đích (test/main)
- [ ] Báo cáo kết quả từng gói ngay trong UI + log CSV
- [ ] Kiểm tra trên EPLAN Education trước, tài liệu hóa giới hạn (nếu có)

## v0.3 — EDZ Writer / Editor

- [ ] `EdzArchiveWriter`: tạo & repack `.edz` (zip trước; 7z nghiên cứu sau)
- [ ] Tạo gói từ CSV/Excel: part fields + function templates + connection points
- [ ] Gắn/thay macro vào gói (`P_ARTICLE_GROUPSYMBOLMACRO` / `P_ARTICLE_MACRO`), giữ nguyên phần khác
- [ ] Validation trước/sau xuất (bắt buộc, độ dài, trùng chân, tính toàn vẹn archive)
- [ ] Export từ Parts DB ra EDZ (backup thư viện cá nhân)

## v0.4+ — Bridge chuyển đổi (từng bước, có kiểm chứng)

- [ ] EDZ → QElectroTech `.elmt` (chỉ dữ liệu factual; tham chiếu importer sẵn có của QET)
- [ ] DXF: đóng gói/xuất phần `dxf_data` bên trong EDZ
- [ ] (Xa hơn) Nghiên cứu luồng EPLAN ↔ AutoCAD qua `HEServices.Import/Export` trong add-in
- [ ] (Xa hơn) Đóng góp cải thiện cho QET (hỗ trợ thêm container zip cho EDZ import)

## Việc chất lượng xuyên suốt

- [ ] Localization EN (UI hiện tiếng Việt)
- [ ] High-DPI + icon ứng dụng
- [ ] Test cho `MacroInstaller`, `OverrideStore`, config round-trip
- [ ] Ghi chú eCl@ss code thực tế cho classifier (khi có dữ liệu kiểm chứng)
- [ ] Screenshot trong README + demo GIF
- [ ] Ký số gói phát hành (SignPath/certificate) khi có điều kiện
