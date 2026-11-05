TOOL OUMIJIA — THIRD-PARTY ADD-INS
==================================================

File này chứa hai add-in của VIET EPLAN & ELECTRIC, được phân phối
cùng Tool Oumijia theo sự đồng ý của tác giả:

- VEE.EplTableMaker.dll   (v1.2.0.0)
- VEE.ExportConnections.dll (v1.0.0.0)

Nội dung:
  third-party\
    VEE.EplTableMaker\         add-in tạo technical table
    VEE.ExportConnections\     add-in xuất dữ liệu kết nối
    OumijiaRegisterThirdParty.cs  script đăng ký/gỡ đăng ký
    THIRD-PARTY-NOTICES.txt    thông tin tác giả + điều khoản phân phối

NGƯỜI DÙNG CHỈ CẦN CHẠY:
  Install-Tool-Oumijia.bat

Installer sẽ tự:
  1. Cài Tool Oumijia phù hợp với phiên bản EPLAN;
  2. Copy 2 DLL vào %APPDATA%\Tool Oumijia\ThirdParty\;
  3. Đăng ký bằng @start\Script trong nháy máy EPLAN:

     VEE.EplTableMaker\AddInModule
     VE_E_ConnectionExport\AddInModule

     và các action:

     VEECreateEplanTable
     VE_E_ExportConnections

KHÔNG cần dùng dialog "API modules (assemblies) > Load" nếu installer chạy thành công.
Nếu không muốn cài tính năng này, chọn bỏ qua phần third-party và cài Tool riêng.

Dữ liệu độc quyền:
  Các DLL bên trên không phải mã nguồn Tool Oumijia.
  Quyền sở hữu, logo và tên thương hiệu thuộc VIET EPLAN & ELECTRIC.
  Vui lòng xem THIRD-PARTY-NOTICES.txt.
