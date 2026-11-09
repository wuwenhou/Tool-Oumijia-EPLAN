TOOL OUMIJIA - EPLAN 2026 (SCRIPT EDITION)

Target: EPLAN Education 2026.0.3, x64. This ZIP is the SCRIPT EDITION only.
It does not contain an Add-in DLL and does not contain a 2022 payload.
Other EPLAN 2026 variants and other versions are rejected by the installer.

Why Script Edition: the Script Edition was GUI UAT'd on Education 2026.0.3
(ribbon, native Data Portal, open/close Import and Export forms). The 2026
Add-in DLL was never runtime UAT'd and this package does not ship it.
No license bypass, credential storage, WebView2 replacement, or EDZ database
import is included.

Close all EPLAN instances before each command. Open PowerShell in this folder:

  powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-Tool-Oumijia.ps1 -ListInstalled
  powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-Tool-Oumijia.ps1 -TargetW3u 'C:\Program Files\EPLAN\Education\2026.0.3\Bin\W3u.exe'

The installer selects only Education 2026.0.3. If several EPLAN versions are
installed, it refuses to guess; pass the exact W3u.exe returned by
-ListInstalled. It backs up the target settings and current Tool Oumijia
runtime with SHA-256, then registers the Script Edition headlessly.

After installation, open EPLAN and check the Tool Oumijia ribbon:
  Quan ly EDZ: Nhap EDZ, Xuat EDZ, Menu bar.
  Eplan Cloud: Eplan Cloud dashboard, Data Portal, Project generation (Free),
               User management, Parts management (deprecated),
               Eplan eLearning, Guided Installation.
The Cloud group clones EPLAN's native commands by runtime ID; the original
Eplan Cloud tab is untouched. Data Portal uses EPLAN's native pane and the
user's existing EPLAN session. The user must complete SSO/login; the installer
never handles credentials.

UAT status (source: docs/releases/v0.7.0-2026.md):
  Ribbon 3 + 7 native commands: PASS on Education 2026.0.3 (previous UAT).
  Data Portal native pane open/close/move/reopen: PASS (previous UAT).
  Import/Export forms open and close without writing data: PASS (previous UAT).
  v0.7.x adds per-part selection in Import EDZ (checkboxes) plus a local 7z
    writer. The NEW shared runtime has NOT been GUI UAT'd on 2026 yet;
    if it fails to load, uninstall this and install v0.6.0-2026.
  v0.7.1 vs v0.7.0 (2026): no logic changes; shared runtime rebuilt with
    assembly version 0.7.1.0 (version attributes only).
  EDZ import into a production Parts Database: NOT TESTED. Use a test database.
  Add-in runtime on 2026: NOT TESTED; not shipped.
  Instances other than Education 2026.0.3: NOT TESTED; blocked by installer.

Installation layout:
  script\ToolOumijiaScript.cs        Script Edition loaded by EPLAN
  script\OumijiaRegisterScript.cs    registers auto-load in STATION
  script\portal\                     net48 shared UI/EDZ runtime
  docs\                              guides and release notes
No Oumijia.PortalHost/WebView2 DLL is present or installed.

Uninstall (keeps shared runtime used by other EPLAN versions):
  powershell -NoProfile -ExecutionPolicy Bypass -File .\Uninstall-Tool-Oumijia.ps1 -ListInstalled
  powershell -NoProfile -ExecutionPolicy Bypass -File .\Uninstall-Tool-Oumijia.ps1 -TargetW3u 'C:\Program Files\EPLAN\Education\2026.0.3\Bin\W3u.exe'

Logs:
  %APPDATA%\Tool Oumijia\logs\register-script.log
  %APPDATA%\Tool Oumijia\logs\addin.log
Integrity: see VERSION.txt and SHA256.csv.
Project: https://github.com/wuwenhou/Tool-Oumijia-EPLAN
