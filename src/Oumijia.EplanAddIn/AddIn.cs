using System;
using Eplan.EplApi.ApplicationFramework;
using System.IO;
using System.Reflection;
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

                var group = tab.GetCommandGroup(CommandGroupName);
                if (group == null) group = tab.AddCommandGroup(CommandGroupName);

                EnsureCommand(group, "Nhập EDZ…", "OumijiaImportEdz");
                EnsureCommand(group, "Xuất EDZ…", "OumijiaExportEdz");
                EnsureCommand(group, "Data Portal…", "OumijiaDataPortal");
                EnsureCommand(group, "Menu bar", "MfToggleMainMenuAction");

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

        private static Assembly ResolveBundledAssembly(object sender, ResolveEventArgs args)
        {
            try
            {
                string simpleName = new AssemblyName(args.Name).Name;
                if (String.IsNullOrEmpty(simpleName)) return null;

                bool isSharedAssembly =
                    simpleName.Equals("Oumijia.EplanUi", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("Oumijia.PortalHost", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("Oumijia.EdzReader", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("SharpCompress", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("System.Buffers", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("System.Memory", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("System.Numerics.Vectors", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("System.Runtime.CompilerServices.Unsafe", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("System.Text.Encoding.CodePages", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("System.Threading.Tasks.Extensions", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("Microsoft.Bcl.AsyncInterfaces", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.StartsWith("Microsoft.Web.WebView2", StringComparison.OrdinalIgnoreCase);
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
