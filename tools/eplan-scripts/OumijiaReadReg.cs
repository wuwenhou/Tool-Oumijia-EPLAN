//#################################################################################################################################################
// Tool Oumijia — đọc trạng thái đăng ký (chẩn đoán)
//#################################################################################################################################################
// Chạy trong EPLAN (ExecuteScript) → ghi toàn bộ giá trị settings đăng ký add-in
// (STATION + USER, module + actions, cho cả Oumijia và VEE làm tham chiếu)
// vào %APPDATA%\Tool Oumijia\logs\readreg.log
//#################################################################################################################################################

using System;
using System.IO;
using Eplan.EplApi.Base;
using Eplan.EplApi.Scripting;

public class OumijiaReadReg
{
    [Start]
    public void Run()
    {
        string logPath = Path.Combine(Path.Combine(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Tool Oumijia"), "logs"), "readreg.log");

        Settings settings = new Settings();
        try
        {
            WriteLog(logPath, "=== READ REG " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ===");

            string[] names = { "Oumijia.EplAddin.EDZ", "VEE.ExportConnections" };
            foreach (string name in names)
            {
                string escaped = name.Replace(".", "@");
                foreach (string scope in new[] { "STATION", "USER" })
                {
                    string moduleBase = scope + ".EplanEplApiModule.Module." + escaped;
                    Dump(settings, logPath, moduleBase + ".AssemblyName");
                    Dump(settings, logPath, moduleBase + ".AssemblyPath");
                    Dump(settings, logPath, moduleBase + ".TypeName");
                    Dump(settings, logPath, moduleBase + ".LoadOnStart");
                }
            }

            string[] actions = { "OumijiaOpenTool", "OumijiaImportEdz", "OumijiaExportEdz", "OumijiaSelfTest", "OumijiaDiagParts", "OumijiaDataPortal" };
            int[] ordinals = { 20, 21, 22, 23, 24, 25 };
            for (int i = 0; i < actions.Length; i++)
            {
                string actionBase = "STATION.EplanEplApiModule.Services.AsmActions." + actions[i] + "." + ordinals[i];
                Dump(settings, logPath, actionBase + ".AssemblyPath");
            }
        }
        finally
        {
            try { settings.Dispose(); } catch { }
        }
    }

    private static void Dump(Settings settings, string logPath, string path)
    {
        string value;
        try
        {
            if (!settings.ExistSetting(path))
            {
                value = "(khong ton tai)";
            }
            else
            {
                try
                {
                    value = settings.GetStringSetting(path, 0);
                }
                catch
                {
                    try { value = settings.GetBoolSetting(path, 0).ToString(); }
                    catch { value = "(khong doc duoc)"; }
                }
            }
        }
        catch (Exception ex)
        {
            value = "(loi: " + ex.Message + ")";
        }

        WriteLog(logPath, path + "  =>  " + value);
    }

    private static void WriteLog(string logPath, string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));
            File.AppendAllText(logPath, message + Environment.NewLine);
        }
        catch
        {
            // best effort
        }
    }
}
