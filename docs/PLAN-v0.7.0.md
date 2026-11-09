# Kế hoạch v0.7.0 — Nhập EDZ theo từng part + dọn 2026-11-09

> **Trạng thái:** triển khai và đóng gói ngày 2026-11-09, theo yêu cầu người dùng ngày 2026-11-09.
> **Nền:** `main` ở tag `v0.6.0` (`82a3439`).
> **Mục tiêu:** Nhập EDZ cho phép tick từng part (không bắt buộc nhập nguyên gói); đồng bộ giao diện Nhập/Xuất; sửa đọc EDZ; đóng gói `Tool-Oumijia-v0.7.0-2022.zip` và `Tool-Oumijia-v0.7.0-2026.zip`; phát hành GitHub release pre-release.

## 1. Vì sao cần v0.7.0

- v0.6.0 (2026-11-08) PASS UAT 2022.0.3 về ribbon/giao diện, nhưng Nhập EDZ chỉ nhập được **cả gói EDZ**: không tick được part, tên part hiển thị chuỗi đa ngôn ngữ thô (`cs_CZ@…;en_US@…`) và `??_??@X`, và một gói hợp lệ do Tool xuất bị báo "Không tìm thấy part trong EDZ".
- Xuất EDZ có dòng `→ sẽ tạo: …` bị che mất (khởi tạo `_outputGroup` cao 126 px).

## 2. Phạm vi

### 2.1 Trong phạm vi

- Nhập EDZ: checkbox trên cây `gói EDZ → part`; tick gói = chọn hết part trong gói; bỏ tick 1 part = loại part đó khỏi lần nhập; nút toolbar **Chọn hết / Bỏ chọn**; status **Chọn: X/Y part**; nút `NHẬP VÀO PARTS DATABASE ▸` chỉ gửi `MDPartsManagement.ImportParts` với các part đã tick; không tick part nào trong gói → bỏ qua gói đó và ghi log.
- `Oumijia.EdzReader`:
  - `PartXmlParser` ghi nhận toàn bộ attribute XML part của EPLAN (trước đó bị bỏ qua → preview 0 part);
  - `EdzText.CleanMultiLanguage()` dọn chuỗi đa ngôn ngữ (ưu tiên `en_US` → `de_DE` → đoạn hợp lệ) cho Tên 1/2/3, mô tả;
  - `EdzPartInfo.PackageKey` lấy từ `manifest.xml` (`items/partxml/<locator>` → `<package type="part" key="…">`) để lọc đúng part;
  - `EdzStreamFilter`/`EdzPackageFilter` tạo EDZ tạm chỉ chứa `manifest.xml` đã lọc `<package>` + các file được package đó tham chiếu; dùng `SevenZipCopyWriter` (7z 1 folder, coder Copy) — không phụ thuộc 7-Zip bên ngoài.
- Xuất EDZ: `UpdateModes()` chạy ngay lúc dựng form; `_outputGroup` 158 px (cây) / 228 px (theo loại bản ghi); panel đáy 340/400 px.
- Đồng bộ giao diện: `EdzDialogStyle` dùng chung cho Nhập/Xuất (1120×780, header 10F bold `#173858`, toolbar 34/28 px, details 190/320, action row 44 px, log Consolas 8.5F); nhãn cây dạng `PARTNR (Designation)`.
- Đóng gói và phát hành GitHub release pre-release v0.7.0 (2 zip + SHA256SUMS.txt).

### 2.2 Ngoài phạm vi

- Không ghi vào Parts Database trong quá trình kiểm thử tự động; nhập thật chỉ theo chỉ dẫn người dùng (database test).
- Không thay đổi Ribbon, không đụng Cloud/Portal, không thay `libcef.dll`/WebView2, không credential, không bypass license.
- Không thử lại thí nghiệm "nút Cloud mở trình duyệt" (AddIn 2026-11-08): đã rollback về v0.6.0, không nằm trong bản này.
- Add-in native cho EPLAN 2026: máy này không có Platform 2026.0.3, không build/UAT được; 2026 nhận **Script Edition** với shared runtime mới.

## 3. Kế hoạch kiểm thử

| Hạng mục | Cách kiểm | Kết quả |
|---|---|---|
| Reader attributes (`PARTNR`…) | PowerShell harness, `parts.edz` 15 part, `SIE.6ES7131-4BD01-0AA0.edz` 1 part | PASS |
| Đọc tên đa ngôn ngữ | `EdzText.CleanMultiLanguage` unit check (`??_??@X`, `cs_CZ@…;en_US@…`) | PASS |
| Lọc EDZ 2/15 part | sinh EDZ tạm → `7z t` (Everything is Ok) → đọc lại bằng reader → 2 part đúng; macro 3D + manufacturer dùng chung được giữ; part không chọn + `errors.txt` bị bỏ | PASS |
| Lọc hết (SIE 1/1) | không tạo file tạm, nhập tệp gốc | PASS |
| Tham số lỗi (0 part / key lạ) | ném `ArgumentException` / `InvalidOperationException` với thông báo tiếng Việt | PASS |
| GUI Nhập EDZ | EPLAN Electric P8 2022.0.3: tick/untick part, tick/untick gói, **Chọn hết / Bỏ chọn**, status `Chọn: X/15`, nút NHẬP enable/disable đúng trạng thái; không bấm nút nhập | PASS |
| GUI Xuất EDZ | không ghi DB/file; kiểm tra off-screen layout (`ui-check.ps1`) | PASS |
| Build/giới hạn | net48 + Platform 2022.0.3 API: 0 warning/0 error | PASS |
| Nhập thật vào Parts DB | chỉ DB test, do người dùng quyết định | **CHƯA LÀM** |

## 4. Đóng gói / phát hành

1. Bump `<Version>0.7.0</Version>` (3 csproj) và `ScriptVersion = "0.7.0-script.1"`.
2. `tools/pack-2022.ps1 -Version 0.7.0` (sau khi sửa tham số hoá; v0.6.0 không bị ghi đè) → `output\Tool-Oumijia-v0.7.0-2022.zip`.
3. Ráp `output\Tool-Oumijia-v0.7.0-2026.zip` theo mẫu v0.6.0-2026: Script Edition + `script\portal\` (shared runtime net48 mới) + docs cập nhật.
4. Commit `release: v0.7.0`, tag `v0.7.0`, push `main`, `gh release create --prerelease` với SHA256SUMS.txt.

## 5. Rủi ro

- EPLAN có thể từ chối tệp EDZ do `SevenZipCopyWriter` ghi → lỗi sẽ hiện qua `ImportPartsSupport`; người dùng dùng database test; rollback về v0.6.0 bằng Install-2022.ps1/DLL đã backup.
- Shared runtime net48 trong 2026 chưa GUI UAT trên 2026 → v0.7.0-2026.zip ghi rõ, giữ nguyên payload Script Edition v0.6.0 cho ai chưa kiểm.
- Tệp tạm EDZ nằm trong `%TEMP%\ToolOumijia\edz-filter\`, tự xóa sau import; lỗi giữa chừng cũng được xóa trong `finally`.
