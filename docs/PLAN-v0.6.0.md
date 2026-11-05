# Kế hoạch v0.6.0 — “Tool Oumijia Classic + hệ sinh thái VEE”

> Trạng thái: **đã phê duyệt phạm vi cơ bản** — chỉ còn chờ người dùng duyệt bản kế hoạch này trước khi triển khai.
> Ngày: 2026-11-05
> Bản hiện tại: v0.5.0 đã phát hành.

---

## 0. Mục tiêu và phạm vi

### Mục tiêu kinh doanh / người dùng
1. Tab **Tool Oumijia** trong EPLAN trông và hoạt động giống bố cục mẫu do người dùng cung cấp:
   `Quản lý EDZ | Thang menu cũ | VEE | GIRD`.
2. Các thao tác EDZ được đặt đúng vị trí; các tiện ích mở rộng được gom về một nơi, giảm số tab riêng.
3. Tool có thể **gọi add-in VIET EPLAN đã cài riêng**, nhưng không sao chép thành phần của add-in và không tự phân phối chúng.
4. Menu cũ bị ẩn/loại bỏ trên EPLAN mới được thay bằng giao diện tiện dụng hơn; không khẳng định EPLAN cho phép bật lại theo một cơ chế không được hỗ trợ.

### Lựa chọn đã chốt
- **VEE = Phương án A (launcher / detect)**  
  Tool chỉ kiểm tra add-in đã cài và hiển thị nút/thông báo hướng dẫn cài riêng. Không nhúng `VEE.EplTableMaker.dll`, `VEE.ExportConnections.dll`, `Show Menu Bar.dll` hoặc DLL/tác phẩm của bên thứ ba vào gói release.
- **Phạm vi = “tất cả”** nghĩa là triển khai mọi hạng mục khả thi và hợp pháp; hàng hóa chưa đủ điều kiện sẽ được ẩn hoặc nêu rõ trong tài liệu thay vì làm giả.

### Không thuộc phạm vi v0.6.0
- Không xóa tab/block các add-in của bên thứ ba.
- Không patch EPLAN hoặc license.
- Không sao chép source/code/UI/icon của VIET EPLAN & ELECTRIC.
- Không bind bất kỳ add-in bản quyền đóng gói nào vào release.
- Không hỗ trợ “Show Menu Bar” nếu chưa có bản cài chính thức hoặc người dùng tự xác nhận đã cài/biết cách dùng.
- Không tự cài add-in bên ngoài thay người dùng, trừ khi người dùng chọn rõ và có tài liệu/license hợp lệ.

---

## 1. Kết quả nghiên cứu cần đưa vào sản phẩm

### 1.1 Hai video người dùng cung cấp

| Video | Kênh | Nội dung |
|---|---|---|
| `VsEqFxQM6Sg` — Part 15: Eplan Data Portal And Download Device Library | Nguyễn Văn Nguyên PLC | Hướng dẫn dùng **EPLAN Data Portal** để tìm/tải thư viện thiết bị/.edz. Không phải video tool của VIET EPLAN. |
| `snyfriYe9KM` — EPLAN ELECTRIC - Add-in: Auto Update Report, Export PDF & Backup in One Click | VIET EPLAN & ELECTRIC | Giới thiệu add-in tự update report, xuất PDF và backup dự án cùng lúc. Mô tả video nói công cụ **đang hoàn thiện và sẽ phát hành sau**; chưa thấy link tải công khai. |

> EPLAN Data Portal là cơ chế chính thức của EPLAN; không cần và không nên tái triển khai trong Tool.

### 1.2 Các công cụ VIET EPLAN & ELECTRIC đã thấy

