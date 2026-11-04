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
    internal sealed class ExportEdzForm : Form
    {
        private const int ImageUnchecked = 0;
        private const int ImageChecked = 1;
        private const int ImageMixed = 2;

        private readonly ImageList _stateImages = CreateStateImages();
        private readonly Font _boldFont = new Font("Segoe UI", 9F, FontStyle.Bold);

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

        // ----- đáy
        private readonly Label _countLabel = new Label();
        private readonly Button _exportButton = new Button();
        private readonly TextBox _log = new TextBox();

        private readonly Timer _searchTimer = new Timer();

        private MDPartsManagement _management;
        private MDPartsDatabase _database;
        private List<PartEntry> _entries = new List<PartEntry>();
        private readonly HashSet<int> _checkedIds = new HashSet<int>();
        private string _searchText = string.Empty;

        public ExportEdzForm()
        {
            Text = "Tool Oumijia — Xuất EDZ (chọn part cần xuất)";
            ClientSize = new Size(1080, 780);
            MinimumSize = new Size(940, 640);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);
            BuildUi();
        }

        // ================================================================ UI

        private void BuildUi()
        {
            // ----- hàng trên
            var topPanel = new Panel { Dock = DockStyle.Top, Height = 92 };

            _dbLabel.Dock = DockStyle.Top;
            _dbLabel.Height = 24;
            _dbLabel.Padding = new Padding(4, 4, 0, 0);
            _dbLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            var searchRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 34,
                WrapContents = false,
                Padding = new Padding(4, 2, 0, 0)
            };

            var lblSearch = new Label { Text = "Tìm:", AutoSize = true, Margin = new Padding(0, 7, 4, 0) };
            _searchBox.Width = 240;
            _searchBox.Margin = new Padding(0, 4, 8, 0);

            _btnSelectAll.Text = "Chọn tất cả";
            _btnSelectAll.Size = new Size(96, 26);
            _btnSelectAll.Margin = new Padding(0, 3, 4, 0);
            _btnSelectAll.Click += OnSelectAllClicked;

            _btnClear.Text = "Bỏ chọn";
            _btnClear.Size = new Size(80, 26);
            _btnClear.Margin = new Padding(0, 3, 4, 0);
            _btnClear.Click += OnClearAllClicked;

            _btnFromPartsMgmt.Text = "Lấy từ Parts Management ▸";
            _btnFromPartsMgmt.Size = new Size(190, 26);
            _btnFromPartsMgmt.Margin = new Padding(0, 3, 0, 0);
            _btnFromPartsMgmt.Click += OnUseSelectionClicked;

            searchRow.Controls.Add(lblSearch);
            searchRow.Controls.Add(_searchBox);
            searchRow.Controls.Add(_btnSelectAll);
            searchRow.Controls.Add(_btnClear);
            searchRow.Controls.Add(_btnFromPartsMgmt);

            _hintLabel.Dock = DockStyle.Top;
            _hintLabel.Height = 22;
            _hintLabel.Padding = new Padding(4, 0, 0, 0);
            _hintLabel.ForeColor = Color.DimGray;
            _hintLabel.Text = "Mẹo: tick ô vuông trước mỗi mục để chọn; tick nhóm = chọn cả nhánh · bấm dòng để xem chi tiết.";

            topPanel.Controls.Add(_hintLabel);
            topPanel.Controls.Add(searchRow);
            topPanel.Controls.Add(_dbLabel);

            // ----- giữa: cây | chi tiết
            _tree.Dock = DockStyle.Fill;
            _tree.BorderStyle = BorderStyle.FixedSingle;
            _tree.HideSelection = false;
            _tree.ShowLines = true;
            _tree.ShowRootLines = true;
            _tree.ItemHeight = 20;
            _tree.ImageList = _stateImages;
            _tree.MouseDown += OnTreeMouseDown;
            _tree.AfterSelect += (s, e) => ShowDetails(e.Node);
            EnableDoubleBuffering(_tree);

            _detailsHeader.Dock = DockStyle.Top;
            _detailsHeader.Height = 24;
            _detailsHeader.Padding = new Padding(4, 3, 0, 0);
            _detailsHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _detailsHeader.Text = "Chi tiết";

            _details.Dock = DockStyle.Fill;
            _details.View = View.Details;
            _details.FullRowSelect = true;
            _details.MultiSelect = false;
            _details.HideSelection = false;
            _details.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            _details.Columns.Add("Thuộc tính", 190);
            _details.Columns.Add("Giá trị", 320);
            _details.DoubleClick += OnDetailsDoubleClick;

            _split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical
            };
            // Lưu ý: Panel1MinSize/Panel2MinSize và SplitterDistance chỉ đặt được khi
            // SplitContainer đã có kích thước thật — sẽ áp dụng trong OnShown (có guard).
            _split.Panel1.Controls.Add(_tree);
            _split.Panel2.Controls.Add(_details);
            _split.Panel2.Controls.Add(_detailsHeader);
            _split.Panel2.Resize += (s, e) => AdjustDetailsColumns();
            _split.SplitterMoved += (s, e) => AdjustDetailsColumns();

            // ----- đáy: tùy chọn + đếm + log
            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 330 };

            BuildOutputGroup();

            var actionPanel = new Panel { Dock = DockStyle.Top, Height = 42 };
            var actionLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            _countLabel.Text = "Đã chọn: 0 part";
            _countLabel.AutoSize = true;
            _countLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _countLabel.Anchor = AnchorStyles.Left;
            _countLabel.Margin = new Padding(4, 10, 0, 0);

            _exportButton.Text = "XUẤT RA EDZ ▸";
            _exportButton.Size = new Size(160, 32);
            _exportButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _exportButton.Enabled = false;
            _exportButton.Dock = DockStyle.Right;
            _exportButton.Click += OnExportClicked;

            actionLayout.Controls.Add(_countLabel, 0, 0);
            actionLayout.Controls.Add(_exportButton, 1, 0);
            actionPanel.Controls.Add(actionLayout);

            _log.Dock = DockStyle.Fill;
            _log.Multiline = true;
            _log.ReadOnly = true;
            _log.ScrollBars = ScrollBars.Both;
            _log.WordWrap = false;
            _log.Font = new Font("Consolas", 8.5F);

            bottomPanel.Controls.Add(_log);
            bottomPanel.Controls.Add(actionPanel);
            bottomPanel.Controls.Add(_outputGroup);

            Controls.Add(_split);
            Controls.Add(bottomPanel);
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
        }

        private void BuildOutputGroup()
        {
            _outputGroup.Dock = DockStyle.Top;
            _outputGroup.Height = 126;
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
                Height = 62,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(6, 0, 6, 0)
            };
            targetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            targetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            targetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            targetLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            targetLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));

            _rbSingle.Text = "Một file EDZ:";
            _rbSingle.AutoSize = true;
            _rbSingle.Checked = true;
            _rbSingle.Anchor = AnchorStyles.Left;
            _rbSingle.Margin = new Padding(0, 4, 8, 0);

            _fileTarget.Dock = DockStyle.Fill;
            _fileTarget.Margin = new Padding(0, 3, 6, 3);

            _browseFile.Text = "Chọn file…";
            _browseFile.Size = new Size(104, 26);
            _browseFile.Anchor = AnchorStyles.Left;
            _browseFile.Margin = new Padding(0, 2, 0, 0);
            _browseFile.Click += OnBrowseFileClicked;

            _rbPerPart.Text = "Mỗi part một file (vào thư mục):";
            _rbPerPart.AutoSize = true;
            _rbPerPart.Anchor = AnchorStyles.Left;
            _rbPerPart.Margin = new Padding(0, 4, 8, 0);

            _folderTarget.Dock = DockStyle.Fill;
            _folderTarget.Margin = new Padding(0, 3, 6, 3);

            _browseFolder.Text = "Chọn thư mục…";
            _browseFolder.Size = new Size(116, 26);
            _browseFolder.Anchor = AnchorStyles.Left;
            _browseFolder.Margin = new Padding(0, 2, 0, 0);
            _browseFolder.Click += OnBrowseFolderClicked;

            targetLayout.Controls.Add(_rbSingle, 0, 0);
            targetLayout.Controls.Add(_fileTarget, 1, 0);
            targetLayout.Controls.Add(_browseFile, 2, 0);
            targetLayout.Controls.Add(_rbPerPart, 0, 1);
            targetLayout.Controls.Add(_folderTarget, 1, 1);
            targetLayout.Controls.Add(_browseFolder, 2, 1);

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
                _stateImages.Dispose();
                _boldFont.Dispose();
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
                using (var border = new Pen(Color.FromArgb(120, 120, 120)))
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
                // Chỉ là tối ưu vẽ.
            }
        }

        private void AdjustDetailsColumns()
        {
            try
            {
                if (_details.Columns.Count >= 2)
                {
                    _details.Columns[1].Width = Math.Max(120, _details.ClientSize.Width - _details.Columns[0].Width - 26);
                }
            }
            catch
            {
                // bỏ qua
            }
        }

        // ================================================================ nạp dữ liệu

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Áp dụng kích thước splitter sau khi form đã có kích thước thật (có guard —
            // SplitContainer ném lỗi nếu min-size vượt quá bề rộng hiện tại).
            try
            {
                _split.Panel1MinSize = 380;
                _split.Panel2MinSize = 250;
            }
            catch
            {
                // giữ mặc định
            }
            try
            {
                _split.SplitterDistance = Math.Max(400, (int)(_split.Width * 0.58));
            }
            catch
            {
                // giữ mặc định
            }

            AdjustDetailsColumns();

            if (string.IsNullOrWhiteSpace(_fileTarget.Text))
            {
                _fileTarget.Text = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "parts.edz");
            }

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
                treeNode.Nodes.Add(new TreeNode(entry.Label) { Tag = entry });
            }
            foreach (var child in node.Children.Values)
            {
                treeNode.Nodes.Add(CreateTreeNode(child));
            }

            RefreshNodeState(treeNode);
            return treeNode;
        }

        /// <summary>Tính lại trạng thái (ảnh tick + in đậm) của một node — gọi theo thứ tự lá → gốc.</summary>
        private void RefreshNodeState(TreeNode node)
        {
            if (node.Tag is PartEntry entry)
            {
                var isChecked = _checkedIds.Contains(entry.Part.AdoId);
                node.ImageIndex = isChecked ? ImageChecked : ImageUnchecked;
                node.NodeFont = isChecked ? _boldFont : null;
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
            node.NodeFont = null;
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

        private void ForceTreeRepaint()
        {
            // Windows TreeView có thể không vẽ lại ô tick khi đổi hình trạng thái —
            // buộc vẽ lại để dấu ✓ hiện ngay.
            _tree.Invalidate();
            _tree.Update();
        }

        private void OnTreeMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            var hit = _tree.HitTest(e.Location);
            if (hit.Node == null) return;

            // Chỉ toggle khi bấm đúng vào ô vuông (ảnh trạng thái); bấm chữ chỉ chọn dòng.
            if (hit.Location != TreeViewHitTestLocations.Image) return;

            // Chế độ theo loại bản ghi không dùng tick trên cây.
            if (_rbRecords.Checked)
            {
                MessageBox.Show(this,
                    "Đang ở chế độ \"Theo loại bản ghi\" — tick trên cây không được dùng.\n" +
                    "Chuyển về \"Part đã chọn trên cây\" nếu muốn chọn từng part.",
                    "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

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
            ForceTreeRepaint();
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
            ForceTreeRepaint();
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
            ForceTreeRepaint();
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
            ForceTreeRepaint();
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
            _outputGroup.Height = records ? 196 : 126;

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
        }

        // ================================================================ chọn đích

        private void OnBrowseFileClicked(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Chọn file EDZ đích";
                dialog.Filter = "Gói EPLAN (*.edz)|*.edz";
                dialog.FileName = "parts.edz";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _fileTarget.Text = dialog.FileName;
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
                }
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

            var target = (_rbSingle.Checked ? _fileTarget.Text : _folderTarget.Text) ?? string.Empty;
            target = target.Trim();
            if (target.Length == 0)
            {
                MessageBox.Show(this, "Chưa chọn đích xuất (file hoặc thư mục).", "Tool Oumijia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_rbSingle.Checked && !target.EndsWith(".edz", StringComparison.OrdinalIgnoreCase))
            {
                target += ".edz";
            }

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
            var target = (_fileTarget.Text ?? string.Empty).Trim();
            if (target.Length == 0)
            {
                MessageBox.Show(this, "Chưa chọn file EDZ đích.", "Tool Oumijia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!target.EndsWith(".edz", StringComparison.OrdinalIgnoreCase))
            {
                target += ".edz";
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
