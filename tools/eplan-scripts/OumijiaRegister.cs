//#################################################################################################################################################
// Tool Oumijia — EPLAN add-in registration script
//#################################################################################################################################################
// Đăng ký / gỡ đăng ký add-in "Oumijia.EplAddin.EDZ" trong EPLAN settings (phạm vi STATION).
//
// Chạy headless (EPLAN tự khởi động, thực thi script, rồi thoát):
//   W3u.exe /Auto /Quiet /NoSplash /NoLicenseDialog /NoUserRightsDialog /NoLoadWorkspace OumijiaRegisterAction
//   W3u.exe /Auto /Quiet /NoSplash /NoLicenseDialog /NoUserRightsDialog /NoLoadWorkspace OumijiaUnregisterAction
//
// Marker log: %APPDATA%\Tool Oumijia\logs\register-script.log
//
// Script này phải nằm trong thư mục script của EPLAN, ví dụ:
//   C:\Users\Public\EPLAN\Data\Scripts\Company name\OumijiaRegister.cs
//#################################################################################################################################################

using System;
using System.IO;
using Eplan.EplApi.Base;
using Eplan.EplApi.Scripting;

public class OumijiaRegister
{
    private const string ModuleName = "Oumijia.EplAddin.EDZ";
    private const string ModuleType = "Oumijia.EplanAddIn.AddIn";

    /// <summary>
    /// Chạy ngay khi script được start (File → Extras → Interfaces → Scripts → Run...
    /// hoặc qua action ExecuteScript từ command line).
    /// Nếu tồn tại file cờ "%APPDATA%\Tool Oumijia\unregister.flag" thì GỠ đăng ký,
    /// ngược lại thì ĐĂNG KÝ add-in.
    /// </summary>
    [Start]
    public void OnStart()
    {
        string flagPath = GetFlagPath();
        if (File.Exists(flagPath))
        {
            Unregister();
            try { File.Delete(flagPath); } catch { }
        }
        else
        {
            Register();
        }
    }

    [DeclareAction("OumijiaRegisterAction")]
    public void Register()
    {
        string logPath = GetLogPath();
        try
        {
            string dllPath = GetDllPath();
            if (!File.Exists(dllPath))
            {
                WriteLog(logPath, "ERROR: add-in dll not found: " + dllPath);
                return;
            }

            string assemblyFullName = System.Reflection.AssemblyName.GetAssemblyName(dllPath).FullName;
            string basePath = GetSettingBasePath();

            Settings settings = new Settings();
            try
            {
                // QUAN TRỌNG: XÓA TRƯỚC KHI GHI.
                // Settings của EPLAN có thể giữ nhiều giá trị lịch sử cho cùng một key
                // (vd do đăng ký thủ công qua dialog "API add-ins" bằng đường dẫn khác).
                // Nếu chỉ SetStringSetting, giá trị CŨ vẫn là thứ EPLAN đọc khi khởi động.
                DeleteSetting(settings, basePath + ".AssemblyName");
                DeleteSetting(settings, basePath + ".AssemblyPath");
                DeleteSetting(settings, basePath + ".TypeName");
                DeleteSetting(settings, basePath + ".LoadOnStart");

                SetString(settings, basePath + ".AssemblyName", assemblyFullName, logPath);
                SetString(settings, basePath + ".AssemblyPath", dllPath, logPath);
                SetString(settings, basePath + ".TypeName", ModuleType, logPath);
                SetBool(settings, basePath + ".LoadOnStart", true, logPath);

                // v0.3.0: action "OumijiaOpenTool" (cửa sổ app) đã bị bỏ — dọn key cũ nếu còn.
                DeleteAction(settings, "OumijiaOpenTool", 20);
                // Các action hiện hành: xóa trước rồi ghi lại (lý do như trên).
                DeleteAction(settings, "OumijiaImportEdz", 21);
                DeleteAction(settings, "OumijiaExportEdz", 22);
                DeleteAction(settings, "OumijiaSelfTest", 23);
                DeleteAction(settings, "OumijiaDiagParts", 24);

                WriteAction(settings, "OumijiaImportEdz", 21,
                    "Oumijia.EplanAddIn.Actions.ImportEdzAction", dllPath, assemblyFullName, logPath);
                WriteAction(settings, "OumijiaExportEdz", 22,
                    "Oumijia.EplanAddIn.Actions.ExportEdzAction", dllPath, assemblyFullName, logPath);
                WriteAction(settings, "OumijiaSelfTest", 23,
                    "Oumijia.EplanAddIn.Actions.SelfTestAction", dllPath, assemblyFullName, logPath);
                WriteAction(settings, "OumijiaDiagParts", 24,
                    "Oumijia.EplanAddIn.Actions.DiagPartsAction", dllPath, assemblyFullName, logPath);

                // Đọc lại để tự xác nhận giá trị EPLAN sẽ dùng khi khởi động.
                try
                {
                    WriteLog(logPath, "VERIFY AssemblyPath  => " + settings.GetStringSetting(basePath + ".AssemblyPath", 0));
                    WriteLog(logPath, "VERIFY AssemblyName  => " + settings.GetStringSetting(basePath + ".AssemblyName", 0));
                }
                catch (Exception exVerify)
                {
                    WriteLog(logPath, "VERIFY loi doc lai: " + exVerify.Message);
                }
            }
            finally
            {
                settings.Dispose();
            }

            WriteLog(logPath, "OK: registered " + assemblyFullName);
            WriteLog(logPath, "OK: path " + dllPath);
        }
        catch (Exception ex)
        {
            WriteLog(logPath, "ERROR: " + ex.ToString());
        }
    }

