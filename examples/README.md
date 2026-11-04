# Sample files (synthetic)

These files are **invented for testing** Tool Oumijia's scan / classify / file
workflow. They contain no vendor data and are not real EPLAN packages.

Regenerate with:

    powershell -ExecutionPolicy Bypass -File tools/make-samples.ps1

Try it:

1. Open Tool-Oumijia.exe
2. In **Xuất (Export)**, add this `examples` folder as inbox and press **Quét ngay**
3. Watch how each package is classified (ABB/MCB, Siemens/PLC, ...)
4. Press **XUẤT** to file them into your library tree
