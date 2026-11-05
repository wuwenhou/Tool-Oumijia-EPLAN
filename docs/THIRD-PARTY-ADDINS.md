# Third-party add-ins — chính sách đóng gói v0.6.2+

**Trạng thái hiện tại:** hai add-in VIET EPLAN & ELECTRIC được **phân phối cùng Tool Oumijia** theo quyền từ tác giả.

Tool Oumijia **không sửa/không biên dịch lại/không sao chép mã nguồn** các add-in này.
Các file được copy nguyên bản và đăng ký qua registry `STATION.EplanEplApiModule` của EPLAN.

## Được đóng gói và tự đăng ký

| Add-in | File | Action | Module type |
|---|---|---|---|
| **VEE Table Maker** | `VEE.EplTableMaker.dll` (v1.2.0.0) | `VEECreateEplanTable` | `VEE.EplTableMaker.AddInModule` |
| **VEE Export Connections** | `VEE.ExportConnections.dll` (v1.0.0.0) | `VE_E_ExportConnections` | `VE_E_ConnectionExport.AddInModule` |

Thông tin tác giả: `third-party/THIRD-PARTY-NOTICES.txt`.

## Cách cài

1. Giải nén release zip.
2. Chạy **`Install-Tool-Oumijia.bat`**.
3. Installer copy 2 DLL tới `%APPDATA%\Tool Oumijia\ThirdParty\` và đăng ký module/action.
4. Mở lại EPLAN.

## Cách gỡ

Chạy **`Uninstall-Tool-Oumijia.bat`** — gỡ Tool và cả 2 add-in VEE.

## Điều chắc chắn

- **Không** vượt license hoặc bypass registration.
- **Không** dùng 2 DLL để xây dựng bản clone độc lập.
- **Không** cam kết chúng chạy trên mọi build EPLAN/2026; xem log
  `%APPDATA%\Tool Oumijia\logs\register-third-party.log` nếu thất bại.
- **Không** tự động cập nhật 2 add-in theo bản mới; chỉ cài bản kèm theo release của Tool.

## License

Tác giả đã cho phép mang chúng vào release công khai cho người dùng.
Nếu quyền này thay đổi, các bản release sau sẽ cần điều chỉnh lại hoặc gỡ chúng khỏi gói.
