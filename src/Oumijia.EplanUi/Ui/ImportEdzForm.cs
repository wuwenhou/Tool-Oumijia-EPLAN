using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Eplan.EplApi.MasterData;
using Oumijia.EdzReader;

namespace Oumijia.EplanUi
{
    /// <summary>
    /// Nhập batch gói EDZ vào Parts Database với preview cục bộ và bố cục hai cột đồng bộ với Xuất EDZ.
    /// </summary>
    public sealed class ImportEdzForm : Form
    {
        private const MDPartsManagement.ImportMode FixedImportMode = MDPartsManagement.ImportMode.UpdateAndAppend;
        private const MDPartsManagement.ImportIdentifier FixedIdentifier = MDPartsManagement.ImportIdentifier.PartNumber;
        private const bool FixedAdditionalLanguage = false;

        private readonly TreeView _tree = new TreeView();
        private readonly TextBox _log = new TextBox();
        private readonly Button _startButton = new Button();
        private readonly SplitContainer _split = new SplitContainer();
        private readonly Label _detailsHeader = new Label();
        private readonly ListView _details = new ListView();
        private readonly Label _status = new Label();
        private readonly Dictionary<string, ImportItem> _items =
            new Dictionary<string, ImportItem>(StringComparer.OrdinalIgnoreCase);
        private bool _updatingChecks;

        public ImportEdzForm()
            : this(null)
        {
        }

        /// <summary>Mở hộp Nhập và nạp sẵn các tệp EDZ đã tải từ Data Portal.</summary>
        public ImportEdzForm(IEnumerable<string> initialFiles)
        {
            EdzDialogStyle.ApplyForm(this, "Tool Oumijia — Nhập EDZ");
            BuildUi();
            if (initialFiles != null) AddDistinct(initialFiles);
            UpdateCount();
        }

        private void BuildUi()
        {
            Panel topPanel = new Panel { Dock = DockStyle.Top, Height = 96, Padding = new Padding(8, 6, 8, 0) };
            Label label = new Label
            {
                Text = "Các gói EDZ cần nhập"
            };
            EdzDialogStyle.ApplyHeader(label);

            FlowLayoutPanel buttonFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 34,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 0)
            };

            Button btnAddFiles = new Button { Text = "Thêm tệp…", Width = 104, Height = EdzDialogStyle.ToolbarButtonHeight };
            btnAddFiles.Click += OnAddFilesClicked;
            Button btnAddFolder = new Button { Text = "Thêm thư mục…", Width = 124, Height = EdzDialogStyle.ToolbarButtonHeight };
            btnAddFolder.Click += OnAddFolderClicked;
            Button btnRemove = new Button { Text = "Xóa gói chọn", Width = 112, Height = EdzDialogStyle.ToolbarButtonHeight };
            btnRemove.Click += OnRemoveClicked;
            Button btnClear = new Button { Text = "Xóa hết", Width = 82, Height = EdzDialogStyle.ToolbarButtonHeight };
            btnClear.Click += delegate
            {
                _tree.Nodes.Clear();
                _items.Clear();
                ShowDetails(null);
                UpdateCount();
                Log("Đã xóa toàn bộ danh sách.");
            };

            Button btnCheckAll = new Button { Text = "Chọn hết", Width = 84, Height = EdzDialogStyle.ToolbarButtonHeight };
            btnCheckAll.Click += delegate { SetAllPartsChecked(true); };

            Button btnUncheckAll = new Button { Text = "Bỏ chọn", Width = 84, Height = EdzDialogStyle.ToolbarButtonHeight };
            btnUncheckAll.Click += delegate { SetAllPartsChecked(false); };

            EdzDialogStyle.ApplyToolbarButton(btnAddFiles);
            EdzDialogStyle.ApplyToolbarButton(btnAddFolder);
            EdzDialogStyle.ApplyToolbarButton(btnRemove);
            EdzDialogStyle.ApplyToolbarButton(btnClear);
            EdzDialogStyle.ApplyToolbarButton(btnCheckAll);
            EdzDialogStyle.ApplyToolbarButton(btnUncheckAll);

