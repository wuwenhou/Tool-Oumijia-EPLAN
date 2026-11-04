================================================================
  TOOL OUMIJIA (EPLAN EDITION) — HUONG DAN NHANH
================================================================

Day la cong cu ho tro lam viec voi goi du lieu linh kien EPLAN
(file .edz) va macro EPLAN (.ema/.emp/.ems).

1) CHAY CONG CU
   - Giai nen toan bo file ZIP nay ra mot thu muc bat ky.
   - Chay: Tool-Oumijia.exe
   - Windows SmartScreen co the hien canh bao lan dau:
     bam "More info" -> "Run anyway".

2) CHUC NANG XUAT (EXPORT) — phan loai thu vien
   - Tab "Xuat (Export)": them thu muc chua file .edz/.ema (inbox),
     bam "Quet ngay".
   - Bang ket qua hien thi Hang / Nhom / Ma hang va thu muc dich de xuat.
     Cot "Den thu muc" co the sua truc tiep.
   - Bam "XUAT" de sao chep vao cay thu vien:
         <thu vien>\<Hang>\<Nhom>\<file>
     Log day du duoc luu trong %APPDATA%\Tool Oumijia\logs.

3) CHUC NANG NHAP (IMPORT)
   - Tab "Nhap (Import)".
   - Muc 2: cai macro .ema/.emp/.ems vao thu muc master data cua EPLAN
     (khoi dong lai EPLAN sau khi cai).
   - Muc 1: gom danh sach EDZ can nhap vao Parts Database.
     Trong EPLAN: Master data -> Parts -> Management -> [Extras] -> Import.
     (Nhap hang loat bang Add-in EPLAN se co o phien ban tiep theo.)

4) CAU HINH
   - Tab "Cai dat": thu muc thu vien, thu muc quet, chinh sach trung ten.
   - rules.json (trong %APPDATA%\Tool Oumijia) cho phep chinh quy tac
     phan loai Hang/Nhom theo nhu cau.

5) LUU Y
   - Cong cu nay khong thuoc EPLAN; chi dung API/tep tieu chuan.
   - Du lieu linh kien (EDZ) thuoc ban quyen cua cac hang san xuat /
   EPLAN — chi dung cho muc dich ca nhan/noi bo, khong phat tan lai.

Du an: https://github.com/wuwenhou/Tool-Oumijia-EPLAN
