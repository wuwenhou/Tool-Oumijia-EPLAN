using Eplan.EplApi.ApplicationFramework;

namespace Oumijia.EplanAddIn
{
    /// <summary>
    /// EPLAN add-in entry point ("Tool Oumijia").
    ///
    /// Registration on a machine (EPLAN 2.9+ / Platform):
    ///   Utilities &gt; API Add-ins &gt; register "Oumijia.EplanAddIn.dll"
    /// or ship as an EPLAN add-on (see docs/BUILD.md).
    ///
    /// TODO(v0.2): register a ribbon button for the "OumijiaOpenTool" action, and
    /// implement batch EDZ import into the parts database via the EPLAN API
    /// (Eplan.EplApi.MasterData / parts import services). Verify interface and
    /// method signatures against your platform version:
    ///   EPLAN Help &gt; Eplan Platform API &gt; Add-ins / Actions (IEplAddIn, IEplAction).
    /// </summary>
    public class AddIn : IEplAddIn
    {
        public bool OnRegister(ref bool bLoadOnStart)
        {
            // Load on EPLAN start so the API action is available immediately.
            bLoadOnStart = true;
            return true;
        }

        public bool OnUnregister()
        {
            return true;
        }

        public bool OnInit()
        {
            return true;
        }

        public bool OnInitGui()
        {
            return true;
        }

        public bool OnExit()
        {
            return true;
        }
    }
}
