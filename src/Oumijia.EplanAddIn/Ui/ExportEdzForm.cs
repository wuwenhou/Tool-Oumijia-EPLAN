using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Eplan.EplApi.MasterData;

namespace Oumijia.EplanAddIn.Ui
{
    /// <summary>
    /// Xuất part ra EDZ — chọn part bằng CÂY NHÓM kiểu navigator EPLAN
    /// (Nhóm sản phẩm → nhóm → [phân nhóm] → hãng → part), tick chọn, tìm kiếm,
    /// xuất 1 file hoặc mỗi part 1 file. Dữ liệu qua API chính thức (ExportParts/EDZ).
    /// </summary>
    internal sealed class ExportEdzForm : Form
    {
        private const int ImageUnchecked = 0;
        private const int ImageChecked = 1;
        private const int ImageMixed = 2;

        private readonly ImageList _stateImages = CreateStateImages();

        private readonly Label _dbLabel = new Label();
        private readonly TextBox _searchBox = new TextBox();
        private readonly TreeView _tree = new TreeView();
        private readonly Label _hintLabel = new Label();
        private readonly Label _countLabel = new Label();
        private readonly RadioButton _outputSingle = new RadioButton();
        private readonly RadioButton _outputPerPart = new RadioButton();
        private readonly TextBox _target = new TextBox();
        private readonly Button _browseButton = new Button();
        private readonly TextBox _log = new TextBox();
        private readonly Button _exportButton = new Button();
        private readonly Timer _searchTimer = new Timer();

        private MDPartsManagement _management;
        private MDPartsDatabase _database;
        private List<PartEntry> _entries = new List<PartEntry>();
        private readonly HashSet<int> _checkedIds = new HashSet<int>();
        private string _searchText = string.Empty;

        public ExportEdzForm()
        {
            Text = "Tool Oumijia — Xuất EDZ (chọn part cần xuất)";
            ClientSize = new Size(900, 660);
            MinimumSize = new Size(760, 560);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);

            BuildUi();
        }

        // ------------------------------------------------------------------ UI

        private void BuildUi()
        {
            // ----- hàng 1: database + tìm kiếm + nút
            var topPanel = new Panel { Dock = DockStyle.Top, Height = 86 };

            _dbLabel.Dock = DockStyle.Top;
            _dbLabel.Height = 24;
            _dbLabel.Padding = new Padding(4, 4, 0, 0);
            _dbLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            var searchRow = new Panel { Dock = DockStyle.Top, Height = 34 };

            var lblSearch = new Label { Text = "Tìm:", AutoSize = true, Location = new Point(4, 8) };
            _searchBox.Location = new Point(40, 4);
            _searchBox.Width = 240;
            _searchBox.PlaceholderTextSafe("mã hàng / tên / hãng…");

            var btnSelectAll = new Button { Text = "Chọn tất cả", Location = new Point(292, 3), Size = new Size(96, 26) };
            btnSelectAll.Click += OnSelectAllClicked;
            var btnClear = new Button { Text = "Bỏ chọn", Location = new Point(394, 3), Size = new Size(80, 26) };
            btnClear.Click += OnClearAllClicked;
            var btnFromPartsMgmt = new Button { Text = "Lấy từ Parts Management ▸", Location = new Point(480, 3), Size = new Size(190, 26) };
            btnFromPartsMgmt.Click += OnUseSelectionClicked;

            searchRow.Controls.Add(lblSearch);
            searchRow.Controls.Add(_searchBox);
            searchRow.Controls.Add(btnSelectAll);
            searchRow.Controls.Add(btnClear);
            searchRow.Controls.Add(btnFromPartsMgmt);

            _hintLabel.Dock = DockStyle.Top;
            _hintLabel.Height = 20;
            _hintLabel.Padding = new Padding(4, 0, 0, 0);
            _hintLabel.ForeColor = Color.DimGray;
            _hintLabel.Text = "Mẹo: tick ô vuông trước mỗi mục để chọn; tick vào nhóm = chọn cả nhánh; gõ vào ô Tìm để lọc nhanh.";

            topPanel.Controls.Add(_hintLabel);
            topPanel.Controls.Add(searchRow);
            topPanel.Controls.Add(_dbLabel);

            // ----- cây
            _tree.Dock = DockStyle.Fill;
            _tree.BorderStyle = BorderStyle.FixedSingle;
            _tree.HideSelection = false;
            _tree.ShowLines = true;
            _tree.ShowRootLines = true;
            _tree.ItemHeight = 20;
            _tree.ImageList = _stateImages;
            _tree.MouseDown += OnTreeMouseDown;
            EnableDoubleBuffering(_tree);

            // ----- đáy: tùy chọn xuất + đếm + log
            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 250 };

