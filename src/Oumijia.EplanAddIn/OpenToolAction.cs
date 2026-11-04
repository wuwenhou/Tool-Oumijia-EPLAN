using Eplan.EplApi.ApplicationFramework;

namespace Oumijia.EplanAddIn
{
    /// <summary>
    /// EPLAN action "OumijiaOpenTool" — opens the Tool Oumijia window.
    ///
    /// Usage once the add-in is registered:
    ///   - command line / action mechanism: OumijiaOpenTool
    ///   - bind it to a ribbon button or keyboard shortcut in EPLAN settings.
    ///
    /// TODO(v0.2): implement the batch EDZ import action ("OumijiaImportEdz")
    /// using the EPLAN parts/master-data API; see docs/ROADMAP.md.
    /// </summary>
    public class OpenToolAction : IEplAction
    {
        public bool Execute(ActionCallingContext oActionCallingContext)
        {
            try
            {
                Oumijia.Tool.Program.ShowMainWindow();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool OnRegister(ref string Name, ref bool OnActionStart, ref int Ordinal)
        {
            Name = "OumijiaOpenTool";
            OnActionStart = true;
            Ordinal = 20;
            return true;
        }

        public void GetActionProperties(ref ActionProperties actionProperties)
        {
            actionProperties.Description = "Mở Tool Oumijia — EPLAN EDZ/EMA assistant.";
        }

        public bool OnUnregister()
        {
            return true;
        }
    }
}
