using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Eplan.EplApi.MasterData;

namespace Oumijia.EplanUi
{
    /// <summary>
    /// Xuất EDZ — v3:
    ///  - Cây nhóm kiểu navigator EPLAN (tick 3 trạng thái, dòng đã chọn in đậm, tìm kiếm)
    ///  - Panel CHI TIẾT bên phải (thuộc tính part kiểu EPLAN Overview)
    ///  - Hai phạm vi xuất: "Part đã chọn trên cây" hoặc "Theo loại bản ghi (như EPLAN)"
    ///    (Parts / Accessory list / Accessory placement / Drilling pattern /
    ///     Connection point pattern / Customer + Manufacturer/Supplier)
    ///
    /// Cờ API đã kiểm chứng thực tế (giải nén EDZ đối chiếu số liệu database):
    ///   bExportParts               → partxml/*.part.xml
    ///   bExportAddresses           → *.manufacturer.xml (+ customer nếu có)
    ///   bExportConstructions       → *.construction.xml (Drilling pattern) + items/contour
    ///   bExportTerminals           → *.connectionpoints.xml (Connection point pattern)
    ///   bExportAccessoryLists      → *.accessorylist*.xml (Accessory list)
    ///   bExportAccessoryPlacements → *.accessoryplacementlist.xml (Accessory placement)
    /// </summary>
    public sealed class ExportEdzForm : Form
    {
        private const string PartsDefaultFileName = "parts.edz";

        private readonly Font _boldFont = new Font("Segoe UI", 9F, FontStyle.Bold);
        private bool _updatingChecks;

        // ----- hàng trên
        private readonly Label _dbLabel = new Label();
        private readonly TextBox _searchBox = new TextBox();
        private readonly Button _btnSelectAll = new Button();
        private readonly Button _btnClear = new Button();
        private readonly Button _btnFromPartsMgmt = new Button();
        private readonly Label _hintLabel = new Label();

        // ----- giữa: cây + chi tiết
        private readonly TreeView _tree = new TreeView();
        private readonly Label _detailsHeader = new Label();
        private readonly ListView _details = new ListView();
        private SplitContainer _split;

        // ----- tùy chọn xuất
        private readonly GroupBox _outputGroup = new GroupBox();
        private readonly RadioButton _rbTree = new RadioButton();
        private readonly RadioButton _rbRecords = new RadioButton();
        private readonly FlowLayoutPanel _recordsPanel = new FlowLayoutPanel();
        private readonly CheckBox _cbParts = new CheckBox();
        private readonly CheckBox _cbAccessoryLists = new CheckBox();
        private readonly CheckBox _cbAccessoryPlacements = new CheckBox();
        private readonly CheckBox _cbDrilling = new CheckBox();
        private readonly CheckBox _cbConnectionPoints = new CheckBox();
        private readonly CheckBox _cbAddresses = new CheckBox();
        private readonly Label _recordsHint = new Label();
        private readonly RadioButton _rbSingle = new RadioButton();
        private readonly TextBox _fileTarget = new TextBox();
        private readonly Button _browseFile = new Button();
        private readonly RadioButton _rbPerPart = new RadioButton();
        private readonly TextBox _folderTarget = new TextBox();
        private readonly Button _browseFolder = new Button();
        private readonly Label _targetPreview = new Label();

        // ----- đáy
        private readonly Label _countLabel = new Label();
        private readonly Button _exportButton = new Button();
        private readonly TextBox _log = new TextBox();
        private Panel _bottomPanel;

        private readonly Timer _searchTimer = new Timer();

        private MDPartsManagement _management;
        private MDPartsDatabase _database;
        private List<PartEntry> _entries = new List<PartEntry>();
        private readonly HashSet<int> _checkedIds = new HashSet<int>();
        private string _searchText = string.Empty;

        public ExportEdzForm()
        {
            EdzDialogStyle.ApplyForm(this, "Tool Oumijia — Xuất EDZ");
            BuildUi();
        }

        // ================================================================ UI

        private void BuildUi()
        {
            // ----- hàng trên
            var topPanel = new Panel { Dock = DockStyle.Top, Height = 96, Padding = new Padding(8, 6, 8, 0) };

            _dbLabel.Text = "Parts Database: …";
            EdzDialogStyle.ApplyHeader(_dbLabel);

            var searchRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 34,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 0)
            };

            var lblSearch = new Label { Text = "Tìm:", AutoSize = true, Margin = new Padding(0, 7, 4, 0) };
            _searchBox.Width = 240;
            _searchBox.Margin = new Padding(0, 4, 8, 0);

            _btnSelectAll.Text = "Chọn tất cả";
            _btnSelectAll.Size = new Size(96, EdzDialogStyle.ToolbarButtonHeight);
            _btnSelectAll.Click += OnSelectAllClicked;

