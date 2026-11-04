// Cleanup một lần: xóa toàn bộ danh sách script tự nạp của EPLAN (STATION scope).
// Dùng khi cần dọn dẹp entry thử nghiệm. KHÔNG dùng cho uninstall thường
// (uninstall chuẩn chỉ gỡ entry trỏ về ToolOumijiaScript.cs).
using System;
using System.IO;
using Eplan.EplApi.Base;
using Eplan.EplApi.Scripting;

public class OumijiaCleanupScripts
{
    [Start]
    public void Run()
    {
        string logPath = Path.Combine(Path.Combine(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Tool Oumijia"), "logs"), "probe-setlist.log");
        try
        {
            Settings settings = new Settings();
            settings.RemoveAllIndexedSetting("STATION.EplanEplApiScriptGui.Scripts");
            int count = settings.GetCountOfValues("STATION.EplanEplApiScriptGui.Scripts");
            File.AppendAllText(logPath,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  cleanup OK, count=" + count + Environment.NewLine);
        }
        catch (Exception ex)
        {
            File.AppendAllText(logPath,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  cleanup ERROR: " + ex + Environment.NewLine);
        }
    }
}
