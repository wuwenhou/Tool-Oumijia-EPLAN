# Third-party EPLAN add-ins — nguồn tham khảo, không phân phối lại

Tool Oumijia **không đóng gói / không bán kèm / không sao chép** các add-in của tác giả khác.
Tài liệu này chỉ liệt kê nguồn chính thức và cách gọi có điều kiện từ EPLAN.

Mọi tên sản phẩm, logo, tệp tin và quyền sở hữu thuộc về tác giả/hãng tương ứng.

## Hiện đã đăng ký vào Tool Oumijia

| Add-in | Tác giả / nguồn | Tool sẽ làm gì |
|---|---|---|
| **VEE Export connections data** | VIET EPLAN & ELECTRIC; video: <https://www.youtube.com/watch?v=a8u1O2i7Wxs>; link tác giả: `https://t.me/EPLAN4UCHAT/13646` | Tìm action `VE_E_ExportConnections`. Nếu có, nút gọi nó; nếu không, hiển thị thông báo chưa cài và mở link của tác giả. Không phân phối DLL. |
| **VEE Table Maker** | VIET EPLAN & ELECTRIC; video: <https://www.youtube.com/watch?v=ACp_sD4vdAw>; link tác giả: `https://t.me/EPLAN4U/12788` | Tìm action `VEECreateEplanTable`. Nếu có, nút gọi nó; nếu không, hiển thị thông báo chưa cài và mở link của tác giả. Không phân phối DLL. |
| **Show Menu Bar** | VIET EPLAN & ELECTRIC; video: <https://www.youtube.com/watch?v=SXERO8SbehA>; link tác giả: `https://t.me/EPLAN4U/12797` | Tool **không phân phối và không tự cài**. Nút **Menu bar** trong Tool gọi action EPLAN chính thức `MfToggleMainMenuAction`; add-in của tác giả không bắt buộc. |

## Chưa phát hành / không có đủ căn cứ để tích hợp

| Add-in | Tình trạng | Quyết định |
|---|---|---|
| **Auto Update Report, Export PDF & Backup in One Click** | Theo mô tả video `snyfriYe9KM`, công cụ đang hoàn thiện và sẽ phát hành sau | Không nút/không link gọi chính thức cho đến khi tác giả phát hành |
| **VEE EDZ Killer** | Add-in thu thập/chuẩn hóa dữ liệu Part Data; chưa có link tải công khai trong mô tả video `d7DVz9nddeA` | Không đưa vào release cho đến khi có nguồn chính thức |
| **VEE Tank Game 1** | Game giải trí, không phải công cụ kỹ thuật | Ngoài phạm vi Tool Oumijia |

## EPLAN Data Portal và nguồn .edz chính thức

Tool Oumijia chỉ import/export file người dùng đã có quyền sử dụng.

- EPLAN Data Portal: `https://dataportal.eplan.com/`
- Các nguồn .edz của hãng: Phoenix Contact, Delta, Omron, Siemens, Eaton, Lenze, Weidmüller, ifm, WAGO…
- Không bypass đăng nhập, license, điều khoản sử dụng hoặc quyền truy cập của hãng.
- Không commit/redistribute .edz của hãng vào repository này.

## Điều kiện để thêm add-in bên ngoài vào release

Chỉ xem xét khi **tất cả** các điều sau được thỏa mãn:

1. Tác giả cung cấp license cho việc phân phối/đi kèm, hoặc
2. Có thỏa thuận hợp tác bằng văn bản, hoặc
3. Tool chỉ gọi add-in độc lập đã được cài đặt bởi người dùng và không chứa/copy mã nguồn/tài nguyên của add-in đó.

Nếu không đủ điều kiện: chỉ cập nhật tài liệu hướng dẫn, **không** nhúng Binary/DLL/tài nguyên hay icon của add-in bên ngoài vào gói release.
