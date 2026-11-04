================================================================
  TOOL OUMIJIA (EPLAN EDITION) — HUONG DAN NHANH
================================================================

Cong cu ho tro lam viec voi goi du lieu linh kien EPLAN (file .edz):
phan loai thu vien (app standalone) + nhap/xuat Parts Database
(add-in trong EPLAN). Phien ban nay CHI xu ly file .edz.

1) CHAY CONG CU (STANDALONE)
   - Giai nen toan bo file ZIP nay ra mot thu muc bat ky.
   - Chay: Tool-Oumijia.exe
   - Windows SmartScreen co the hien canh bao lan dau:
     bam "More info" -> "Run anyway".

2) CHUC NANG XUAT (EXPORT) — phan loai thu vien
   - Tab "Xuat (Export)": them thu muc chua file .edz (inbox),
     bam "Quet ngay".
   - Bang ket qua hien thi Hang / Nhom / Ma hang va thu muc dich.
     Cot "Den thu muc" co the sua truc tiep.
   - Bam "XUAT" de sao chep vao cay thu vien:
         <thu vien>\<Hang>\<Nhom>\<file>
   - Log day du: %APPDATA%\Tool Oumijia\logs.

3) CHUC NANG NHAP (IMPORT)
   - Tab "Nhap (Import)": gom danh sach .edz can nhap vao Parts Database
     (nut "Copy danh sach" de theo doi).

4) ADD-IN EPLAN (KHUYEN NGHI) — trong thu muc addin\
   - Chay: addin\Install-AddIn.bat   (khong can Administrator)
   - Mo EPLAN -> ribbon "Tool Oumijia" xuat hien voi 3 nut:
       - "Nhap EDZ..."  : BATCH nhap nhieu goi .edz vao Parts Database
                          (API chinh thuc cua EPLAN, co log CSV)
       - "Xuat EDZ..."  : xuat parts ra EDZ (backup / chia se noi bo)
       - "Cua so Tool Oumijia": mo app standalone
   - Go cai dat: addin\Uninstall-AddIn.bat
   - Chi tiet: addin\README.txt

5) CAU HINH
   - Tab "Cai dat": thu muc thu vien, thu muc quet, chinh sach trung ten.
   - rules.json (trong %APPDATA%\Tool Oumijia) cho phep chinh quy tac
     phan loai Hang/Nhom theo nhu cau.

6) LUU Y
   - Cong cu nay khong thuoc EPLAN; chi dung API/tep chuan.
   - Du lieu linh kien (EDZ) thuoc ban quyen cua cac hang san xuat /
     EPLAN — chi dung cho muc dich ca nhan/noi bo, khong phat tan lai.
   - Kiem tra SHA-256 cua Tool-Oumijia.exe trong VERSION.txt neu can.

Du an: https://github.com/wuwenhou/Tool-Oumijia-EPLAN