| Công cụ | Video / nguồn | Trạng thái |
|---|---|---|
| **Show Menu Bar** | `SXERO8SbehA` — “EPLAN Tip: Always Show the Menu Bar on Startup” | Add-in dự định tự hiện Menu Bar khi khởi động, bật/ẩn một lần bấm, ghi nhớ trạng thái. Download Telegram: `https://t.me/EPLAN4U/12797`. |
| **VEE Export connections data** | `a8u1O2i7Wxs` — “Export EPLAN Wire Names To CSV For Tube Printer In Seconds With VEE Add-In” | Xuất hiệu ứng kết nối: Connection Designation, màu dây, tiết diện, chiều dài, source/target, page… ra CSV/Excel. Telegram: `https://t.me/EPLAN4UCHAT/13646`. Local ZIP có DLL nhưng file giải nén trong máy hiện tại có kích thước 0 byte; còn một bản hoạt động trong ShadowCopy. |
| **VEE Table Maker** | `ACp_sD4vdAw` — “EPLAN Table Maker: Create Tables Quickly and Easily” | Tạo technical table trực tiếp trên trang EPLAN. v1.2.0.0, Company: VIET EPLAN ELECTRIC. Telegram: `https://t.me/EPLAN4U/12788`. |
| **VEE EDZ Killer** | `d7DVz9nddeA` — “AI-Powered EPLAN Part Data Generator in 1 minute” | Add-in Thu thập/chuẩn hóa Part Data, liên quan trực tiếp đến tạo thư viện/EDZ. Chưa thấy link tải công khai. |
| **Auto Update Report / Export PDF / Backup** | `snyfriYe9KM` | Add-in một lần hoàn thiện toàn bộ bước sau thiết kế. Chưa được công bố công khai theo mô tả video. |
| **VEE Tank Game** | `fGOb_nWJABY` — “Yes, This Tank Game Runs Inside EPLAN” | Trò chơi nhỏ chạy trên EPLAN; không phải tính năng Tool/EDA. Telegram: `https://t.me/EPLAN4U/12790`, password `123456`. Không đưa vào Product Scope. |

### 1.3 Các nội dung không phải công cụ riêng
- `Restore EPLAN 2.9 Menu Bar in EPLAN 2022 (Easy Method)` — hướng dẫn, không phải installer có thể nhúng.
- Các video dùng Data Portal / tải Phoenix Contact / Delta EDZ — hướng dẫn dữ liệu và nguồn chính thức của hãng.
- Các video “Convert image to DXF” hoặc website chuyển đổi ảnh — dịch vụ web bên ngoài, không phải Tool Oumijia.
- Tài liệu EDZ của hãng: Phoenix Contact, Delta, Omron, Siemens, Eaton, Lenze, Weidmüller, ifm, WAGO — chỉ ghi nhận hướng dẫn/link; không embed dữ liệu.

### 1.4 Nguyên tắc pháp lý bắt buộc
1. **Không ngược mã hóa** add-in của bên thứ ba để tái sử dụng.
2. **Không phân phối/relicense** DLL của bên thứ ba.
3. Chỉ:
   - Trỏ tới nguồn tải chính thức do tác giả công bố;
   - Phát hiện trạng thái cài;
   - Gọi action công khai nếu add-in đã được cài và người dùng đồng ý;
   - Hoặc tự triển khai tính năng tương đương khi có yêu cầu chi tiết và không sao chép triển khai.
4. Công khai credit: tác giả, email, YouTube, Telegram; không dùng tên “VEE”/logo/tệp của họ để làm brand Tool.

---

## 2. Phát hành theo thứ tự

### v0.5.1 — “Native tick” (phát hành trước)

#### Lý do
Người dùng báo **ô tick không hiển thị dấu ✓** dù selection đã được đếm. Các lần tự vẽ icon đã thất bại:

1. Icon tự vẽ + thay `ImageList`;
2. `ForceTreeRepaint()` / `NodeFont` bold;
3. Windows TreeView không đảm bảo vẽ lại checkbox tùy chỉnh đúng cách.

#### Fix
- Bỏ tiêm ảnh tick tự vẽ khỏi TreeView Xuất EDZ.
- Bật `TreeView.CheckBoxes = true` (checkbox native của Windows).
- Giao diện tự xử lý `AfterCheck` hoặc mouse/hit-test? Trong v0.5.1 cần cây vẫn tick hiệu quả khi bấm ô.
- Nhóm chọn một phần: node hiển thị không tick; dòng được chọn vẫn **bold** và số đếm luôn đúng.
- Áp dụng cho cả bản add-in và bản script.
- Xóa code icon tự vẽ không còn cần (hoặc để lại dead code sẽ xóa).
- Test: construct-test diagnostic, headless self-test, manual UAT marker/screenshot; release `v0.5.1`.

