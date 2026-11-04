using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Oumijia.Core.Library;
using Oumijia.Core.Macros;

namespace Oumijia.Tool.Ui;

/// <summary>
/// "Import" tab:
///  1) EDZ → EPLAN Parts Database (guided queue; batch import via the add-in is on the roadmap)
///  2) EMA/EMP/EMS → EPLAN master-data macros (option A: copy into the macro directory).
/// </summary>
public sealed class ImportPanel : UserControl
{
    private readonly ListBox _edzList = new();
    private readonly ListBox _macroList = new();
    private readonly ComboBox _macroDirCombo = new();
    private readonly TextBox _log = new();

    public ImportPanel()
    {
        BuildUi();
        RefreshMacroDirectories();
    }

    private void BuildUi()
    {
        Dock = DockStyle.Fill;
        Padding = new Padding(8);

        // ------------------------------------------------ 1) EDZ → Parts DB
        var edzGroup = new GroupBox
        {
            Text = "1) EDZ → EPLAN Parts Database",
            Dock = DockStyle.Top,
            Height = 250,
            Padding = new Padding(8)
        };

        var edzNote = new Label
        {
            Dock = DockStyle.Top,
            Height = 62,
            Text = "Batch import EDZ vào Parts DB sẽ do Add-in EPLAN đảm nhiệm ở phiên bản kế tiếp (v0.2).\r\n" +
                   "Hiện tại: trong EPLAN mở Master data → Parts → Management → [Extras] → Import → chọn từng tệp .edz.\r\n" +
                   "Danh sách dưới đây giúp bạn gom nhóm, kiểm tra và theo dõi các gói cần nhập."
        };

        var edzButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        var btnEdzAddFiles = new Button { Text = "Thêm tệp…", Width = 96, Height = 26 };
        btnEdzAddFiles.Click += OnAddEdzFilesClicked;
        var btnEdzAddFolder = new Button { Text = "Thêm thư mục…", Width = 116, Height = 26 };
        btnEdzAddFolder.Click += OnAddEdzFolderClicked;
        var btnEdzRemove = new Button { Text = "Xóa mục chọn", Width = 100, Height = 26 };
        btnEdzRemove.Click += (_, __) => RemoveSelected(_edzList);
        var btnEdzClear = new Button { Text = "Xóa hết", Width = 80, Height = 26 };
        btnEdzClear.Click += (_, __) => _edzList.Items.Clear();
        var btnEdzCopy = new Button { Text = "Copy danh sách", Width = 110, Height = 26 };
        btnEdzCopy.Click += (_, __) => CopyListToClipboard(_edzList);
        var btnEdzOpen = new Button { Text = "Mở thư mục", Width = 100, Height = 26 };
        btnEdzOpen.Click += (_, __) => OpenFolderOfSelection(_edzList);

        edzButtons.Controls.Add(btnEdzAddFiles);
        edzButtons.Controls.Add(btnEdzAddFolder);
        edzButtons.Controls.Add(btnEdzRemove);
        edzButtons.Controls.Add(btnEdzClear);
        edzButtons.Controls.Add(btnEdzCopy);
        edzButtons.Controls.Add(btnEdzOpen);

        _edzList.Dock = DockStyle.Fill;
        _edzList.SelectionMode = SelectionMode.MultiExtended;

        edzGroup.Controls.Add(_edzList);
        edzGroup.Controls.Add(edzButtons);
        edzGroup.Controls.Add(edzNote);

        // ------------------------------------------------ 2) macros → master data
        var macroGroup = new GroupBox
        {
            Text = "2) EMA/EMP/EMS → EPLAN master data macros  (kiểu A: copy vào thư mục macro của EPLAN)",
            Dock = DockStyle.Fill,
            Padding = new Padding(8)
        };

        var macroTop = new Panel { Dock = DockStyle.Top, Height = 68 };

        var lblDir = new Label { Text = "Thư mục macro đích:", AutoSize = true, Location = new Point(6, 8) };
        _macroDirCombo.Location = new Point(6, 30);
        _macroDirCombo.Width = 600;
        _macroDirCombo.DropDownStyle = ComboBoxStyle.DropDown;

        var btnDetect = new Button { Text = "Dò tìm", Location = new Point(614, 29), Size = new Size(80, 24) };
        btnDetect.Click += (_, __) =>
        {
            RefreshMacroDirectories();
            Log("Đã dò tìm thư mục macro EPLAN trên máy.");
        };

        var btnInstall = new Button
        {
            Text = "CÀI VÀO EPLAN ▸",
            Location = new Point(704, 26),
            Size = new Size(160, 30),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        btnInstall.Click += OnInstallMacrosClicked;

        macroTop.Controls.Add(lblDir);
        macroTop.Controls.Add(_macroDirCombo);
        macroTop.Controls.Add(btnDetect);
        macroTop.Controls.Add(btnInstall);

        var macroButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        var btnMacroAddFiles = new Button { Text = "Thêm tệp…", Width = 96, Height = 26 };
        btnMacroAddFiles.Click += OnAddMacroFilesClicked;
        var btnMacroAddFolder = new Button { Text = "Thêm thư mục…", Width = 116, Height = 26 };
        btnMacroAddFolder.Click += OnAddMacroFolderClicked;
        var btnMacroRemove = new Button { Text = "Xóa mục chọn", Width = 100, Height = 26 };
        btnMacroRemove.Click += (_, __) => RemoveSelected(_macroList);
        var btnMacroClear = new Button { Text = "Xóa hết", Width = 80, Height = 26 };
        btnMacroClear.Click += (_, __) => _macroList.Items.Clear();
        var btnMacroOpen = new Button { Text = "Mở thư mục đích", Width = 120, Height = 26 };
        btnMacroOpen.Click += (_, __) => OpenFolder(_macroDirCombo.Text);

        macroButtons.Controls.Add(btnMacroAddFiles);
        macroButtons.Controls.Add(btnMacroAddFolder);
        macroButtons.Controls.Add(btnMacroRemove);
        macroButtons.Controls.Add(btnMacroClear);
        macroButtons.Controls.Add(btnMacroOpen);

        _macroList.Dock = DockStyle.Fill;
        _macroList.SelectionMode = SelectionMode.MultiExtended;

        macroGroup.Controls.Add(_macroList);
        macroGroup.Controls.Add(macroButtons);
        macroGroup.Controls.Add(macroTop);

        // ------------------------------------------------ log
        var logPanel = new Panel { Dock = DockStyle.Bottom, Height = 150 };
        var lblLog = new Label { Text = "Nhật ký:", Dock = DockStyle.Top, Height = 18 };
        _log.Dock = DockStyle.Fill;
        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = ScrollBars.Both;
        _log.WordWrap = false;
        _log.Font = new Font("Consolas", 8.5F);
        logPanel.Controls.Add(_log);
        logPanel.Controls.Add(lblLog);

        Controls.Add(macroGroup);
        Controls.Add(logPanel);
        Controls.Add(edzGroup);
    }

    // ------------------------------------------------------------------- EDZ

    private void OnAddEdzFilesClicked(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Chọn gói EDZ",
            Filter = "Gói EPLAN (*.edz)|*.edz|Tất cả tệp (*.*)|*.*",
            Multiselect = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        AddDistinct(_edzList, dialog.FileNames);
    }

    private void OnAddEdzFolderClicked(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog { Description = "Chọn thư mục chứa EDZ (bao gồm thư mục con)" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var files = Directory.EnumerateFiles(dialog.SelectedPath, "*.edz", SearchOption.AllDirectories);
            AddDistinct(_edzList, files);
        }
        catch (Exception ex)
        {
            Log("LỖI đọc thư mục: " + ex.Message);
        }
    }

    // ----------------------------------------------------------------- macros

    private void OnAddMacroFilesClicked(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Chọn macro EPLAN",
            Filter = "Macro EPLAN (*.ema;*.emp;*.ems)|*.ema;*.emp;*.ems|Tất cả tệp (*.*)|*.*",
            Multiselect = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        AddDistinct(_macroList, dialog.FileNames);
    }

    private void OnAddMacroFolderClicked(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog { Description = "Chọn thư mục chứa macro (bao gồm thư mục con)" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var patterns = new[] { "*.ema", "*.emp", "*.ems" };
            var files = new List<string>();
            foreach (var pattern in patterns)
            {
                files.AddRange(Directory.EnumerateFiles(dialog.SelectedPath, pattern, SearchOption.AllDirectories));
            }
            files.Sort(StringComparer.OrdinalIgnoreCase);
            AddDistinct(_macroList, files);
        }
        catch (Exception ex)
        {
            Log("LỖI đọc thư mục: " + ex.Message);
        }
    }

    private void RefreshMacroDirectories()
    {
        var dirs = new List<string>();
        try
        {
            foreach (var dir in MacroPathDetector.DetectCandidates())
            {
                if (!dirs.Contains(dir)) dirs.Add(dir);
            }
        }
        catch
        {
            // Detection is best-effort.
        }

        foreach (var dir in App.Config.MacroDirectories)
        {
            if (!dirs.Contains(dir)) dirs.Add(dir);
        }

        var current = _macroDirCombo.Text;
        _macroDirCombo.Items.Clear();
        foreach (var dir in dirs) _macroDirCombo.Items.Add(dir);

        if (!string.IsNullOrWhiteSpace(current))
        {
            _macroDirCombo.Text = current;
        }
        else if (_macroDirCombo.Items.Count > 0)
        {
            _macroDirCombo.Text = _macroDirCombo.Items[0]?.ToString() ?? "";
        }
        else
        {
            var publicRoot = Environment.GetEnvironmentVariable("PUBLIC") ?? "C:\\Users\\Public";
            _macroDirCombo.Text = Path.Combine(publicRoot, "EPLAN", "Data", "Macros");
        }
    }

    private void OnInstallMacrosClicked(object? sender, EventArgs e)
    {
        if (_macroList.Items.Count == 0)
        {
            MessageBox.Show(this, "Chưa có tệp macro nào trong danh sách.", "Tool Oumijia",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var targetDir = (_macroDirCombo.Text ?? "").Trim();
        if (targetDir.Length == 0)
        {
            MessageBox.Show(this, "Hãy chọn hoặc nhập thư mục macro đích.", "Tool Oumijia",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!Directory.Exists(targetDir))
        {
            var answer = MessageBox.Show(this,
                "Thư mục chưa tồn tại:\n" + targetDir + "\n\nTạo mới và tiếp tục?",
                "Tool Oumijia", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;
            try
            {
                Directory.CreateDirectory(targetDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Không tạo được thư mục:\n" + ex.Message, "Tool Oumijia",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        var installer = new MacroInstaller(App.Config.PolicyEnum);
        var logPath = App.Paths.NewImportLogPath();

        int ok = 0, skipped = 0, errors = 0;

        using (var csv = new CsvLog(logPath, new[] { "Thời gian", "Nguồn", "Đích", "Hành động", "SHA256" }))
        {
            foreach (var item in _macroList.Items)
            {
                var file = item?.ToString() ?? "";
                var result = installer.Install(file, targetDir);

                switch (result.Status)
                {
                    case LibraryWriteStatus.New:
                    case LibraryWriteStatus.Renamed:
                    case LibraryWriteStatus.Overwritten:
                        ok++;
                        break;
                    case LibraryWriteStatus.SkippedIdentical:
                    case LibraryWriteStatus.SkippedExisting:
                        skipped++;
                        break;
                    default:
                        errors++;
                        break;
                }

                Log("[" + result.StatusText + "] " + Path.GetFileName(file) + " → " + result.DestinationPath + "  " + result.Message);

                csv.Append(
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    result.SourcePath,
                    result.DestinationPath,
                    result.StatusText,
                    result.Sha256);
            }
        }

        // Remember the used target folder in the configuration.
        if (!App.Config.MacroDirectories.Any(x => x.Equals(targetDir, StringComparison.OrdinalIgnoreCase)))
        {
            App.Config.MacroDirectories.Add(targetDir);
            App.Config.Save(App.Paths.ConfigFile);
        }

        MessageBox.Show(this,
            "Kết quả cài macro:\n" +
            "• Đã cài: " + ok + "\n" +
            "• Bỏ qua: " + skipped + "\n" +
            "• Lỗi: " + errors + "\n\n" +
            "Hãy KHỞI ĐỘNG LẠI EPLAN để thấy macro trong Macro Navigator.\n\n" +
            "Log: " + logPath,
            "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ------------------------------------------------------------------- misc

    private static void AddDistinct(ListBox list, IEnumerable<string> files)
    {
        var existing = new HashSet<string>(list.Items.Cast<object>().Select(o => o?.ToString() ?? ""), StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            if (string.IsNullOrWhiteSpace(file)) continue;
            if (existing.Add(file)) list.Items.Add(file);
        }
    }

    private static void RemoveSelected(ListBox list)
    {
        var selected = list.SelectedItems.Cast<object>().ToList();
        foreach (var item in selected) list.Items.Remove(item);
    }

    private void CopyListToClipboard(ListBox list)
    {
        if (list.Items.Count == 0)
        {
            Clipboard.Clear();
            Log("Danh sách trống — không có gì để copy.");
            return;
        }

        var text = string.Join(Environment.NewLine, list.Items.Cast<object>().Select(o => o?.ToString() ?? ""));
        Clipboard.SetText(text);
        Log("Đã copy " + list.Items.Count + " đường dẫn vào clipboard.");
    }

    private static void OpenFolderOfSelection(ListBox list)
    {
        if (list.SelectedItems.Count == 0) return;
        var first = list.SelectedItems[0]?.ToString() ?? "";
        if (File.Exists(first))
        {
            OpenFolder(Path.GetDirectoryName(first) ?? "");
        }
    }

    private static void OpenFolder(string? path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            Process.Start("explorer.exe", path);
        }
        catch
        {
            // Best-effort.
        }
    }

    private void Log(string message)
    {
        try
        {
            _log.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + message + Environment.NewLine);
        }
        catch
        {
            // Never crash on logging.
        }
    }
}
