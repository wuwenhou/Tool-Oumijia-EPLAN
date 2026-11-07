================================================================
  TOOL OUMIJIA (EPLAN EDITION) — huong dan cai dat
================================================================

v0.6.0-rc.1 PRE-RELEASE THU NGHIEM: chua du UAT de phat hanh on dinh.
Ribbon: Quan ly EDZ = Nhap EDZ > Xuat EDZ > Menu bar (doc).
Nhom Eplan Cloud co bay nut native gom mot Data Portal; tab Cloud goc con nguyen.

Nut "Data Portal" mo Data Portal native cua EPLAN (tab Eplan Cloud),
dung phien da dang nhap. Khong con WebView2/PortalHost.

Source co 2 EDITION, nhung payload cài da kiem chung GUI moi chi la:

  addin\   Ban ADD-IN (DLL):
            - Chi compile-check tren 2026; chua UAT runtime.
            - Can EPLAN cho phep DLL + API license neu yeu cau.

  script\  Ban SCRIPT (EPLAN Scripting):
            - GUI UAT Education 2026.0.3; cac ban khac chua UAT.
            - Khong bypass license hay dieu khoan Data Portal.

------------------------------------------------------------
  CAI THEO DICH (installer chua UAT headless; khong phat hanh som)
------------------------------------------------------------
1. Chay:   Install-Tool-Oumijia.bat -ListInstalled
   -> Read-only. Khong cai thu tren EPLAN chua duoc UAT.
2. Khi co nhieu ban, chay:
   Install-Tool-Oumijia.bat -TargetW3u "C:\Program Files\EPLAN\Education\2026.0.3\Bin\W3u.exe"
   -> Chi Education 2026.0.3 duoc phep chon Script candidate.
   -> Installer KHONG lay W3u.exe dau tien; can goi da build day du script\portal.
3. Mo EPLAN, kiem tra ribbon/Portal native va form Nhap/Xuat.

------------------------------------------------------------
  CAI THU CONG (neu can)
------------------------------------------------------------
- Installer rieng edition la legacy, con chon W3u.exe dau tien; khong dung
  khi co nhieu ban EPLAN hoac payload chua kiem chung.
- Go cai dat theo dich: Uninstall-Tool-Oumijia.bat -ListInstalled
  roi Uninstall-Tool-Oumijia.bat -TargetW3u "<duong dan W3u.exe>"

------------------------------------------------------------
  GO CAI DAT
------------------------------------------------------------
Go dang ky trong EPLAN da chon; giu lai shared runtime/Script/AddIn cho
nhung ban EPLAN khac co the su dung. Co backup settings SHA-256.

------------------------------------------------------------
  YEU CAU / GHI CHU
------------------------------------------------------------
- Windows 10/11 x64; GUI UAT local Education 2026.0.3 Script only.
- Khong can quyen Administrator; khong dung gi toi license EPLAN
  (ban script dung chuc nang scripting mien phi cua EPLAN).
- Khong can WebView2 Runtime. Data Portal dung tinh nang native cua
  EPLAN; neu khong mo duoc, mo truc tiep Eplan Cloud > Data Portal.
- Khuyen nghi: thu Nhap EDZ tren database test truoc khi nhap vao
  database chinh.
- Kiem tra toan ven: xem VERSION.txt (SHA-256 cua cac file chinh).
- Nhat ky: %APPDATA%\Tool Oumijia\logs\

Du an: https://github.com/wuwenhou/Tool-Oumijia-EPLAN