### v0.6.0 — “Classic & Ecosystem”

#### Deliverables
1. Ribbon Tool Oumijia mới, có icons/tooltip và layout 4 nhóm.
2. Command Center `EPLAN Classic` với 12 danh mục cũ.
3. Hai nút `VEE` launcher: `VEE Export connections`, `VEE Table Maker` — hỗ trợ trạng thái cài/ngay lập tức tìm thấy, nếu chưa cài thì hướng dẫn.
4. Ghi nhận link/hướng dẫn/credits cho các tool VIET EPLAN & ELECTRIC.
5. GIRD — chỉ hiển thị nếu action/command đã được xác minh; nếu không xác minh được thì nhóm bị om hoặc chỉ chứa nút hướng dẫn, không dựng nút sập.
6. Tài liệu `THIRD-PARTY-ADDINS.md` + cập nhật README / INSTALL / AGENTS / CHANGELOG.
7. Release v0.6.0; v0.5.1 phát hành trước v0.6.0.

---

## 3. UX mới — bố cục tab Tool Oumijia

```text
┌─ Tool Oumijia ─────────────────────────────────────────────────────────────┐
│ Nhập EDZ…   Xuất EDZ…  |  EPLAN Classic  |  VEE Export…  VEE Table Maker… │
│                        |  Command Center |  status       status           │
├────────────────────────────────────────────────────────────────────────────┤
│ Quản lý EDZ            | Thang menu cũ    | VEE          | GIRD (optional)│
└────────────────────────────────────────────────────────────────────────────┘
```

### 3.1 Nhóm `Quản lý EDZ`
- `Nhập EDZ…`
- `Xuất EDZ…`
- Icon/tooltips theo ngôn ngữ tiếng Việt.
- Tự tạo lại an toàn khi ribbon bị reset.
- Add-in và script cùng hiển thị.

### 3.2 Nhóm `Thang menu cũ` / `EPLAN Classic`

**Một nút lớn “EPLAN Classic”** mở cửa sổ:

```text
┌ EPLAN Classic ────────────────────────────────────────────────┐
│ 🔍 Tìm lệnh____________________________                       │
├────────────────┬──────────────────────────────────────────────┤
│ Project        │ Các lệnh ...                                │
│ Page           │                                              │
│ Layout space   │ [ Sử dụng được ] / [ Không hỗ trợ ]          │
│ Edit           │                                              │
│ View           │ Double-click lệnh khả dụng để gọi action     │
│ Insert         │                                              │
│ Project data   │                                              │
│ Find           │                                              │
│ Options        │                                              │
│ Utilities      │                                              │
│ Window         │                                              │
│ Help           │                                              │
└────────────────┴──────────────────────────────────────────────┘
```

#### Phân loại 12 danh mục
`Project, Page, Layout space, Edit, View, Insert, Project data, Find, Options, Utilities, Window, Help`

### 3.3 Nhóm `VEE`

**Nút 1 — VEE Export connections**
- Nếu action/add-in đã cài: gọi nó hoặc mở Dialog/cửa sổ tương ứng.
- Nếu chưa cài: tooltip “Chưa cài VEE Export connections”; click hiển thị:
  - Telegram link `EPLAN4UCHAT/13646`;
  - Cảnh báo không đi kèm license;
  - Nút “Đóng” và “Mở hướng dẫn”.
- Nếu bản cài bị lỗi (DLL 0 byte), báo rõ lỗi cấu hình/bản sao, không tưởng tượng lỗi.

**Nút 2 — VEE Table Maker**
- Tương tự.
- Nếu phát hiện `VIET EPLAN ELECTRIC`/nhóm Tools/action `VEECreateEplanTable`, hiển thị sẵn sàng.
- Hiển thị `v1.2.0.0` nếu đọc được từ file/version information (không can thiệp registry nguy hiểm).

**Không có nút Auto Update / PDF / Backup / EDZ Killer**
- Các sản phẩm này chưa phát hành công khai; chỉ liệt kê trong tài liệu “future / chưa phát hành”.

