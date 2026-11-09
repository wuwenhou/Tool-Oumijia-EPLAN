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
#if !EPLAN2022
        private const string NativeCloudTabName = "Eplan Cloud";
        private const string CloudGroupName = "Eplan Cloud";
        private static readonly string[] NativeCloudCommands = {
            "Eplan Cloud dashboard", "Data Portal", "Project generation (Free)",
            "User management", "Parts management (deprecated)",
            "Eplan eLearning", "Guided Installation"
        };
#else
        private const string CloudGroupName2022 = "Eplan Cloud";
        private const string DashboardCommandLine = "XMVCCloudSolutionAction /Url:https://apps.eplan.com";
        private const string DataPortalCommandLine = "XMVCCloudSolutionAction /Url:https://dataportal.eplan.com";
#endif
        // Chỉ dùng để dọn command cũ trên ribbon; đây KHÔNG phải action đã đăng ký.
        private const string LegacyPortalCommandLine = "XDataPortalNavigatorToggle";
        private const string LegacyMenuActionName = "MfToggleMainMenuAction";
        private const string ToggleMenuActionName = "OumijiaToggleMainMenu";
#if !EPLAN2022
        private static Timer _cloudRibbonTimer;
        private static int _cloudRibbonAttempts;
#else
        private static Timer _ribbon2022Timer;
#endif

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
#if EPLAN2022
                // In 2022 the visible ribbon is reconstructed after OnInitGui. Do not
                // inspect/migrate RibbonCommand wrappers during that reconstruction:
                // stale native wrappers can throw AccessViolationException.
                AddinLog.Write("OnInitGui: 2022 native menu=" + (HasNativeMenu2022() ? "found" : "notfound"));
                StartRibbonTimer2022();
                AddinLog.Write("OnInitGui: 2022 ribbon scheduled");
#else
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
#endif
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
#if !EPLAN2022
            StopCloudRibbonTimer();
#else
            StopRibbonTimer2022();
#endif
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
#if EPLAN2022
                // EPLAN 2022 has no AddCommand(text, action, tooltip, description) overload.
                // The three-argument overload takes an icon, not text, so add with two
                // arguments and set the optional texts on the returned command.
                RibbonCommand command = group.AddCommand(buttonText, actionName);
                if (command != null)
                {
                    if (!String.IsNullOrEmpty(tooltip)) command.TooltipText = tooltip;
                    if (!String.IsNullOrEmpty(description)) command.Description = description;
                }
#else
                if (String.IsNullOrEmpty(tooltip) && String.IsNullOrEmpty(description))
                {
                    group.AddCommand(buttonText, actionName);
                }
                else
                {
                    group.AddCommand(buttonText, actionName, tooltip ?? String.Empty, description ?? String.Empty);
                }
#endif
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
            // EPLAN 2022's RibbonCommand has no IndexInCommandGroup property. The
            // dictionary keeps ribbon order, but validating the action set is enough
            // to detect a stale/partial group without depending on that order.
            bool hasImport = false;
            bool hasExport = false;
            bool hasMenu = false;
            string expectedMenuAction = hasLegacyMenu ? LegacyMenuActionName : ToggleMenuActionName;
            foreach (RibbonCommand command in commands)
            {
                string action = command.ActionCommandLine;
                if (String.Equals(action, "OumijiaImportEdz", StringComparison.OrdinalIgnoreCase)) hasImport = true;
                else if (String.Equals(action, "OumijiaExportEdz", StringComparison.OrdinalIgnoreCase)) hasExport = true;
                else if (String.Equals(action, expectedMenuAction, StringComparison.OrdinalIgnoreCase)) hasMenu = true;
            }
            return hasImport && hasExport && hasMenu;
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

