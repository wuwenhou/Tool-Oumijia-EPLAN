================================================================
  TOOL OUMIJIA — EPLAN ADD-IN (huong dan cai dat)
================================================================

Add-in "Tool Oumijia" cho EPLAN Electric P8: nhap/xuat goi du lieu
linh kien EDZ (file .edz) truc tiep trong EPLAN — khong can app ngoai.

TINH NANG (ribbon "Tool Oumijia" — 4 nut):
  - "Nhap EDZ"     : chon file/thu muc .edz -> NHAP HANG LOAT vao
                     Parts Database (API chinh thuc EPLAN; 3 che do:
                     chi them moi / chi cap nhat / cap nhat + them moi;
                     chon cach nhan dang part; log CSV kiem toan).
  - "Xuat EDZ"     : chon part bang CAY NHOM kieu navigator EPLAN,
                     tick chon tung part/nhom, tim kiem nhanh;
                     xuat 1 file .edz hoac moi part 1 file.
  - "Data Portal"  : mo Data Portal native cua EPLAN (tab Eplan Cloud),
                     dung phien da dang nhap - KHONG can WebView2.
  - "Menu bar"     : bat/tat classic menu neu EPLAN con ho tro.

--- CACH CAI (1 buoc) ---
1. Dam bao EPLAN da TAT (khong bat buoc, nhung nen tat).
2. Chay:   Install-AddIn.bat
3. Mo EPLAN -> ribbon "Tool Oumijia" xuat hien ngay.

Cai dat se:
  - Copy Oumijia.EplAddin.EDZ.dll + shared runtime (EplanUi/EdzReader/
    SharpCompress) vao %APPDATA%\Tool Oumijia\AddIn
  - Dang ky add-in trong EPLAN bang cach chay an script
    OumijiaRegister.cs (EPLAN tu khoi dong, dang ky, roi tu thoat)
  - Don DLL PortalHost/WebView2 con sot tu ban 0.5.x
  - Khong can quyen Administrator, khong dung gi toi license EPLAN

--- GO CAI DAT ---
1. Chay:   Uninstall-AddIn.bat

--- PHUONG AN DU PHONG (neu Install-AddIn.bat bao loi) ---
Dang ky thu cong trong 30 giay:
1. Mo EPLAN -> File -> Extras -> Interfaces -> Scripts -> Run...
2. Chon file OumijiaRegister.cs (nam canh file nay)
3. Khoi dong lai EPLAN.

--- YEU CAU ---
- EPLAN Platform 2022 tro len (x64).
  Da kiem chung day du tren EPLAN Platform 2022.0.3.
- Windows 10/11.
- Khong can WebView2 Runtime. Neu nut Data Portal khong mo duoc,
  mo truc tiep Eplan Cloud > Data Portal trong EPLAN.

--- GHI CHU KY THUAT ---
- Registration duoc luu trong EPLAN settings (pham vi STATION):
    STATION.EplanEplApiModule.Module.Oumijia@EplAddin@EDZ.*
    STATION.EplanEplApiModule.Services.AsmActions.Oumijia*.***
- Script dang ky co the chay lai bat cu luc nao (idempotent;
  tu xoa key cu truoc khi ghi de — tranh gia tri lich su).
- Neu truoc day ban da dang ky thu cong bang dialog "API add-ins"
  voi duong dan DLL khac: chay lai Install-AddIn.bat de ghi de
  ve dung duong dan chuan (%APPDATA%).
- Nhat ky: %APPDATA%\Tool Oumijia\logs\
    register-script.log = qua trinh dang ky
    addin.log           = hoat dong add-in
    import-*.csv / export-*.csv = nhat ky kiem toan tung thao tac

Du an: https://github.com/wuwenhou/Tool-Oumijia-EPLAN