### 3.4 Nhóm `GIRD`
- Trước khi làm nút, phải xác minh lệnh EPLAN hợp lệ có thể được gọi từ API/action.
- Nếu xác minh được:
  - Snap to grid on/off;
  - Object snap on/off;
  - Design mode;
  - Align on grid;
  - Grid A..E nếu có.
- Nếu không xác minh được: mục không có trong v0.6.0; không chạy `Toggle` vô điều kiện.

---

## 4. Kiến trúc / triển khai

### 4.1 Codebase cần sửa

```text
src/Oumijia.EplanAddIn/AddIn.cs
  └─ OnInitGui: gọi EnsureRibbon(…)
src/Oumijia.EplanAddIn/Ui/ToolOumijiaRibbon.cs (mới)
  └─ tạo tab/groups/buttons + icon + DPI handling
src/Oumijia.EplanAddIn/Ui/EplanClassicForm.cs (mới)
  └─ Command Center search/catalog/invoke
src/Oumijia.EplanAddIn/Ui/VeeIntegrationStatus.cs (mới)
  └─ detection/installer state logic (read-only)
src/Oumijia.EplanAddIn/Ui/ExportEdzForm.cs
  └─ v0.5.1 native CheckBoxes; shared tree check behavior
src/Oumijia.EplanAddIn/Ui/PartsTreeModel.cs
  └─ remove custom check image contract
tools/script-edition/ToolOumijiaScript.cs
  └─ port ribbon/click handling/UI native checkbox parity
tools/script-edition/OumijiaRegisterScript.cs
  └─ giữ registration script behavior
tools/pack.ps1
  └─ package v0.6.0 both editions; docs update
```

### 4.2 Detect logic cho VEEAA IsAddInRegistered
Đọc settings từ một probe script an toàn / COM / EplApi API, không ghi tùy tiện:

- Tìm module `VIET EPLAN ELECTRIC` hoặc action/module names của VEE.
- Hoặc tìm module key/assembly registration của EPLAN tương ứng.
- Không copy file user content nếu không được phép.
- Cache kết quả vài giây để tránh scan file mỗi khi bật tab.

Nếu không verify được ổn định trên cả 2022/2026, dùng `Not installed / unknown → unavailable`.

### 4.3 Command Center catalog schema
Mỗi entry:

```text
{
  Category, DisplayName, CommandId, Enabled,
  Availability: "known" | "unknown" | "unsupported",
  Tooltip, Source, TestVersion
}
```

- Không để `Invoke()` command unknown/unsupported.
- Luôn log `Test EPLAN Classic command` vào addin.log.
- Default tất cả `Disabled`; command được bật lần lượt sau UAT.

### 4.4 GIRD verification
- Quét binary/config/docs để tìm command/action IDs.
- Tất cả phải được kiểm tra trên EPLAN 2022 và 2026.
- Nếu action có tham số toggle cần biết chính xác semantics, không kêu “đã bật/tắt”.

---

## 5. Test plan

| ID | Test | Kỳ vọng |
|---|---|---|
| T1 | `dotnet build` add-in Release | 0 warning / 0 error |
| T2 | csc C#5 script compile | 0 lỗi |
| T3 | Add-in selftest headless | `OnInit 0.6.0.0` + `SELFTEST OK` |
| T4 | Script selftest | `SELFTEST OK - script loaded` |
| T5 | Construct `ImportEdzForm`, `ExportEdzForm`, `EplanClassicForm` | không crash; check DPI `100/125/150/175%` |
| T6 | Native checkbox behavior | tick hiện/show; chọn nhánh chọn hết/xóa hết; phần được chọn đúng; NodeFont bold |
| T7 | Ribbon reset/load workspace/on main start | tab/groups/buttons xuất hiện lại; không duplicate; không đè ribbon của add-in khác |
| T8 | VEE absent | VEE buttons hiển thị disabled + thông báo đúng |
| T9 | VEE installed test | action/button gọi được, không sửa/lặp ribbon `VIET EPLAN ELECTRIC` |
| T10 | Command unsupported | nhập từ khóa chỉ trong supported commands; command unknown không invoke |
| T11 | Add-in uninstall/reinstall/installer | cleanup sạch; script bundle không include VEE DLLs |
| T12 | `pack.ps1` zip audit | chỉ có Tool add-in + script + docs, không có DLL/tệp bên thứ ba |
| T13 | EPLAN 2022 add-in UAT | EDZ import/export OK; menu/classic layout OK |
| T14 | EPLAN 2026 script UAT | script loaded; EDZ import/export OK; VEE status đúng |

