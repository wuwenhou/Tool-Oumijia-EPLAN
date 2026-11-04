using System;
using System.Diagnostics;
using System.IO;
using Eplan.EplApi.ApplicationFramework;

namespace Oumijia.EplanAddIn.Actions
{
    /// <summary>Action "OumijiaOpenTool": opens the standalone Tool Oumijia window.</summary>
    public class OpenToolAction : IEplAction
    {
        public bool Execute(ActionCallingContext oActionCallingContext)
        {
            try
            {
                var executable = ToolLocator.FindToolExecutable();
                if (executable == null) executable = ToolLocator.PromptForToolExecutable();
                if (executable == null)
                {
                    AddinLog.Write("OpenTool: executable not found; user cancelled");
                    return true;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = executable,
                    WorkingDirectory = Path.GetDirectoryName(executable) ?? string.Empty,
                    UseShellExecute = true
                });

                AddinLog.Write("OpenTool: started " + executable);
                return true;
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("OpenTool", ex);
                return false;
            }
        }

        public bool OnRegister(ref string Name, ref int Ordinal)
        {
            Name = "OumijiaOpenTool";
            Ordinal = 20;
            return true;
        }

        public void GetActionProperties(ref ActionProperties actionProperties)
        {
            // EPLAN 2022: ActionProperties exposes no writable public members.
        }
    }
}
