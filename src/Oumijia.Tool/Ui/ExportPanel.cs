using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Oumijia.Core.Classification;
using Oumijia.Core.Edz;
using Oumijia.Core.Library;

namespace Oumijia.Tool.Ui;

/// <summary>
/// "Export" tab: scans inbox folders for .edz / .ema files, classifies them
/// (manufacturer / category) and files the selection into the managed library tree.
/// </summary>
public sealed class ExportPanel : UserControl
{
    private readonly DataGridView _grid = new();
    private readonly ListBox _inboxList = new();
    private readonly TextBox _log = new();
    private readonly CheckBox _recursive = new();
    private readonly Label _summary = new();
    private readonly Button _scanButton = new();
    private readonly BindingList<ExportRow> _rows = new();
    private readonly OverrideStore _overrides;

    public ExportPanel()
    {
        _overrides = OverrideStore.Load(App.Paths.OverridesFile);
        BuildUi();
        RefreshInboxList();
    }

    private void BuildUi()
    {
        Dock = DockStyle.Fill;
        Padding = new Padding(8);

        // ---------- top: inbox folders + scan ----------
        var top = new Panel { Dock = DockStyle.Top, Height = 86 };

        var lblInbox = new Label { Text = "Thư mục quét (inbox):", AutoSize = true, Location = new Point(0, 6) };

        _inboxList.Location = new Point(0, 28);
        _inboxList.Size = new Size(430, 52);
        _inboxList.IntegralHeight = false;
        _inboxList.SelectionMode = SelectionMode.MultiExtended;

        var btnAddInbox = new Button { Text = "Thêm…", Location = new Point(438, 28), Size = new Size(84, 24) };
        btnAddInbox.Click += OnAddInboxClicked;
        var btnRemoveInbox = new Button { Text = "Xóa", Location = new Point(438, 56), Size = new Size(84, 24) };
        btnRemoveInbox.Click += OnRemoveInboxClicked;

        _scanButton.Text = "🔍 Quét ngay";
        _scanButton.Location = new Point(532, 28);
        _scanButton.Size = new Size(120, 52);
        _scanButton.Click += OnScanClicked;

        _recursive.Text = "Gồm thư mục con";
        _recursive.AutoSize = true;
        _recursive.Location = new Point(664, 30);

        var btnOpenLibrary = new Button { Text = "Mở thư viện", Location = new Point(664, 56), Size = new Size(120, 24) };
        btnOpenLibrary.Click += (_, __) => OpenFolder(App.Config.LibraryRoot);

        top.Controls.AddRange(new Control[] { lblInbox, _inboxList, btnAddInbox, btnRemoveInbox, _scanButton, _recursive, btnOpenLibrary });

        // ---------- middle: grid + log ----------
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
        Load += (_, __) =>
        {
            try
            {
                if (split.Height > 320) split.SplitterDistance = Math.Max(200, split.Height - 220);
            }
            catch
            {
                // Splitter distance is cosmetic; ignore layout races.
            }
        };

        ConfigureGrid();

        var bottomBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 38,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 0)
        };

        var btnSelectAll = new Button { Text = "Chọn tất cả", Width = 96, Height = 26 };
        btnSelectAll.Click += (_, __) => SetAllSelected(true);
        var btnSelectNone = new Button { Text = "Bỏ chọn", Width = 80, Height = 26 };
        btnSelectNone.Click += (_, __) => SetAllSelected(false);
        var btnReapply = new Button { Text = "Áp dụng lại quy tắc", Width = 140, Height = 26 };
        btnReapply.Click += OnReapplyClicked;
        var btnSaveOverrides = new Button { Text = "Lưu đích đã sửa", Width = 120, Height = 26 };
        btnSaveOverrides.Click += OnSaveOverridesClicked;

        var btnExport = new Button { Text = "XUẤT ▸", Width = 130, Height = 30, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        btnExport.Click += OnExportClicked;

        _summary.AutoSize = true;
        _summary.Padding = new Padding(12, 6, 0, 0);

        bottomBar.Controls.Add(btnSelectAll);
        bottomBar.Controls.Add(btnSelectNone);
        bottomBar.Controls.Add(btnReapply);
        bottomBar.Controls.Add(btnSaveOverrides);
        bottomBar.Controls.Add(btnExport);
        bottomBar.Controls.Add(_summary);

        var logPanel = new Panel { Dock = DockStyle.Fill };
        _log.Dock = DockStyle.Fill;
        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = ScrollBars.Both;
        _log.WordWrap = false;
        _log.Font = new Font("Consolas", 8.5F);
        logPanel.Controls.Add(_log);

        split.Panel1.Controls.Add(_grid);
        split.Panel2.Controls.Add(logPanel);
        split.Panel2.Controls.Add(bottomBar);

        Controls.Add(split);
        Controls.Add(top);
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = true;
        _grid.ReadOnly = false;

        var colSelected = new DataGridViewCheckBoxColumn
        {
            HeaderText = "Chọn",
            Width = 46,
            DataPropertyName = nameof(ExportRow.Selected),
            ReadOnly = false,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
        var colFile = new DataGridViewTextBoxColumn { HeaderText = "Tệp", Width = 230, DataPropertyName = nameof(ExportRow.FileName), ReadOnly = true };
        var colType = new DataGridViewTextBoxColumn { HeaderText = "Loại", Width = 52, DataPropertyName = nameof(ExportRow.TypeText), ReadOnly = true };
        var colManufacturer = new DataGridViewTextBoxColumn { HeaderText = "Hãng", Width = 130, DataPropertyName = nameof(ExportRow.ManufacturerFolder), ReadOnly = true };
        var colCategory = new DataGridViewTextBoxColumn { HeaderText = "Nhóm", Width = 100, DataPropertyName = nameof(ExportRow.CategoryFolder), ReadOnly = true };
        var colPart = new DataGridViewTextBoxColumn { HeaderText = "Mã hàng", Width = 150, DataPropertyName = nameof(ExportRow.PartNumber), ReadOnly = true };
        var colDest = new DataGridViewTextBoxColumn
        {
            HeaderText = "Đến thư mục (sửa được)",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 170,
            DataPropertyName = nameof(ExportRow.DestinationFolder),
            ReadOnly = false
        };
        var colStatus = new DataGridViewTextBoxColumn { HeaderText = "Trạng thái", Width = 260, DataPropertyName = nameof(ExportRow.Status), ReadOnly = true };

        _grid.Columns.AddRange(colSelected, colFile, colType, colManufacturer, colCategory, colPart, colDest, colStatus);
        _grid.DataSource = _rows;
    }

    // ------------------------------------------------------------------ inbox

    private void RefreshInboxList()
    {
        _inboxList.Items.Clear();
        foreach (var dir in App.Config.InboxFolders)
        {
            _inboxList.Items.Add(dir);
        }
    }

    private void OnAddInboxClicked(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog { Description = "Chọn thư mục cần quét (.edz / .ema)" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var path = dialog.SelectedPath;
        if (!App.Config.InboxFolders.Any(x => x.Equals(path, StringComparison.OrdinalIgnoreCase)))
        {
            App.Config.InboxFolders.Add(path);
            App.Config.Save(App.Paths.ConfigFile);
            RefreshInboxList();
        }
    }

    private void OnRemoveInboxClicked(object? sender, EventArgs e)
    {
        var toRemove = _inboxList.SelectedItems.Cast<string>().ToList();
        if (toRemove.Count == 0) return;
        foreach (var item in toRemove)
        {
            App.Config.InboxFolders.RemoveAll(x => x.Equals(item, StringComparison.OrdinalIgnoreCase));
        }
        App.Config.Save(App.Paths.ConfigFile);
        RefreshInboxList();
    }

    // ------------------------------------------------------------------- scan

    private async void OnScanClicked(object? sender, EventArgs e)
    {
        var inboxes = App.Config.InboxFolders.Where(Directory.Exists).ToList();
        if (inboxes.Count == 0)
        {
            MessageBox.Show(this,
                "Chưa có thư mục quét hợp lệ. Hãy thêm thư mục (nút \"Thêm…\") hoặc cấu hình trong tab Cài đặt.",
                "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _scanButton.Enabled = false;
        Log("— Bắt đầu quét " + inboxes.Count + " thư mục…");
        try
        {
            var extensions = App.Config.ScanExtensions.ToList();
            var recursive = _recursive.Checked;
            var rows = await Task.Run(() => ScanFiles(inboxes, extensions, recursive));

            _rows.Clear();
            foreach (var row in rows) _rows.Add(row);

            Log("Tìm thấy " + rows.Count + " tệp.");
            UpdateSummary();
        }
        catch (Exception ex)
        {
            Log("LỖI quét: " + ex.Message);
        }
        finally
        {
            _scanButton.Enabled = true;
        }
    }

    private List<ExportRow> ScanFiles(List<string> inboxes, List<string> extensions, bool recursive)
    {
        var results = new List<ExportRow>();
        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        foreach (var dir in inboxes)
        {
            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(dir, "*", option);
            }
            catch
            {
                continue;
            }

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file);
                if (!extensions.Any(x => x.Equals(ext, StringComparison.OrdinalIgnoreCase))) continue;
                try
                {
                    results.Add(BuildRow(file));
                }
                catch (Exception ex)
                {
                    results.Add(new ExportRow
                    {
                        FullPath = file,
                        FileName = Path.GetFileName(file),
                        TypeText = ext.TrimStart('.'),
                        Selected = false,
                        Status = "Lỗi: " + ex.Message
                    });
                }
            }
        }

        results.Sort((a, b) => string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase));
        return results;
    }

    private ExportRow BuildRow(string file)
    {
        var fileName = Path.GetFileName(file);
        var ext = Path.GetExtension(file).ToLowerInvariant();

        var row = new ExportRow
        {
            FullPath = file,
            FileName = fileName,
            TypeText = ext.TrimStart('.'),
            Selected = true
        };

        EdzPackageInfo? info = null;
        if (ext == ".edz")
        {
            try
            {
                info = EdzPackageReader.Read(file);
                row.Status = info.Summary() + " · " + info.ContainerKind;
            }
            catch (Exception ex)
            {
                row.Status = "Lỗi đọc: " + ex.Message;
            }
        }
        else if (EdzPackageReader.IsMacroExtension(ext))
        {
            row.Status = "Tệp macro EPLAN (phân loại thủ công)";
        }
        else
        {
            row.Status = "Không nhận diện";
        }

        var classification = App.Classifier.Classify(file, info);
        row.Classification = classification;
        row.Info = info;

        var isMacro = EdzPackageReader.IsMacroExtension(ext);
        if (isMacro)
        {
            row.ManufacturerFolder = "-";
            row.CategoryFolder = "-";
        }
        else
        {
            row.ManufacturerFolder = classification.ManufacturerFolder;
            row.CategoryFolder = classification.CategoryFolder;
            row.PartNumber = info?.PrimaryPartNumber ?? "";
        }

        var suggested = isMacro
            ? "_Macros"
            : (info != null ? classification.SuggestedRelativeFolder() : App.Config.FallbackCategory);

        var overrideFolder = _overrides.Get(fileName);
        row.SuggestedDestination = suggested;
        row.DestinationFolder = string.IsNullOrWhiteSpace(overrideFolder) ? suggested : overrideFolder!;
        return row;
    }

    // -------------------------------------------------------- row bulk actions

    private void SetAllSelected(bool selected)
    {
        foreach (var row in _rows) row.Selected = selected;
        UpdateSummary();
    }

    private void OnReapplyClicked(object? sender, EventArgs e)
    {
        foreach (var row in _rows)
        {
            var isMacro = EdzPackageReader.IsMacroExtension(Path.GetExtension(row.FullPath));
            if (isMacro)
            {
                row.SuggestedDestination = "_Macros";
            }
            else
            {
                var classification = App.Classifier.Classify(row.FullPath, row.Info);
                row.ManufacturerFolder = classification.ManufacturerFolder;
                row.CategoryFolder = classification.CategoryFolder;
                row.SuggestedDestination = row.Info != null
                    ? classification.SuggestedRelativeFolder()
                    : App.Config.FallbackCategory;
            }

            var overrideFolder = _overrides.Get(row.FileName);
            row.DestinationFolder = string.IsNullOrWhiteSpace(overrideFolder) ? row.SuggestedDestination : overrideFolder!;
        }

        Log("Đã áp dụng lại quy tắc phân loại cho " + _rows.Count + " tệp.");
    }

    private void OnSaveOverridesClicked(object? sender, EventArgs e)
    {
        var count = 0;
        foreach (var row in _rows)
        {
            if (!string.IsNullOrWhiteSpace(row.DestinationFolder) &&
                !string.Equals(row.DestinationFolder, row.SuggestedDestination, StringComparison.OrdinalIgnoreCase))
            {
                _overrides.Set(row.FileName, row.DestinationFolder);
                count++;
            }
        }

        _overrides.Save();
        MessageBox.Show(this,
            "Đã lưu " + count + " đích đến tùy chỉnh.\nLần quét sau, các tệp cùng tên sẽ được đề xuất đúng như bạn đã sửa.\n\n" + _overrides.FilePath,
            "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ----------------------------------------------------------------- export

    private void OnExportClicked(object? sender, EventArgs e)
    {
        var selected = _rows.Where(r => r.Selected).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show(this, "Chưa chọn tệp nào để xuất.", "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var root = App.Config.LibraryRoot;
        if (string.IsNullOrWhiteSpace(root))
        {
            MessageBox.Show(this, "Chưa cấu hình thư mục thư viện (tab Cài đặt).", "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var writer = new LibraryWriter(root, App.Config.PolicyEnum, App.Config.MoveInsteadOfCopy);
        var logPath = App.Paths.NewExportLogPath();

        int added = 0, renamed = 0, overwritten = 0, skipped = 0, errors = 0;

        using (var csv = new CsvLog(logPath, new[]
               {
                   "Thời gian", "Nguồn", "Đích", "Hành động", "Hãng", "Nhóm", "Mã hàng", "Kích thước (bytes)", "SHA256"
               }))
        {
            foreach (var row in selected)
            {
                var result = writer.Write(row.FullPath, row.DestinationFolder);
                row.Status = result.StatusText + " — " + result.Message;

                switch (result.Status)
                {
                    case LibraryWriteStatus.New: added++; break;
                    case LibraryWriteStatus.Renamed: renamed++; break;
                    case LibraryWriteStatus.Overwritten: overwritten++; break;
                    case LibraryWriteStatus.SkippedIdentical:
                    case LibraryWriteStatus.SkippedExisting: skipped++; break;
                    default: errors++; break;
                }

                Log("[" + result.StatusText + "] " + row.FileName + " → " + result.DestinationPath);

                csv.Append(
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    result.SourcePath,
                    result.DestinationPath,
                    result.StatusText,
                    row.ManufacturerFolder,
                    row.CategoryFolder,
                    row.PartNumber,
                    SafeSize(result.SourcePath),
                    result.Sha256);
            }
        }

        UpdateSummary();
        MessageBox.Show(this,
            "Hoàn tất xuất thư viện:\n" +
            "• Thêm mới: " + added + "\n" +
            "• Đổi tên (trùng): " + renamed + "\n" +
            "• Ghi đè: " + overwritten + "\n" +
            "• Bỏ qua: " + skipped + "\n" +
            "• Lỗi: " + errors + "\n\n" +
            "Log: " + logPath,
            "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ------------------------------------------------------------------ misc

    private void UpdateSummary()
    {
        _summary.Text = _rows.Count + " tệp · đã chọn " + _rows.Count(r => r.Selected);
    }

    private void Log(string message)
    {
        try
        {
            _log.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + message + Environment.NewLine);
        }
        catch
        {
            // Logging must never crash the UI.
        }
    }

    private static string SafeSize(string path)
    {
        try
        {
            return new FileInfo(path).Length.ToString();
        }
        catch
        {
            return "";
        }
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
            // Opening a folder is best-effort.
        }
    }
}

/// <summary>One row in the export grid.</summary>
public sealed class ExportRow : INotifyPropertyChanged
{
    private bool _selected = true;
    private string _destinationFolder = "";
    private string _status = "";
    private string _manufacturerFolder = "";
    private string _categoryFolder = "";

    public bool Selected
    {
        get => _selected;
        set { if (_selected != value) { _selected = value; Raise(nameof(Selected)); } }
    }

    public string FileName { get; set; } = "";
    public string TypeText { get; set; } = "";

    public string ManufacturerFolder
    {
        get => _manufacturerFolder;
        set { if (_manufacturerFolder != value) { _manufacturerFolder = value; Raise(nameof(ManufacturerFolder)); } }
    }

    public string CategoryFolder
    {
        get => _categoryFolder;
        set { if (_categoryFolder != value) { _categoryFolder = value; Raise(nameof(CategoryFolder)); } }
    }

    public string PartNumber { get; set; } = "";

    public string DestinationFolder
    {
        get => _destinationFolder;
        set { if (_destinationFolder != value) { _destinationFolder = value; Raise(nameof(DestinationFolder)); } }
    }

    public string Status
    {
        get => _status;
        set { if (_status != value) { _status = value; Raise(nameof(Status)); } }
    }

    public string FullPath { get; set; } = "";
    public string SuggestedDestination { get; set; } = "";
    public EdzPackageInfo? Info { get; set; }
    public ClassificationResult? Classification { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Raise(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
