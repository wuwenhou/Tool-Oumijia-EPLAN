using System;
using Eplan.EplApi.ApplicationFramework;
using Oumijia.EplanUi;

namespace Oumijia.EplanAddIn.Actions
{
    /// <summary>
    /// Headless self test: proves that EPLAN loaded the add-in and that its actions
    /// are callable (e.g. from the command line). Writes a marker line to addin.log.
    ///
    /// Usage:
    ///   W3u.exe /Auto /Quiet /NoSplash /NoLicenseDialog /NoUserRightsDialog /NoLoadWorkspace OumijiaSelfTest
    /// </summary>
    public class SelfTestAction : IEplAction
    {
        public bool Execute(ActionCallingContext oActionCallingContext)
        {
            AddinLog.Write("SELFTEST OK — add-in loaded, actions callable");
            return true;
        }

        public bool OnRegister(ref string Name, ref int Ordinal)
        {
            Name = "OumijiaSelfTest";
            Ordinal = 23;
            return true;
        }

        public void GetActionProperties(ref ActionProperties actionProperties)
        {
            // EPLAN 2022: ActionProperties exposes no writable public members.
        }
    }
}
