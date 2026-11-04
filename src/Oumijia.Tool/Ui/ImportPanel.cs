using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Oumijia.Tool.Ui;

/// <summary>
/// "Import" tab (EDZ-only): collect EDZ packages and import them into the EPLAN
/// parts database.
///
/// Batch import is executed by the EPLAN add-in (ribbon "Tool Oumijia" → "Nhập EDZ…").
/// This tab prepares, checks and documents the list for the manual import dialog
/// in EPLAN as a fallback.
/// </summary>
public sealed class ImportPanel : UserControl
{
    private readonly ListBox _edzList = new ListBox();
    private readonly TextBox _log = new TextBox();

    public ImportPanel()
    {
        BuildUi();
    }

    private void BuildUi()
    {
        Dock = DockStyle.Fill;
        Padding = new Padding(8);

        // ------------------------------------------------ EDZ → Parts DB
        var edzGroup = new GroupBox
        {
            Text = "EDZ → EPLAN Parts Database",
            Dock = DockStyle.Fill,
            Padding = new Padding(8)
        };

        var edzNote = new Label
        {
            Dock = DockStyle.Top,
            Height = 64,
            Text = "Batch import: mở EPLAN → ribbon \"Tool Oumijia\" → \"Nhập EDZ…\" (add-in tự nhập vào Parts Database).\r\n" +
                   "Fallback thủ công: Master data → Parts → Management → [Extras] → Import → chọn từng tệp .edz.\r\n" +
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
        btnEdzAddFiles.Click += OnAddFilesClicked;
        var btnEdzAddFolder = new Button { Text = "Thêm thư mục…", Width = 116, Height = 26 };
        btnEdzAddFolder.Click += OnAddFolderClicked;
        var btnEdzRemove = new Button { Text = "Xóa mục chọn", Width = 100, Height = 26 };
        btnEdzRemove.Click += (_, __) =>
        {
            foreach (var item in _edzList.SelectedItems.Cast<object>().ToList())
            {
                _edzList.Items.Remove(item);
            }
        };
        var btnEdzClear = new Button { Text = "Xóa hết", Width = 80, Height = 26 };
        btnEdzClear.Click += (_, __) => _edzList.Items.Clear();
        var btnEdzCopy = new Button { Text = "Copy danh sách", Width = 110, Height = 26 };
        btnEdzCopy.Click += (_, __) => CopyListToClipboard();
        var btnEdzOpen = new Button { Text = "Mở thư mục", Width = 100, Height = 26 };
        btnEdzOpen.Click += (_, __) => OpenFolderOfSelection();

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

        // ------------------------------------------------ log
        var logPanel = new Panel { Dock = DockStyle.Bottom, Height = 160 };
        var lblLog = new Label { Text = "Nhật ký:", Dock = DockStyle.Top, Height = 18 };
        _log.Dock = DockStyle.Fill;
        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = ScrollBars.Both;
        _log.WordWrap = false;
        _log.Font = new Font("Consolas", 8.5F);
        logPanel.Controls.Add(_log);
        logPanel.Controls.Add(lblLog);

        Controls.Add(edzGroup);
        Controls.Add(logPanel);
    }

    private void OnAddFilesClicked(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Chọn gói EDZ",
            Filter = "Gói EPLAN (*.edz)|*.edz|Tất cả tệp (*.*)|*.*",
            Multiselect = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        AddDistinct(dialog.FileNames);
    }

    private void OnAddFolderClicked(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog { Description = "Chọn thư mục chứa EDZ (bao gồm thư mục con)" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            AddDistinct(Directory.EnumerateFiles(dialog.SelectedPath, "*.edz", SearchOption.AllDirectories));
        }
        catch (Exception ex)
        {
            Log("LỖI đọc thư mục: " + ex.Message);
        }
    }

    private void AddDistinct(IEnumerable<string> files)
    {
        var existing = new HashSet<string>(_edzList.Items.Cast<object>().Select(o => o?.ToString() ?? ""), StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            if (string.IsNullOrWhiteSpace(file)) continue;
            if (existing.Add(file)) _edzList.Items.Add(file);
        }
    }

    private void CopyListToClipboard()
    {
        if (_edzList.Items.Count == 0)
        {
            Clipboard.Clear();
            Log("Danh sách trống — không có gì để copy.");
            return;
        }

        var text = string.Join(Environment.NewLine, _edzList.Items.Cast<object>().Select(o => o?.ToString() ?? ""));
        Clipboard.SetText(text);
        Log("Đã copy " + _edzList.Items.Count + " đường dẫn vào clipboard.");
    }

    private void OpenFolderOfSelection()
    {
        if (_edzList.SelectedItems.Count == 0) return;
        var first = _edzList.SelectedItems[0]?.ToString() ?? "";
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
