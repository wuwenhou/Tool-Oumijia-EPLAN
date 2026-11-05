using System;
using Eplan.EplApi.ApplicationFramework;
using Oumijia.EplanAddIn.Ui;

namespace Oumijia.EplanAddIn.Actions
{
    internal static class EplanActionInvoker
    {
        public static bool Invoke(string actionName, string displayName)
        {
            try
            {
                var manager = new ActionManager();
                var action = manager.FindAction(actionName);
                if (action == null)
                {
                    AddinLog.Write("Action not found: " + actionName);
                    return false;
                }

                bool ok = action.Execute(new ActionCallingContext());
                AddinLog.Write("Action invoked: " + displayName + " (" + actionName + ", ok=" + ok + ")");
                return ok;
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("Invoke " + actionName, ex);
                return false;
            }
        }
    }

    public sealed class EplanClassicAction : IEplAction
    {
        public bool OnRegister(ref string Name, ref int Ordinal) { Name = "OumijiaEplanClassic"; Ordinal = 25; return true; }
        public void GetActionProperties(ref ActionProperties actionProperties) { /* EPLAN 2022: no writable public members. */ }
        public bool Execute(ActionCallingContext ctx) { return EplanClassicLauncher.Open(); }
    }

    public sealed class VeeExportConnectionsAction : IEplAction
    {
        public bool OnRegister(ref string Name, ref int Ordinal) { Name = "OumijiaVeeExportConnections"; Ordinal = 26; return true; }
        public void GetActionProperties(ref ActionProperties actionProperties) { /* EPLAN 2022: no writable public members. */ }
        public bool Execute(ActionCallingContext ctx) { return VeeIntegration.OpenExportConnections(); }
    }

    public sealed class VeeTableMakerAction : IEplAction
    {
        public bool OnRegister(ref string Name, ref int Ordinal) { Name = "OumijiaVeeTableMaker"; Ordinal = 27; return true; }
        public void GetActionProperties(ref ActionProperties actionProperties) { /* EPLAN 2022: no writable public members. */ }
        public bool Execute(ActionCallingContext ctx) { return VeeIntegration.OpenTableMaker(); }
    }

    public sealed class ToggleMainMenuAction : IEplAction
    {
        public bool OnRegister(ref string Name, ref int Ordinal) { Name = "OumijiaToggleMenuBar"; Ordinal = 28; return true; }
        public void GetActionProperties(ref ActionProperties actionProperties) { /* EPLAN 2022: no writable public members. */ }
        public bool Execute(ActionCallingContext ctx) { return EplanActionInvoker.Invoke("MfToggleMainMenuAction", "Main menu bar"); }
    }

    public sealed class ToggleGridSnapAction : IEplAction
    {
        public bool OnRegister(ref string Name, ref int Ordinal) { Name = "OumijiaToggleGridSnap"; Ordinal = 29; return true; }
        public void GetActionProperties(ref ActionProperties actionProperties) { /* EPLAN 2022: no writable public members. */ }
        public bool Execute(ActionCallingContext ctx) { return EplanActionInvoker.Invoke("XGedToggleGridSnapAction", "Grid snap"); }
    }

    public sealed class ToggleObjectSnapAction : IEplAction
    {
        public bool OnRegister(ref string Name, ref int Ordinal) { Name = "OumijiaToggleObjectSnap"; Ordinal = 30; return true; }
        public void GetActionProperties(ref ActionProperties actionProperties) { /* EPLAN 2022: no writable public members. */ }
        public bool Execute(ActionCallingContext ctx) { return EplanActionInvoker.Invoke("XGedToggleObjectSnapAction", "Object snap"); }
    }

    public sealed class ToggleGridVisibilityAction : IEplAction
    {
        public bool OnRegister(ref string Name, ref int Ordinal) { Name = "OumijiaToggleGridVisibility"; Ordinal = 31; return true; }
        public void GetActionProperties(ref ActionProperties actionProperties) { /* EPLAN 2022: no writable public members. */ }
        public bool Execute(ActionCallingContext ctx) { return EplanActionInvoker.Invoke("XGedToggleGridVisibilityAction", "Grid visibility"); }
    }

    public sealed class ToggleCursorDesignAction : IEplAction
    {
        public bool OnRegister(ref string Name, ref int Ordinal) { Name = "OumijiaToggleCursorDesign"; Ordinal = 32; return true; }
        public void GetActionProperties(ref ActionProperties actionProperties) { /* EPLAN 2022: no writable public members. */ }
        public bool Execute(ActionCallingContext ctx) { return EplanActionInvoker.Invoke("XGedToggleCursorDesignAction", "Design mode"); }
    }

    public sealed class ArrangeOnGridAction : IEplAction
    {
        public bool OnRegister(ref string Name, ref int Ordinal) { Name = "OumijiaArrangeOnGrid"; Ordinal = 33; return true; }
        public void GetActionProperties(ref ActionProperties actionProperties) { /* EPLAN 2022: no writable public members. */ }
        public bool Execute(ActionCallingContext ctx) { return EplanActionInvoker.Invoke("XGedIaArrangeOnGrid", "Align on grid"); }
    }
}
