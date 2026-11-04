================================================================
  TOOL OUMIJIA (EPLAN EDITION) — huong dan cai dat
================================================================

Tool Oumijia: nhap/xuat goi du lieu linh kien EPLAN (*.edz) truc tiep
trong EPLAN — ribbon "Tool Oumijia" voi 2 nut: Nhap EDZ... / Xuat EDZ...

Goi nay chua 2 PHIEN BAN:

  addin\   Ban ADD-IN (DLL):
           - Nhanh va day du nhat.
           - Can EPLAN co API license HOAC ban EPLAN cho phep DLL chua ky
             (vd EPLAN 2022 tro ve truoc).

  script\  Ban SCRIPT (EPLAN Scripting):
           - KHONG can API license, KHONG can ky assembly.
           - Dung cho EPLAN ban moi (2024+/2026) chan add-in chua ky.

------------------------------------------------------------
  CACH CAI KHUYEN NGHI (tu chon ban phu hop)
------------------------------------------------------------
1. Chay:   Install-Tool-Oumijia.bat
   -> Tu thu ban ADD-IN truoc (dang ky + self-test).
   -> Neu EPLAN vo hieu hoa add-in (thieu license) -> tu dong go add-in
      va chuyen sang cai ban SCRIPT + kiem chung lai.
2. Mo EPLAN -> ribbon "Tool Oumijia" xuat hien.

------------------------------------------------------------
  CAI THU CONG (neu can)
------------------------------------------------------------
- Ban add-in:   addin\Install-AddIn.bat
- Ban script:   script\Install-Script.bat
- Go cai dat:   Uninstall-Tool-Oumijia.bat   (go ca hai ban)

------------------------------------------------------------
  GO CAI DAT
------------------------------------------------------------
Chay:   Uninstall-Tool-Oumijia.bat

------------------------------------------------------------
  YEU CAU / GHI CHU
------------------------------------------------------------
- Windows 10/11 x64; EPLAN Platform 2022 tro len.
- Khong can quyen Administrator; khong dung gi toi license EPLAN
  (ban script dung chuc nang scripting mien phi cua EPLAN).
- Khuyen nghi: thu Nhap EDZ tren database test truoc khi nhap vao
  database chinh.
- Kiem tra toan ven: xem VERSION.txt (SHA-256 cua cac file chinh).
- Nhat ky: %APPDATA%\Tool Oumijia\logs\

Du an: https://github.com/wuwenhou/Tool-Oumijia-EPLAN
