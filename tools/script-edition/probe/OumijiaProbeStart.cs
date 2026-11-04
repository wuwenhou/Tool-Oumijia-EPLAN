// Probe toi gian: chi ghi log khi Register / OnMainStart / action duoc goi.
// Dung de kiem chung co che "registered scripts" cua EPLAN.
using System;
using System.IO;
using Eplan.EplApi.Scripting;

public class OumijiaProbeStart
{
    [DeclareRegister]
    public void Reg()
    {
        Log("PROBE Register fired");
    }

    [DeclareUnregister]
    public void Unreg()
    {
        Log("PROBE Unregister fired");
    }

    [DeclareEventHandler("Eplan.EplApi.OnMainStart")]
    public void OnMainStartProbe()
    {
        Log("PROBE OnMainStart fired");
    }

    [DeclareAction("OumijiaProbeAlive")]
    public void Alive()
    {
        Log("PROBE action Alive called");
    }

    private static void Log(string message)
    {
        try
        {
            string logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Tool Oumijia", "logs");
            Directory.CreateDirectory(logDir);
            File.AppendAllText(Path.Combine(logDir, "probe-start.log"),
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + message + Environment.NewLine);
        }
        catch
        {
            // ignore
        }
    }
}
