using System;
using Eplan.EplApi.ApplicationFramework;
using Oumijia.EplanUi;

namespace Oumijia.EplanAddIn.Actions
{
    /// <summary>Action "OumijiaImportEdz": batch import of EDZ packages into the parts database.</summary>
    public class ImportEdzAction : IEplAction
    {
        public bool Execute(ActionCallingContext oActionCallingContext)
        {
            try
            {
                using (var form = new ImportEdzForm())
                {
                    form.ShowDialog();
                }
                return true;
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("ImportEdz", ex);
                return false;
            }
        }

        public bool OnRegister(ref string Name, ref int Ordinal)
        {
            Name = "OumijiaImportEdz";
            Ordinal = 21;
            return true;
        }

        public void GetActionProperties(ref ActionProperties actionProperties)
        {
            // EPLAN 2022: ActionProperties exposes no writable public members.
        }
    }
}
