================================================================
  TOOL OUMIJIA — ADD-IN CHO EPLAN (huong dan cai dat)
================================================================

Cai dat add-in vao EPLAN (tua-bin "Tool Oumijia" voi 3 nut:
"Nhap EDZ...", "Xuat EDZ...", "Cua so Tool Oumijia").

--- CACH CAI (1 buoc) ---
1. Dam bao EPLAN da TAT (khong bat buoc, nhung nen tat).
2. Chay:   Install-AddIn.bat
3. Xong. Mo EPLAN -> ribbon "Tool Oumijia" xuat hien ngay.

Cai dat se:
  - Copy Oumijia.EplAddin.EDZ.dll vao %APPDATA%\Tool Oumijia\AddIn
  - Dang ky add-in trong EPLAN bang cach chay an script
    OumijiaRegister.cs (EPLAN tu khoi dong, dang ky, roi tu thoat)
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

--- GHI CHU KY THUAT ---
- Registration duoc luu trong EPLAN settings (pham vi STATION):
    STATION.EplanEplApiModule.Module.Oumijia@EplAddin@EDZ.*
    STATION.EplanEplApiModule.Services.AsmActions.Oumijia*.**
- Script dang ky cung co the chay lai bat cu luc nao (idempotent).
- Nhat ky dang ky: %APPDATA%\Tool Oumijia\logs\register-script.log
- Nhat ky add-in luc chay: %APPDATA%\Tool Oumijia\logs\addin.log

Du an: https://github.com/wuwenhou/Tool-Oumijia-EPLAN
