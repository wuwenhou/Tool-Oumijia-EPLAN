# Bàn giao cho AI Agent — ribbon, Cloud native và tương thích nhiều EPLAN

> Trạng thái ngày 2026-11-07. Đây là bản trích xuất **không có `.git`**. Phân biệt rõ *đã UAT*, *compile-check* và *ý tưởng/chưa thử*. Đọc `AGENTS.md` trước khi sửa. Không phát hành/tag/push nếu thiếu repo Git và UAT.

## 1. Mục tiêu người dùng đã chốt

1. Tab **Tool Oumijia** có nhóm **Quản lý EDZ**, đúng ba nút theo thứ tự hiển thị: **Nhập EDZ → Xuất EDZ → Menu bar**. Nút Menu bar chiếm vị trí thứ ba trước đây của Data Portal.
2. Chỉ giữ **một Data Portal trong tab Tool**, tại nhóm **Eplan Cloud** cùng sáu nút Cloud khác. Tab **Eplan Cloud** gốc của EPLAN phải còn nguyên.
3. Trên Education 2026, `MfToggleMainMenuAction` không có: nút Menu bar **chỉ giải thích giới hạn**, không được báo đã bật menu cổ điển. Không tạo menu giả/đụng native UI không chính thức.
4. Tìm hướng cho **một bộ cài** chọn đúng phiên bản/edition EPLAN. Một installer có thể chứa nhiều payload, nhưng **không** tuyên bố một Script/DLL chạy mọi phiên bản khi chưa UAT từng bản.
5. Không nhúng browser của Tool, không lưu credential/token, không bypass license; không thực hiện import EDZ thật trên Parts Database chưa được xác nhận là DB thử nghiệm.

## 2. Công nghệ và nguồn kiểm chứng