            var outputGroup = new GroupBox { Dock = DockStyle.Top, Height = 90, Text = "Cách xuất" };

            _outputSingle.Text = "Một file EDZ duy nhất:";
            _outputSingle.AutoSize = true;
            _outputSingle.Location = new Point(12, 22);
            _outputSingle.Checked = true;
            _outputSingle.CheckedChanged += (s, e) => UpdateTargetHint();

            _target.Location = new Point(178, 20);
            _target.Width = 500;
            _target.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            _browseButton.Text = "Chọn…";
            _browseButton.Location = new Point(690, 18);
            _browseButton.Size = new Size(96, 25);
            _browseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _browseButton.Click += OnBrowseClicked;

            _outputPerPart.Text = "Mỗi part một file EDZ (xuất vào thư mục):";
            _outputPerPart.AutoSize = true;
            _outputPerPart.Location = new Point(12, 54);
            _outputPerPart.CheckedChanged += (s, e) => UpdateTargetHint();

            outputGroup.Controls.Add(_outputSingle);
            outputGroup.Controls.Add(_target);
            outputGroup.Controls.Add(_browseButton);
            outputGroup.Controls.Add(_outputPerPart);

            var actionPanel = new Panel { Dock = DockStyle.Top, Height = 40 };

            _countLabel.Text = "Đã chọn: 0 part";
            _countLabel.AutoSize = true;
            _countLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _countLabel.Location = new Point(12, 10);

            _exportButton.Text = "XUẤT RA EDZ ▸";
            _exportButton.Size = new Size(150, 32);
            _exportButton.Location = new Point(730, 4);
            _exportButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _exportButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _exportButton.Enabled = false;
            _exportButton.Click += OnExportClicked;

            actionPanel.Controls.Add(_countLabel);
            actionPanel.Controls.Add(_exportButton);

            _log.Dock = DockStyle.Fill;
            _log.Multiline = true;
            _log.ReadOnly = true;
            _log.ScrollBars = ScrollBars.Both;
            _log.WordWrap = false;
            _log.Font = new Font("Consolas", 8.5F);

            bottomPanel.Controls.Add(_log);
            bottomPanel.Controls.Add(actionPanel);
            bottomPanel.Controls.Add(outputGroup);

