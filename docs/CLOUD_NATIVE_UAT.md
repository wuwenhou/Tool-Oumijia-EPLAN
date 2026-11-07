# Kế hoạch và kết quả UAT — Eplan Cloud native (2026-11-06)

> Tài liệu lịch sử ngày 06-11: nhóm EDZ lúc đó còn Portal thứ tư. Bản source 07-11 đã đổi thành nhóm **Quản lý EDZ** ba nút và Portal chỉ trong nhóm Cloud. Kết quả mới ở `AGENT-HANDOFF-2026-11-07.md`.

## Phạm vi

- Giữ nhóm đầu tiên đúng thứ tự **Nhập EDZ | Xuất EDZ | Data Portal | Menu bar**.
- Thêm nhóm **Eplan Cloud** vào tab **Tool Oumijia**, sao chép bảy nút native từ tab **Eplan Cloud** gốc; giữ nguyên tab gốc.
- Không nhúng browser của Tool, không lưu thông tin đăng nhập, không bỏ qua license, không thay đổi project/Parts Database.

## Kế hoạch kiểm chứng

1. Sao lưu USER settings và runtime hiện có, kiểm tra SHA-256; chỉ deploy khi EPLAN đã đóng.
2. Tìm command ID theo tên trên tab Eplan Cloud lúc chạy; nếu chưa sẵn sàng, retry trên UI thread. Không hard-code ID hoặc gọi riêng `DataPortalTabView`.
3. Thử clone một nút Data Portal trong nhóm tạm, kiểm tra cửa sổ có title/X, kéo ra floating, di chuyển, đóng và mở lại. Gỡ nhóm thử.
4. Compile-check hai edition; deploy **Script Edition** đã sao lưu; khởi động lại EPLAN, kiểm tra thứ tự nút, nhóm Cloud, nhấn Data Portal và UAT lại thao tác cửa sổ.
5. Mở/đóng form Nhập/Xuất mà không thực hiện nhập/xuất; kiểm tra log startup, action trace và hash runtime.

## Kết quả trên EPLAN Education 2026.0.3

- Sao lưu đã xác thực: `backups/Tool-Oumijia-cloud-preuat-20261106-192251` (86 mục, manifest SHA-256).
- Probe `AddCommandWithId` thành công. Nút thử Data Portal mở cửa sổ native; kéo pane thành floating, di chuyển, đóng bằng X và mở lại đều đạt. Nhóm UAT đã gỡ.
- Script Edition mới khởi động bình thường; Eplan Cloud command có sau một lần retry (log `Native Cloud ribbon ready after retries=1`).
- Nhóm chính theo `IndexInCommandGroup`: `Nhập EDZ (0)`, `Xuất EDZ (1)`, `Data Portal (2, native)`, `Menu bar (3)`; nhóm Eplan Cloud có đúng bảy command native. Tab Eplan Cloud gốc vẫn hiện.
- Sau deploy, nút chính và nút trong nhóm Cloud đều mở cửa sổ **Data Portal** native; nút chính được kiểm tra đóng X, mở lại và kéo pane ra floating. Trace đi qua `MfEPulseCommandExecuteAction → XMVCCloudSolutionAction → ECloudSolutionsViewBarN → GfDialogManagerToggle`.
- Nhập EDZ mở form preview (0 gói, nút nhập disabled); Xuất EDZ mở form (0 part chọn, nút xuất disabled). Đóng cả hai, không ghi dữ liệu.
- Session mới không có `S046006/S046007`; thông báo mức cao duy nhất là EPLAN báo có bản cập nhật.

## Chưa kiểm chứng / giới hạn

- Sáu nút Cloud còn lại đã được sao chép nhưng chưa UAT chức năng từng nút; quyền dịch vụ vẫn do EPLAN kiểm soát.
- Add-in source compile-check bằng C#/.NET Framework với EPLAN 2026; không có .NET SDK trên máy nên chưa build package hoặc deploy Add-in. Bản chạy ở đây là Script Edition.
- Import EDZ thật chờ Parts Database thử nghiệm được xác nhận; không thử trên DB hiện tại.
- Nhận diện command theo tên tiếng Anh của EPLAN 2026; ngôn ngữ/bản EPLAN khác cần UAT tương thích.
- Chưa tạo release, tag hoặc push: thư mục source là bản trích xuất không có `.git`.