            buttonFlow.Controls.Add(btnAddFiles);
            buttonFlow.Controls.Add(btnAddFolder);
            buttonFlow.Controls.Add(btnRemove);
            buttonFlow.Controls.Add(btnClear);
            buttonFlow.Controls.Add(btnCheckAll);
            buttonFlow.Controls.Add(btnUncheckAll);

            Label hint = new Label
            {
                Text = "Tick part cần nhập; bỏ tick để chỉ nhập một phần gói EDZ."
            };
            EdzDialogStyle.ApplyHint(hint);
            topPanel.Controls.Add(hint);
            topPanel.Controls.Add(buttonFlow);
            topPanel.Controls.Add(label);

            EdzDialogStyle.ApplyTree(_tree);
            _tree.Dock = DockStyle.Fill;
            _tree.CheckBoxes = true;
            _tree.AfterCheck += OnTreeAfterCheck;
            _tree.AfterSelect += delegate (object sender, TreeViewEventArgs args) { ShowDetails(args.Node); };

            Panel listPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 8, 4, 8) };
            Label listCaption = new Label
            {
                Text = "Gói EDZ → part (tick part cần nhập)"
            };
            EdzDialogStyle.ApplyCaption(listCaption);
            listPanel.Controls.Add(_tree);
            listPanel.Controls.Add(listCaption);

            Panel detailPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 8, 10, 8) };
            _detailsHeader.Dock = DockStyle.Top;
            _detailsHeader.Height = 24;
            _detailsHeader.Padding = new Padding(4, 3, 0, 0);
            _detailsHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _detailsHeader.Text = "Chi tiết";

            EdzDialogStyle.ApplyDetails(_details);
            _details.DoubleClick += OnDetailsDoubleClick;

            detailPanel.Controls.Add(_details);
            detailPanel.Controls.Add(_detailsHeader);

            _split.Dock = DockStyle.Fill;
            _split.Panel1.Controls.Add(listPanel);
            _split.Panel2.Controls.Add(detailPanel);
            _split.Orientation = Orientation.Vertical;
            _split.FixedPanel = FixedPanel.None;
            _split.Panel2.Resize += delegate { EdzDialogStyle.AdjustDetailsColumns(_details); };
            _split.SplitterMoved += delegate { EdzDialogStyle.AdjustDetailsColumns(_details); };

            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 190 };

            Panel actionPanel = new Panel { Dock = DockStyle.Top, Height = EdzDialogStyle.ActionRowHeight };
            TableLayoutPanel actionLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            actionLayout.Padding = new Padding(10, 4, 10, 4);

            _status.Text = "Đã nạp: 0 | Preview: 0 | Lỗi: 0";
            _status.Dock = DockStyle.Fill;
            EdzDialogStyle.ApplyStatus(_status);

            _startButton.Text = "NHẬP VÀO PARTS DATABASE ▸";
            _startButton.Size = new Size(260, EdzDialogStyle.PrimaryButtonHeight);
            EdzDialogStyle.ApplyPrimaryButton(_startButton);
            _startButton.Enabled = false;
            _startButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _startButton.Click += OnStartClicked;

            actionLayout.Controls.Add(_status, 0, 0);
            actionLayout.Controls.Add(_startButton, 1, 0);
            actionPanel.Controls.Add(actionLayout);

            EdzDialogStyle.ApplyLog(_log);

            bottomPanel.Controls.Add(_log);
            bottomPanel.Controls.Add(actionPanel);

            Controls.Add(_split);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try
            {
                _split.Panel1MinSize = 340;
                _split.Panel2MinSize = 360;
            }
            catch { /* giữ mặc định nếu form chưa đủ rộng */ }
            try { _split.SplitterDistance = Math.Max(380, (int)(_split.Width * 0.54)); }
            catch { /* giữ mặc định */ }
            AdjustDetailsColumns();
        }

        private void OnAddFilesClicked(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Chọn gói EDZ";
                dialog.Filter = "Gói EPLAN (*.edz)|*.edz|Tất cả tệp (*.*)|*.*";
                dialog.Multiselect = true;
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                AddDistinct(dialog.FileNames);
            }
        }

        private void OnAddFolderClicked(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Chọn thư mục chứa EDZ (bao gồm thư mục con)";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try { AddDistinct(Directory.EnumerateFiles(dialog.SelectedPath, "*.edz", SearchOption.AllDirectories)); }
                catch (Exception ex) { Log("LỖI đọc thư mục: " + ex.Message); }
            }
        }

        private void OnRemoveClicked(object sender, EventArgs e)
        {
            TreeNode selected = _tree.SelectedNode;
            if (selected == null) return;
            TreeNode packageNode = selected.Parent ?? selected;
            ImportItem item = packageNode.Tag as ImportItem;
            if (item == null) return;
            _tree.Nodes.Remove(packageNode);
            _items.Remove(item.Path);
            Log("Đã xóa gói " + Path.GetFileName(item.Path) + ".");
            ShowDetails(_tree.SelectedNode);
            UpdateCount();
        }

        private void AddDistinct(IEnumerable<string> files)
        {
            int added = 0;
            int failed = 0;
            foreach (string file in files)
            {
                if (String.IsNullOrWhiteSpace(file)) continue;
                if (!_items.ContainsKey(file))
                {
                    ImportItem item = new ImportItem(file);
                    _items[file] = item;
                    TreeNode node = new TreeNode(Path.GetFileName(file));
                    node.Tag = item;
                    node.ToolTipText = file;
                    item.Node = node;
                    _tree.Nodes.Add(node);
                    added++;
                }
                else failed++;
            }

            Log("Đã thêm " + added + " gói EDZ" + (failed > 0 ? "; bỏ qua " + failed + " mục trùng" : "") + ".");
            UseWaitCursor = true;
            _startButton.Enabled = false;
            try
            {
                foreach (TreeNode node in _tree.Nodes)
                {
                    ImportItem item = node.Tag as ImportItem;
                    if (item != null && !item.PreviewLoaded) ReadPreview(item);
                }
            }
            finally
            {
                UseWaitCursor = false;
                UpdateCount();
                if (_tree.SelectedNode == null && _tree.Nodes.Count > 0)
                    _tree.SelectedNode = _tree.Nodes[0];
                ShowDetails(_tree.SelectedNode);
            }
        }

        /// <summary>Tick/untick one node: a package drives all its parts; a part updates its package.</summary>
        private void OnTreeAfterCheck(object sender, TreeViewEventArgs e)
        {
            if (_updatingChecks) return;
            _updatingChecks = true;
            try
            {
                ImportItem item = e.Node.Tag as ImportItem;
                if (item != null)
                {
                    foreach (TreeNode child in e.Node.Nodes) child.Checked = e.Node.Checked;
                }
                else if (e.Node.Parent != null)
                {
                    TreeNode parent = e.Node.Parent;
                    bool all = parent.Nodes.Count > 0;
                    foreach (TreeNode child in parent.Nodes)
                    {
                        if (child.Checked) continue;
                        all = false;
                        break;
                    }
                    parent.Checked = all;
                }
            }
            finally
            {
                _updatingChecks = false;
            }
            UpdateCount();
        }

        private void SetAllPartsChecked(bool value)
        {
            _updatingChecks = true;
            try
            {
                foreach (TreeNode packageNode in _tree.Nodes)
                {
                    packageNode.Checked = value;
                    foreach (TreeNode child in packageNode.Nodes) child.Checked = value;
                }
            }
            finally
            {
                _updatingChecks = false;
            }
            UpdateCount();
        }

        private int CountSelectedParts()
        {
            int count = 0;
            foreach (TreeNode packageNode in _tree.Nodes)
                foreach (TreeNode child in packageNode.Nodes)
                    if (child.Tag is EdzPartInfo && child.Checked) count++;
            return count;
        }

        private int CountTotalParts()
        {
            int count = 0;
            foreach (TreeNode packageNode in _tree.Nodes)
                foreach (TreeNode child in packageNode.Nodes)
                    if (child.Tag is EdzPartInfo) count++;
            return count;
        }

        private static string FilterTempDirectory
        {
            get { return Path.Combine(Path.GetTempPath(), "ToolOumijia", "edz-filter"); }
        }

        private void CleanupTempFiles(List<string> tempFiles)
        {
            if (tempFiles == null) return;
            foreach (string file in tempFiles)
            {
                try { if (File.Exists(file)) File.Delete(file); }
                catch (Exception ex) { Log("Không xóa được tệp tạm: " + ex.Message); }
            }
        }

        private void ReadPreview(ImportItem item)
        {            item.PreviewLoaded = true;
            item.Error = null;
            try
            {
                item.Info = EdzPackageReader.Read(item.Path, new EdzReadOptions { MaxParts = 500 });
                item.Status = item.Info.Parts.Count > 0 ? "Sẵn sàng" : "Không tìm thấy part trong EDZ";
            }
            catch (Exception ex)
            {
                item.Error = ex.Message;
                item.Status = "Không đọc được preview";
                item.Info = null;
            }
            RefreshPackageNode(item);
        }

        private void RefreshPackageNode(ImportItem item)
        {
            TreeNode node = item.Node;
            if (node == null) return;
            node.Nodes.Clear();
            node.Text = Path.GetFileName(item.Path) + (item.Error != null ? " [Lỗi preview]" :
                item.Info != null ? " (" + item.Info.Parts.Count + " part)" : "");
            if (item.Info == null) return;
            foreach (EdzPartInfo part in item.Info.Parts)
            {
                string label = BuildPartLabel(part);
                TreeNode partNode = new TreeNode(EdzDialogStyle.Shorten(label));
                partNode.Tag = part;
                partNode.ToolTipText = OrEmpty(label);
                partNode.Checked = true;
                node.Nodes.Add(partNode);
            }
            node.Checked = item.Info.Parts.Count > 0;
        }

        /// <summary>Same row format as Xuất EDZ: "PARTNR (Designation)" with TypeNumber fallback.</summary>
        private static string BuildPartLabel(EdzPartInfo part)
        {
            if (part == null) return String.Empty;

            string number = FirstValue(part.PartNumber, part.TypeNumber, part.OrderNumber);
            string name = FirstValue(part.Designation, part.TypeNumber, part.OrderNumber);

            if (String.IsNullOrWhiteSpace(number)) return OrEmpty(name);
            number = number.Trim();
            if (String.IsNullOrWhiteSpace(name) ||
                String.Equals(name.Trim(), number, StringComparison.OrdinalIgnoreCase))
            {
                return number;
            }
            return number + " (" + name.Trim() + ")";
        }

        private void ShowDetails(TreeNode node)
        {
            _details.BeginUpdate();
            try
            {
                _details.Items.Clear();
                _detailsHeader.Text = "Chi tiết";
                if (node == null) return;

                ImportItem item = node.Tag as ImportItem;
                if (item != null)
                {
                    AddDetail("Tên gói EDZ", Path.GetFileName(item.Path));
                    AddDetail("Đường dẫn", item.Path);
                    AddDetail("Kích thước", item.LastWrite == DateTime.MinValue ? null : ByteSize(item.FileSize));
                    AddDetail("Sửa đổi cuối", item.LastWrite == DateTime.MinValue ? null : item.LastWrite.ToString("dd/MM/yyyy HH:mm"));
                    AddDetail("Định dạng", item.Info == null ? null : item.Info.ContainerKind);
                    AddDetail("Số part preview", item.Info == null ? null : item.Info.Parts.Count.ToString());
                    AddDetail("Trạng thái", item.Status);
                    AddDetail("Lỗi preview", item.Error);
                    return;
                }

                EdzPartInfo partInfo = node.Tag as EdzPartInfo;
                if (partInfo != null) ShowPartDetails(partInfo);
            }
            finally
            {
                _details.EndUpdate();
            }
        }

        private void ShowPartDetails(EdzPartInfo part)
        {
            AddDetail("Mã hàng (Part number)", part.PartNumber);
            AddDetail("Số bản (Type number)", part.TypeNumber);
            AddDetail("Số đặt hàng (Order number)", part.OrderNumber);
            AddDetail("ERP number", ReadProperty(part, "ARTICLE_ERPNR", "ERP_NUMBER", "ERPNR"));
            AddDetail("Hãng (Manufacturer)", CombineCodeAndName(part.Manufacturer,
                ReadPropertyText(part, "ARTICLE_MANUFACTURER_NAME")));
            AddDetail("Nhà cung cấp (Supplier)", CombineCodeAndName(
                ReadProperty(part, "ARTICLE_SUPPLIER"), ReadPropertyText(part, "ARTICLE_SUPPLIER_NAME")));
            AddDetail("Nhóm sản phẩm", part.ProductGroup);
            AddDetail("Tên 1 (Designation 1)", EdzText.CleanMultiLanguage(part.Designation));
            AddDetail("Tên 2 (Designation 2)", ReadPropertyText(part, "ARTICLE_DESCR2", "DESIGNATION2"));
            AddDetail("Tên 3 (Designation 3)", ReadPropertyText(part, "ARTICLE_DESCR3", "DESIGNATION3"));
            AddDetail("Mô tả (Description)", ReadPropertyText(part, "ARTICLE_NOTE", "NOTE"));
            AddDetail("Văn bản bổ sung", ReadPropertyText(part, "ARTICLE_ADDITIONALTEXT", "ADDITIONALTEXT"));
            AddDetail("eCl@ss", FirstValue(part.EClassCode, ReadProperty(part, "PART_ECLASS")));
            AddDetail("Mã nhận dạng (EAN)",
                (FirstValue(ReadProperty(part, "ARTICLE_IDENTTYPE"), "") + " " +
                 FirstValue(ReadProperty(part, "ARTICLE_IDENTCODE", "EAN"), "")).Trim());
            AddDetail("Macro ký hiệu (schematic)", ReadProperty(part, "ARTICLE_GROUPSYMBOLMACRO"));
            AddDetail("Macro đồ họa (graphical)", ReadProperty(part, "ARTICLE_MACRO"));
            AddDetail("Kích thước R×C×S (mm)", ComposeDimensions(part));
            AddDetail("Ngày tạo", ReadProperty(part, "PART_CREATE_DATE"));
            AddDetail("Người tạo", ReadProperty(part, "PART_CREATE_USER"));
            AddDetail("Sửa đổi cuối", ReadProperty(part, "PART_LASTCHANGE_DATE"));
            AddDetail("Người sửa cuối", ReadProperty(part, "PART_LASTCHANGE_USER"));
        }

        private static string ComposeDimensions(EdzPartInfo part)
        {
            List<string> values = new List<string>();
            foreach (string key in new[] { "ARTICLE_WIDTH", "ARTICLE_HEIGHT", "ARTICLE_DEPTH" })
            {
                string value = ReadProperty(part, key);
                if (!String.IsNullOrWhiteSpace(value)) values.Add(value);
            }
            return String.Join(" × ", values);
        }

        private static string ReadProperty(EdzPartInfo part, params string[] aliases)
        {
            if (part == null || part.Properties == null) return null;
            foreach (string alias in aliases)
            {
                string value;
                if (part.Properties.TryGetValue(alias, out value) && !String.IsNullOrWhiteSpace(value)) return value;
                string normalized = NormalizeKey(alias);
                if (part.Properties.TryGetValue(normalized, out value) && !String.IsNullOrWhiteSpace(value)) return value;
                if (part.Properties.TryGetValue("P_" + alias, out value) && !String.IsNullOrWhiteSpace(value)) return value;
                if (part.Properties.TryGetValue("P" + normalized, out value) && !String.IsNullOrWhiteSpace(value)) return value;
            }
            return null;
        }

        private static string ReadPropertyText(EdzPartInfo part, params string[] aliases)
        {
            return EdzText.CleanMultiLanguage(ReadProperty(part, aliases));
        }

        private static string NormalizeKey(string key)
        {
            return new string(key.Where(Char.IsLetterOrDigit).Select(Char.ToUpperInvariant).ToArray());
        }

        private static string FirstValue(params string[] values)
        {
            foreach (string value in values)
                if (!String.IsNullOrWhiteSpace(value)) return value;
            return null;
        }

        private static string CombineCodeAndName(string code, string name)
        {
            if (String.IsNullOrWhiteSpace(code)) return name;
            if (String.IsNullOrWhiteSpace(name) ||
                String.Equals(code, name, StringComparison.OrdinalIgnoreCase)) return code;
            return code + " (" + name + ")";
        }

        private void AddDetail(string name, string value)
        {
            ListViewItem row = new ListViewItem(name);
            row.SubItems.Add(OrEmpty(value));
            _details.Items.Add(row);
        }

        private void AdjustDetailsColumns()
        {
            EdzDialogStyle.AdjustDetailsColumns(_details);
        }

        private void OnDetailsDoubleClick(object sender, EventArgs e)
        {
            if (_details.SelectedItems.Count == 0 || _details.SelectedItems[0].SubItems.Count < 2) return;
            string value = _details.SelectedItems[0].SubItems[1].Text;
            if (String.IsNullOrEmpty(value) || value == "—") return;
            try { Clipboard.SetText(value); Log("Đã copy giá trị vào clipboard."); }
            catch (Exception ex) { Log("Không copy được: " + ex.Message); }
        }

        private void UpdateCount()
        {
            int ok = _items.Values.Count(delegate (ImportItem value) { return value.Info != null; });
            int failed = _items.Values.Count(delegate (ImportItem value) { return value.Error != null; });
            int unreadable = _items.Count - ok;
            int selected = CountSelectedParts();
            int total = CountTotalParts();
            _status.Text = String.Format("Đã nạp: {0} gói | Chọn: {1}/{2} part | Lỗi: {3}",
                _items.Count, selected, total, failed);
            _startButton.Enabled = selected > 0 || unreadable > 0;
        }

        private void OnStartClicked(object sender, EventArgs e)
        {
            if (_items.Count == 0)
            {
                MessageBox.Show(this, "Chưa có gói EDZ nào trong danh sách.", "Tool Oumijia",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            List<string> plan = new List<string>();
            List<string> tempFiles = new List<string>();
            int selectedParts = 0;
            int skipped = 0;

            try
            {
                foreach (TreeNode packageNode in _tree.Nodes)
                {
                    ImportItem item = packageNode.Tag as ImportItem;
                    if (item == null) continue;

                    if (item.Info == null)
                    {
                        // Preview failed: keep the previous behaviour and import the file as-is.
                        plan.Add(item.Path);
                        continue;
                    }

                    List<string> keys = new List<string>();
                    foreach (TreeNode child in packageNode.Nodes)
                    {
                        if (!child.Checked) continue;
                        EdzPartInfo part = child.Tag as EdzPartInfo;
                        if (part == null) continue;
                        string key = FirstValue(part.PackageKey, part.PartNumber, part.TypeNumber, part.OrderNumber);
                        if (!String.IsNullOrWhiteSpace(key)) keys.Add(key.Trim());
                    }

                    if (keys.Count == 0)
                    {
                        skipped++;
                        Log("Bỏ qua " + Path.GetFileName(item.Path) + " (không tick part nào).");
                        continue;
                    }

                    selectedParts += keys.Count;

                    if (keys.Count >= item.Info.Parts.Count)
                    {
                        plan.Add(item.Path);
                        continue;
                    }

                    EdzFilterResult filtered = EdzPackageFilter.Create(item.Path, keys, FilterTempDirectory);
                    if (!filtered.CreatedTempFile)
                    {
                        plan.Add(item.Path);
                        continue;
                    }

                    plan.Add(filtered.FileToImport);
                    tempFiles.Add(filtered.FileToImport);
                    Log(String.Format("Lọc {0}/{1} part: {2} → {3}",
                        filtered.SelectedParts, filtered.TotalParts,
                        Path.GetFileName(item.Path), Path.GetFileName(filtered.FileToImport)));
                    if (filtered.MissingEntries.Count > 0)
                    {
                        Log("Cảnh báo: không tìm thấy " + filtered.MissingEntries.Count +
                            " mục được manifest tham chiếu (ví dụ: " + filtered.MissingEntries[0] + ").");
                    }
                }
            }
            catch (Exception ex)
            {
                CleanupTempFiles(tempFiles);
                Log("LỖI lọc EDZ: " + ex.Message);
                AddinLog.WriteError("FilterEdz", ex);
                MessageBox.Show(this, "Không lọc được gói EDZ để nhập từng part:\n" + ex.Message,
                    "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (plan.Count == 0)
            {
                CleanupTempFiles(tempFiles);
                MessageBox.Show(this, "Chưa có part nào được tick để nhập.",
                    "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string detail = skipped > 0
                ? "\nBỏ qua " + skipped + " gói không tick part."
                : String.Empty;
            DialogResult confirm = MessageBox.Show(this,
                "Nhập " + plan.Count + " tệp EDZ (" + selectedParts + " part đã tick) vào Parts Database?" +
                detail +
                "\n\nMặc định: cập nhật part đã có + thêm part mới (theo mã hàng)." +
                "\nBạn nên kiểm tra trên database test trước khi nhập vào database chính.",
                "Tool Oumijia", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
            {
                CleanupTempFiles(tempFiles);
                return;
            }

            _startButton.Enabled = false;
            try
            {
                ArrayList files = new ArrayList(plan.ToArray());
                Log("Bắt đầu nhập " + files.Count + " tệp (" + selectedParts + " part; cập nhật + thêm mới, theo mã hàng) …");
                AddinLog.Write("Import: " + files.Count + " files, " + selectedParts +
                    " parts, mode=UpdateAndAppend, identifier=PartNumber");

                MDPartsManagement management = new MDPartsManagement();
                string scheme = ImportPartsSupport.Import(management, files,
                    FixedImportMode, FixedAdditionalLanguage, FixedIdentifier);

                Log("EPLAN đã xử lý xong yêu cầu nhập (scheme: '" + scheme + "').");
                AddinLog.Write("Import finished OK (scheme='" + scheme + "')");
                AddinCsvLog.Append("import", "OK", FixedImportMode.ToString(), FixedIdentifier.ToString(),
                    files.Count.ToString(), scheme);
                MessageBox.Show(this,
                    "Đã gửi yêu cầu nhập " + files.Count + " tệp EDZ cho EPLAN.\n" +
                    "Kiểm tra Parts Management để xác nhận kết quả.",
                    "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log("LỖI: " + ex.Message);
                AddinLog.WriteError("Import", ex);
                AddinCsvLog.Append("import", "ERROR", FixedImportMode.ToString(), ex.Message);
                MessageBox.Show(this, "Nhập thất bại:\n" + ex.Message, "Tool Oumijia",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                CleanupTempFiles(tempFiles);
                _startButton.Enabled = CountSelectedParts() > 0 || _items.Values.Any(v => v.Info == null);
                UpdateCount();
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
                // Không bao giờ để log làm hỏng thao tác.
            }
        }

        private static string OrEmpty(string value)
        {
            return String.IsNullOrWhiteSpace(value) ? "—" : value;
        }

        private static string ByteSize(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB" };
            double value = bytes;
            int unit = 0;
            while (value >= 1024 && unit < units.Length - 1) { value /= 1024; unit++; }
            return String.Format("{0:0.#} {1}", value, units[unit]);
        }

        private sealed class ImportItem
        {
            public readonly string Path;
            public readonly long FileSize;
            public readonly DateTime LastWrite;
            public EdzPackageInfo Info;
            public string Error;
            public bool PreviewLoaded;
            public string Status;
            public TreeNode Node;

            public ImportItem(string path)
            {
                Path = path;
                FileInfo file = new FileInfo(path);
                FileSize = file.Exists ? file.Length : 0;
                LastWrite = file.Exists ? file.LastWriteTime : DateTime.MinValue;
                Status = "Chưa đọc";
            }
        }
    }
}
