//#################################################################################################################################################
// Tool Oumijia — Third-party add-ins registration script
//#################################################################################################################################################
// Đăng ký / gỡ đăng ký 2 add-in do VIET EPLAN & ELECTRIC cung cấp:
//   - VEE.EplTableMaker       (action VEECreateEplanTable)
//   - VEE.ExportConnections  (action VE_E_ExportConnections)
// Hai DLL được cài vào %APPDATA%\Tool Oumijia\ThirdParty\ trước khi chạy script.
//
// CÀI:   recycle/start EPLAN với ExecuteScript /ScriptFile:"<thư mục>\OumijiaRegisterThirdParty.cs"
// GỠ:    tạo file cờ %APPDATA%\Tool Oumijia\third-party-unregister.flag rồi chạy lại.
//
// Marker log: %APPDATA%\Tool Oumijia\logs\register-third-party.log
//#################################################################################################################################################

using System;
using System.IO;
using Eplan.EplApi.Base;
using Eplan.EplApi.Scripting;

public class OumijiaRegisterThirdParty
{
    private sealed class Entry
    {
        public readonly string ModuleId;
        public readonly string RelativePath;
        public readonly string ModuleType;
        public readonly string ActionName;
        public readonly string ActionType;
        public readonly int Ordinal;

        public Entry(string moduleId, string relativePath, string moduleType,
                     string actionName, string actionType, int ordinal)
        {
            ModuleId = moduleId;
            RelativePath = relativePath;
            ModuleType = moduleType;
            ActionName = actionName;
            ActionType = actionType;
            Ordinal = ordinal;
        }
    }

    private static readonly Entry[] Entries = new Entry[]
    {
        new Entry("VEE@EplTableMaker",
                  Path.Combine("VEE.EplTableMaker", "VEE.EplTableMaker.dll"),
                  "VEE.EplTableMaker.AddInModule",
                  "VEECreateEplanTable",
                  "VEE.EplTableMaker.CreateTableAction",
                  50),
        new Entry("VE_E_ConnectionExport",
                  Path.Combine("VEE.ExportConnections", "VEE.ExportConnections.dll"),
                  "VE_E_ConnectionExport.AddInModule",
                  "VE_E_ExportConnections",
                  "VE_E_ConnectionExport.ExportConnectionAction",
                  20)
    };

    [Start]
    public void Run()
    {
        string logPath = GetLogPath();
        string flagPath = GetFlagPath();

        try
        {
            WriteLog(logPath, "bat dau dang ky third-party (unregister=" + File.Exists(flagPath) + ")");

            if (File.Exists(flagPath))
            {
                foreach (Entry entry in Entries) Unregister(entry, logPath);
                try { File.Delete(flagPath); } catch { }
            }
            else
            {
                foreach (Entry entry in Entries) Register(entry, logPath);
            }
        }
        catch (Exception ex)
        {
            WriteLog(logPath, "ERROR: " + ex);
        }
    }

    private static void Register(Entry entry, string logPath)
    {
        string dllPath = GetDllPath(entry);
        if (!File.Exists(dllPath))
        {
            WriteLog(logPath, "ERROR: DLL khong ton tai: " + dllPath);
            return;
        }

        long size = new FileInfo(dllPath).Length;
        if (size <= 0)
        {
            WriteLog(logPath, "ERROR: DLL rong: " + dllPath);
            return;
        }

        try
        {
            AssemblyName_Verify(dllPath, entry, logPath);
        }
        catch (Exception ex)
        {
            WriteLog(logPath, "ERROR: DLL khong doc duoc metadata: " + ex.Message);
            return;
        }

        string assemblyFullName = System.Reflection.AssemblyName.GetAssemblyName(dllPath).FullName;
        string basePath = "STATION.EplanEplApiModule.Module." + entry.ModuleId;

        Settings settings = new Settings();
        try
        {
            DeleteSetting(settings, basePath + ".AssemblyName");
            DeleteSetting(settings, basePath + ".AssemblyPath");
            DeleteSetting(settings, basePath + ".TypeName");
            DeleteSetting(settings, basePath + ".LoadOnStart");
            DeleteAction(settings, entry.ActionName, entry.Ordinal);

            SetString(settings, basePath + ".AssemblyName", assemblyFullName, logPath);
            SetString(settings, basePath + ".AssemblyPath", dllPath, logPath);
            SetString(settings, basePath + ".TypeName", entry.ModuleType, logPath);
            SetBool(settings, basePath + ".LoadOnStart", true, logPath);

            string actionBase = "STATION.EplanEplApiModule.Services.AsmActions." + entry.ActionName + "." + entry.Ordinal;
            SetString(settings, actionBase + ".AssemblyName", assemblyFullName, logPath);
            SetString(settings, actionBase + ".AssemblyPath", dllPath, logPath);
            SetString(settings, actionBase + ".TypeName", entry.ActionType, logPath);
            SetString(settings, actionBase + ".ModuleName", entry.ModuleId, logPath);
            SetBool(settings, actionBase + ".EnableInterface", false, logPath);
            SetBool(settings, actionBase + ".CheckInterface", false, logPath);
            SetString(settings, actionBase + ".EnableCondition", "", logPath);
            SetBool(settings, actionBase + ".ActionLicense", false, logPath);

            WriteLog(logPath, "[THIRD] OK: registered " + entry.ModuleId + " => " + assemblyFullName);
            try
            {
                WriteLog(logPath, "[THIRD] VERIFY AssemblyPath => " + settings.GetStringSetting(basePath + ".AssemblyPath", 0));
                WriteLog(logPath, "[THIRD] VERIFY Action => " + settings.GetStringSetting(actionBase + ".ModuleName", 0) + "/" + settings.GetStringSetting(actionBase + ".TypeName", 0));
            }
            catch (Exception exVerify)
            {
                WriteLog(logPath, "[THIRD] VERIFY loi: " + exVerify.Message);
            }
        }
        finally
        {
            settings.Dispose();
        }
    }