#if EPLAN2022
        private static bool HasNativeMenu2022()
        {
            try { return new ActionManager().FindAction(LegacyMenuActionName, true) != null; }
            catch (Exception ex)
            {
                AddinLog.Write("Native Menu bar probe failed: " + ex.Message);
                return false;
            }
        }

        private static void StartRibbonTimer2022()
        {
            if (_ribbon2022Timer != null) return;
            _ribbon2022Timer = new Timer { Interval = 5000 };
            _ribbon2022Timer.Tick += delegate {
                StopRibbonTimer2022(); // Single attempt. Never churn live native command wrappers.
                try
                {
                    RibbonBar ribbon = new RibbonBar();
                    RibbonTab tab = ribbon.GetTab(RibbonTabName);
                    if (tab == null) tab = ribbon.AddTab(RibbonTabName);
                    if (tab == null) throw new InvalidOperationException("Cannot create Tool Oumijia tab.");

                    EnsureEdzGroup2022(tab);
                    EnsureCloudGroup2022(tab);
                    RibbonCommandGroup edz = tab.GetCommandGroup(CommandGroupName);
                    RibbonCommandGroup cloud = tab.GetCommandGroup(CloudGroupName2022);
                    AddinLog.Write("2022 ribbon one-shot ready; visible=" + tab.IsVisible +
                        "; edz=" + (edz == null ? -1 : GroupCommands(edz).Count) +
                        "; cloud=" + (cloud == null ? -1 : GroupCommands(cloud).Count));
                }
                catch (Exception ex)
                {
                    AddinLog.WriteError("2022 ribbon one-shot", ex);
                }
            };
            _ribbon2022Timer.Start();
        }

        private static void StopRibbonTimer2022()
        {
            if (_ribbon2022Timer == null) return;
            _ribbon2022Timer.Stop();
            _ribbon2022Timer.Dispose();
            _ribbon2022Timer = null;
        }

        private static readonly string[] StaleEdzActions2022 = {
            "OumijiaDataPortal", "XDataPortalNavigatorToggle", "OumijiaOpenTool",
            "OumijiaEnsureRibbon", "OumijiaEplanClassic", "OumijiaSelfTest",
            "OumijiaDiagParts", "OumijiaArrangeOnGrid", "OumijiaAutomationConnections",
            "OumijiaAutomationDevices", "OumijiaAutomationExport",
            "OumijiaAutomationPlc", "OumijiaCreateTable", "OumijiaExportConnections"
        };

        private static void EnsureEdzGroup2022(RibbonTab tab)
        {
            bool hasLegacyMenu = HasNativeMenu2022();

            RibbonCommandGroup old = tab.GetCommandGroup(OldCommandGroupName);
            if (old != null && AllCommandsAreOumijiaEdz(old))
            {
                old.Remove();
                AddinLog.Write("2022 ribbon: removed obsolete group " + OldCommandGroupName);
            }

            RibbonCommandGroup group = tab.GetCommandGroup(CommandGroupName);
            bool expected = false;
            if (group != null)
            {
                try { expected = IsExpectedEdzGroup(group, hasLegacyMenu); }
                catch (Exception ex) { AddinLog.WriteError("IsExpectedEdzGroup2022", ex); }
            }
            if (group != null && (!expected || !IsFirstGroup2022(tab)))
            {
                // This command group belongs to Tool Oumijia. Rebuild it as a whole:
                // an old Data Portal button can be a dead command whose action line
                // no longer resolves, so removing it by action name is unreliable.
                group.Remove();
                group = null;
                AddinLog.Write("2022 ribbon: rebuilt group " + CommandGroupName);
            }

            if (group == null) group = tab.AddCommandGroup(CommandGroupName, 0);
            RemoveStaleEdzCommands2022(group);
            EnsureCommand(group, "Nhập EDZ", "OumijiaImportEdz");
            EnsureCommand(group, "Xuất EDZ", "OumijiaExportEdz");
            EnsureMenuCommand(group, hasLegacyMenu);
            LogCommands2022("EDZ", group);
        }

        private static bool IsFirstGroup2022(RibbonTab tab)
        {
            RibbonCommandGroup[] groups = tab.CommandGroups;
            return groups != null && groups.Length > 0 && groups[0] != null &&
                String.Equals(groups[0].Name, CommandGroupName, StringComparison.OrdinalIgnoreCase);
        }

        private static bool AllCommandsAreOumijiaEdz(RibbonCommandGroup group)
        {
            List<RibbonCommand> commands = GroupCommands(group);
            if (commands.Count == 0) return false;
            foreach (RibbonCommand command in commands)
            {
                if (command == null) return false;
                string action;
                try { action = command.ActionCommandLine; }
                catch { return false; }
                if (!IsKnownEdzAction2022(action)) return false;
            }
            return true;
        }

        private static bool IsKnownEdzAction2022(string action)
        {
            if (String.Equals(action, "OumijiaImportEdz", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(action, "OumijiaExportEdz", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(action, LegacyMenuActionName, StringComparison.OrdinalIgnoreCase) ||
                String.Equals(action, ToggleMenuActionName, StringComparison.OrdinalIgnoreCase))
                return true;
            foreach (string stale in StaleEdzActions2022)
                if (String.Equals(action, stale, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static void RemoveStaleEdzCommands2022(RibbonCommandGroup group)
        {
            foreach (string action in StaleEdzActions2022)
            {
                try
                {
                    RibbonCommand stale = group.GetCommand(action);
                    if (stale != null)
                    {
                        stale.Remove();
                        AddinLog.Write("2022 ribbon: removed stale command " + action);
                    }
                }
                catch (Exception ex)
                {
                    AddinLog.WriteError("RemoveStaleEdzCommands2022(" + action + ")", ex);
                }
            }
        }

        // Leftover commands from the 2026-11-08 in-place browser experiment.
        // They are no longer registered actions, so the buttons are dead but keep
        // occupying the Eplan Cloud group; drop them on startup.
        private static readonly string[] StaleCloudActions2022 = {
            "OumijiaOpenDashboard", "OumijiaOpenDataPortal"
        };

        private static void RemoveStaleCloudCommands2022(RibbonCommandGroup group)
        {
            foreach (string action in StaleCloudActions2022)
            {
                try
                {
                    RibbonCommand stale = group.GetCommand(action);
                    if (stale != null)
                    {
                        stale.Remove();
                        AddinLog.Write("2022 ribbon: removed stale cloud command " + action);
                    }
                }
                catch (Exception ex)
                {
                    AddinLog.WriteError("RemoveStaleCloudCommands2022(" + action + ")", ex);
                }
            }
        }

        private static void EnsureCloudGroup2022(RibbonTab tab)
        {
            RibbonCommandGroup group = tab.GetCommandGroup(CloudGroupName2022);
            if (group == null) group = tab.AddCommandGroup(CloudGroupName2022, -1);
            else RemoveStaleCloudCommands2022(group);
            EnsureNativeCommand2022(group, "Eplan Cloud dashboard", DashboardCommandLine);
            EnsureNativeCommand2022(group, "Data Portal", DataPortalCommandLine);
            LogCommands2022("Cloud", group);
        }

        private static void LogCommands2022(string label, RibbonCommandGroup group)
        {
            try
            {
                List<RibbonCommand> commands = GroupCommands(group);
                AddinLog.Write("2022 " + label + " commands=" + commands.Count);
                foreach (RibbonCommand command in commands)
                {
                    try
                    {
                        AddinLog.Write("2022 " + label + " action=" +
                            (command == null ? "(null)" : command.ActionCommandLine));
                    }
                    catch (Exception ex)
                    {
                        AddinLog.Write("2022 " + label + " action read failed: " + ex.GetType().Name);
                    }
                }
            }
            catch (Exception ex) { AddinLog.WriteError("LogCommands2022(" + label + ")", ex); }
        }

        private static void EnsureNativeCommand2022(RibbonCommandGroup group, string text, string commandLine)
        {
            try
            {
                foreach (RibbonCommand command in GroupCommands(group))
                {
                    if (command != null &&
                        String.Equals(command.ActionCommandLine, commandLine, StringComparison.OrdinalIgnoreCase))
                        return;
                }
                group.AddCommand(text, commandLine);
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("EnsureNativeCommand2022(" + text + ")", ex);
            }
        }

#endif

#if !EPLAN2022
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
#endif

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
