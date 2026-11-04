//#################################################################################################################################################
// Tool Oumijia — đăng ký Script Edition vào danh sách "script tự nạp" của EPLAN
//#################################################################################################################################################
// Ghi đường dẫn ToolOumijiaScript.cs vào STATION.EplanEplApiScriptGui.Scripts
// (danh sách script được EPLAN tự nạp lại ở mọi lần khởi động).
//
// CÀI:   W3u.exe /Auto /Quiet /NoSplash /NoLicenseDialog /NoUserRightsDialog /NoLoadWorkspace
//            ExecuteScript /ScriptFile:"<thư mục>\OumijiaRegisterScript.cs"
// GỠ:    tạo file cờ %APPDATA%\Tool Oumijia\script-unregister.flag rồi chạy lại lệnh trên.
//
// Marker log: %APPDATA%\Tool Oumijia\logs\register-script.log  (dòng có [SCRIPT])
//
// Vì sao không dùng action RegisterScript của EPLAN: trên EPLAN 2022.0.3 action đó
// chạy không lỗi nhưng KHÔNG ghi gì vào danh sách (đã kiểm chứng thực tế) — trong khi
// ghi settings trực tiếp bằng script chạy trong EPLAN là cách đã kiểm chứng hoạt động.
//#################################################################################################################################################

using System;
using System.Collections.Generic;
using System.IO;
using Eplan.EplApi.Base;
using Eplan.EplApi.Scripting;

public class OumijiaRegisterScript
{
    private const string ScriptsSetting = "STATION.EplanEplApiScriptGui.Scripts";
    private const string ScriptFileName = "ToolOumijiaScript.cs";

    [Start]
    public void Run()
    {
        string logPath = GetLogPath();
        try
        {
            string scriptPath = GetScriptPath();
            string flagPath = GetFlagPath();

            WriteLog(logPath, "chay dang ky script edition (flag=" + File.Exists(flagPath) + ")");

            if (File.Exists(flagPath))
            {
                Unregister(scriptPath, logPath);
                try { File.Delete(flagPath); } catch { }
            }
            else
            {
                Register(scriptPath, logPath);
            }
        }
        catch (Exception ex)
        {
            WriteLog(logPath, "ERROR: " + ex);
        }
    }

    private static void Register(string scriptPath, string logPath)
    {
        if (!File.Exists(scriptPath))
        {
            WriteLog(logPath, "ERROR: khong thay script: " + scriptPath);
            return;
        }

        Settings settings = new Settings();
        List<string> current = ReadValues(settings, logPath);

        if (Contains(current, scriptPath))
        {
            WriteLog(logPath, "OK: da dang ky tu truoc: " + scriptPath);
            return;
        }

        int index = current.Count;
        bool written = false;
        try
        {
            settings.SetStringSetting(ScriptsSetting, scriptPath, index);
            written = true;
        }
        catch (Exception ex)
        {
            WriteLog(logPath, "  (ghi theo index loi: " + ex.Message + ") — thu ghi khong index");
        }

        if (!written)
        {
            try
            {
                settings.SetStringSetting(ScriptsSetting, scriptPath);
            }
            catch (Exception ex)
            {
                WriteLog(logPath, "ERROR: khong ghi duoc registration: " + ex.Message);
                return;
            }
        }

        List<string> after = ReadValues(settings, logPath);
        WriteLog(logPath, "OK: da dang ky script tu nap (" + after.Count + " muc):");
        foreach (string value in after)
        {
            WriteLog(logPath, "    - " + value);
        }
    }

    private static void Unregister(string scriptPath, string logPath)
    {
        Settings settings = new Settings();
        List<string> current = ReadValues(settings, logPath);

        List<string> keep = new List<string>();
        foreach (string value in current)
        {
            if (!string.Equals(value, scriptPath, StringComparison.OrdinalIgnoreCase))
            {
                keep.Add(value);
            }
        }

        try
        {
            settings.RemoveAllIndexedSetting(ScriptsSetting);
        }
        catch (Exception ex)
        {
            WriteLog(logPath, "  (xoa toan bo loi: " + ex.Message + ")");
        }

        for (int i = 0; i < keep.Count; i++)
        {
            try
            {
                settings.SetStringSetting(ScriptsSetting, keep[i], i);
            }
            catch (Exception ex)
            {
                WriteLog(logPath, "  (ghi lai muc " + i + " loi: " + ex.Message + ")");
            }
        }

        List<string> after = ReadValues(settings, logPath);
        WriteLog(logPath, "OK: da go dang ky; con lai " + after.Count + " script.");
    }

    private static List<string> ReadValues(Settings settings, string logPath)
    {
        List<string> values = new List<string>();
        try
        {
            int count = settings.GetCountOfValues(ScriptsSetting);
            for (int i = 0; i < count; i++)
            {
                try
                {
                    values.Add(settings.GetStringSetting(ScriptsSetting, i));
                }
                catch
                {
                    // bỏ qua mục đọc lỗi
                }
            }
        }
        catch (Exception ex)
        {
            WriteLog(logPath, "  (dem so muc loi: " + ex.Message + ")");
        }
        return values;
    }

    private static bool Contains(List<string> list, string value)
    {
        foreach (string item in list)
        {
            if (string.Equals(item, value, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static string GetScriptPath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(Path.Combine(Path.Combine(appData, "Tool Oumijia"), "Script"), ScriptFileName);
    }

    private static string GetFlagPath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(Path.Combine(appData, "Tool Oumijia"), "script-unregister.flag");
    }

    private static string GetLogPath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(Path.Combine(Path.Combine(appData, "Tool Oumijia"), "logs"), "register-script.log");
    }

    private static void WriteLog(string logPath, string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));
            File.AppendAllText(logPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  [SCRIPT] " + message + Environment.NewLine);
        }
        catch
        {
            // không bao giờ để log làm hỏng thao tác
        }
    }
}
