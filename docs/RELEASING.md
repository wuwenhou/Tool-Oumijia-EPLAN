# Releasing — Tool Oumijia (EPLAN Edition)

Quy trình phát hành chuẩn cho **add-in-only** (v0.3.0+):

## 1. Chuẩn bị

```powershell
git checkout main
git pull
# Đảm bảo máy có EPLAN để build add-in (xem docs/BUILD.md)
```

## 2. Bump version

- `src/Oumijia.EplanAddIn/Oumijia.EplanAddIn.csproj` — `<Version>X.Y.Z</Version>`
- `CHANGELOG.md` — thêm mục `[X.Y.Z]` (ngày), cập nhật link refs cuối file

## 3. Build add-in + kiểm tra compile script

```powershell
dotnet build src/Oumijia.EplanAddIn/Oumijia.EplanAddIn.csproj -c Release `
  -p:EplanApiDir="C:\Program Files\EPLAN\Platform\2022.0.3\Bin"
# + csc-check OumijiaRegister.cs (recipe: docs/BUILD.md)
```

## 4. Test headless (khuyến nghị; recipe: docs/BUILD.md)

```powershell
# Deploy + register + selftest + diagparts — xem docs/BUILD.md mục "Kiểm thử headless"
```

## 5. Đóng gói

```powershell
powershell -ExecutionPolicy Bypass -File tools/pack.ps1 -Version X.Y.Z
# -> output\Tool-Oumijia-AddIn-vX.Y.Z.zip  (~26 KB)
# -> in ra ZIP SHA256 + DLL SHA256
```

## 6. Commit + tag + push

```powershell
git add -A
git commit -m "release: vX.Y.Z"
git tag vX.Y.Z
git push origin main
git push origin vX.Y.Z
```

## 7. Tạo GitHub Release

```powershell
gh release create vX.Y.Z "output\Tool-Oumijia-AddIn-vX.Y.Z.zip" `
  --title "Tool Oumijia (EPLAN) vX.Y.Z" `
  --notes-file "docs\releases\vX.Y.Z.md"
```

- **docs/releases/vX.Y.Z.md**: tóm tắt thay đổi + mục *Tải về* + **dán SHA-256 của ZIP vào đây**.
- Kiểm tra lại trang Release: asset tải được, notes hiển thị đúng.

## Quy ước đặt tên

| Thứ | Quy ước | Ví dụ |
|---|---|---|
| Tag | `v` + SemVer | `v0.3.0` |
| Asset | `Tool-Oumijia-AddIn-v<ver>.zip` | `Tool-Oumijia-AddIn-v0.4.0.zip` |
| Release title | `Tool Oumijia (EPLAN) v<ver>` | `Tool Oumijia (EPLAN) v0.3.0` |
| Commit phát hành | `release: vX.Y.Z` | |
