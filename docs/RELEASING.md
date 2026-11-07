# Releasing — Tool Oumijia (EPLAN Edition)

Quy trình phát hành chuẩn (2 editions — v0.5.0+):

## 1. Chuẩn bị

```powershell
git checkout main
git pull
# Đảm bảo máy có EPLAN để build đúng API target + test script (xem docs/BUILD.md)
```

## 2. Bump version

- Cả ba project trong `src/` — `<Version>X.Y.Z</Version>`
- `tools/script-edition/ToolOumijiaScript.cs` — hằng số `ScriptVersion = "X.Y.Z-script.1"`
- `CHANGELOG.md` — thêm mục `[X.Y.Z]` (ngày), cập nhật link refs cuối file
- `docs/releases/vX.Y.Z.md` — release note (pack.ps1 bắt buộc file này)

## 3. Build add-in + kiểm tra compile script

```powershell
dotnet build src/Oumijia.EplanAddIn/Oumijia.EplanAddIn.csproj -c Release `
  -p:EplanTargetFramework=net481 `
  -p:EplanApiDir="C:\Program Files\EPLAN\Platform\2026.0.3\Bin"
# + csc-check OumijiaRegister.cs + csc C#5-check ToolOumijiaScript.cs (recipe: docs/BUILD.md)
# Máy không có .NET SDK: build thủ công bằng csc + AssemblyInfo.cs tạm (docs/BUILD.md)
```

## 4. Test headless (khuyến nghị; recipe: docs/BUILD.md)

```powershell
# Deploy + register + selftest + diagparts (bao gồm import smoke test) — xem docs/BUILD.md
# + test đăng ký script edition: tools\script-edition\Install-Script.ps1 (nếu máy phù hợp)
```

## 5. Đóng gói

```powershell
powershell -ExecutionPolicy Bypass -File tools/pack.ps1 -Version 0.6.0-rc.1 -TargetFramework net481
# -> output\Tool-Oumijia-v0.6.0-rc.1.zip (addin\ + script\ + component dùng chung)
# -> pack.ps1 phải văng lỗi nếu thiếu: Add-in, UI, EdzReader, SharpCompress
# -> pack.ps1 lọc bỏ PortalHost/WebView2 còn sót và yêu cầu docs\releases\vX.Y.Z.md
```

## 6. Cổng UAT trước phát hành

Chỉ release khi **kiểm thử thật** đạt:

- Ribbon đúng nhóm **Quản lý EDZ** ba nút Nhập → Xuất → Menu bar theo cột; một Portal ở nhóm Cloud của Tool (7 nút), tab Cloud gốc còn nguyên.
- Nhập EDZ preview cây gói→part, bên phải chỉ `Chi tiết` + `Thuộc tính | Giá trị`; lỗi preview không chặn queue.
- Xuất EDZ không hồi quy.
- Data Portal **thực sự hiện** tab native EPLAN (không chỉ action trả `true`), dùng phiên đã đăng nhập và quyền tài khoản được phép.
- Không còn WebView2/PortalHost trong thư mục cài đặt và log không còn lỗi WebView2.
- `.edz` tải xong nhập lại Parts Database.
- Máy phù hợp được IT cho phép; lỗi portal không được sửa bằng workaround license.
- Build payload theo Variant/Version, test `-ListInstalled`/`-TargetW3u` khi máy có nhiều bản; installer/gỡ headless PASS đúng target và không phá shared runtime bản khác. Add-in và Script Edition chỉ gắn nhãn hỗ trợ ở bản thực sự UAT.

Nếu chưa đạt UAT: không tạo **Release ổn định**. Chỉ tạo **pre-release** khi người dùng chấp thuận rõ, có build/package hợp lệ, ghi công khai chính xác phần chưa UAT và giới hạn target; tuyệt đối không gắn nhãn hỗ trợ các phiên bản chưa thử.

## 7. Commit + tag + push

```powershell
git add -A
git commit -m "release: vX.Y.Z"
git tag vX.Y.Z
git push origin main
git push origin vX.Y.Z
```

## 8. Tạo GitHub Release

```powershell
gh release create vX.Y.Z "output\Tool-Oumijia-vX.Y.Z.zip" `
  --title "Tool Oumijia (EPLAN) vX.Y.Z" `
  --notes-file "docs\releases\vX.Y.Z.md" --prerelease # RC thử nghiệm; bỏ cờ khi đủ UAT ổn định
```

- **docs/releases/vX.Y.Z.md**: tóm tắt thay đổi + mục *Tải về* + **dán SHA-256 sau khi tạo ZIP**; bản notes trong ZIP chưa có hash để tránh vòng lặp.
- Kiểm tra lại trang Release: asset tải được, notes hiển thị đúng.

## Quy ước đặt tên

| Thứ | Quy ước | Ví dụ |
|---|---|---|
| Tag | `v` + SemVer | `v0.5.0` |
| Asset | `Tool-Oumijia-v<ver>.zip` (2 editions) | `Tool-Oumijia-v0.6.0.zip` |
| Release title | `Tool Oumijia (EPLAN) v<ver>` | `Tool Oumijia (EPLAN) v0.6.0` |
| Commit phát hành | `release: vX.Y.Z` | |
