// Registration for Electric P8 2022.0.3 ONLY. Run from inside that EPLAN process:
//   W3u.exe /Auto /Quiet /NoSplash /NoLicenseDialog /NoUserRightsDialog /NoLoadWorkspace
//     ExecuteScript /ScriptFile:"...\OumijiaRegister2022.cs"
//     /AssemblyPath:"%APPDATA%\Tool Oumijia\AddIn\2022.staging\Oumijia.EplAddin.EDZ.dll"
//     /Operation:Register
// The installer must take a verified settings backup before this script runs.
using System;
using System.IO;
using System.Reflection;
using Eplan.EplApi.Base;
using Eplan.EplApi.Scripting;

public class OumijiaRegister2022
{
    private const string ModuleName = "Oumijia.EplAddin.EDZ";
    private const string ModuleType = "Oumijia.EplanAddIn.AddIn";
    private const string ModulePath = "STATION.EplanEplApiModule.Module.Oumijia@EplAddin@EDZ";
    private const string ActionsPath = "STATION.EplanEplApiModule.Services.AsmActions.";

    private static readonly string[] ActionNames = {
        "OumijiaImportEdz", "OumijiaExportEdz", "OumijiaSelfTest",
        "OumijiaDiagParts", "OumijiaDataPortal", "OumijiaToggleMainMenu"
    };
    private static readonly string[] ActionTypes = {
        "Oumijia.EplanAddIn.Actions.ImportEdzAction",
        "Oumijia.EplanAddIn.Actions.ExportEdzAction",
        "Oumijia.EplanAddIn.Actions.SelfTestAction",
        "Oumijia.EplanAddIn.Actions.DiagPartsAction",
        "Oumijia.EplanAddIn.Actions.DataPortalAction",
        "Oumijia.EplanAddIn.Actions.ToggleMainMenuAction"
    };
    private static readonly string[] ActionFields = {
        "AssemblyName", "AssemblyPath", "TypeName", "ModuleName",
        "EnableInterface", "CheckInterface", "EnableCondition", "ActionLicense"
    };
    private static readonly string[] ModuleFields = {
        "AssemblyName", "AssemblyPath", "TypeName", "LoadOnStart"
    };
    private static readonly string[] StaleActionNames = {
        "OumijiaOpenTool", "OumijiaArrangeOnGrid", "OumijiaAutomationConnections",
        "OumijiaAutomationDevices", "OumijiaAutomationExport", "OumijiaAutomationPlc",
        "OumijiaCreateTable", "OumijiaEnsureRibbon", "OumijiaEplanClassic"
    };
    private static readonly int[] StaleActionOrdinals = { 20, 33, 25, 28, 27, 26, 29, 31, 25 };

    [Start]
    public void Run(string AssemblyPath, string Operation)
    {
        string log = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Tool Oumijia", "logs", "register-2022.log");
        try
        {
            ValidateEnvironment(AssemblyPath, Operation);
            Settings settings = new Settings();
            try
            {
                if (String.Equals(Operation, "Register", StringComparison.OrdinalIgnoreCase))
                    Register(settings, AssemblyPath, log);
                else if (String.Equals(Operation, "Unregister", StringComparison.OrdinalIgnoreCase))
                    Unregister(settings, AssemblyPath, log);
                else if (String.Equals(Operation, "Verify", StringComparison.OrdinalIgnoreCase))
                {
                    Verify(settings, AssemblyPath, AssemblyName.GetAssemblyName(AssemblyPath).FullName);
                    WriteLog(log, "OK: verified " + AssemblyPath);
                }
                else
                    throw new InvalidOperationException("Operation must be Register, Verify, or Unregister.");
            }
            finally { settings.Dispose(); }
        }
        catch (Exception ex)
        {
            WriteLog(log, "ERROR: " + ex);
            // EPLAN may still exit with code 0; the installer MUST inspect this fresh marker.
        }
    }