    [DeclareAction("OumijiaUnregisterAction")]
    public void Unregister()
    {
        string logPath = GetLogPath();
        try
        {
            string basePath = GetSettingBasePath();

            Settings settings = new Settings();
            try
            {
                DeleteSetting(settings, basePath + ".AssemblyName");
                DeleteSetting(settings, basePath + ".AssemblyPath");
                DeleteSetting(settings, basePath + ".TypeName");
                DeleteSetting(settings, basePath + ".LoadOnStart");

                DeleteAction(settings, "OumijiaOpenTool", 20);
                DeleteAction(settings, "OumijiaImportEdz", 21);
                DeleteAction(settings, "OumijiaExportEdz", 22);
                DeleteAction(settings, "OumijiaSelfTest", 23);
                DeleteAction(settings, "OumijiaDiagParts", 24);
            }
            finally
            {
                settings.Dispose();
            }

            WriteLog(logPath, "OK: unregistered");
        }
        catch (Exception ex)
        {
            WriteLog(logPath, "ERROR: " + ex.ToString());
        }
    }

    private static string GetSettingBasePath()
    {
        return "STATION.EplanEplApiModule.Module." + ModuleName.Replace(".", "@");
    }

    private static string GetDllPath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(Path.Combine(Path.Combine(appData, "Tool Oumijia"), "AddIn"), ModuleName + ".dll");
    }

    private static string GetLogPath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(Path.Combine(Path.Combine(appData, "Tool Oumijia"), "logs"), "register-script.log");
    }

    private static string GetFlagPath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(Path.Combine(appData, "Tool Oumijia"), "unregister.flag");
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

    private static void WriteAction(Settings settings, string actionName, int ordinal, string actionType,
        string dllPath, string assemblyFullName, string logPath)
    {
        string actionBase = "STATION.EplanEplApiModule.Services.AsmActions." + actionName + "." + ordinal;
        SetString(settings, actionBase + ".AssemblyName", assemblyFullName, logPath);
        SetString(settings, actionBase + ".AssemblyPath", dllPath, logPath);
        SetString(settings, actionBase + ".TypeName", actionType, logPath);
        SetString(settings, actionBase + ".ModuleName", ModuleName, logPath);
        SetBool(settings, actionBase + ".EnableInterface", false, logPath);
        SetBool(settings, actionBase + ".CheckInterface", false, logPath);
        SetString(settings, actionBase + ".EnableCondition", "", logPath);
        SetBool(settings, actionBase + ".ActionLicense", false, logPath);
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

    private static void WriteLog(string logPath, string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));
            File.AppendAllText(logPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + message + Environment.NewLine);
        }
        catch
        {
            // Never fail the script because of logging.
        }
    }
}