---

## 6. Tài liệu và legal/content

### Cập nhật
- `docs/PLAN-v0.6.0.md` — tài liệu này.
- `docs/THIRD-PARTY-ADDINS.md` — nguồn/tải/usage/credits/limitations.
- `docs/INSTALL.md` — cài Tool; cài riêng VEE nếu muốn; không cài bằng Tool.
- `README.md` — lợi ích; **không** mập mờ về license.
- `CHANGELOG.md`, `Changelog link`, `VERSION.txt`, `SHA256`.
- `AGENTS.md` — rules: không bundle third-party DLLs; verify commands; no fake ribbon buttons.
- `docs/releases/v0.5.1.md`, `docs/releases/v0.6.0.md`.

### nội dung bắt buộc trong THIRD-PARTY-ADDINS.md
| Tool | Tác giả | Nguồn | Điều kiện | Cấp phép/distribution |
|---|---|---|---|---|
| VEE Export connections | VIET EPLAN & ELECTRIC | YouTube / Telegram `12797`? | EPLAN 2022+ | Chỉ hướng dẫn local install; không đi kèm |
| VEE Table Maker | VIET EPLAN & ELECTRIC | YouTube / Telegram `12788` | EPLAN 2022+ / .NET 4.8 | Chỉ hướng dẫn local install; không đi kèm |
| Show Menu Bar | VIET EPLAN & ELECTRIC | YouTube / Telegram `12797` | bản tương thích | Không giả định công khai miễn phí |
| VEE EDZ Killer | idem | video `d7DVz9nddeA` | 未公开/unreleased | Không mention release date; do not call stable |
| EPLAN Data Portal | EPLAN | official | Data Portal license/account | Chính thức của EPLAN |

---

## 7. Quyết định rủi ro / câu hỏi đã giải quyết

1. **MenuBar cũ ẩn/dùng lại trên EPLAN 2026**:
   - Không có bằng chứng bật lại bằng UI; chỉ có add-in bên thứ ba (`Show Menu Bar`) hoặc không dùng menu.
   - Tool không tính năng “bật menu cũ”; lệnh chỉ gọi action khi đã xác minh.
2. **Gộp VEE vào Tool**:
   - Không copy; chỉ launcher.
3. **Giao diện v0.6.0**:
   - Tab + 4 nhóm là bố cục mục tiêu; nút ngoài đảm bảo DPI.
4. **Thiếu lệnh/cần research thêm**:
   - Các command chưa được test không được đem vào release như là tính năng working.
5. **Nguồn tài nguyên bên ngoài**:
   - Telegram links chỉ được ghi rõ “author-controlled external link; no warranty; trademark belongs to author”.

---

## 8. Kết luận / quyết định release

- **v0.5.1** phát hành trước, không bỏ.
- **v0.6.0** không bundle add-in bên thứ ba; chỉ nhận diện nó nếu đã cài.
- **EPLAN Classic Command Center** là tính năng chính — thay thế menu cũ bằng command palette/catalog với search.
- **GIRD** chỉ xuất hiện sau khi kiểm thử action; không phát minh lệnh.
- **EDZ workflow** vẫn là phạm vi lõi; không hy sinh EDZ để làm icon/ribbon.

## 9. Checklist triển khai

- [ ] v0.5.1 native checkbox (add-in + script)
- [ ] construct test
- [ ] build/test/release v0.5.1
- [ ] ribbon DPI redesign
- [ ] Command Center catalog & UI
- [ ] VEE status detection & launcher docs
- [ ] GIRD audit and gated buttons
- [ ] docs/THIRD-PARTY-ADDINS.md
- [ ] README / INSTALL / AGENTS / CHANGELOG update
- [ ] dual-edition pack + hashes
- [ ] v0.6.0 release notes + tag/release
- [ ] UAT script on EPLAN 2026 + add-in on EPLAN 2022
