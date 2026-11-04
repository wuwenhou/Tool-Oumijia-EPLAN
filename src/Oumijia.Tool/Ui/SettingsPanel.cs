using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Oumijia.Core.Library;
using Oumijia.Core.Macros;

namespace Oumijia.Tool.Ui;

/// <summary>"Settings" tab: library root, inbox folders, conflict policy, macro directories.</summary>
public sealed class SettingsPanel : UserControl
{
    private readonly TextBox _libraryRoot = new();
    private readonly ListBox _inboxList = new();
    private readonly ComboBox _policy = new();
    private readonly CheckBox _move = new();
    private readonly TextBox _fallback = new();
    private readonly ListBox _macroDirs = new();
    private readonly Label _configPath = new();

    public SettingsPanel()
    {
        BuildUi();
        LoadFromConfig();
    }

    private void BuildUi()
    {
        Dock = DockStyle.Fill;
        Padding = new Padding(8);
        AutoScroll = true;

        var lblLibrary = new Label { Text = "Thư mục gốc thư viện (library root):", AutoSize = true, Location = new Point(0, 6) };
        _libraryRoot.Location = new Point(0, 26);
        _libraryRoot.Width = 560;
        var btnBrowseLibrary = new Button { Text = "Chọn…", Location = new Point(568, 25), Size = new Size(90, 24) };
        btnBrowseLibrary.Click += OnBrowseLibraryClicked;

        var lblInbox = new Label { Text = "Thư mục quét (inbox):", AutoSize = true, Location = new Point(0, 62) };
        _inboxList.Location = new Point(0, 82);
        _inboxList.Size = new Size(560, 92);
        _inboxList.IntegralHeight = false;
        var btnInboxAdd = new Button { Text = "Thêm…", Location = new Point(568, 82), Size = new Size(90, 24) };
        btnInboxAdd.Click += OnInboxAddClicked;
        var btnInboxRemove = new Button { Text = "Xóa", Location = new Point(568, 110), Size = new Size(90, 24) };
        btnInboxRemove.Click += OnInboxRemoveClicked;

        var lblPolicy = new Label { Text = "Chính sách khi trùng tên:", AutoSize = true, Location = new Point(0, 188) };
        _policy.Location = new Point(0, 208);
        _policy.Width = 430;
        _policy.DropDownStyle = ComboBoxStyle.DropDownList;
        _policy.Items.Add("Rename — giữ cả hai, thêm (2), (3)…");
        _policy.Items.Add("Skip — bỏ qua tệp mới");
        _policy.Items.Add("Overwrite — ghi đè tệp cũ");

        _move.Text = "Di chuyển (move) thay vì sao chép";
        _move.AutoSize = true;
        _move.Location = new Point(444, 210);

        var lblFallback = new Label { Text = "Thư mục cho tệp không phân loại:", AutoSize = true, Location = new Point(0, 248) };
        _fallback.Location = new Point(200, 245);
        _fallback.Width = 160;

        var lblMacroDirs = new Label { Text = "Thư mục macro EPLAN (bổ sung cho phần dò tự động):", AutoSize = true, Location = new Point(0, 286) };
        _macroDirs.Location = new Point(0, 306);
        _macroDirs.Size = new Size(560, 92);
        _macroDirs.IntegralHeight = false;
        var btnMacroAdd = new Button { Text = "Thêm…", Location = new Point(568, 306), Size = new Size(90, 24) };
        btnMacroAdd.Click += OnMacroDirAddClicked;
        var btnMacroRemove = new Button { Text = "Xóa", Location = new Point(568, 334), Size = new Size(90, 24) };
        btnMacroRemove.Click += (_, __) =>
        {
            var selected = _macroDirs.SelectedItems.Cast<object>().ToList();
            foreach (var item in selected) _macroDirs.Items.Remove(item);
        };
        var btnMacroDetect = new Button { Text = "Dò tìm", Location = new Point(568, 362), Size = new Size(90, 24) };
        btnMacroDetect.Click += (_, __) =>
        {
            foreach (var dir in MacroPathDetector.DetectCandidates())
            {
                if (!_macroDirs.Items.Cast<object>().Any(o => string.Equals(o?.ToString(), dir, StringComparison.OrdinalIgnoreCase)))
                {
                    _macroDirs.Items.Add(dir);
                }
            }
        };

        var btnSave = new Button
        {
            Text = "💾 Lưu cài đặt",
            Location = new Point(0, 412),
            Size = new Size(150, 32),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        btnSave.Click += OnSaveClicked;

        var btnOpenConfig = new Button { Text = "Mở thư mục cấu hình", Location = new Point(162, 414), Size = new Size(160, 28) };
        btnOpenConfig.Click += (_, __) => OpenFolder(App.Paths.BaseDirectory);

        var btnOpenRules = new Button { Text = "Mở rules.json", Location = new Point(334, 414), Size = new Size(130, 28) };
        btnOpenRules.Click += (_, __) => OpenFileInExplorer(App.Paths.RulesFile);

        var btnOpenOverrides = new Button { Text = "Mở overrides.json", Location = new Point(476, 414), Size = new Size(150, 28) };
        btnOpenOverrides.Click += (_, __) => OpenFileInExplorer(App.Paths.OverridesFile);

        _configPath.AutoSize = true;
        _configPath.Location = new Point(0, 456);
        _configPath.ForeColor = Color.DimGray;

        Controls.AddRange(new Control[]
        {
            lblLibrary, _libraryRoot, btnBrowseLibrary,
            lblInbox, _inboxList, btnInboxAdd, btnInboxRemove,
            lblPolicy, _policy, _move,
            lblFallback, _fallback,
            lblMacroDirs, _macroDirs, btnMacroAdd, btnMacroRemove, btnMacroDetect,
            btnSave, btnOpenConfig, btnOpenRules, btnOpenOverrides,
            _configPath
        });
    }

    private void LoadFromConfig()
    {
        _libraryRoot.Text = App.Config.LibraryRoot;

        _inboxList.Items.Clear();
        foreach (var dir in App.Config.InboxFolders) _inboxList.Items.Add(dir);

        switch (App.Config.PolicyEnum)
        {
            case ConflictPolicy.Skip: _policy.SelectedIndex = 1; break;
            case ConflictPolicy.Overwrite: _policy.SelectedIndex = 2; break;
            default: _policy.SelectedIndex = 0; break;
        }

        _move.Checked = App.Config.MoveInsteadOfCopy;
        _fallback.Text = App.Config.FallbackCategory;

        _macroDirs.Items.Clear();
        foreach (var dir in App.Config.MacroDirectories) _macroDirs.Items.Add(dir);

        _configPath.Text = "Cấu hình: " + App.Paths.ConfigFile;
    }

    private void OnBrowseLibraryClicked(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog { Description = "Chọn thư mục gốc thư viện" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _libraryRoot.Text = dialog.SelectedPath;
        }
    }

    private void OnInboxAddClicked(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog { Description = "Chọn thư mục quét (inbox)" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _inboxList.Items.Add(dialog.SelectedPath);
        }
    }

    private void OnInboxRemoveClicked(object? sender, EventArgs e)
    {
        var selected = _inboxList.SelectedItems.Cast<object>().ToList();
        foreach (var item in selected) _inboxList.Items.Remove(item);
    }

    private void OnMacroDirAddClicked(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog { Description = "Chọn thư mục macro EPLAN" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _macroDirs.Items.Add(dialog.SelectedPath);
        }
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        App.Config.LibraryRoot = _libraryRoot.Text.Trim();
        App.Config.InboxFolders = _inboxList.Items.Cast<object>().Select(o => o?.ToString() ?? "").Where(s => s.Length > 0).ToList();
        App.Config.MoveInsteadOfCopy = _move.Checked;

        switch (_policy.SelectedIndex)
        {
            case 1: App.Config.ConflictPolicyName = "Skip"; break;
            case 2: App.Config.ConflictPolicyName = "Overwrite"; break;
            default: App.Config.ConflictPolicyName = "Rename"; break;
        }

        if (!string.IsNullOrWhiteSpace(_fallback.Text)) App.Config.FallbackCategory = _fallback.Text.Trim();

        App.Config.MacroDirectories = _macroDirs.Items.Cast<object>().Select(o => o?.ToString() ?? "").Where(s => s.Length > 0).ToList();

        App.Config.Save(App.Paths.ConfigFile);
        App.Reload();
        LoadFromConfig();

        MessageBox.Show(this, "Đã lưu cài đặt vào:\n" + App.Paths.ConfigFile, "Tool Oumijia",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void OpenFolder(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            Directory.CreateDirectory(path);
            Process.Start("explorer.exe", path);
        }
        catch
        {
            // Best-effort.
        }
    }

    private static void OpenFileInExplorer(string path)
    {
        try
        {
            if (!File.Exists(path)) File.WriteAllText(path, "{}");
            Process.Start("explorer.exe", "/select,\"" + path + "\"");
        }
        catch
        {
            // Best-effort.
        }
    }
}