            _btnClear.Text = "Bỏ chọn";
            _btnClear.Size = new Size(80, EdzDialogStyle.ToolbarButtonHeight);
            _btnClear.Click += OnClearAllClicked;

            _btnFromPartsMgmt.Text = "Lấy từ Parts Management ▸";
            _btnFromPartsMgmt.Size = new Size(190, EdzDialogStyle.ToolbarButtonHeight);
            _btnFromPartsMgmt.Click += OnUseSelectionClicked;

            EdzDialogStyle.ApplyToolbarButton(_btnSelectAll);
            EdzDialogStyle.ApplyToolbarButton(_btnClear);
            EdzDialogStyle.ApplyToolbarButton(_btnFromPartsMgmt);

            searchRow.Controls.Add(lblSearch);
            searchRow.Controls.Add(_searchBox);
            searchRow.Controls.Add(_btnSelectAll);
            searchRow.Controls.Add(_btnClear);
            searchRow.Controls.Add(_btnFromPartsMgmt);

            EdzDialogStyle.ApplyHint(_hintLabel);
            _hintLabel.Text = "Mẹo: tick ô vuông trước mỗi mục để chọn; tick nhóm = chọn cả nhánh · bấm dòng để xem chi tiết.";

            topPanel.Controls.Add(_hintLabel);
            topPanel.Controls.Add(searchRow);
            topPanel.Controls.Add(_dbLabel);

            // ----- giữa: cây | chi tiết
            EdzDialogStyle.ApplyTree(_tree);
            _tree.Dock = DockStyle.Fill;
            _tree.CheckBoxes = true;
            _tree.AfterCheck += OnTreeAfterCheck;
            _tree.AfterSelect += (s, e) => ShowDetails(e.Node);

            _detailsHeader.Dock = DockStyle.Top;
            _detailsHeader.Height = 24;
            _detailsHeader.Padding = new Padding(4, 3, 0, 0);
            _detailsHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _detailsHeader.Text = "Chi tiết";

            EdzDialogStyle.ApplyDetails(_details);
            _details.DoubleClick += OnDetailsDoubleClick;

