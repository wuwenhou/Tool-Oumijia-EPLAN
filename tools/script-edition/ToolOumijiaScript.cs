//#################################################################################################################################################
// Tool Oumijia — EPLAN Script Edition (v0.6.0)
//#################################################################################################################################################
// Nhập / Xuất EDZ dùng Oumijia.EplanUi.dll đã biên dịch; Data Portal và nhóm
// Cloud sao chép command native từ tab Eplan Cloud — không còn WebView2.
// Script này không cần EPLAN API license. Người dùng tự đăng nhập vào EPLAN
// và tuân theo quyền, điều khoản, giấy phép của EPLAN.
//
// CÀI ĐẶT (một lần):
//   Chạy Install-Script.bat hoặc Install-Tool-Oumijia.bat.
//
// SỬ DỤNG:
//   Ribbon "Tool Oumijia" > nhóm "Quản lý EDZ":
//   Nhập EDZ | Xuất EDZ | Menu bar
//   Nhóm "Eplan Cloud" có một nút Data Portal native.
//
// GHI CHÚ (đã kiểm chứng trên EPLAN Education 2026.0.3):
//   - Nút "Data Portal" dùng chính command ID của EPLAN; không gọi
//     DataPortalTabView (chỉ là phần nội dung, không phải cửa sổ dockable).
//     EPLAN tự quản lý phiên đăng nhập; không có trình duyệt riêng của Tool.
//   - Không còn WebView2/PortalHost, không còn profile WebView2 riêng.
//   - Nếu EPLAN không cung cấp action native, Tool báo rõ và hướng dẫn mở
//     Eplan Cloud > Data Portal thay vì chết lặng.
//   - EPLAN 2026 không còn MfToggleMainMenuAction; nút "Menu bar" báo rõ.
//
// LOG:
//   %APPDATA%\Tool Oumijia\logs\addin.log
//
// GHI CHÚ:
//   - Mọi xử lý UI chạy qua assemblies net48 trong %APPDATA%\Tool Oumijia\Portal.
//   - Script dùng Reflection để nạp chúng, nên EPLAN compiler không cần WebView2/SharpCompress.
//#################################################################################################################################################

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Eplan.EplApi.ApplicationFramework;
using Eplan.EplApi.Gui;
using Eplan.EplApi.Scripting;

namespace Oumijia.EdzScript
{
    /// <summary>
    /// Điểm vào Script Edition: nhóm EDZ ba lệnh và nhóm Eplan Cloud native.
    /// </summary>
    public class ToolOumijiaScript
    {
        private const string ScriptVersion = "0.6.0-rc.1-script.1";
        private const string RibbonTabName = "Tool Oumijia";
        private const string CommandGroupName = "Quản lý EDZ";
        private const string OldCommandGroupName = "Nhập EDZ";
        private const string ImportActionName = "OumijiaScriptImportEdz";
        private const string ExportActionName = "OumijiaScriptExportEdz";
        private const string DataPortalActionName = "OumijiaScriptDataPortal";
        private const string NativeCloudTabName = "Eplan Cloud";
        private const string CloudGroupName = "Eplan Cloud";
        private static readonly string[] NativeCloudCommands = {
            "Eplan Cloud dashboard", "Data Portal", "Project generation (Free)",
            "User management", "Parts management (deprecated)",
            "Eplan eLearning", "Guided Installation"
        };
        // Chỉ dùng để dọn nút cũ trên ribbon, không tìm hoặc thực thi action này.
        private const string LegacyPortalCommandLine = "XDataPortalNavigatorToggle";
        private const string LegacyMenuActionName = "MfToggleMainMenuAction";
        private const string ToggleMenuActionName = "OumijiaScriptToggleMainMenu";

        private static bool _sharedResolverAttached;
        private static Timer _cloudRibbonTimer;
        private static int _cloudRibbonAttempts;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        // ------------------------------------------------------------------ vòng đời script

