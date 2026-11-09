TOOL OUMIJIA - EPLAN ELECTRIC P8 2022.0.3

Scope: exactly Electric P8 2022.0.3. This ZIP has only the 2022/net48 add-in.
It does not contain a 2026 payload. Do not copy these DLLs to EPLAN 2026.
No license bypass, credential handling, WebView2 replacement, or EDZ database import.

Close all EPLAN instances before each command. Open PowerShell in this folder:

  powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-Tool-Oumijia.ps1 -ListInstalled
  powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-Tool-Oumijia.ps1 -TargetW3u 'C:\Program Files\EPLAN\Electric P8\2022.0.3\Bin\W3u.exe' -Phase2022 Stage

Stage makes a SHA-256-verified backup of the 2022 Cfg and current Tool Oumijia
AppData in %LOCALAPPDATA%\Tool Oumijia\backups. It copies ONLY into
%APPDATA%\Tool Oumijia\AddIn\2022.staging and registers that DLL in 2022.
It never overwrites the shared AddIn/Portal folders or the EPLAN 2026 settings.

Open Electric P8 2022.0.3 and check the Tool Oumijia ribbon:
  Quan ly EDZ: Nhap EDZ, Xuat EDZ, Menu bar.
  Eplan Cloud: Eplan Cloud dashboard, Data Portal.
Verify both Cloud panes are native (the same EPLAN actions as the built-in
Eplan Cloud commands), can be closed, moved, and reopened.
IMPORTANT - EPLAN 2022.0.3 embedded browser limitation:
  The Eplan Cloud panes may stay blank or show "Loading..." because the
  Chromium/CEF embedded in Electric P8 2022.0.3 is too old to parse the
  current EPLAN Cloud JavaScript bundles. EplWebControl.log then contains:
    Uncaught SyntaxError: Unexpected token '{'
    source: https://apps.eplan.com/main-*.js
    source: https://dataportal.eplan.com/main-*.js
  The native action and the fallback GfDialogManagerShow /
  ECloudSolutionsViewBar8|9 both open the pane, but they use the same old
  embedded browser and fail the same way. This is an EPLAN 2022.0.3/CEF
  limitation, not an add-in error and not an SSO/network error.
  This package intentionally does NOT replace the native pane with WebView2,
  a private browser, cached content, or a simulated login. Use EPLAN 2026
  (or another EPLAN release whose embedded browser supports the current
  Cloud bundles) for a working Cloud pane. The two native Cloud buttons
  remain in the ribbon so the commands are reachable when EPLAN/CEF is fixed.
Data Portal SSO/login must be completed by the user, not the installer.
Do not import EDZ into a production Parts Database as a test.

2022.0.3 UAT (2026-11-08, candidate r5 payload):
  Ribbon PASS: Quan ly EDZ has exactly 3 commands; Eplan Cloud has 2.
  Import form PASS: 3 test EDZ packages previewed, 0 errors, no database write.
  Export form PASS: 649 parts loaded, 0 selected, no file written.
  Cloud native command PASS, rendering BLOCKED by the CEF limitation above.
  Finalize PASS: state Finalized, final registration/verify/self-test PASS
  (2026-11-08 09:28). Finalize copies 2022.staging to 2022, registers and
  verifies the final copy, runs self-test, then removes staging. It keeps
  staging valid during final registration so a headless EPLAN start cannot
  point at a missing DLL.
  Details: docs/UAT-2022-2026-11-08.md (source repository).

If GUI UAT passes, close EPLAN and run:
  powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-Tool-Oumijia.ps1 -TargetW3u 'C:\Program Files\EPLAN\Electric P8\2022.0.3\Bin\W3u.exe' -Phase2022 Finalize

v0.7.1 note (2026-11-09, thay cho v0.7.0 note):
  - Nhap EDZ: tick tung part tren cay; tick goi = chon het part; nut
    Chon het / Bo chon; chi cac part da tick duoc gui cho ImportParts.
    Neu chi chon mot phan goi, Tool tao EDZ tam (manifest da loc + cac
    file duoc package tham chieu, writer 7z noi bo) roi tu xoa sau nap.
  - Eplan Cloud: chi 2 lenh native (Eplan Cloud dashboard, Data Portal).
    Ban v0.7.0 cung co the hien 4 nut neu may da tung cai ban thu nghiem
    2026-11-08; v0.7.1 tu dong xoa 2 lenh chet OumijiaOpenDashboard /
    OumijiaOpenDataPortal khi Add-in khoi dong.
  - Chi chon mot phan goi: EDZ tam da qua `7z t` o may phat trien;
    EPLAN co chap nhan tep do Tool ghi hay khong chua duoc xac nhan
    bang import that. Chi dung database test.
  - UI Nhap/Xuat dong bo (EdzDialogStyle); nhan cay "PARTNR (Ten)";
    ten part khong con chuoi da ngon ngu; "-> se tao" khong con bi che.
  - GUI Nhap EDZ da UAT tren 2022.0.3 ngay 2026-11-09: tick/bo tick part,
    tick/bo tick goi, Chon het / Bo chon, status "15/15 -> 0/15 -> 15/15",
    nut nhap enable/disable; khong ghi DB. Chua nhap EDZ that vao Parts DB.

If it fails, close EPLAN and run:
  powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-Tool-Oumijia.ps1 -TargetW3u 'C:\Program Files\EPLAN\Electric P8\2022.0.3\Bin\W3u.exe' -Phase2022 Rollback

Rollback restores only the target's STATION settings from the verified backup;
it does not roll back USER settings (potentially including user SSO changes).
Keep the backup and logs for diagnosis. If the staging DLL/registration changed
after UAT, automatic rollback refuses to overwrite newer work.
