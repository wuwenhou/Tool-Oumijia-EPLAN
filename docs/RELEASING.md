# Releasing — Tool Oumijia (EPLAN Edition)

Quy trình phát hành chuẩn (2 editions — v0.5.0+):

## 1. Chuẩn bị

```powershell
git checkout main
git pull
# Đảm bảo máy có EPLAN để build add-in + test script (xem docs/BUILD.md)
```

## 2. Bump version

- `src/Oumijia.EplanAddIn/Oumijia.EplanAddIn.csproj` — `<Version>X.Y.Z</Version>`
- `tools/script-edition/ToolOumijiaScript.cs` — hằng số `ScriptVersion = "X.Y.Z-script.1"`
- `CHANGELOG.md` — thêm mục `[X.Y.Z]` (ngày), cập nhật link refs cuối file

## 3. Build add-in + kiểm tra compile script

```powershell
dotnet build src/Oumijia.EplanAddIn/Oumijia.EplanAddIn.csproj -c Release `
  -p:EplanApiDir="C:\Program Files\EPLAN\Platform\2022.0.3\Bin"
# + csc-check OumijiaRegister.cs + csc C#5-check ToolOumijiaScript.cs (recipe: docs/BUILD.md)
```

## 4. Test headless (khuyến nghị; recipe: docs/BUILD.md)

```powershell
# Deploy + register + selftest + diagparts (bao gồm import smoke test) — xem docs/BUILD.md
# + test đăng ký script edition: tools\script-edition\Install-Script.ps1 (nếu máy phù hợp)
```

## 5. Đóng gói

```powershell
powershell -ExecutionPolicy Bypass -File tools/pack.ps1 -Version X.Y.Z
# -> output\Tool-Oumijia-vX.Y.Z.zip  (~100 KB: addin\ + script\ + installer thông minh)
# -> in ra ZIP SHA256 + DLL SHA256 + CS SHA256
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
gh release create vX.Y.Z "output\Tool-Oumijia-vX.Y.Z.zip" `
  --title "Tool Oumijia (EPLAN) vX.Y.Z" `
  --notes-file "docs\releases\vX.Y.Z.md"
```

- **docs/releases/vX.Y.Z.md**: tóm tắt thay đổi + mục *Tải về* + **dán SHA-256 của ZIP vào đây**.
- Kiểm tra lại trang Release: asset tải được, notes hiển thị đúng.

## Quy ước đặt tên

| Thứ | Quy ước | Ví dụ |
|---|---|---|
| Tag | `v` + SemVer | `v0.5.0` |
| Asset | `Tool-Oumijia-v<ver>.zip` (2 editions) | `Tool-Oumijia-v0.5.0.zip` |
| Release title | `Tool Oumijia (EPLAN) v<ver>` | `Tool Oumijia (EPLAN) v0.5.0` |
| Commit phát hành | `release: vX.Y.Z` | |
