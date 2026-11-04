using System.Drawing;
using System.Windows.Forms;
using Oumijia.Tool.Ui;

namespace Oumijia.Tool;

public sealed class MainForm : Form
{
    public MainForm()
    {
        Text = "Tool Oumijia — EPLAN EDZ/EMA Assistant  v" + App.VersionText;
        Width = 1200;
        Height = 780;
        MinimumSize = new Size(900, 620);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);

        var tabs = new TabControl { Dock = DockStyle.Fill };

        var exportTab = new TabPage("Xuất (Export)") { UseVisualStyleBackColor = true };
        exportTab.Controls.Add(new ExportPanel { Dock = DockStyle.Fill });

        var importTab = new TabPage("Nhập (Import)") { UseVisualStyleBackColor = true };
        importTab.Controls.Add(new ImportPanel { Dock = DockStyle.Fill });

        var settingsTab = new TabPage("Cài đặt") { UseVisualStyleBackColor = true };
        settingsTab.Controls.Add(new SettingsPanel { Dock = DockStyle.Fill });

        tabs.TabPages.Add(exportTab);
        tabs.TabPages.Add(importTab);
        tabs.TabPages.Add(settingsTab);

        Controls.Add(tabs);
    }
}
