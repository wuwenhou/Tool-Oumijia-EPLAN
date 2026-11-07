# Kế hoạch kiểm thử candidate sau v0.6.0 — 06/11/2026

> Hồ sơ lịch sử 06-11: opener `GfDialogManagerShow /DialogName:DataPortalTabView` đã được thử và loại bỏ vì thiếu cửa sổ native. Bản vá `AddCommandWithId` và UAT 07-11 xem `CLOUD_NATIVE_UAT.md` + `AGENT-HANDOFF-2026-11-07.md`.

## Hiện trạng và quyết định

- Bản **đang deploy**: v0.6.0 (Script `0.6.0-script.1`, Add-in `0.6.0.0`). Giữ nguyên để rollback; chưa có tag/Release.
- UAT v0.6.0: ribbon 4 nút, Import/Export mở được; nút Portal thất bại do `XDataPortalNavigatorToggle` không phải action được đăng ký.
- Source candidate: Import theo phương án B (cây gói→part bên trái, chỉ header `Chi tiết` + bảng hai cột bên phải). Nút Portal yêu cầu EPLAN hiện `DataPortalTabView` qua `GfDialogManagerShow`, sau đó `DialogManagerShow` nếu action đầu trả `false`/không có/lỗi.
- `GfDialogManagerShow`/`DialogManagerShow` và `DataPortalTabView` đã có trong `regPlatform.xml` của EPLAN 2026.0.3; **chưa kiểm chứng rằng action thực sự hiện tab khi chạy GUI**. Không dùng `XDataPortalNavigate` hoặc helper login làm opener.
- Máy này không có .NET SDK, có Visual Studio 2019 Roslyn `csc.exe` để build UI/Add-in và Framework `csc.exe` C#5 để compile-check Script. Bản source được giải nén **không có `.git`**, nên không thể commit/push trực tiếp từ thư mục hiện tại.

## Các cổng kiểm thử

| Cổng | Việc làm | Điều kiện đạt / dừng |
|---|---|---|
| 1. Build tách biệt | Compile `Oumijia.EplanUi.dll`, `Oumijia.EplAddin.EDZ.dll` và script C#5; staging tại `%LOCALAPPDATA%\Temp\opencode\Tool-Oumijia-UAT-20261106`. | Cả ba compile không lỗi; các dependency và assembly version khớp. Không ghi đè `output/Tool-Oumijia-v0.6.0` hoặc runtime đang deploy. |
| 2. UI offscreen | Dùng ZIP `.edz` giả lập không có dữ liệu hãng và một đường dẫn lỗi. Mở form offscreen; kiểm tra cây, bảng hai cột, nhãn/giá trị part, lỗi và thao tác xóa. | Không crash; node gói/part đúng; bên phải không còn grid part; part thiếu giá trị là `—`. Đã **đạt** trên build staging. |
| 3. Deploy UAT | Sau khi người dùng cho phép thử GUI, lưu hash/backup các DLL và script hiện tại; chép candidate vào shared runtime và edition đang dùng, rồi khởi động EPLAN. | Không tự đóng project/đổi DB. Nếu registration/shadow-copy không tải bản mới, dừng và kiểm tra log trước khi thay settings. |
| 4. Portal GUI | Bấm `Data Portal` trên ribbon; quan sát tab EPLAN native và log `DataPortal native attempt … execute=...`. | Phải **nhìn thấy đúng UI native của EPLAN**, không chỉ `Execute=true`. Nếu không thấy, không gọi là đạt; dùng fallback hướng dẫn `Eplan Cloud → Data Portal`, chẩn đoán tiếp. Không dùng browser riêng hoặc lách quyền. |
| 5. Import/Export | Thêm nhiều gói, gói lỗi, chọn gói/part, resize/copy; xác nhận nút nhập vẫn gửi toàn bộ queue. Export thử tập nhỏ, không chọn chế độ xuất toàn bộ loại bản ghi. | Không hồi quy; ribbon giữ đúng `Nhập EDZ | Xuất EDZ | Data Portal | Menu bar`. |
| 6. DB test | Người dùng xác nhận bằng tên/đường dẫn **Parts Database thử nghiệm** đang active; chỉ sau đó nhập EDZ kiểm thử có quyền sử dụng. | Xác nhận kết quả trong Parts Management + `addin.log` và `import-*.csv`. Không import vào DB thật nếu chưa có đồng ý riêng. |
| 7. Release | Sau khi người dùng xác nhận UAT đạt: bump v0.6.1, cập nhật CHANGELOG/guide/release notes, build+pack mới, SHA-256. | Không tag/push/GitHub Release trước cổng này; cần checkout git thực nếu phát hành. |

## Hành vi Import cần giữ

- `UpdateAndAppend`, nhận dạng theo `PartNumber`, tự dò import scheme; preview lỗi không chặn gửi gói cho API EPLAN.
- Cây WinForms chọn một node: `Xóa gói chọn` xóa gói cha kể cả khi đang chọn part; `Xóa hết` xóa toàn bộ queue. Nhấn nhập vẫn xử lý **tất cả** gói trong queue, không phụ thuộc node đang chọn.
- Preview cục bộ tối đa 500 part/gói; không suy diễn thuộc tính không có trong EDZ. Panel phải chỉ có bảng `Thuộc tính | Giá trị`, nhãn/order theo Export; double-click giá trị để copy.
- Không commit tệp EDZ thật/macro/ảnh. File thử nghiệm giả lập ở thư mục staging, không ở source.

## Giữ an toàn và rollback

- Backup hiện có: `%USERPROFILE%\Desktop\HSG\backups\Tool-Oumijia-v060-deploy-20261106-111942`.
- Runtime hiện có: `%APPDATA%\Tool Oumijia\Portal` và `%APPDATA%\Tool Oumijia\AddIn`. Không thay các thư mục này trong cổng build/offscreen; trước deploy mới lưu hash/backup mới và xác định edition đang active.
- Nếu UAT thất bại: khôi phục đúng các tệp candidate đã thay từ backup sau khi EPLAN đóng; không reset/xóa các thay đổi khác của người dùng.
- Không thay proxy/hosts/license/cookie, không lưu thông tin đăng nhập hoặc URL phiên trong log.
