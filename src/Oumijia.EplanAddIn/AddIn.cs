using System;
using System.Collections.Generic;
using Eplan.EplApi.ApplicationFramework;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Eplan.EplApi.Gui;
using Oumijia.EplanUi;

namespace Oumijia.EplanAddIn
{
    /// <summary>
    /// EPLAN add-in entry point ("Tool Oumijia — EDZ assistant").
    ///
    /// Registration (see Oumijia.Register tool / docs/BUILD.md):
    ///   STATION.EplanEplApiModule.Module.Oumijia@EplAddin@EDZ.AssemblyPath  = path to this dll
    ///   ...AssemblyName / ...TypeName / ...LoadOnStart
    /// </summary>
    public class AddIn : IEplAddIn
    {
        private const string RibbonTabName = "Tool Oumijia";
        private const string CommandGroupName = "Quản lý EDZ";
        private const string OldCommandGroupName = "Nhập EDZ";
        private const string NativeCloudTabName = "Eplan Cloud";
        private const string CloudGroupName = "Eplan Cloud";
        private static readonly string[] NativeCloudCommands = {
            "Eplan Cloud dashboard", "Data Portal", "Project generation (Free)",
            "User management", "Parts management (deprecated)",
            "Eplan eLearning", "Guided Installation"
        };
        // Chỉ dùng để dọn command cũ trên ribbon; đây KHÔNG phải action đã đăng ký.
        private const string LegacyPortalCommandLine = "XDataPortalNavigatorToggle";
        private const string LegacyMenuActionName = "MfToggleMainMenuAction";
        private const string ToggleMenuActionName = "OumijiaToggleMainMenu";
        private static Timer _cloudRibbonTimer;
        private static int _cloudRibbonAttempts;

        static AddIn()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveBundledAssembly;
        }

        public bool OnRegister(ref bool bLoadOnStart)
        {
            bLoadOnStart = true;
            AddinLog.Write("OnRegister");
            return true;
        }

        public bool OnUnregister()
        {
            AddinLog.Write("OnUnregister");
            return true;
        }

        public bool OnInit()
        {
            var version = typeof(AddIn).Assembly.GetName().Version;
            AddinLog.Write("OnInit (version " + (version != null ? version.ToString() : "?") + ")");
            return true;
        }

        public bool OnInitGui()
        {
            try
            {
                var ribbon = new RibbonBar();

                var tab = ribbon.GetTab(RibbonTabName);
                if (tab == null) tab = ribbon.AddTab(RibbonTabName);

                bool hasLegacyMenu = RibbonHasAction(ribbon, LegacyMenuActionName);
                AddinLog.Write("OnInitGui: ribbon probe: " + LegacyMenuActionName +
                    "=" + (hasLegacyMenu ? "found" : "notfound"));
                EnsureEdzGroup(tab, hasLegacyMenu);
                if (!EnsureNativeCloudRibbon(ribbon, tab))
                    StartCloudRibbonTimer();

                AddinLog.Write("OnInitGui: ribbon ready");
                return true;
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("OnInitGui", ex);
                return false;
            }
        }

        public bool OnExit()
        {
            StopCloudRibbonTimer();
            AddinLog.Write("OnExit");
            return true;
        }

        private static Assembly ResolveBundledAssembly(object sender, ResolveEventArgs args)
        {
            try
            {
                string simpleName = new AssemblyName(args.Name).Name;
                if (String.IsNullOrEmpty(simpleName)) return null;

                bool isSharedAssembly =
                    simpleName.Equals("Oumijia.EplanUi", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("Oumijia.EdzReader", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("SharpCompress", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("System.Buffers", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("System.Memory", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("System.Numerics.Vectors", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("System.Runtime.CompilerServices.Unsafe", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("System.Text.Encoding.CodePages", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("System.Threading.Tasks.Extensions", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("Microsoft.Bcl.AsyncInterfaces", StringComparison.OrdinalIgnoreCase);
                if (!isSharedAssembly) return null;

                string directory = Path.GetDirectoryName(typeof(AddIn).Assembly.Location);
                if (String.IsNullOrEmpty(directory)) return null;
                string candidate = Path.Combine(directory, simpleName + ".dll");
                return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
            }
            catch { return null; }
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
            return String.Equals(action, "OumijiaImportEdz", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(action, "OumijiaExportEdz", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(action, "OumijiaDataPortal", StringComparison.OrdinalIgnoreCase) ||
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
                String.Equals(commands[0].ActionCommandLine, "OumijiaImportEdz", StringComparison.OrdinalIgnoreCase) &&
                String.Equals(commands[1].ActionCommandLine, "OumijiaExportEdz", StringComparison.OrdinalIgnoreCase) &&
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
                EnsureCommand(group, "Nhập EDZ", "OumijiaImportEdz");
                EnsureCommand(group, "Xuất EDZ", "OumijiaExportEdz");
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
                _cloudRibbonTimer = new Timer { Interval = 1000 };
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
    }
}
