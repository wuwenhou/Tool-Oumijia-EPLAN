// Probe: doc trang thai "registered scripts" tu settings (STATION + USER),
// ghi ket qua %APPDATA%\Tool Oumijia\logs\probe-settings.log
using System;
using System.IO;
using System.Text;
using Eplan.EplApi.Base;
using Eplan.EplApi.Scripting;

public class OumijiaProbeSettings
{
    [Start]
    public void Run()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ===");

        Settings settings = new Settings();
        string[] paths = new string[]
        {
            "STATION.EplanEplApiScriptGui.Scripts",
            "USER.EplanEplApiScriptGui.Scripts",
            "STATION.EplanEplApiScriptGui.Scripts.0",
            "USER.EplanEplApiScriptGui.Scripts.0",
            "USER.EplanEplApiScriptGui.Default",
            "STATION.EplanEplApiScriptGui.Default"
        };

        foreach (string path in paths)
        {
            try
            {
                bool exists = settings.ExistSetting(path);
                int count = -1;
                try { count = settings.GetCountOfValues(path); }
                catch (Exception ex) { sb.AppendLine("  GetCountOfValues(" + path + ") loi: " + ex.Message); }

                sb.AppendLine(path + "  exists=" + exists + "  count=" + count);

                if (exists && count > 0 && count < 50)
                {
                    for (int i = 0; i < count; i++)
                    {
                        string value;
                        try { value = settings.GetStringSetting(path, i); }
                        catch (Exception ex) { value = "(loi: " + ex.Message + ")"; }
                        sb.AppendLine("    [" + i + "] " + value);
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine(path + "  LOI: " + ex.Message);
            }
        }

        // Liet ke cac key con trong nhanh EplanEplApiScriptGui neu co the
        string[] guesses = new string[]
        {
            "USER.EplanEplApiScriptGui.LastLoadedScript",
            "STATION.EplanEplApiScriptGui.LoadRegisteredScripts",
            "USER.EplanEplApiScriptGui.LoadRegisteredScripts"
        };
        foreach (string path in guesses)
        {
            try
            {
                bool exists = settings.ExistSetting(path);
                sb.AppendLine(path + "  exists=" + exists);
                if (exists)
                {
                    sb.AppendLine("    value = " + settings.GetStringSetting(path, 0));
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine(path + "  LOI: " + ex.Message);
            }
        }

        string logPath = Path.Combine(Path.Combine(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Tool Oumijia"), "logs"), "probe-settings.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath));
        File.AppendAllText(logPath, sb.ToString());
    }
}
