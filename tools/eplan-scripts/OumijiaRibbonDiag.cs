//#################################################################################################################################################
// Tool Oumijia — ribbon diagnostic script
//#################################################################################################################################################
// Liệt kê toàn bộ ribbon tab / command group / command (kèm IsVisible, IsCustom, Identifier)
// vào %APPDATA%\Tool Oumijia\logs\ribbon-diag.log — dùng để chẩn đoán tab add-in không hiển thị.
//
// Chạy headless:
//   W3u.exe /Auto /Quiet /NoSplash /NoLicenseDialog /NoUserRightsDialog /NoLoadWorkspace ExecuteScript /ScriptFile:"...\OumijiaRibbonDiag.cs"
//#################################################################################################################################################

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Eplan.EplApi.Gui;
using Eplan.EplApi.Scripting;

public class OumijiaRibbonDiag
{
    [Start]
    public void Run()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("==================================================");
        sb.AppendLine("Ribbon diagnostic  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("==================================================");

        try
        {
            RibbonBar bar = new RibbonBar();
            RibbonTab[] tabs = bar.Tabs;
            sb.AppendLine("TOTAL TABS: " + (tabs == null ? 0 : tabs.Length));
            if (tabs != null)
            {
                for (int i = 0; i < tabs.Length; i++)
                {
                    RibbonTab t = tabs[i];
                    try
                    {
                        sb.AppendLine("TAB[" + i + "] Name='" + t.Name + "' DisplayName='" + t.DisplayName +
                                      "' IsVisible=" + t.IsVisible + " IsCustom=" + t.IsCustom +
                                      " Identifier=" + t.Identifier);
                    }
                    catch (Exception exTab)
                    {
                        sb.AppendLine("TAB[" + i + "] (error reading: " + exTab.Message + ")");
                    }

                    try
                    {
                        RibbonCommandGroup[] groups = t.CommandGroups;
                        if (groups != null)
                        {
                            for (int g = 0; g < groups.Length; g++)
                            {
                                RibbonCommandGroup grp = groups[g];
                                sb.AppendLine("    GROUP['" + grp.Name + "'] IsCustom=" + grp.IsCustom);
                                try
                                {
                                    Dictionary<uint, RibbonCommand> cmds = grp.GetCommands(false);
                                    if (cmds != null)
                                    {
                                        foreach (KeyValuePair<uint, RibbonCommand> kv in cmds)
                                        {
                                            string cmdText = "(null)";
                                            string cmdLine = "(null)";
                                            try
                                            {
                                                if (kv.Value != null)
                                                {
                                                    cmdText = kv.Value.Text;
                                                    cmdLine = kv.Value.ActionCommandLine;
                                                }
                                            }
                                            catch { }
                                            sb.AppendLine("        CMD '" + kv.Key + "' text='" + cmdText + "' action='" + cmdLine + "'");
                                        }
                                    }
                                }
                                catch (Exception exCmd)
                                {
                                    sb.AppendLine("        (commands error: " + exCmd.Message + ")");
                                }
                            }
                        }
                    }
                    catch (Exception exGrp)
                    {
                        sb.AppendLine("    (groups error: " + exGrp.Message + ")");
                    }
                }
            }

            sb.AppendLine("");
            sb.AppendLine("--- GetTab('Tool Oumijia') probe ---");
            try
            {
                RibbonTab mine = bar.GetTab("Tool Oumijia");
                if (mine == null)
                {
                    sb.AppendLine("GetTab('Tool Oumijia') => NULL");
                }
                else
                {
                    sb.AppendLine("GetTab('Tool Oumijia') => Name='" + mine.Name + "' DisplayName='" + mine.DisplayName +
                                  "' IsVisible=" + mine.IsVisible + " IsCustom=" + mine.IsCustom);
                }
            }
            catch (Exception exProbe)
            {
                sb.AppendLine("GetTab probe error: " + exProbe.Message);
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine("FATAL: " + ex);
        }

        try
        {
            string logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Tool Oumijia", "logs");
            Directory.CreateDirectory(logDir);
            File.AppendAllText(Path.Combine(logDir, "ribbon-diag.log"), sb.ToString());
        }
        catch
        {
            // best effort
        }
    }
}