    private static void ValidateEnvironment(string dll, string operation)
    {
        DirectoryInfo bin = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\'));
        if (bin.Parent == null || bin.Parent.Parent == null ||
            !String.Equals(bin.Name, "Bin", StringComparison.OrdinalIgnoreCase) ||
            !String.Equals(bin.Parent.Name, "2022.0.3", StringComparison.OrdinalIgnoreCase) ||
            !String.Equals(bin.Parent.Parent.Name, "Platform", StringComparison.OrdinalIgnoreCase) ||
            Environment.CommandLine.IndexOf("/Variant:\"Electric P8\"", StringComparison.OrdinalIgnoreCase) < 0)
            throw new InvalidOperationException("Refusing registration outside Electric P8 2022.0.3.");

        if (String.IsNullOrWhiteSpace(dll) || !Path.IsPathRooted(dll) ||
            !String.Equals(Path.GetFileName(dll), ModuleName + ".dll", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("AssemblyPath must point to the 2022 add-in DLL.");

        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string root = Path.Combine(appData, "Tool Oumijia", "AddIn");
        string parent = Path.GetFullPath(Path.GetDirectoryName(dll)).TrimEnd('\\');
        bool staging = String.Equals(parent, Path.Combine(root, "2022.staging"), StringComparison.OrdinalIgnoreCase);
        bool final = String.Equals(parent, Path.Combine(root, "2022"), StringComparison.OrdinalIgnoreCase);
        if (!staging && !final)
            throw new InvalidOperationException("AssemblyPath must be in the isolated 2022 or 2022.staging folder.");
        if (!String.Equals(operation, "Register", StringComparison.OrdinalIgnoreCase) &&
            !String.Equals(operation, "Verify", StringComparison.OrdinalIgnoreCase) &&
            !String.Equals(operation, "Unregister", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Operation must be Register, Verify, or Unregister.");
        if (!String.Equals(operation, "Unregister", StringComparison.OrdinalIgnoreCase) && !File.Exists(dll))
            throw new FileNotFoundException("2022 DLL not found", dll);
    }

    private static void Register(Settings settings, string dll, string log)
    {
        string name = AssemblyName.GetAssemblyName(dll).FullName;
        if (!String.Equals(new AssemblyName(name).Name, ModuleName, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("DLL has the wrong assembly identity.");

        string previous = ReadString(settings, ModulePath + ".AssemblyPath");
        WriteLog(log, "BEFORE AssemblyPath=" + (previous ?? "(absent)"));
        if (previous != null &&
            previous.IndexOf("Oumijia.EplAddin.EDZ.dll", StringComparison.OrdinalIgnoreCase) < 0)
            throw new InvalidOperationException("Existing module is not an Oumijia DLL; refusing to replace it.");

        foreach (string field in ModuleFields) Delete(settings, ModulePath + "." + field);
        for (int i = 0; i < StaleActionNames.Length; ++i)
            DeleteAction(settings, StaleActionNames[i], StaleActionOrdinals[i]);
        for (int i = 0; i < ActionNames.Length; ++i) DeleteAction(settings, ActionNames[i], 21 + i);

        SetString(settings, ModulePath + ".AssemblyName", name);
        SetString(settings, ModulePath + ".AssemblyPath", dll);
        SetString(settings, ModulePath + ".TypeName", ModuleType);
        SetBool(settings, ModulePath + ".LoadOnStart", true);

        for (int i = 0; i < ActionNames.Length; ++i)
            WriteAction(settings, ActionNames[i], 21 + i, ActionTypes[i], dll, name);

        Verify(settings, dll, name);
        WriteLog(log, "OK: registered " + dll);
    }

    private static void Verify(Settings settings, string dll, string name)
    {
        AssertString(settings, ModulePath + ".AssemblyName", name);
        AssertString(settings, ModulePath + ".AssemblyPath", dll);
        AssertString(settings, ModulePath + ".TypeName", ModuleType);
        if (!settings.GetBoolSetting(ModulePath + ".LoadOnStart", 0))
            throw new InvalidOperationException("LoadOnStart read-back is false.");
        for (int i = 0; i < ActionNames.Length; ++i)
        {
            string baseKey = ActionsPath + ActionNames[i] + "." + (21 + i);
            AssertString(settings, baseKey + ".AssemblyPath", dll);
            AssertString(settings, baseKey + ".TypeName", ActionTypes[i]);
            AssertString(settings, baseKey + ".ModuleName", ModuleName);
        }
    }

    private static void Unregister(Settings settings, string dll, string log)
    {
        string registered = ReadString(settings, ModulePath + ".AssemblyPath");
        if (!String.Equals(registered, dll, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Refusing to unregister a different or missing AssemblyPath: " +
                (registered ?? "(absent)"));
        foreach (string field in ModuleFields) Delete(settings, ModulePath + "." + field);
        for (int i = 0; i < ActionNames.Length; ++i) DeleteAction(settings, ActionNames[i], 21 + i);
        if (settings.ExistSetting(ModulePath + ".AssemblyPath"))
            throw new InvalidOperationException("AssemblyPath still exists after unregister.");
        WriteLog(log, "OK: unregistered " + dll);
    }

    private static void WriteAction(Settings settings, string action, int ordinal, string type, string dll, string name)
    {
        string key = ActionsPath + action + "." + ordinal;
        SetString(settings, key + ".AssemblyName", name);
        SetString(settings, key + ".AssemblyPath", dll);
        SetString(settings, key + ".TypeName", type);
        SetString(settings, key + ".ModuleName", ModuleName);
        SetBool(settings, key + ".EnableInterface", false);
        SetBool(settings, key + ".CheckInterface", false);
        SetString(settings, key + ".EnableCondition", "");
        SetBool(settings, key + ".ActionLicense", false);
    }

    private static void DeleteAction(Settings settings, string action, int ordinal)
    {
        string key = ActionsPath + action + "." + ordinal;
        foreach (string field in ActionFields) Delete(settings, key + "." + field);
    }

    private static void Delete(Settings settings, string key)
    {
        if (settings.ExistSetting(key)) settings.DeleteSetting(key);
    }

    private static string ReadString(Settings settings, string key)
    {
        return settings.ExistSetting(key) ? settings.GetStringSetting(key, 0) : null;
    }

    private static void SetString(Settings settings, string key, string value)
    {
        if (!settings.ExistSetting(key))
            settings.AddStringSetting(key, new string[0], new string[0], ISettings.CreationFlag.Insert);
        settings.SetStringSetting(key, value);
    }

    private static void SetBool(Settings settings, string key, bool value)
    {
        if (!settings.ExistSetting(key))
            settings.AddBoolSetting(key, new bool[0], ISettings.CreationFlag.Insert);
        settings.SetBoolSetting(key, value);
    }

    private static void AssertString(Settings settings, string key, string expected)
    {
        string actual = ReadString(settings, key);
        if (!String.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Read-back mismatch: " + key + " => " + (actual ?? "(absent)"));
    }

    private static void WriteLog(string path, string message)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.AppendAllText(path, DateTime.Now.ToString("o") + " " + message + Environment.NewLine);
    }
}
