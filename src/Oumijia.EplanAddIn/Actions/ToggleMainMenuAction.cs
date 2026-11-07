using System;
using System.Windows.Forms;
using Eplan.EplApi.ApplicationFramework;
using Oumijia.EplanUi;

namespace Oumijia.EplanAddIn.Actions
{
    /// <summary>
    /// Action "OumijiaToggleMainMenu": Menu bar khi EPLAN không còn
    /// MfToggleMainMenuAction (2026). Báo rõ thay vì chết lặng.
    /// Không giả lập menu của EPLAN.
    /// </summary>
    public class ToggleMainMenuAction : IEplAction
    {
        public bool Execute(ActionCallingContext oActionCallingContext)
        {
            try
            {
                AddinLog.Write("Action OumijiaToggleMainMenu - EPLAN khong con MfToggleMainMenuAction, khong the hien classic menu qua API");
                MessageBox.Show(
                    "EPLAN bản này không còn action MfToggleMainMenuAction nên không bật được classic menu qua API.\n\n" +
                    "Hãy dùng ribbon của EPLAN (File, Home, Insert, Edit, View, Devices, Connections, Tools, Pre-planning, Master data, Eplan Cloud) thay thế.\n\n" +
                    "Chi tiết đã ghi vào log addin.log (dòng ribbon probe).",
                    "Tool Oumijia — Menu bar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("ToggleMainMenu", ex);
                return false;
            }
        }

        public bool OnRegister(ref string Name, ref int Ordinal)
        {
            Name = "OumijiaToggleMainMenu";
            Ordinal = 26;
            return true;
        }

        public void GetActionProperties(ref ActionProperties actionProperties)
        {
            // EPLAN 2022: ActionProperties exposes no writable public members.
        }
    }
}