        [DeclareRegister]
        public void Register()
        {
            AddinLog.Write("Script Register (version " + ScriptVersion + ")");
            EnsureRibbon("Register");
        }

        [DeclareUnregister]
        public void Unregister()
        {
            AddinLog.Write("Script Unregister");
        }

        [DeclareEventHandler("Eplan.EplApi.OnMainStart")]
        public void OnMainStart()
        {
            AddinLog.Write("Event OnMainStart");
            EnsureRibbon("OnMainStart");
        }

        [DeclareEventHandler("Eplan.EplApi.OnLoadWorkspace")]
        public void OnLoadWorkspace()
        {
            AddinLog.Write("Event OnLoadWorkspace");
            EnsureRibbon("OnLoadWorkspace");
        }

        [DeclareEventHandler("Eplan.EplApi.OnResetRibbon")]
        public void OnResetRibbon()
        {
            AddinLog.Write("Event OnResetRibbon");
            EnsureRibbon("OnResetRibbon");
        }

        [DeclareEventHandler("Eplan.EplApi.OnMainEnd")]
        public void OnMainEnd()
        {
            StopCloudRibbonTimer();
            AddinLog.Write("Event OnMainEnd");
        }

        // ------------------------------------------------------------------ action

        [DeclareAction("OumijiaScriptImportEdz")]
        public void ImportEdz()
        {
            AddinLog.Write("Action OumijiaScriptImportEdz");
            OpenSharedForm("Oumijia.EplanUi.ImportEdzForm", null, "Không mở được hộp thoại Nhập EDZ");
        }

        [DeclareAction("OumijiaScriptExportEdz")]
        public void ExportEdz()
        {
            AddinLog.Write("Action OumijiaScriptExportEdz");
            OpenSharedForm("Oumijia.EplanUi.ExportEdzForm", null, "Không mở được hộp thoại Xuất EDZ");
        }

        /// <summary>Lệnh tương thích cũ: không mở riêng DataPortalTabView vì nó
        /// không có cửa sổ native. Nút ribbon mới sao chép command ID của EPLAN.</summary>
        [DeclareAction("OumijiaScriptDataPortal")]
        public void DataPortal()
        {
            AddinLog.Write("Action OumijiaScriptDataPortal: native ribbon command unavailable; no content-only dialog opened");
            ShowNativePortalUnavailable("Lệnh native của EPLAN chưa sẵn sàng trên ribbon.");
        }

