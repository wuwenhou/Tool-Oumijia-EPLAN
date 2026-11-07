================================================================
  TOOL OUMIJIA — SCRIPT EDITION (huong dan cai dat)
================================================================

Ban SCRIPT cua Tool Oumijia — dung cho may KHONG co EPLAN API license
(va/hoac EPLAN ban moi chan add-in chua ky). Chay bang EPLAN Scripting
nen khong can license, khong can cai .NET, khong can Administrator.

TINH NANG: giong ban add-in (cung ribbon "Tool Oumijia"):
  - "Nhap EDZ"     : batch nhieu goi .edz vao Parts Database
  - "Xuat EDZ"     : chon part bang cay nhom (nhu navigator EPLAN)
                     hoac xuat theo loai ban ghi (nhu EPLAN)
  - "Data Portal"  : mo Data Portal native cua EPLAN (tab Eplan Cloud),
                     dung phien da dang nhap - KHONG can WebView2
  - "Menu bar"     : bat/tat classic menu neu EPLAN con ho tro

--- CACH CAI (1 buoc) ---
1. Chay:   Install-Script.bat
2. Mo EPLAN -> ribbon "Tool Oumijia" xuat hien.
   Script tu dong duoc nap lai o MOI lan khoi dong EPLAN.

Cai dat se:
  - Copy ToolOumijiaScript.cs vao %APPDATA%\Tool Oumijia\Script
  - Dang ky script vao danh sach "script tu nap" cua EPLAN
    (STATION.EplanEplApiScriptGui.Scripts) bang script dang ky chay an

--- GO CAI DAT ---
1. Chay:   Uninstall-Script.bat

--- LUU Y: MOT SO BAN EPLAN CU THIEU REFERENCE CHO SCRIPT ---
Vd EPLAN 2022: compiler script KHONG nap san Eplan.EplApi.MasterData
-> script bao loi compile trong log. Tren cac ban do hay dung ban ADD-IN
(trong thu muc addin\) — ban add-in chay tot tren EPLAN cu ma khong can
license. Installer thong minh (Install-Tool-Oumijia.bat o goc goi) tu
chon ban phu hop.

--- GHI CHU KY THUAT ---
- Ribbon duoc tao lai o su kien Eplan.EplApi.OnMainStart (+ OnLoadWorkspace,
  OnResetRibbon tren ban EPLAN moi).
- Actions: OumijiaScriptImportEdz / OumijiaScriptExportEdz / OumijiaScriptSelfTest
  (dat ten khac ban add-in de khong xung dot neu cai ca hai).
- Nhat ky: %APPDATA%\Tool Oumijia\logs\
    register-script.log (dong [SCRIPT]) = qua trinh dang ky
    addin.log                            = hoat dong script
    import-*.csv / export-*.csv          = nhat ky kiem toan

- Neu nut "Data Portal" bao khong mo duoc: mo truc tiep
  Eplan Cloud > Data Portal trong EPLAN, roi dung "Nhap EDZ"
  de nhap file .edz da tai. Khong can WebView2.

Du an: https://github.com/wuwenhou/Tool-Oumijia-EPLAN
