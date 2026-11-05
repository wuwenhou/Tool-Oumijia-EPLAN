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
        private const string ClassicCommandGroupName = "EPLAN Classic";
        private const string VeeCommandGroupName = "VEE";
        private const string GridCommandGroupName = "GIRD";

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

                var group = GetOrAddCommandGroup(tab, CommandGroupName);
                EnsureCommand(group, "Nhập EDZ…", "OumijiaImportEdz");
                EnsureCommand(group, "Xuất EDZ…", "OumijiaExportEdz");

                var classicGroup = GetOrAddCommandGroup(tab, ClassicCommandGroupName);
                EnsureCommand(classicGroup, "EPLAN Classic…", "OumijiaEplanClassic");

                var veeGroup = GetOrAddCommandGroup(tab, VeeCommandGroupName);
                EnsureCommand(veeGroup, "VEE Export connections…", "OumijiaVeeExportConnections");
                EnsureCommand(veeGroup, "VEE Table Maker…", "OumijiaVeeTableMaker");

                var gridGroup = GetOrAddCommandGroup(tab, GridCommandGroupName);
                EnsureCommand(gridGroup, "Menu bar", "OumijiaToggleMenuBar");
                EnsureCommand(gridGroup, "Grid snap", "OumijiaToggleGridSnap");
                EnsureCommand(gridGroup, "Object snap", "OumijiaToggleObjectSnap");
                EnsureCommand(gridGroup, "Grid view", "OumijiaToggleGridVisibility");
                EnsureCommand(gridGroup, "Design mode", "OumijiaToggleCursorDesign");
                EnsureCommand(gridGroup, "Align on grid", "OumijiaArrangeOnGrid");

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

        private static RibbonCommandGroup GetOrAddCommandGroup(RibbonTab tab, string name)
        {
            var group = tab.GetCommandGroup(name);
            return group ?? tab.AddCommandGroup(name);
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