            Controls.Add(_tree);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);

            // ----- tìm kiếm có debounce nhẹ
            _searchTimer.Interval = 250;
            _searchTimer.Tick += (s, e) =>
            {
                _searchTimer.Stop();
                var text = (_searchBox.Text ?? string.Empty).Trim();
                if (text == _searchText) return;
                _searchText = text;
                RebuildTree();
            };
            _searchBox.TextChanged += (s, e) =>
            {
                _searchTimer.Stop();
                _searchTimer.Start();
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stateImages.Dispose();
                _searchTimer.Dispose();
            }
            base.Dispose(disposing);
        }

        private static ImageList CreateStateImages()
        {
            var list = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
            list.Images.Add(DrawStateImage(0)); // unchecked
            list.Images.Add(DrawStateImage(1)); // checked
            list.Images.Add(DrawStateImage(2)); // mixed
            return list;
        }

        private static Bitmap DrawStateImage(int kind)
        {
            var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                using (var fill = new SolidBrush(Color.White))
                using (var border = new Pen(Color.FromArgb(130, 130, 130)))
                {
                    g.FillRectangle(fill, 2, 2, 11, 11);
                    g.DrawRectangle(border, 2, 2, 11, 11);
                }

                if (kind == 1)
                {
                    using (var pen = new Pen(Color.FromArgb(0, 120, 60), 2f))
                    {
                        g.DrawLines(pen, new[]
                        {
                            new Point(4, 8),
                            new Point(7, 11),
                            new Point(12, 5)
                        });
                    }
                }
                else if (kind == 2)
                {
                    using (var brush = new SolidBrush(Color.FromArgb(0, 120, 60)))
                    {
                        g.FillRectangle(brush, 4, 6, 7, 3);
                    }
                }
            }
            return bitmap;
        }

        private static void EnableDoubleBuffering(Control control)
        {
            try
            {
                typeof(Control)
                    .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(control, true, null);
            }
            catch
            {
                // Không bắt buộc — chỉ là tối ưu vẽ.
            }
        }

        // ------------------------------------------------------------------ nạp dữ liệu

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            LoadDatabase();
        }

        private void LoadDatabase()
        {
            try
            {
                _management = new MDPartsManagement();
                _database = _management.SelectedPartsDatabase;
            }
            catch (Exception ex)
            {
                AddinLog.Write("Export: đọc SelectedPartsDatabase lỗi: " + ex.Message);
            }

            if (_database == null)
            {
                MessageBox.Show(this,
                    "Chưa có Parts Database nào đang mở trong phiên EPLAN này.\n\n" +
                    "Hãy mở: Master data → Parts → Management (chọn database) rồi thực hiện lại.",
                    "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Close();
                return;
            }

            UseWaitCursor = true;
            var dbName = SafeDatabaseName();
            _dbLabel.Text = "Database: " + dbName + " — đang tải danh sách part…";
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var parts = new List<MDPart>(_database.Parts);
                var enumMs = stopwatch.ElapsedMilliseconds;

                stopwatch.Restart();
                _entries = new List<PartEntry>(parts.Count);
                foreach (var part in parts)
                {
                    _entries.Add(PartEntry.FromPart(part));
                }
                stopwatch.Stop();

                AddinLog.Write("Export: nạp " + parts.Count + " part (enum=" + enumMs +
                               " ms, đọc property=" + stopwatch.ElapsedMilliseconds + " ms)");

                _dbLabel.Text = "Database: " + dbName + " — " + parts.Count + " part";
                RebuildTree();
                Log("Đã nạp " + parts.Count + " part từ database '" + dbName + "'.");
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("Export: nạp danh sách part", ex);
                MessageBox.Show(this, "Không đọc được danh sách part:\n" + ex.Message,
                    "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        private string SafeDatabaseName()
        {
            try
            {
                var name = MDPartsManagement.SelectedPartsDatabaseAsString;
                return string.IsNullOrWhiteSpace(name) ? "(không rõ)" : name.Trim();
            }
            catch
            {
                return "(không rõ)";
            }
        }

        // ------------------------------------------------------------------ cây

        private void RebuildTree()
        {
            var filtered = string.IsNullOrEmpty(_searchText)
                ? _entries
                : _entries.Where(x => x.Matches(_searchText)).ToList();

            var model = PartsTreeModel.Build(filtered);

            _tree.BeginUpdate();
            try
            {
                _tree.Nodes.Clear();
                foreach (var child in model.Root.Children.Values)
                {
                    _tree.Nodes.Add(CreateTreeNode(child));
                }
            }
            finally
            {
                _tree.EndUpdate();
            }

            if (_searchText.Length > 0)
            {
                _tree.ExpandAll();
            }
            else
            {
                foreach (TreeNode top in _tree.Nodes)
                {
                    top.Expand();
                }
            }

            UpdateCount();
        }

        private TreeNode CreateTreeNode(PartsTreeModel.Node node)
        {
            var treeNode = new TreeNode(node.Name) { Tag = node };

            foreach (var entry in node.Entries)
            {
                treeNode.Nodes.Add(new TreeNode(entry.Label) { Tag = entry });
            }
            foreach (var child in node.Children.Values)
            {
                treeNode.Nodes.Add(CreateTreeNode(child));
            }

            RefreshNodeState(treeNode);
            return treeNode;
        }

        /// <summary>Tính lại trạng thái (ảnh tick) của một node từ dữ liệu bên dưới — gọi theo thứ tự lá → gốc.</summary>
        private void RefreshNodeState(TreeNode node)
        {
            if (node.Tag is PartEntry entry)
            {
                node.ImageIndex = _checkedIds.Contains(entry.Part.AdoId) ? ImageChecked : ImageUnchecked;
                return;
            }

            var total = node.Nodes.Count;
            var checkedCount = 0;
            foreach (TreeNode child in node.Nodes)
            {
                if (child.ImageIndex == ImageChecked) checkedCount++;
            }

            node.ImageIndex = checkedCount == 0
                ? ImageUnchecked
                : (checkedCount == total ? ImageChecked : ImageMixed);
        }

        private void RefreshTreeStatesDeep(TreeNode node)
        {
            foreach (TreeNode child in node.Nodes)
            {
                RefreshTreeStatesDeep(child);
            }
            RefreshNodeState(node);
        }

        private void RefreshAncestors(TreeNode node)
        {
            var parent = node.Parent;
            while (parent != null)
            {
                RefreshNodeState(parent);
                parent = parent.Parent;
            }
        }

        private void SetCheckedForSubtree(TreeNode node, bool value)
        {
            if (node.Tag is PartEntry entry)
            {
                if (value) _checkedIds.Add(entry.Part.AdoId);
                else _checkedIds.Remove(entry.Part.AdoId);
            }

            foreach (TreeNode child in node.Nodes)
            {
                SetCheckedForSubtree(child, value);
            }

            RefreshNodeState(node);
        }

        private void OnTreeMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            var hit = _tree.HitTest(e.Location);
            if (hit.Node == null) return;

            // Chỉ toggle khi bấm đúng vào ô vuông (ảnh trạng thái) — bấm chữ chỉ chọn dòng.
            if (hit.Location == TreeViewHitTestLocations.Image)
            {
                var node = hit.Node;
                bool turnOn;
                if (node.Tag is PartEntry entry)
                {
                    turnOn = !_checkedIds.Contains(entry.Part.AdoId);
                }
                else
                {
                    turnOn = node.ImageIndex != ImageChecked;
                }

                SetCheckedForSubtree(node, turnOn);
                RefreshAncestors(node);
                UpdateCount();
            }
        }

        private void OnSelectAllClicked(object sender, EventArgs e)
        {
            foreach (TreeNode top in _tree.Nodes)
            {
                SetCheckedForSubtree(top, true);
            }
            UpdateCount();
            Log("Đã chọn tất cả part đang hiển thị.");
        }

        private void OnClearAllClicked(object sender, EventArgs e)
        {
            _checkedIds.Clear();
            foreach (TreeNode top in _tree.Nodes)
            {
                RefreshTreeStatesDeep(top);
            }
            UpdateCount();
            Log("Đã bỏ chọn tất cả.");
        }

        private void OnUseSelectionClicked(object sender, EventArgs e)
        {
            if (_management == null)
            {
                return;
            }

            var added = 0;
            try
            {
                foreach (var item in _management.GetSelectedPartDatabaseItems(false))
                {
                    var part = item as MDPart;
                    if (part == null) continue;
                    if (_checkedIds.Add(part.AdoId)) added++;
                }
            }
            catch (Exception ex)
            {
                Log("Không đọc được danh sách đang chọn trong Parts Management: " + ex.Message);
            }

            foreach (TreeNode top in _tree.Nodes)
            {
                RefreshTreeStatesDeep(top);
            }
            UpdateCount();
            Log("Lấy " + added + " part từ Parts Management (tổng đã chọn: " + _checkedIds.Count + ").");
        }

        private void UpdateCount()
        {
            _countLabel.Text = "Đã chọn: " + _checkedIds.Count + " part";
            _exportButton.Enabled = _checkedIds.Count > 0;
        }

        // ------------------------------------------------------------------ xuất

        private void UpdateTargetHint()
        {
            // Placeholder động (giữ trống nếu người dùng đã gõ) — chỉ cập nhật gợi ý.
            if (string.IsNullOrWhiteSpace(_target.Text))
            {
                _target.PlaceholderTextSafe(_outputSingle.Checked ? "ví dụ: D:\\Export\\parts.edz" : "ví dụ: D:\\Export\\EDZ");
            }
        }

        private void OnBrowseClicked(object sender, EventArgs e)
        {
            if (_outputSingle.Checked)
            {
                using (var dialog = new SaveFileDialog())
                {
                    dialog.Title = "Chọn file EDZ đích";
                    dialog.Filter = "Gói EPLAN (*.edz)|*.edz";
                    dialog.FileName = "parts.edz";
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        _target.Text = dialog.FileName;
                    }
                }
            }
            else
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.Description = "Chọn thư mục đích (mỗi part một file .edz)";
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        _target.Text = dialog.SelectedPath;
                    }
                }
            }
        }

        private void OnExportClicked(object sender, EventArgs e)
        {
            var selected = _entries.Where(x => _checkedIds.Contains(x.Part.AdoId)).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show(this, "Chưa chọn part nào để xuất.", "Tool Oumijia",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var target = (_target.Text ?? string.Empty).Trim();
            if (target.Length == 0)
            {
                MessageBox.Show(this, "Chưa chọn đích xuất (file hoặc thư mục).", "Tool Oumijia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_outputSingle.Checked && !target.EndsWith(".edz", StringComparison.OrdinalIgnoreCase))
            {
                target += ".edz";
            }

            var confirm = MessageBox.Show(this,
                "Xuất " + selected.Count + " part ra EDZ?\n\n" +
                "Đích: " + target + "\n" +
                "Cách xuất: " + (_outputSingle.Checked ? "1 file duy nhất" : "mỗi part 1 file"),
                "Tool Oumijia", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            _exportButton.Enabled = false;
            UseWaitCursor = true;
            try
            {
                if (_outputSingle.Checked)
                {
                    var parts = selected.Select(x => x.Part).ToList();
                    Log("Xuất " + parts.Count + " part → " + target);
                    _database.ExportParts(target, MDPartsDatabase.DataFormat.EDZ, parts);
                    Log("Hoàn tất.");
                    AddinLog.Write("Export single OK: " + parts.Count + " parts -> " + target);
                    AddinCsvLog.Append("export", "OK-single", target, parts.Count.ToString());

                    MessageBox.Show(this, "Đã xuất " + parts.Count + " part ra:\n" + target,
                        "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    Directory.CreateDirectory(target);
                    int ok = 0, failed = 0;

                    foreach (var entry in selected)
                    {
                        var name = entry.Number.Length > 0 ? entry.Number : "part";
                        try
                        {
                            var filePath = Path.Combine(target, SanitizeFileName(name) + ".edz");
                            _database.ExportParts(filePath, MDPartsDatabase.DataFormat.EDZ, new[] { entry.Part });
                            ok++;
                            Log("OK: " + Path.GetFileName(filePath));
                            AddinCsvLog.Append("export", "OK", filePath);
                        }
                        catch (Exception exPart)
                        {
                            failed++;
                            Log("LỖI " + name + ": " + exPart.Message);
                            AddinCsvLog.Append("export", "ERROR", name, exPart.Message);
                        }
                    }

                    AddinLog.Write("Export per-part done: ok=" + ok + ", failed=" + failed + ", dir=" + target);
                    MessageBox.Show(this,
                        "Hoàn tất xuất từng part:\n• Thành công: " + ok + "\n• Lỗi: " + failed +
                        "\n\nThư mục: " + target,
                        "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Log("LỖI: " + ex.Message);
                AddinLog.WriteError("Export", ex);
                AddinCsvLog.Append("export", "ERROR", ex.Message);
                MessageBox.Show(this, "Xuất thất bại:\n" + ex.Message, "Tool Oumijia",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                _exportButton.Enabled = _checkedIds.Count > 0;
            }
        }

        private static string SanitizeFileName(string name)
        {
            var value = string.IsNullOrWhiteSpace(name) ? "part" : name.Trim();
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }
            return value;
        }

        // ------------------------------------------------------------------ log

        private void Log(string message)
        {
            try
            {
                _log.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + message + Environment.NewLine);
            }
            catch
            {
                // Không bao giờ để log làm hỏng thao tác.
            }
        }
    }

    /// <summary>PlaceholderText chỉ có ở .NET 5+; helper này để dùng an toàn trên net48.</summary>
    internal static class TextBoxPlaceholderExtensions
    {
        public static void PlaceholderTextSafe(this TextBox box, string text)
        {
            try
            {
                var property = typeof(TextBox).GetProperty("PlaceholderText",
                    BindingFlags.Instance | BindingFlags.Public);
                if (property != null)
                {
                    property.SetValue(box, text, null);
                    return;
                }
            }
            catch
            {
                // fallthrough
            }

            // net48: không có PlaceholderText — bỏ qua (giữ UI gọn).
        }
    }
}