| Công nghệ/sự thật | Nguồn tra cứu | Mức chứng cứ |
|---|---|---|
| C# 5 EPLAN Scripting: `[DeclareAction]`, `[DeclareEventHandler]`, script đăng ký `[Start]` | `tools/script-edition/ToolOumijiaScript.cs`, `tools/script-edition/OumijiaRegisterScript.cs`; `Eplan.EplApi.AFu.dll`/`Guiu.dll` của bản cài | Compile và chạy trong Education 2026.0.3 |
| Add-in `IEplAddIn`/`IEplAction`, net48/x64 | `src/Oumijia.EplanAddIn/`, `.csproj`; [EPLAN Add-ins API](https://www.eplan.help/en-us/Infoportal/Content/api/2022/AddIns.html) | Source compile-check trên 2026, Add-in chưa deploy ở máy này |
| Forms Nhập/Xuất WinForms dùng chung, `Oumijia.EdzReader` và SharpCompress đọc EDZ preview | `src/Oumijia.EplanUi/`, `src/Oumijia.EdzReader/`, `docs/BUILD.md` | Forms mở/đóng trong GUI 2026; không import DB thật trong UAT Cloud |
| Import `MDPartsManagement.ImportParts` / export `MDPartsDatabase.ExportParts` và converter `IXPartsImportExportEdz` | Source `src/Oumijia.EplanUi/`; [EPLAN API](https://www.eplan.help/en-us/Infoportal/Content/api/2022/AddIns.html); `AGENTS.md` R12–R15 | Lịch sử source/UAT cũ; kiểm lại trên mỗi bản EPLAN |
| Native Cloud ribbon `RibbonCommandGroup.AddCommandWithId(uint)` | `C:\Program Files\EPLAN\Platform\2026.0.3\Bin\Eplan.EplApi.Guiu.xml` (`AddCommandWithId`); `docs/CLOUD_NATIVE_UAT.md` | Probe và GUI UAT thành công trên 2026 |
| Data Portal gốc chạy `MfEPulseCommandExecuteAction → XMVCCloudSolutionAction → ECloudSolutionsViewBarN → GfDialogManagerToggle` | `C:\Users\Public\EPLAN\Education\PerfLogSession.xml` trong UAT; read-only ribbon probe ở temp; `docs/CLOUD_NATIVE_UAT.md` | Trace thực tế; không suy từ `execute=True` |
| EPLAN Cloud IDs tìm theo text từ tab gốc lúc chạy; bản 2026 từng trả `38900`–`38906` | `docs/CLOUD_NATIVE_UAT.md`, read-only probe và `verify-tool-ribbon.log` trong temp UAT | Chỉ là ID quan sát ở session 2026, **không hard-code** |
| `MfToggleMainMenuAction` không còn trong bản cài 2026 | `addin.log` dòng `Ribbon probe ... notfound`; `CHANGELOG.md` v0.5.3; kiểm tra `Cfg`/Bin | Đã xác minh trên 2026. Không khẳng định mọi bản khác |
| Settings đăng ký của EPLAN tách theo Variant/Version; ghi từ trong EPLAN | `AGENTS.md` R1–R11/R18, `OumijiaRegister.cs`, `OumijiaRegisterScript.cs`; `C:\Users\Public\EPLAN\Settings\<Variant>\<Version>\x64\Cfg` | Kiểm chứng trên các máy/phiên lịch sử; đừng ghi settings từ PowerShell ngoài EPLAN |
| Đường build/deploy hiện tại | `docs/BUILD.md`, `tools/install-all/{EplanTargets,Install-Tool-Oumijia,Uninstall-Tool-Oumijia}.ps1`, `tools/pack.ps1` | Smart installer candidate đã liệt kê/chọn target; installer **riêng từng edition** còn chọn W3u đầu tiên, không dùng chúng khi nhiều bản. Chưa UAT cài/gỡ headless |

Không lấy mã hay cấu hình từ một bản EPLAN khác để sửa file trong `Program Files`. Ưu tiên tài liệu API chính thức + XML API đi kèm **chính bản EPLAN đang kiểm thử**; xem log runtime và GUI để xác nhận hành vi.

## 3. Lịch sử thay đổi (ghi theo nguồn, không suy diễn)

| Mốc | Thay đổi | Nguồn/trạng thái |
|---|---|---|
| v0.2.0 (2026-11-04) | Tạo add-in trong EPLAN, nhập/xuất EDZ qua API, đăng ký action và ribbon | `CHANGELOG.md`; chỉ lấy các mục đã ghi |
| v0.3.0 | Bỏ ứng dụng standalone theo yêu cầu; thêm cây xuất, registration delete-first/read-back | `CHANGELOG.md`, `AGENTS.md` R11 |
| v0.4.0 | Chi tiết part, xuất theo loại bản ghi; sửa crash/layout WinForms | `CHANGELOG.md`, `AGENTS.md` R15–R16 |
| v0.5.0–0.5.1 | Script Edition cho máy không có API license; checkbox native của TreeView | `CHANGELOG.md`, `AGENTS.md` R17–R18 |
| v0.5.2–0.5.3 | Từng thử PortalHost/WebView2 rồi thấy profile riêng và lỗi menu; đổi nhóm thành `Nhập EDZ` | `CHANGELOG.md` (lịch sử, **không** tái sử dụng WebView2) |
| v0.6.0 | Bỏ PortalHost/WebView2, thử opener `XDataPortalNavigatorToggle` nhưng action không đăng ký | `CHANGELOG.md`, `AGENTS.md` R19 |
| UAT 2026-11-06 | `GfDialogManagerShow /DialogName:DataPortalTabView` chỉ bung content; dùng clone native command từ Eplan Cloud mới có cửa sổ dock/di chuyển/đóng/mở lại | `docs/CLOUD_NATIVE_UAT.md`, `AGENTS.md` R20; Script Edition đã deploy thử, Add-in chỉ compile-check |
| Source/UAT 2026-11-07 | Bỏ Data Portal trùng ở nhóm EDZ, đổi nhóm `Quản lý EDZ`, Menu bar là nút thứ ba; thêm target selection + gỡ theo đích, giữ shared runtime; deploy Script Edition trên Education 2026 và GUI UAT lại | Source + GUI/log trong phần 7. **Chưa phát hành**, Add-in chưa deploy, installer chưa UAT ghi thực tế |

## 4. Cấu trúc đang chạy và giới hạn tương thích

- Script Edition: `%APPDATA%\Tool Oumijia\Script\ToolOumijiaScript.cs`, EPLAN tự compile, shared UI nạp qua reflection từ `%APPDATA%\Tool Oumijia\Portal`. Register vào `STATION.EplanEplApiScriptGui.Scripts` **bên trong EPLAN**. Chỉ Edition này đã UAT trong Education 2026.0.3.
- Add-in: `src/Oumijia.EplanAddIn/` + cùng shared UI, đăng ký module/actions trong EPLAN settings. Máy Education này từng chặn add-in chưa ký/không có API license; không bypass.
- Shared DLL hiện biên dịch với API EPLAN 2026.0.3. Assembly identity không phải chứng minh khả năng chạy nhị phân trên bản EPLAN khác. Script compiler/reference, EDZ API, Cloud command text/ID và license có thể khác.
- Settings nằm theo **Variant/Version**, còn installer hiện dò `W3u.exe` đầu tiên. Cùng một đường `%APPDATA%` chỉ an toàn nếu payload được xác nhận tương thích; nếu khác phải tách runtime theo target và sửa script đăng ký tương ứng.
- **Ma trận đã biết:** Education 2026.0.3 / Script = GUI UAT đạt cho ribbon/Portal/forms. Add-in 2026 = compile-check, chưa chạy. Installer/gỡ = discovery/selection fixture PASS, nhưng **chưa chạy cài/gỡ headless**. 2022/2023/2024/2025 và các bản mới hơn = không gắn nhãn “được hỗ trợ” chỉ vì có source cũ hoặc cùng assembly version.

## 5. Kế hoạch sửa theo thứ tự an toàn

### A. Ribbon — Script Edition và Add-in

1. Giữ `Eplan Cloud` gốc. Chỉ sửa tab/group do Tool sở hữu; không xóa nhóm có lệnh người dùng thêm mà chưa kiểm tra.
2. Di trú tên nhóm `Nhập EDZ` → `Quản lý EDZ`; tránh bẫy `OldCommandGroupName = "Quản lý EDZ"` hiện đang tự xóa chính tên đích. Dọn nút Portal cũ/clone native ở nhóm EDZ theo action/ID được nhận diện **trong nhóm Tool**, không đụng bản ở nhóm Cloud.
3. Tạo đúng ba nút theo thứ tự Import, Export, Menu bar; UAT **vị trí hiển thị** Menu bar xuống hàng thứ ba, không chỉ `IndexInCommandGroup`.
4. Giữ đủ bảy clone native ở nhóm Cloud, tìm ID theo text tại runtime; timer retry chạy trên UI thread vì tab Cloud có thể chưa có command trong `OnMainStart`.
5. Trên 2026, nút Menu bar chỉ hiện giải thích `MfToggleMainMenuAction` không có. Nếu bản EPLAN khác có action này thì bind native sau khi probe; không mô phỏng menu cổ điển bằng Win32.

### B. Bộ cài đa EPLAN (không đoán tương thích)

1. Thêm bước **liệt kê tất cả** `W3u.exe`, nhận Variant/Version và đường dẫn API tương ứng; có `-ListInstalled` read-only, `-TargetW3u` để chọn rõ một đích. Nếu nhiều đích mà không chỉ định, **không** âm thầm lấy cái đầu tiên.
2. Chỉ chạy đăng ký/self-test trong đúng EPLAN được chọn. Hiện installer đọc log mới theo byte-offset và snapshot settings/runtime có SHA-256, **chưa UAT invocation headless thật**. Trước khi mở rộng cài tất cả, thêm marker per-run/per-target và xác nhận process W3u/EPLAN đã thoát giữa các bước, không lấy log cũ làm bằng chứng.
3. Để cài toàn bộ bằng một lần chạy về sau: manifest payload tương thích từng Variant/Version, xử lý tuần tự (không chạy nhiều EPLAN headless đồng thời), cô lập runtime nếu DLL khác nhau; bản chưa UAT phải được báo **unsupported/untested** và không tự cài. Gỡ cài không xóa shared runtime khi đích khác còn dùng.
4. Không quảng cáo “một DLL universal”. Build/compile-check với API của từng phiên bản mục tiêu và test trên máy thật trước khi bật chế độ cài tất cả.

### C. Kiểm thử và bàn giao

1. Khi EPLAN còn mở: **không ghi đè runtime**. Sao lưu USER settings và các runtime đụng đến, lưu SHA-256; không khôi phục toàn bộ USER settings tùy tiện.
2. Compile Script bằng `csc /langversion:5` với EPLAN API hiện tại; compile-check Add-in với `AFu`, `Baseu`, `Guiu`, `MasterDatau`, shared UI. Không có .NET SDK thì không giả là đã build/release package chuẩn.
3. Sau khi EPLAN đóng an toàn, chỉ deploy runtime Script Edition đã UAT trên 2026; khởi động lại. GUI: nhóm `Quản lý EDZ` 3 nút xếp dọc, nhóm Cloud có một Data Portal, tab Cloud gốc còn; Portal có title/X, kéo/đóng/mở lại. Mở/đóng form Nhập/Xuất **không** bấm import/export. Kiểm tra `addin.log`, `EplLog.eldb` (S046006/7) và hash.
4. Test installer **read-only discovery** trên máy hiện tại và fixture mô phỏng nhiều W3u; không đăng ký vào các bản EPLAN không có trên máy. Ghi thiếu sót và cách rollback bằng bản sao lưu đã xác thực.

## 7. Kết quả thực hiện ngày 2026-11-07

- Source Script/Add-in đổi nhóm `Quản lý EDZ`, dọn command EDZ cũ có nhận diện sở hữu, tạo nhóm mới tại index 0. Cloud native clone bằng ID động, không thêm Portal fallback vào EDZ. Cả hai compile-check C# 5 với API 2026 PASS. Add-in **không deploy/build bằng SDK**.
- `tools/install-all/EplanTargets.ps1`: dò Variant/Version/W3u dưới các root Program Files, `-ListInstalled`, `-TargetW3u`; nhiều đích không chỉ định sẽ fail. `Test-EplanTargets.ps1` PASS fixture hai variant, dedup, chọn/không chọn đích. `-ListInstalled` trên máy thật chỉ ra `Education 2026.0.3`; `-TargetW3u` không tồn tại trả exit 1. Thử installer khi EPLAN đang mở trả exit 1 **trước khi ghi**, hash runtime và số backup dirs không đổi. Đường cài candidate **chỉ cho Education 2026.0.3 / Script**, không cài bản chưa UAT. Custom install path ngoài Program Files chưa được discovery hỗ trợ; bổ sung/tự kiểm chứng sau.
- Smart uninstaller chỉ gỡ đăng ký trong target, giữ runtime chung kể cả khi gỡ xong để không làm hỏng EPLAN khác; chưa kiểm thử gỡ headless. Preflight gỡ khi EPLAN đang mở trả exit 1, hash runtime/số backup dirs không đổi. Installer repo không dùng ngầm `bin\Release` vì `Oumijia.EplanUi.dll` ở đó khác hash runtime đã UAT. `pack.ps1` từ chối DLL cũ hơn source; gói chỉ được build khi có `script\portal` và Add-in build mới.
- Sao lưu trước deploy: `%USERPROFILE%\Desktop\HSG\backups\Tool-Oumijia-edz-preuat-20261107-085754-1941db`, **60 file** runtime và USER/STATION settings, manifest `SHA256.csv` so khớp. Chỉ copy `ToolOumijiaScript.cs` khi EPLAN đã đóng. Hash source/runtime sau deploy `C8A040866151AD1AD2335A969F268A6F0EED7CEE067FA20C5B5A6E14BF3971B8`; bản runtime trước UAT ở backup hash `FF8B715469F5CD0593D30AA45AD9CD379083A6A7D616CB93B1DE55860CB74C68`.
- EPLAN Education 2026 GUI: nhóm đầu `Quản lý EDZ` gồm ba nút **Nhập EDZ / Xuất EDZ / Menu bar** xếp dọc; nhóm `Eplan Cloud` có `Data Portal` (không còn ở nhóm EDZ) và sáu lệnh native khác. Tab `Eplan Cloud` gốc vẫn có nút Portal. Nhấn Portal Tool mở cửa sổ native có title/X; đóng X, mở lại, kéo cửa sổ đều PASS. Trang Portal xuất hiện banner cookie; **không chấp nhận cookie hay thay đổi đăng nhập**. Menu bar mở thông báo giới hạn `MfToggleMainMenuAction`. Form Nhập EDZ (0 gói, nút nhập disabled) và Xuất EDZ (0 part chọn, nút xuất disabled) mở/đóng; Xuất đọc 555 part từ DB hiện tại nhưng **không xuất**. `addin.log` ghi migration, probe `notfound`, Cloud retry=1; `EplLog.eldb` đọc share-mode không có S046006/7 mang ngày 07.11.2026 (có lỗi lịch sử ngày 06.11).
- Chưa UAT cài/gỡ headless, chưa thử bản EPLAN khác, sáu dịch vụ Cloud còn lại, Add-in runtime hoặc nhập EDZ thật; **không tạo ZIP/tag/release** vì source không có `.git` và DLL Add-in chưa build mới.

### Tiếp tục/rollback nếu GUI bị lỗi

1. Đóng EPLAN sau khi lưu việc; không ghi đè runtime khi có `EPLAN.exe`/`W3u.exe`.
2. Kiểm chứng SHA-256 của `backups\Tool-Oumijia-edz-preuat-20261107-085754-1941db\runtime\Script\ToolOumijiaScript.cs` với `SHA256.csv`; copy **chỉ** file này về `%APPDATA%\Tool Oumijia\Script\ToolOumijiaScript.cs` để rollback Script. Restart EPLAN và kiểm tra lại ribbon/log. **Không phục hồi toàn bộ USER settings** nếu chưa có lý do rõ, vì có thể ghi đè công việc mới.
3. Nếu test installer sau này: đóng EPLAN, build Add-in/shared UI bằng SDK với API chính target, package mới, sao lưu; thử registration/self-test trong Education 2026, kiểm tra log marker chỉ phiên mới và exit process. Tài liệu `docs/INSTALL.md` phải cập nhật theo kết quả thật.

## 6. Checklist cho AI Agent kế nhiệm

1. Đọc toàn bộ `AGENTS.md`, tài liệu này, `docs/CLOUD_NATIVE_UAT.md`, `CHANGELOG.md`, `docs/BUILD.md`; kiểm tra hash source/runtime và tiến trình EPLAN. Tránh ghi đè việc người dùng mới làm.
2. Dùng `functions.read/grep/glob` cho source, GUI qua skill `opencode-computer-use`; dừng indicator sau mỗi phiên thao tác GUI. Tìm API trong `Eplan.EplApi.Guiu.xml`, không hard-code ID Cloud.
3. Không coi `Action.Execute == true` là GUI PASS. Chỉ công nhận Portal khi X-close, di chuyển và mở lại thực tế. Giữ tab Cloud gốc.
4. Không nhập EDZ vào DB chưa được chọn làm thử nghiệm. Không lưu cookie/token, không thay proxy/hosts/license, không khôi phục USER settings cũ không rõ corrupt entry.
5. Báo riêng: source changed, compile-check, runtime deployed, GUI UAT, phiên bản khác chưa thử, có/không có release. Không tạo Git commit/tag/release trong bản trích xuất thiếu `.git`.
