================================================================
  TOOL OUMIJIA (EPLAN EDITION) — hướng dẫn cài đặt
================================================================

Tool Oumijia: nhập/xuất gói dữ liệu linh kiện EPLAN (*.edz) trực tiếp
trong EPLAN — ribbon "Tool Oumijia" với 3 nút:
  Nhập EDZ…  ·  Xuất EDZ…  ·  Menu bar

Gói này chứa:

  addin\        Bản ADD-IN (DLL):
               - Dùng cho EPLAN cũ hoặc máy có API license.
               - Nhanh và đủ tính năng nhất.
  script\       Bản SCRIPT (EPLAN Scripting):
               - KHÔNG cần API license.
               - Dùng cho EPLAN mới (2024+/2026) chặn add-in chưa ký.
  third-party\  Hai add-in VIET EPLAN (author-permitted):
               - VEE.EplTableMaker   — tạo bảng kỹ thuật
               - VEE.ExportConnections — xuất dữ liệu kết nối
               - Installer tự copy + đăng ký chúng.

------------------------------------------------------------
  CÀI KHUYÊN NGHỊ (tự chọn bản phù hợp)
------------------------------------------------------------
1. Chạy:   Install-Tool-Oumijia.bat
   -> Tự thử bản ADD-IN trước (đăng ký + self-test).
   -> Nếu add-in bị vô hiệu hóa (thiếu license) -> gỡ add-in,
      cài bản SCRIPT và kiểm chứng lại.
   -> Đồng thời tự cài/đăng ký 2 add-in VEE.
2. Mở EPLAN:
   Ribbon "Tool Oumijia" = Nhập EDZ… · Xuất EDZ… · Menu bar.
   Ribbon "VIET EPLAN ELECTRIC" = lệnh VEE Export/Table Maker.

------------------------------------------------------------
  CÀI THỦ CÔNG / TÙY CHỌN
------------------------------------------------------------
- Bản add-in    :  addin\Install-AddIn.bat
- Bản script    :  script\Install-Script.bat
- Gỡ cài đặt    :  Uninstall-Tool-Oumijia.bat
                (gỡ cả Tool và 2 add-in VEE)

------------------------------------------------------------
  YÊU CẦU / GHI CHÚ
------------------------------------------------------------
- Windows 10/11 x64; EPLAN Platform 2022 trở lên.
- Không cần Administrator; không đụng license EPLAN.
  (Bản script dùng chức năng scripting miễn phí của EPLAN.)
- Khuyến nghị: nhập thử trên database test trước.
- Kiểm tra toàn vẹn: xem VERSION.txt (SHA-256 của các file chính).
- Nhật ký: %APPDATA%\Tool Oumijia\logs\
  register-script.log        = đăng ký Tool (add-in/script)
  register-third-party.log   = đăng ký 2 add-in VEE
  addin.log                  = hoạt động add-in/script
  import-*.csv / export-*.csv = nhật ký kiểm toán

Du an: https://github.com/wuwenhou/Tool-Oumijia-EPLAN