        private static void ShowNativePortalUnavailable(string detail)
        {
            MessageBox.Show(
                "Không mở được Data Portal native của EPLAN.\n\n" + detail +
                "\n\nHãy mở trực tiếp: tab Eplan Cloud → Data Portal.\n\n" +
                "Sau khi tải .edz bằng EPLAN, dùng nút Nhập EDZ của Tool Oumijia để nhập.\n\n" +
                "Chi tiết đã ghi vào addin.log.",
                "Tool Oumijia — Data Portal", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>Menu bar khi EPLAN không còn MfToggleMainMenuAction (2026):
        /// báo rõ thay vì chết lặng. Không giả lập menu của EPLAN.</summary>
        [DeclareAction("OumijiaScriptToggleMainMenu")]
        public void ToggleMainMenu()
        {
            AddinLog.Write("Action OumijiaScriptToggleMainMenu - EPLAN khong con MfToggleMainMenuAction (da probe khi tao ribbon), khong the hien classic menu qua API");
            MessageBox.Show(
                "EPLAN bản này không còn action MfToggleMainMenuAction nên không bật được classic menu qua API.\n\n" +
                "Hãy dùng ribbon của EPLAN (File, Home, Insert, Edit, View, Devices, Connections, Tools, Pre-planning, Master data, Eplan Cloud) thay thế.\n\n" +
                "Chi tiết đã ghi vào log addin.log (dòng Ribbon probe).",
                "Tool Oumijia — Menu bar", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>Kiểm tra script đã nạp và shared runtime có sẵn.</summary>
        [DeclareAction("OumijiaScriptSelfTest")]
        public void SelfTest()
        {
            if (!File.Exists(Path.Combine(SharedDirectory(), "Oumijia.EplanUi.dll")) ||
                !File.Exists(Path.Combine(SharedDirectory(), "Oumijia.EdzReader.dll")))
            {
                AddinLog.Write("SELFTEST ERROR - shared runtime is missing (EplanUi/EdzReader)");
                return;
            }
            AddinLog.Write("SELFTEST OK - script loaded, shared UI loaded (version " + ScriptVersion + ")");
        }

        /// <summary>Tạo lại ribbon nếu tab bị mất (có thể gọi từ command line).</summary>
        [DeclareAction("OumijiaScriptEnsureRibbon")]
        public void EnsureRibbonAction()
        {
            EnsureRibbon("Action");
        }

        // ------------------------------------------------------------------ ribbon

        private static void EnsureRibbon(string source)
        {
            try
            {
                RibbonBar ribbon = new RibbonBar();
                RibbonTab tab = ribbon.GetTab(RibbonTabName);
                if (tab == null) tab = ribbon.AddTab(RibbonTabName);

                bool hasLegacyMenu = RibbonHasAction(ribbon, LegacyMenuActionName);
                AddinLog.Write("Ribbon probe (" + source + "): " + LegacyMenuActionName +
                    "=" + (hasLegacyMenu ? "found" : "notfound"));
                LogRibbonTabs(ribbon, source);
                EnsureEdzGroup(tab, hasLegacyMenu);
                if (!EnsureNativeCloudRibbon(ribbon, tab))
                    StartCloudRibbonTimer();

                AddinLog.Write("Ribbon ready (" + source + ")");
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("EnsureRibbon(" + source + ")", ex);
            }
        }

        private static void EnsureCommand(RibbonCommandGroup group, string buttonText, string actionName)
        {
            EnsureCommand(group, buttonText, actionName, null, null);
        }

        private static void EnsureCommand(RibbonCommandGroup group, string buttonText, string actionName, string tooltip, string description)
        {
            try
            {
                if (group.GetCommand(actionName) != null) return;
                if (String.IsNullOrEmpty(tooltip) && String.IsNullOrEmpty(description))
                {
                    group.AddCommand(buttonText, actionName);
                }
                else
                {
                    group.AddCommand(buttonText, actionName, tooltip ?? String.Empty, description ?? String.Empty);
                }
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("EnsureCommand(" + actionName + ")", ex);
            }
        }

        private static void EnsureMenuCommand(RibbonCommandGroup group, bool hasLegacyMenu)
        {
            if (hasLegacyMenu)
                EnsureCommand(group, "Menu bar", LegacyMenuActionName,
                    "Bat/tat classic menu cua EPLAN", "Goi action MfToggleMainMenuAction cua EPLAN");
            else
                EnsureCommand(group, "Menu bar", ToggleMenuActionName,
                    "EPLAN ban nay khong con classic menu - bam de xem huong dan",
                    "Bao ro thay vi chet lang khi action MfToggleMainMenuAction khong con");
        }

        private static bool IsToolEdzCommand(RibbonCommand command)
        {
            if (command == null) return false;
            string action = command.ActionCommandLine;
            return String.Equals(action, ImportActionName, StringComparison.OrdinalIgnoreCase) ||
                String.Equals(action, ExportActionName, StringComparison.OrdinalIgnoreCase) ||
                String.Equals(action, DataPortalActionName, StringComparison.OrdinalIgnoreCase) ||
                String.Equals(action, ToggleMenuActionName, StringComparison.OrdinalIgnoreCase) ||
                String.Equals(action, LegacyMenuActionName, StringComparison.OrdinalIgnoreCase) ||
                String.Equals(action, LegacyPortalCommandLine, StringComparison.OrdinalIgnoreCase) ||
                String.Equals(command.Text, "Data Portal", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(command.Text, "Data Portal Web", StringComparison.OrdinalIgnoreCase);
        }

        private static List<RibbonCommand> GroupCommands(RibbonCommandGroup group)
        {
            Dictionary<uint, RibbonCommand> commands = group.GetCommands(false);
            return commands == null ? new List<RibbonCommand>() : new List<RibbonCommand>(commands.Values);
        }

        private static bool IsExpectedEdzGroup(RibbonCommandGroup group, bool hasLegacyMenu)
        {
            List<RibbonCommand> commands = GroupCommands(group);
            if (commands.Count != 3) return false;
            commands.Sort(delegate(RibbonCommand a, RibbonCommand b) {
                return a.IndexInCommandGroup.CompareTo(b.IndexInCommandGroup);
            });
            return String.Equals(commands[0].Text, "Nhập EDZ", StringComparison.OrdinalIgnoreCase) &&
                String.Equals(commands[1].Text, "Xuất EDZ", StringComparison.OrdinalIgnoreCase) &&
                String.Equals(commands[2].Text, "Menu bar", StringComparison.OrdinalIgnoreCase) &&
                String.Equals(commands[0].ActionCommandLine, ImportActionName, StringComparison.OrdinalIgnoreCase) &&
                String.Equals(commands[1].ActionCommandLine, ExportActionName, StringComparison.OrdinalIgnoreCase) &&
                String.Equals(commands[2].ActionCommandLine, hasLegacyMenu ? LegacyMenuActionName : ToggleMenuActionName,
                    StringComparison.OrdinalIgnoreCase);
        }

        private static void EnsureEdzGroup(RibbonTab tab, bool hasLegacyMenu)
        {
            try
            {
                RibbonCommandGroup old = tab.GetCommandGroup(OldCommandGroupName);
                if (old != null) CleanObsoleteEdzGroup(old);

                RibbonCommandGroup group = tab.GetCommandGroup(CommandGroupName);
                bool isFirst = tab.CommandGroups != null && tab.CommandGroups.Length > 0 &&
                    String.Equals(tab.CommandGroups[0].Name, CommandGroupName, StringComparison.OrdinalIgnoreCase);
                if (group != null && (!IsExpectedEdzGroup(group, hasLegacyMenu) || !isFirst))
                {
                    List<RibbonCommand> commands = GroupCommands(group);
                    bool onlyOurs = true;
                    foreach (RibbonCommand command in commands)
                        if (!IsToolEdzCommand(command)) onlyOurs = false;
                    if (onlyOurs)
                    {
                        group.Remove();
                        group = null;
                    }
                    else
                    {
                        foreach (RibbonCommand command in commands)
                            if (IsToolEdzCommand(command)) command.Remove();
                        AddinLog.Write("Ribbon migration: preserved other commands in " + CommandGroupName);
                    }
                }
                if (group == null) group = tab.AddCommandGroup(CommandGroupName, 0);
                EnsureCommand(group, "Nhập EDZ", ImportActionName);
                EnsureCommand(group, "Xuất EDZ", ExportActionName);
                EnsureMenuCommand(group, hasLegacyMenu);
            }
            catch (Exception ex) { AddinLog.WriteError("EnsureEdzGroup", ex); }
        }

        private static void CleanObsoleteEdzGroup(RibbonCommandGroup group)
        {
            List<RibbonCommand> commands = GroupCommands(group);
            bool onlyOurs = true;
            foreach (RibbonCommand command in commands)
                if (!IsToolEdzCommand(command)) onlyOurs = false;
            if (onlyOurs)
            {
                group.Remove();
                AddinLog.Write("Ribbon migration: removed " + OldCommandGroupName);
            }
            else
            {
                foreach (RibbonCommand command in commands)
                    if (IsToolEdzCommand(command)) command.Remove();
                AddinLog.Write("Ribbon migration: kept other commands in " + OldCommandGroupName);
            }
        }

        private static bool EnsureNativeCloudRibbon(RibbonBar ribbon, RibbonTab tab)
        {
            try
            {
                RibbonTab source = ribbon.GetTab(NativeCloudTabName);
                if (source == null) return false;
                Dictionary<uint, string> commands = source.GetCommands(false);
                if (commands == null || commands.Count == 0) return false;

                RibbonCommandGroup cloud = tab.GetCommandGroup(CloudGroupName);
                if (cloud == null) cloud = tab.AddCommandGroup(CloudGroupName);
                bool complete = true;
                foreach (string name in NativeCloudCommands)
                {
                    uint id = 0;
                    foreach (KeyValuePair<uint, string> pair in commands)
                        if (String.Equals(pair.Value, name, StringComparison.OrdinalIgnoreCase)) id = pair.Key;
                    if (id == 0)
                    {
                        AddinLog.Write("Native Cloud command unavailable: " + name);
                        complete = false;
                        continue;
                    }
                    if (cloud.GetCommandWithId(id) == null && cloud.AddCommandWithId(id) == null)
                    {
                        AddinLog.Write("Native Cloud clone failed: " + name);
                        complete = false;
                    }
                }
                return complete;
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("EnsureNativeCloudRibbon", ex);
                return false;
            }
        }

        private static void StartCloudRibbonTimer()
        {
            if (_cloudRibbonTimer == null)
            {
                _cloudRibbonTimer = new Timer();
                _cloudRibbonTimer.Interval = 1000;
                _cloudRibbonTimer.Tick += delegate {
                    if (++_cloudRibbonAttempts > 90)
                    {
                        AddinLog.Write("Native Cloud ribbon unavailable after 90 retries");
                        StopCloudRibbonTimer();
                        return;
                    }
                    try
                    {
                        RibbonBar ribbon = new RibbonBar();
                        RibbonTab tab = ribbon.GetTab(RibbonTabName);
                        if (tab != null && EnsureNativeCloudRibbon(ribbon, tab))
                        {
                            AddinLog.Write("Native Cloud ribbon ready after retries=" + _cloudRibbonAttempts);
                            StopCloudRibbonTimer();
                        }
                    }
                    catch (Exception ex) { AddinLog.Write("Native Cloud retry failed: " + ex.GetType().FullName); }
                };
            }
            if (!_cloudRibbonTimer.Enabled)
            {
                _cloudRibbonAttempts = 0;
                _cloudRibbonTimer.Start();
            }
        }

        private static void StopCloudRibbonTimer()
        {
            if (_cloudRibbonTimer == null) return;
            _cloudRibbonTimer.Stop();
            _cloudRibbonTimer.Dispose();
            _cloudRibbonTimer = null;
        }

        private static bool RibbonHasAction(RibbonBar ribbon, string actionName)
        {
            try
            {
                RibbonTab[] tabs = ribbon.Tabs;
                if (tabs == null) return false;
                foreach (RibbonTab tab in tabs)
                {
                    if (tab == null) continue;
                    try
                    {
                        Dictionary<uint, string> tabCommands = tab.GetCommands(false);
                        if (tabCommands != null)
                        {
                            foreach (string value in tabCommands.Values)
                            {
                                if (String.Equals(value, actionName, StringComparison.OrdinalIgnoreCase)) return true;
                            }
                        }
                    }
                    catch { }
                    try
                    {
                        RibbonCommandGroup[] groups = tab.CommandGroups;
                        if (groups != null)
                        {
                            foreach (RibbonCommandGroup group in groups)
                            {
                                if (group == null) continue;
                                try
                                {
                                    Dictionary<uint, RibbonCommand> groupCommands = group.GetCommands(false);
                                    if (groupCommands != null)
                                    {
                                        foreach (RibbonCommand command in groupCommands.Values)
                                        {
                                            if (command != null && String.Equals(command.ActionCommandLine, actionName, StringComparison.OrdinalIgnoreCase)) return true;
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return false;
        }

        private static void LogRibbonTabs(RibbonBar ribbon, string source)
        {
            try
            {
                RibbonTab[] tabs = ribbon.Tabs;
                if (tabs == null) return;
                foreach (RibbonTab tab in tabs)
                {
                    if (tab == null) continue;
                    try
                    {
                        int count = 0;
                        try
                        {
                            Dictionary<uint, string> cmds = tab.GetCommands(false);
                            if (cmds != null) count = cmds.Count;
                        }
                        catch { }
                        AddinLog.Write("Ribbon tab (" + source + "): " + tab.Name + " commands=" + count);
                    }
                    catch { }
                }
            }
            catch { }
        }

        // ------------------------------------------------------------------ shared runtime

        private static string DataRoot()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tool Oumijia");
        }

        private static string SharedDirectory()
        {
            return Path.Combine(DataRoot(), "Portal");
        }

        private static void EnsureSharedResolver()
        {
            if (_sharedResolverAttached) return;
            try { AppDomain.CurrentDomain.AssemblyResolve += OnSharedAssemblyResolve; } catch { }
            try { SetDllDirectory(SharedDirectory()); } catch { }
            _sharedResolverAttached = true;
        }

        private static Assembly OnSharedAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                AssemblyName assemblyName = new AssemblyName(args.Name);
                string simpleName = assemblyName == null ? null : assemblyName.Name;
                if (String.IsNullOrEmpty(simpleName)) return null;

                bool isOurs =
                    simpleName.StartsWith("Oumijia.", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("SharpCompress", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("System.Buffers", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("System.Memory", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("System.Numerics.Vectors", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("System.Runtime.CompilerServices.Unsafe", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("System.Text.Encoding.CodePages", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("System.Threading.Tasks.Extensions", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("Microsoft.Bcl.AsyncInterfaces", StringComparison.OrdinalIgnoreCase);
                if (!isOurs) return null;

                string candidate = Path.Combine(SharedDirectory(), simpleName + ".dll");
                return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
            }
            catch { return null; }
        }

        private static void OpenSharedForm(string typeName, string[] downloadedFiles, string failureTitle)
        {
            try
            {
                EnsureSharedResolver();
                string assemblyPath = Path.Combine(SharedDirectory(), "Oumijia.EplanUi.dll");
                if (!File.Exists(assemblyPath))
                {
                    ShowMissingShared(assemblyPath);
                    return;
                }
                Assembly assembly = Assembly.LoadFrom(assemblyPath);
                Type formType = assembly.GetType(typeName, true);
                object form;
                if (downloadedFiles == null)
                {
                    form = Activator.CreateInstance(formType);
                }
                else
                {
                    form = Activator.CreateInstance(formType, new object[] { downloadedFiles });
                }
                MethodInfo showDialog = formType.GetMethod("ShowDialog", Type.EmptyTypes);
                showDialog.Invoke(form, null);
                IDisposable disposable = form as IDisposable;
                if (disposable != null) disposable.Dispose();
            }
            catch (Exception ex)
            {
                AddinLog.WriteError(typeName, ex);
                MessageBox.Show(
                    "Không mở được màn hình Tool Oumijia.\n\n" + ex.Message,
                    "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void ShowMissingShared(string requiredPath)
        {
            MessageBox.Show(
                "Thiếu thành phần chạy của Tool Oumijia:\n" + requiredPath + "\n\n" +
                "Hãy chạy lại Install-Tool-Oumijia.bat để cài đầy đủ.",
                "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    //=================================================================================================================================================
    // Logging
    //=================================================================================================================================================

    internal static class AddinLog
    {
        private static readonly object Gate = new object();

        public static string LogDirectory
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tool Oumijia", "logs"); }
        }

        public static string FilePath
        {
            get { return Path.Combine(LogDirectory, "addin.log"); }
        }

        public static void Write(string message)
        {
            lock (Gate)
            {
                try
                {
                    Directory.CreateDirectory(LogDirectory);
                    File.AppendAllText(FilePath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + message + Environment.NewLine, new UTF8Encoding(false));
                }
                catch { }
            }
        }

        public static void WriteError(string context, Exception exception)
        {
            Write(context + " ERROR: " + exception);
        }
    }
}