            var listPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 8, 4, 8) };
            var listCaption = new Label { Text = "Parts Database → part (chọn part để xuất)" };
            EdzDialogStyle.ApplyCaption(listCaption);
            listPanel.Controls.Add(_tree);
            listPanel.Controls.Add(listCaption);

            var detailPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 8, 10, 8) };
            detailPanel.Controls.Add(_details);
            detailPanel.Controls.Add(_detailsHeader);

            _split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical
            };
            // Lưu ý: Panel1MinSize/Panel2MinSize và SplitterDistance chỉ đặt được khi
            // SplitContainer đã có kích thước thật — sẽ áp dụng trong OnShown (có guard).
            _split.Panel1.Controls.Add(listPanel);
            _split.Panel2.Controls.Add(detailPanel);
            _split.Panel2.Resize += (s, e) => EdzDialogStyle.AdjustDetailsColumns(_details);
            _split.SplitterMoved += (s, e) => EdzDialogStyle.AdjustDetailsColumns(_details);

            // ----- đáy: tùy chọn + đếm + log
            _bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 340 };

            BuildOutputGroup();

            var actionPanel = new Panel { Dock = DockStyle.Top, Height = EdzDialogStyle.ActionRowHeight };
            var actionLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10, 4, 10, 4)
            };
            actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            _countLabel.Text = "Đã chọn: 0 part";
            _countLabel.AutoSize = true;
            EdzDialogStyle.ApplyStatus(_countLabel);
            _countLabel.Anchor = AnchorStyles.Left;
            _countLabel.Margin = new Padding(4, 10, 0, 0);

            _exportButton.Text = "XUẤT RA EDZ ▸";
            _exportButton.Size = new Size(200, EdzDialogStyle.PrimaryButtonHeight);
            EdzDialogStyle.ApplyPrimaryButton(_exportButton);
            _exportButton.Enabled = false;
            _exportButton.Dock = DockStyle.Right;
            _exportButton.Click += OnExportClicked;

            actionLayout.Controls.Add(_countLabel, 0, 0);
            actionLayout.Controls.Add(_exportButton, 1, 0);
            actionPanel.Controls.Add(actionLayout);

            EdzDialogStyle.ApplyLog(_log);

            _bottomPanel.Controls.Add(_log);
            _bottomPanel.Controls.Add(actionPanel);
            _bottomPanel.Controls.Add(_outputGroup);

            Controls.Add(_split);
            Controls.Add(_bottomPanel);
            Controls.Add(topPanel);

            // ----- tìm kiếm (debounce)
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

            // Initial layout: the output group must already have its tree-mode height,
            // otherwise the "→ sẽ tạo" row is clipped until the user changes a radio.
            UpdateModes();
        }

        private void BuildOutputGroup()
        {
            _outputGroup.Dock = DockStyle.Top;
            _outputGroup.Height = 158;
            _outputGroup.Text = "Cách xuất";

            // ----- hàng "phạm vi"
            var scopeFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 28,
                WrapContents = false,
                Padding = new Padding(6, 2, 0, 0)
            };

            var lblScope = new Label { Text = "Phạm vi:", AutoSize = true, Margin = new Padding(0, 5, 4, 0) };
            _rbTree.Text = "Part đã chọn trên cây";
            _rbTree.AutoSize = true;
            _rbTree.Checked = true;
            _rbTree.Margin = new Padding(0, 3, 12, 0);
            _rbTree.CheckedChanged += (s, e) => UpdateModes();
            _rbRecords.Text = "Theo loại bản ghi (như EPLAN)";
            _rbRecords.AutoSize = true;
            _rbRecords.Margin = new Padding(0, 3, 0, 0);
            _rbRecords.CheckedChanged += (s, e) => UpdateModes();

            scopeFlow.Controls.Add(lblScope);
            scopeFlow.Controls.Add(_rbTree);
            scopeFlow.Controls.Add(_rbRecords);

            // ----- hàng checkbox loại bản ghi (đầy đủ theo API đã kiểm chứng)
            _recordsPanel.Dock = DockStyle.Top;
            _recordsPanel.Height = 58;
            _recordsPanel.WrapContents = true;
            _recordsPanel.Padding = new Padding(20, 0, 0, 0);
            _recordsPanel.Visible = false;

            SetupRecordCheckBox(_cbParts, "Parts (linh kiện)", true);
            SetupRecordCheckBox(_cbAccessoryLists, "Accessory list (danh mục phụ kiện)", false);
            SetupRecordCheckBox(_cbAccessoryPlacements, "Accessory placement (vị trí phụ kiện)", false);
            SetupRecordCheckBox(_cbDrilling, "Drilling pattern (mẫu khoan)", false);
            SetupRecordCheckBox(_cbConnectionPoints, "Connection point pattern (mẫu điểm nối)", false);
            SetupRecordCheckBox(_cbAddresses, "Customer / Manufacturer / Supplier (KH & hãng)", false);

            _recordsHint.Dock = DockStyle.Top;
            _recordsHint.Height = 20;
            _recordsHint.Padding = new Padding(20, 0, 0, 0);
            _recordsHint.ForeColor = Color.DimGray;
            _recordsHint.Visible = false;
            _recordsHint.Text = "Chế độ này xuất TOÀN BỘ bản ghi theo loại đã tick (không dùng tick trên cây); file kèm macro/tài liệu nên có thể lớn.";

            // ----- hàng đích xuất (bảng 3 cột × 2 hàng)
            var targetLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 90,
                ColumnCount = 3,
                RowCount = 3,
                Padding = new Padding(6, 0, 6, 0)
            };
            targetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            targetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            targetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            targetLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            targetLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            targetLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));

            _rbSingle.Text = "Một tệp EDZ (vào thư mục):";
            _rbSingle.AutoSize = true;
            _rbSingle.Checked = true;
            _rbSingle.Anchor = AnchorStyles.Left;
            _rbSingle.Margin = new Padding(0, 4, 8, 0);
            _rbSingle.CheckedChanged += (s, e) => UpdateTargetPreview();

            _fileTarget.Dock = DockStyle.Fill;
            _fileTarget.Margin = new Padding(0, 3, 6, 3);

            _browseFile.Text = "Chọn thư mục…";
            _browseFile.Size = new Size(116, 26);
            _browseFile.Anchor = AnchorStyles.Left;
            _browseFile.Margin = new Padding(0, 2, 0, 0);
            _browseFile.Click += OnBrowseFileClicked;

            _rbPerPart.Text = "Mỗi part một tệp (vào thư mục):";
            _rbPerPart.AutoSize = true;
            _rbPerPart.Anchor = AnchorStyles.Left;
            _rbPerPart.Margin = new Padding(0, 4, 8, 0);
            _rbPerPart.CheckedChanged += (s, e) => UpdateTargetPreview();

            _folderTarget.Dock = DockStyle.Fill;
            _folderTarget.Margin = new Padding(0, 3, 6, 3);

            _browseFolder.Text = "Chọn thư mục…";
            _browseFolder.Size = new Size(116, 26);
            _browseFolder.Anchor = AnchorStyles.Left;
            _browseFolder.Margin = new Padding(0, 2, 0, 0);
            _browseFolder.Click += OnBrowseFolderClicked;

            _targetPreview.Dock = DockStyle.Fill;
            _targetPreview.ForeColor = Color.DimGray;
            _targetPreview.Margin = new Padding(0, 4, 0, 0);
            _targetPreview.TextAlign = ContentAlignment.MiddleLeft;
            _targetPreview.Text = "→ sẽ tạo: …";

            targetLayout.Controls.Add(_rbSingle, 0, 0);
            targetLayout.Controls.Add(_fileTarget, 1, 0);
            targetLayout.Controls.Add(_browseFile, 2, 0);
            targetLayout.Controls.Add(_rbPerPart, 0, 1);
            targetLayout.Controls.Add(_folderTarget, 1, 1);
            targetLayout.Controls.Add(_browseFolder, 2, 1);
            targetLayout.Controls.Add(_targetPreview, 0, 2);
            targetLayout.SetColumnSpan(_targetPreview, 3);

            // Thứ tự add ngược để dock xếp đúng: targetLayout trên cùng? Không —
            // mong muốn từ trên xuống: scopeFlow, recordsPanel, recordsHint, targetLayout.
            // Dock=Top: control thêm SAU nằm TRÊN → thêm ngược thứ tự hiển thị.
            _outputGroup.Controls.Add(targetLayout);
            _outputGroup.Controls.Add(_recordsHint);
            _outputGroup.Controls.Add(_recordsPanel);
            _outputGroup.Controls.Add(scopeFlow);
        }

        private void SetupRecordCheckBox(CheckBox box, string text, bool isChecked)
        {
            box.Text = text;
            box.AutoSize = true;
            box.Checked = isChecked;
            box.Margin = new Padding(0, 2, 16, 0);
            box.CheckedChanged += (s, e) => UpdateCount();
            _recordsPanel.Controls.Add(box);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _boldFont.Dispose();
                _searchTimer.Dispose();
            }
            base.Dispose(disposing);
        }

        private void AdjustDetailsColumns()
        {
            EdzDialogStyle.AdjustDetailsColumns(_details);
        }

        // ================================================================ nạp dữ liệu

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Áp dụng kích thước splitter sau khi form đã có kích thước thật (có guard —
            // SplitContainer ném lỗi nếu min-size vượt quá bề rộng hiện tại).
            try
            {
                _split.Panel1MinSize = 340;
                _split.Panel2MinSize = 360;
            }
            catch
            {
                // giữ mặc định
            }
            try
            {
                _split.SplitterDistance = Math.Max(380, (int)(_split.Width * 0.54));
            }
            catch
            {
                // giữ mặc định
            }

            AdjustDetailsColumns();
            UpdateModes();

            LoadLastFolders();
            UpdateTargetPreview();

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
            _dbLabel.Text = "Parts Database: " + dbName + " — đang tải danh sách part…";
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

                _dbLabel.Text = "Parts Database: " + dbName + " — " + parts.Count + " part";
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
                var raw = MDPartsManagement.SelectedPartsDatabaseAsString;
                if (string.IsNullOrWhiteSpace(raw)) return "(không rõ)";
                // "$(MD_PARTS)\ESS_part001.alk" → "ESS_part001.alk"
                try
                {
                    var fileName = Path.GetFileName(raw.Trim());
                    if (!string.IsNullOrWhiteSpace(fileName)) return fileName;
                }
                catch
                {
                    // giữ nguyên chuỗi gốc
                }
                return raw.Trim();
            }
            catch
            {
                return "(không rõ)";
            }
        }

        // ================================================================ cây

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
                treeNode.Nodes.Add(new TreeNode(EdzDialogStyle.Shorten(entry.Label))
                {
                    Tag = entry,
                    ToolTipText = entry.Label
                });
            }
            foreach (var child in node.Children.Values)
            {
                treeNode.Nodes.Add(CreateTreeNode(child));
            }

            RefreshNodeState(treeNode);
            return treeNode;
        }

        /// <summary>Đồng bộ checkbox và in đậm sau khi cấu hình/tạo cây (không dùng AddReference thủ công).</summary>
        private void RefreshNodeState(TreeNode node)
        {
            _updatingChecks = true;
            try
            {
                ApplyCheckedStateToSubtree(node);
            }
            finally
            {
                _updatingChecks = false;
            }
        }

        private void ApplyCheckedStateToSubtree(TreeNode node)
        {
            if (node.Tag is PartEntry entry)
            {
                node.Checked = _checkedIds.Contains(entry.Part.AdoId);
                node.NodeFont = node.Checked ? _boldFont : null;
                return;
            }

            int checkedCount = 0;
            foreach (TreeNode child in node.Nodes)
            {
                ApplyCheckedStateToSubtree(child);
                if (child.Checked) checkedCount++;
            }

            // Windows không có checkbox ba trạng thái — nhánh chỉ tick khi đủ toàn bộ con.
            node.Checked = node.Nodes.Count > 0 && checkedCount == node.Nodes.Count;
            node.NodeFont = null;
        }

        private void RefreshTreeStatesDeep(TreeNode node)
        {
            RefreshNodeState(node);
        }

        private void RefreshAncestors(TreeNode node)
        {
            var parent = node.Parent;
            while (parent != null)
            {
                _updatingChecks = true;
                try
                {
                    var checkedCount = 0;
                    foreach (TreeNode child in parent.Nodes)
                    {
                        if (child.Checked) checkedCount++;
                    }
                    parent.Checked = parent.Nodes.Count > 0 && checkedCount == parent.Nodes.Count;
                    parent.NodeFont = null;
                }
                finally
                {
                    _updatingChecks = false;
                }
                parent = parent.Parent;
            }
        }

        private void SetCheckedForSubtree(TreeNode node, bool value)
        {
            _updatingChecks = true;
            try
            {
                ApplyCheckedValue(node, value);
            }
            finally
            {
                _updatingChecks = false;
            }

            RefreshAncestors(node);
        }

        private void ApplyCheckedValue(TreeNode node, bool value)
        {
            if (node.Tag is PartEntry entry)
            {
                if (value) _checkedIds.Add(entry.Part.AdoId);
                else _checkedIds.Remove(entry.Part.AdoId);
            }

            node.Checked = value;
            node.NodeFont = value && node.Tag is PartEntry ? _boldFont : null;

            foreach (TreeNode child in node.Nodes)
            {
                ApplyCheckedValue(child, value);
            }
        }

        private void OnTreeAfterCheck(object sender, TreeViewEventArgs e)
        {
            if (_updatingChecks) return;

            var node = e.Node;
            var isChecked = node.Checked;

            // Ở chế độ theo loại bản ghi, cây không quyết định xuất — hoàn tác.
            if (_rbRecords.Checked)
            {
                _updatingChecks = true;
                try
                {
                    node.Checked = !isChecked;
                }
                finally
                {
                    _updatingChecks = false;
                }

                MessageBox.Show(this,
                    "Đang ở chế độ \"Theo loại bản ghi\" — tick trên cây không được dùng.\n" +
                    "Chuyển về \"Part đã chọn trên cây\" nếu muốn chọn từng part.",
                    "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetCheckedForSubtree(node, isChecked);
            UpdateCount();
            ShowDetails(_tree.SelectedNode);
        }

        private void OnSelectAllClicked(object sender, EventArgs e)
        {
            if (_rbRecords.Checked) return;
            foreach (TreeNode top in _tree.Nodes)
            {
                SetCheckedForSubtree(top, true);
            }
            UpdateCount();
            ShowDetails(_tree.SelectedNode);
            Log("Đã chọn tất cả part đang hiển thị.");
        }

        private void OnClearAllClicked(object sender, EventArgs e)
        {
            if (_rbRecords.Checked) return;
            _checkedIds.Clear();
            foreach (TreeNode top in _tree.Nodes)
            {
                RefreshTreeStatesDeep(top);
            }
            UpdateCount();
            ShowDetails(_tree.SelectedNode);
            Log("Đã bỏ chọn tất cả.");
        }

        private void OnUseSelectionClicked(object sender, EventArgs e)
        {
            if (_management == null || _rbRecords.Checked) return;

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
            ShowDetails(_tree.SelectedNode);
            Log("Lấy " + added + " part từ Parts Management (tổng đã chọn: " + _checkedIds.Count + ").");
        }

        // ================================================================ chi tiết

        private void ShowDetails(TreeNode node)
        {
            _details.BeginUpdate();
            try
            {
                _details.Items.Clear();

                if (node == null)
                {
                    _detailsHeader.Text = "Chi tiết";
                    return;
                }

                if (node.Tag is PartEntry entry)
                {
                    var part = entry.Part;
                    _detailsHeader.Text = entry.Label;

                    AddDetail("Mã hàng (Part number)", entry.Number);
                    AddDetail("Số bản (Type number)", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_TYPENR));
                    AddDetail("Số đặt hàng (Order number)", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_ORDERNR));
                    AddDetail("ERP number", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_ERPNR));

                    var manufacturerName = ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_MANUFACTURER_NAME);
                    AddDetail("Hãng (Manufacturer)",
                        manufacturerName.Length > 0 && !string.Equals(manufacturerName, entry.Manufacturer, StringComparison.OrdinalIgnoreCase)
                            ? entry.Manufacturer + " (" + manufacturerName + ")"
                            : entry.Manufacturer);

                    var supplier = ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_SUPPLIER);
                    var supplierName = ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_SUPPLIER_NAME);
                    AddDetail("Nhà cung cấp (Supplier)",
                        supplier.Length == 0
                            ? supplierName
                            : (supplierName.Length > 0 && !string.Equals(supplierName, supplier, StringComparison.OrdinalIgnoreCase)
                                ? supplier + " (" + supplierName + ")"
                                : supplier));

                    var groupPath = entry.Path.Length > 1
                        ? string.Join(" / ", entry.Path.Take(entry.Path.Length - 1))
                        : string.Empty;
                    AddDetail("Nhóm sản phẩm", groupPath);

                    AddDetail("Tên 1 (Designation 1)", entry.Designation);
                    AddDetail("Tên 2 (Designation 2)", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_DESCR2));
                    AddDetail("Tên 3 (Designation 3)", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_DESCR3));
                    AddDetail("Mô tả (Description)", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_NOTE));
