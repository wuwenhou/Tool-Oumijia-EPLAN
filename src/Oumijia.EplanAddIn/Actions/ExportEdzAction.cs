using System;
using Eplan.EplApi.ApplicationFramework;
using Oumijia.EplanAddIn.Ui;

namespace Oumijia.EplanAddIn.Actions
{
    /// <summary>Action "OumijiaExportEdz": export parts from the parts database to EDZ packages.</summary>
    public class ExportEdzAction : IEplAction
    {
        public bool Execute(ActionCallingContext oActionCallingContext)
        {
            try
            {
                using (var form = new ExportEdzForm())
                {
                    form.ShowDialog();
                }
                return true;
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("ExportEdz", ex);
                return false;
            }
        }

        public bool OnRegister(ref string Name, ref int Ordinal)
        {
            Name = "OumijiaExportEdz";
            Ordinal = 22;
            return true;
        }

        public void GetActionProperties(ref ActionProperties actionProperties)
        {
            // EPLAN 2022: ActionProperties exposes no writable public members.
        }
    }
}
