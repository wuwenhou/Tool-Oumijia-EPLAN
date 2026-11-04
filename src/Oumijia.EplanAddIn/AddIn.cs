using System;
using Eplan.EplApi.ApplicationFramework;
using Eplan.EplApi.Gui;

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

                var group = tab.GetCommandGroup(CommandGroupName);
                if (group == null) group = tab.AddCommandGroup(CommandGroupName);

                EnsureCommand(group, "Nhập EDZ…", "OumijiaImportEdz");
                EnsureCommand(group, "Xuất EDZ…", "OumijiaExportEdz");
                EnsureCommand(group, "Cửa sổ Tool Oumijia", "OumijiaOpenTool");

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
            AddinLog.Write("OnExit");
            return true;
        }

        private static void EnsureCommand(RibbonCommandGroup group, string buttonText, string actionName)
        {
            try
            {
                if (group.GetCommand(actionName) == null)
                {
                    group.AddCommand(buttonText, actionName);
                }
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("EnsureCommand(" + actionName + ")", ex);
            }
        }
    }
}