#pragma warning disable CS0618 // một số property chú thích "internal use" nhưng vẫn đọc được dữ liệu thật
                    AddDetail("Văn bản bổ sung", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_ADDITIONALTEXT));
#pragma warning restore CS0618
#pragma warning disable CS0618
                    AddDetail("eCl@ss", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.PART_ECLASS));
#pragma warning restore CS0618
                    AddDetail("Mã nhận dạng (EAN)",
                        (ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_IDENTTYPE) + " " +
                         ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_IDENTCODE)).Trim());
                    AddDetail("Macro ký hiệu (schematic)", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_GROUPSYMBOLMACRO));
                    AddDetail("Macro đồ họa (graphical)", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_MACRO));
                    AddDetail("Kích thước R×C×S (mm)", ComposeDimensions(part));
                    AddDetail("Ngày tạo", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.PART_CREATE_DATE));
                    AddDetail("Người tạo", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.PART_CREATE_USER));
                    AddDetail("Sửa đổi cuối", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.PART_LASTCHANGE_DATE));
                    AddDetail("Người sửa cuối", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.PART_LASTCHANGE_USER));
                }
                else if (node.Tag is PartsTreeModel.Node group)
                {
                    _detailsHeader.Text = "Nhóm: " + group.Name;
                    AddDetail("Số part trong nhóm", group.TotalParts.ToString());
                    AddDetail("Đã chọn trong nhóm", CountCheckedInSubtree(node).ToString());
                    AddDetail("Gợi ý", "Tick ô vuông trước nhóm để chọn/bỏ chọn cả nhánh.");
                }
                else
                {
                    _detailsHeader.Text = node.Text ?? "Chi tiết";
                }
            }
            finally
            {
                _details.EndUpdate();
            }
        }

        private static string ReadProp(MDPart part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem id)
        {
            return PartInfo.ReadText(part, id);
        }

        private static string ComposeDimensions(MDPart part)
        {
            var width = ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_WIDTH);
            var height = ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_HEIGHT);
            var depth = ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_DEPTH);
            var parts = new List<string>();
            if (width.Length > 0) parts.Add(width);
            if (height.Length > 0) parts.Add(height);
            if (depth.Length > 0) parts.Add(depth);
            return string.Join(" × ", parts);
        }

        private void AddDetail(string name, string value)
        {
            var item = new ListViewItem(name ?? string.Empty);
            item.SubItems.Add(string.IsNullOrWhiteSpace(value) ? "—" : value);
            _details.Items.Add(item);
        }

        private int CountCheckedInSubtree(TreeNode node)
        {
            if (node.Tag is PartEntry entry)
            {
                return _checkedIds.Contains(entry.Part.AdoId) ? 1 : 0;
            }

            var count = 0;
            foreach (TreeNode child in node.Nodes)
            {
                count += CountCheckedInSubtree(child);
            }
            return count;
        }

        private void OnDetailsDoubleClick(object sender, EventArgs e)
        {
            if (_details.SelectedItems.Count == 0) return;
            var item = _details.SelectedItems[0];
            if (item.SubItems.Count < 2) return;

            var value = item.SubItems[1].Text;
            if (string.IsNullOrEmpty(value) || value == "—") return;

            try
            {
                Clipboard.SetText(value);
                Log("Đã copy vào clipboard: " + value);
            }
            catch (Exception ex)
            {
                Log("Không copy được: " + ex.Message);
            }
        }

        // ================================================================ chế độ & đếm

        private void UpdateModes()
        {
            var records = _rbRecords.Checked;

            _recordsPanel.Visible = records;
            _recordsHint.Visible = records;
            _outputGroup.Height = records ? 228 : 158;
            if (_bottomPanel != null)
            {
                _bottomPanel.Height = records ? 400 : 340;
            }

            _btnSelectAll.Enabled = !records;
            _btnClear.Enabled = !records;
            _btnFromPartsMgmt.Enabled = !records;

            _rbPerPart.Enabled = !records;
            _folderTarget.Enabled = !records;
            _browseFolder.Enabled = !records;
            if (records && _rbPerPart.Checked)
            {
                _rbSingle.Checked = true;
            }

            UpdateCount();
            UpdateTargetPreview();
        }

        private void UpdateCount()
        {
            if (_rbRecords.Checked)
            {
                var checkedCount = 0;
                foreach (Control control in _recordsPanel.Controls)
                {
                    var box = control as CheckBox;
                    if (box != null && box.Checked) checkedCount++;
                }

                _countLabel.Text = "Chế độ theo loại bản ghi (" + checkedCount + " loại được chọn)";
                _exportButton.Enabled = checkedCount > 0;
            }
            else
            {
                _countLabel.Text = "Đã chọn: " + _checkedIds.Count + " part";
                _exportButton.Enabled = _checkedIds.Count > 0;
            }

            UpdateTargetPreview();
        }

        // ================================================================ chọn đích

        private void OnBrowseFileClicked(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Chọn THƯ MỤC đích (file EDZ được đặt tên tự động)";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _fileTarget.Text = dialog.SelectedPath;
                    SaveLastFolders();
                    UpdateTargetPreview();
                }
            }
        }

        private void OnBrowseFolderClicked(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Chọn thư mục đích (mỗi part một file .edz)";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _folderTarget.Text = dialog.SelectedPath;
                    SaveLastFolders();
                    UpdateTargetPreview();
                }
            }
        }

        // ================================================================ tên file tự động + nhớ thư mục

        /// <summary>Chế độ "Một file EDZ": 1 part → tên = mã sản phẩm; nhiều part → parts.edz.</summary>
        private string SelectedSingleFileName()
        {
            var selected = _entries.Where(x => _checkedIds.Contains(x.Part.AdoId)).ToList();
            if (selected.Count == 1)
            {
                var number = selected[0].Number;
                if (!string.IsNullOrWhiteSpace(number)) return SanitizeFileName(number) + ".edz";
            }
            return PartsDefaultFileName;
        }

        /// <summary>Cập nhật dòng "→ sẽ tạo: …" theo chế độ + lựa chọn hiện tại.</summary>
        private void UpdateTargetPreview()
        {
            try
            {
                if (_rbRecords.Checked)
                {
                    _targetPreview.Text = "→ sẽ tạo: " + PartsDefaultFileName + " (trong thư mục trên)";
                }
                else if (_rbSingle.Checked)
                {
                    var count = _checkedIds.Count;
                    if (count == 0)
                    {
                        _targetPreview.Text = "→ sẽ tạo: (chưa chọn part nào)";
                    }
                    else if (count == 1)
                    {
                        _targetPreview.Text = "→ sẽ tạo: " + SelectedSingleFileName() + "  (theo mã sản phẩm)";
                    }
                    else
                    {
                        _targetPreview.Text = "→ sẽ tạo: " + PartsDefaultFileName + "  (gộp " + count + " part)";
                    }
                }
                else
                {
                    _targetPreview.Text = "→ mỗi part 1 tệp, tên = mã sản phẩm (trong thư mục trên)";
                }
            }
            catch
            {
                // bỏ qua
            }
        }

        private static string LastFolderFile
        {
            get
            {
                return Path.Combine(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Tool Oumijia"), "export-folders.txt");
            }
        }

        private void LoadLastFolders()
        {
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string single = null;
            string perPart = null;

            try
            {
                if (File.Exists(LastFolderFile))
                {
                    var lines = File.ReadAllLines(LastFolderFile);
                    if (lines.Length > 0) single = lines[0];
                    if (lines.Length > 1) perPart = lines[1];
                }
            }
            catch
            {
                // không đọc được — dùng mặc định
            }

            if (string.IsNullOrWhiteSpace(_fileTarget.Text))
            {
                _fileTarget.Text = !string.IsNullOrWhiteSpace(single) && Directory.Exists(single.Trim())
                    ? single.Trim()
                    : documents;
            }
            if (string.IsNullOrWhiteSpace(_folderTarget.Text))
            {
                _folderTarget.Text = !string.IsNullOrWhiteSpace(perPart) && Directory.Exists(perPart.Trim())
                    ? perPart.Trim()
                    : documents;
            }
        }

        private void SaveLastFolders()
        {
            try
            {
                var dir = Path.GetDirectoryName(LastFolderFile);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllLines(LastFolderFile, new[]
                {
                    (_fileTarget.Text ?? string.Empty).Trim(),
                    (_folderTarget.Text ?? string.Empty).Trim()
                });
            }
            catch
            {
                // ghi nhớ là best-effort
            }
        }

        // ================================================================ xuất

        private void OnExportClicked(object sender, EventArgs e)
        {
            if (_rbRecords.Checked)
            {
                ExportRecordTypes();
            }
            else
            {
                ExportSelectedParts();
            }
        }

        private void ExportSelectedParts()
        {
            var selected = _entries.Where(x => _checkedIds.Contains(x.Part.AdoId)).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show(this, "Chưa chọn part nào để xuất.", "Tool Oumijia",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string target;
            if (_rbSingle.Checked)
            {
                var folder = (_fileTarget.Text ?? string.Empty).Trim();
                if (folder.Length == 0)
                {
                    MessageBox.Show(this, "Chưa chọn thư mục đích.", "Tool Oumijia",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try { Directory.CreateDirectory(folder); } catch { /* báo lỗi khi ghi */ }
                target = Path.Combine(folder, SelectedSingleFileName());

                if (File.Exists(target))
                {
                    var overwrite = MessageBox.Show(this,
                        "File đã tồn tại:\n" + target + "\n\nGhi đè?", "Tool Oumijia",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (overwrite != DialogResult.Yes) return;
                }
            }
            else
            {
                target = (_folderTarget.Text ?? string.Empty).Trim();
                if (target.Length == 0)
                {
                    MessageBox.Show(this, "Chưa chọn thư mục đích.", "Tool Oumijia",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            SaveLastFolders();

            var confirm = MessageBox.Show(this,
                "Xuất " + selected.Count + " part ra EDZ?\n\n" +
                "Đích: " + target + "\n" +
                "Cách xuất: " + (_rbSingle.Checked ? "1 file duy nhất" : "mỗi part 1 file"),
                "Tool Oumijia", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            _exportButton.Enabled = false;
            UseWaitCursor = true;
            try
            {
                if (_rbSingle.Checked)
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
                UpdateCount();
            }
        }

        private void ExportRecordTypes()
        {
            var folder = (_fileTarget.Text ?? string.Empty).Trim();
            if (folder.Length == 0)
            {
                MessageBox.Show(this, "Chưa chọn thư mục đích.", "Tool Oumijia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try { Directory.CreateDirectory(folder); } catch { /* báo lỗi khi ghi */ }
            var target = Path.Combine(folder, PartsDefaultFileName);
            SaveLastFolders();

            if (File.Exists(target))
            {
                var overwrite = MessageBox.Show(this,
                    "File đã tồn tại:\n" + target + "\n\nGhi đè?", "Tool Oumijia",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (overwrite != DialogResult.Yes) return;
            }

            var checkedNames = new List<string>();
            if (_cbParts.Checked) checkedNames.Add("Parts");
            if (_cbAccessoryLists.Checked) checkedNames.Add("Accessory list");
            if (_cbAccessoryPlacements.Checked) checkedNames.Add("Accessory placement");
            if (_cbDrilling.Checked) checkedNames.Add("Drilling pattern");
            if (_cbConnectionPoints.Checked) checkedNames.Add("Connection point pattern");
            if (_cbAddresses.Checked) checkedNames.Add("Customer/Manufacturer/Supplier");

            if (checkedNames.Count == 0)
            {
                MessageBox.Show(this, "Chưa tick loại bản ghi nào để xuất.", "Tool Oumijia",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(this,
                "Xuất TOÀN BỘ bản ghi theo loại đã tick?\n\n" +
                "Loại: " + string.Join(", ", checkedNames) + "\n" +
                "Đích: " + target + "\n\n" +
                "Lưu ý: file kèm macro/tài liệu liên quan nên có thể lớn và mất vài phút;\n" +
                "chế độ này KHÔNG dùng tick chọn trên cây.",
                "Tool Oumijia", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            _exportButton.Enabled = false;
            UseWaitCursor = true;
            try
            {
                Log("Xuất theo loại bản ghi: " + string.Join(", ", checkedNames) + " → " + target);
                var stopwatch = Stopwatch.StartNew();

                // Cờ đã kiểm chứng: xem header file ExportEdzForm.cs
                _management.ExportParts(target, "IXPartsImportExportEdz", string.Empty,
                    _cbParts.Checked, null,
                    _cbAddresses.Checked, null,
                    _cbDrilling.Checked, null,
                    _cbConnectionPoints.Checked, null,
                    _cbAccessoryLists.Checked, null,
                    _cbAccessoryPlacements.Checked, null);

                stopwatch.Stop();
                var sizeMb = 0L;
                try { sizeMb = new FileInfo(target).Length / (1024 * 1024); } catch { }

                Log("Hoàn tất (" + sizeMb + " MB, " + stopwatch.Elapsed.TotalSeconds.ToString("0.0") + " giây).");
                AddinLog.Write("Export record-types OK: [" + string.Join("; ", checkedNames) +
                               "] -> " + target + " (" + sizeMb + " MB, " + stopwatch.Elapsed.TotalSeconds.ToString("0.0") + "s)");
                AddinCsvLog.Append("export", "OK-recordtypes", target, string.Join("|", checkedNames));

                MessageBox.Show(this,
                    "Đã xuất theo loại bản ghi:\n• " + string.Join("\n• ", checkedNames) +
                    "\n\nĐích: " + target + "\nKích thước: " + sizeMb + " MB",
                    "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log("LỖI: " + ex.Message);
                AddinLog.WriteError("Export record-types", ex);
                AddinCsvLog.Append("export", "ERROR-recordtypes", ex.Message);
                MessageBox.Show(this, "Xuất thất bại:\n" + ex.Message, "Tool Oumijia",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                UpdateCount();
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

        // ================================================================ log

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
}