    private static void Unregister(Entry entry, string logPath)
    {
        string basePath = "STATION.EplanEplApiModule.Module." + entry.ModuleId;
        Settings settings = new Settings();
        try
        {
            DeleteSetting(settings, basePath + ".AssemblyName");
            DeleteSetting(settings, basePath + ".AssemblyPath");
            DeleteSetting(settings, basePath + ".TypeName");
            DeleteSetting(settings, basePath + ".LoadOnStart");
            DeleteAction(settings, entry.ActionName, entry.Ordinal);
            WriteLog(logPath, "[THIRD] OK: unregistered " + entry.ModuleId);
        }
        finally
        {
            settings.Dispose();
        }
    }

    private static void AssemblyName_Verify(string dllPath, Entry entry, string logPath)
    {
        System.Reflection.AssemblyName an = System.Reflection.AssemblyName.GetAssemblyName(dllPath);
        string expectedPrefix = entry.ModuleId.Replace("@", ".");
        if (an == null || an.FullName.Length == 0)
        {
            throw new InvalidOperationException("AssemblyName empty");
        }

        WriteLog(logPath, "[THIRD] metadata: " + an.FullName);
        WriteLog(logPath, "[THIRD] expected module: " + expectedPrefix);
    }

    private static string GetThirdPartyRoot()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(Path.Combine(appData, "Tool Oumijia"), "ThirdParty");
    }

    private static string GetDllPath(Entry entry)
    {
        return Path.Combine(GetThirdPartyRoot(), entry.RelativePath);
    }

    private static string GetLogPath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(Path.Combine(Path.Combine(appData, "Tool Oumijia"), "logs"), "register-third-party.log");
    }

    private static string GetFlagPath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(Path.Combine(appData, "Tool Oumijia"), "third-party-unregister.flag");
    }

    private static void SetString(Settings settings, string path, string value, string logPath)
    {
        try
        {
            if (!settings.ExistSetting(path))
            {
                settings.AddStringSetting(path, new string[0], new string[0], ISettings.CreationFlag.Insert);
            }
        }
        catch (Exception ex)
        {
            WriteLog(logPath, "  (add string setting: " + ex.Message + ")");
        }

        try
        {
            settings.SetStringSetting(path, value);
        }
        catch (Exception ex)
        {
            WriteLog(logPath, "  (set string setting " + path + ": " + ex.Message + ")");
        }
    }

    private static void SetBool(Settings settings, string path, bool value, string logPath)
    {
        try
        {
            if (!settings.ExistSetting(path))
            {
                settings.AddBoolSetting(path, new bool[0], ISettings.CreationFlag.Insert);
            }
        }
        catch (Exception ex)
        {
            WriteLog(logPath, "  (add bool setting: " + ex.Message + ")");
        }

        try
        {
            settings.SetBoolSetting(path, value);
        }
        catch (Exception ex)
        {
            WriteLog(logPath, "  (set bool setting " + path + ": " + ex.Message + ")");
        }
    }

    private static void DeleteSetting(Settings settings, string path)
    {
        try
        {
            if (settings.ExistSetting(path))
            {
                settings.DeleteSetting(path);
            }
        }
        catch
        {
            // Best effort.
        }
    }

    private static void DeleteAction(Settings settings, string actionName, int ordinal)
    {
        string actionBase = "STATION.EplanEplApiModule.Services.AsmActions." + actionName + "." + ordinal;
        DeleteSetting(settings, actionBase + ".AssemblyName");
        DeleteSetting(settings, actionBase + ".AssemblyPath");
        DeleteSetting(settings, actionBase + ".TypeName");
        DeleteSetting(settings, actionBase + ".ModuleName");
        DeleteSetting(settings, actionBase + ".EnableInterface");
        DeleteSetting(settings, actionBase + ".CheckInterface");
        DeleteSetting(settings, actionBase + ".EnableCondition");
        DeleteSetting(settings, actionBase + ".ActionLicense");
    }

    private static void WriteLog(string logPath, string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));
            File.AppendAllText(logPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + message + Environment.NewLine);
        }
        catch
        {
            // Never fail because of logging.
        }
    }
}
