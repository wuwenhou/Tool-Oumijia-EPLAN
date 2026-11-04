# Releasing — Tool Oumijia (EPLAN Edition)

Quy trình phát hành chuẩn (làm theo đúng thứ tự):

## 1. Chuẩn bị

```powershell
git checkout main
git pull
dotnet test Tool-Oumijia-EPLAN.sln -c Release   # phải xanh 100%
```

## 2. Bump version (SemVer)

Sửa `<Version>` trong **cả 3 csproj**:

- `src/Oumijia.Core/Oumijia.Core.csproj`
- `src/Oumijia.Tool/Oumijia.Tool.csproj`
- `src/Oumijia.EplanAddIn/Oumijia.EplanAddIn.csproj`

Cập nhật `CHANGELOG.md` (chuyển mục *Unreleased* → phiên bản mới, ghi ngày).

## 3. Đóng gói

```powershell
# (v0.2+) build add-in TRƯỚC khi pack để zip kèm thư mục addin\ (cần máy có EPLAN):
dotnet build src/Oumijia.EplanAddIn/Oumijia.EplanAddIn.csproj -c Release `
  -p:EplanApiDir="C:\Program Files\EPLAN\Platform\2022.0.3\Bin"

powershell -ExecutionPolicy Bypass -File tools/pack.ps1 -Version X.Y.Z
# -> output\Tool-Oumijia-EPLAN-vX.Y.Z-win-x64.zip  (kèm addin\ + VERSION.txt)
# -> in ra ZIP SHA256 + EXE SHA256
```

> Nếu không build add-in, pack.ps1 vẫn chạy nhưng zip thiếu `addin\` (có cảnh báo).

## 4. Commit + tag + push

```powershell
git add -A
git commit -m "release: vX.Y.Z"
git tag vX.Y.Z
git push origin main
git push origin vX.Y.Z
```

## 5. Tạo GitHub Release

```powershell
gh release create vX.Y.Z "output\Tool-Oumijia-EPLAN-vX.Y.Z-win-x64.zip" `
  --title "Tool Oumijia (EPLAN) vX.Y.Z" `
  --notes-file "docs\releases\vX.Y.Z.md"
```

- **docs/releases/vX.Y.Z.md**: mở đầu bằng tóm tắt thay đổi, kèm mục *Tải về* (yêu cầu Windows 10/11,
  self-contained không cần .NET), mục *Kiểm tra toàn vẹn* — **dán SHA-256 của ZIP vào đây**.
- Kiểm tra lại trang Release: asset tải được, notes hiển thị đúng.

## 6. Sau phát hành

- Xác nhận CI xanh trên tag mới (badge trong README).
- Nếu có thay đổi hành vi ảnh hưởng người dùng cũ → ghi rõ trong release notes mục *Breaking changes*.

## Quy ước đặt tên

| Thứ | Quy ước | Ví dụ |
|---|---|---|
| Tag | `v` + SemVer | `v0.2.0` |
| Asset | `Tool-Oumijia-EPLAN-v<ver>-win-x64.zip` | `Tool-Oumijia-EPLAN-v0.2.0-win-x64.zip` |
| Release title | `Tool Oumijia (EPLAN) v<ver>` | `Tool Oumijia (EPLAN) v0.2.0` |
| Commit phát hành | `release: vX.Y.Z` | |
