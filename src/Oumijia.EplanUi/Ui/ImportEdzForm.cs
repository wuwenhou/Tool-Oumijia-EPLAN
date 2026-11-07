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

        public ImportEdzForm()
            : this(null)
        {
        }

        /// <summary>Mở hộp Nhập và nạp sẵn các tệp EDZ đã tải từ Data Portal.</summary>
        public ImportEdzForm(IEnumerable<string> initialFiles)
        {
            Text = "Tool Oumijia — Nhập EDZ";
            ClientSize = new Size(1120, 780);
            MinimumSize = new Size(940, 640);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);
            BuildUi();
            if (initialFiles != null) AddDistinct(initialFiles);
            UpdateCount();
        }

        private void BuildUi()
        {
            Panel topPanel = new Panel { Dock = DockStyle.Top, Height = 96, Padding = new Padding(8, 6, 8, 0) };
            Label label = new Label
            {
                Text = "Các gói EDZ cần nhập",
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(23, 56, 88),
                Padding = new Padding(2, 0, 0, 0)
            };

            FlowLayoutPanel buttonFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 34,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 0)
            };

            Button btnAddFiles = new Button { Text = "Thêm tệp…", Width = 104, Height = 28 };
            btnAddFiles.Click += OnAddFilesClicked;
            Button btnAddFolder = new Button { Text = "Thêm thư mục…", Width = 124, Height = 28 };
            btnAddFolder.Click += OnAddFolderClicked;
            Button btnRemove = new Button { Text = "Xóa gói chọn", Width = 112, Height = 28 };
            btnRemove.Click += OnRemoveClicked;
            Button btnClear = new Button { Text = "Xóa hết", Width = 82, Height = 28 };
            btnClear.Click += delegate
            {
                _tree.Nodes.Clear();
                _items.Clear();
                ShowDetails(null);
                UpdateCount();
                Log("Đã xóa toàn bộ danh sách.");
            };

            buttonFlow.Controls.Add(btnAddFiles);
            buttonFlow.Controls.Add(btnAddFolder);
            buttonFlow.Controls.Add(btnRemove);
            buttonFlow.Controls.Add(btnClear);

            Label hint = new Label
            {
                Dock = DockStyle.Top,
                Height = 20,
                ForeColor = Color.DimGray,
                Padding = new Padding(2, 1, 0, 0),
                Text = "Mặc định: cập nhật part đã có + thêm part mới; nhận dạng theo mã hàng."
            };
            topPanel.Controls.Add(hint);
            topPanel.Controls.Add(buttonFlow);
            topPanel.Controls.Add(label);

            _tree.Dock = DockStyle.Fill;
            _tree.BorderStyle = BorderStyle.FixedSingle;
            _tree.HideSelection = false;
            _tree.ShowLines = true;
            _tree.ShowRootLines = true;
            _tree.ItemHeight = 22;
            _tree.AfterSelect += delegate (object sender, TreeViewEventArgs args) { ShowDetails(args.Node); };

            Panel listPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 8, 4, 8) };
            Label listCaption = new Label
            {
                Text = "Gói EDZ → part (chọn part để xem chi tiết)",
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(23, 56, 88)
            };
            listPanel.Controls.Add(_tree);
            listPanel.Controls.Add(listCaption);

            Panel detailPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 8, 10, 8) };
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

            detailPanel.Controls.Add(_details);
            detailPanel.Controls.Add(_detailsHeader);

            _split.Dock = DockStyle.Fill;
            _split.Panel1.Controls.Add(listPanel);
            _split.Panel2.Controls.Add(detailPanel);
            _split.Orientation = Orientation.Vertical;
            _split.FixedPanel = FixedPanel.None;
            _split.Panel2.Resize += delegate { AdjustDetailsColumns(); };
            _split.SplitterMoved += delegate { AdjustDetailsColumns(); };

            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 190 };

            Panel actionPanel = new Panel { Dock = DockStyle.Top, Height = 44 };
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
            _status.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _status.ForeColor = Color.FromArgb(23, 56, 88);

            _startButton.Text = "NHẬP VÀO PARTS DATABASE ▸";
            _startButton.Size = new Size(260, 34);
            _startButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _startButton.Enabled = false;
            _startButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _startButton.Click += OnStartClicked;

            actionLayout.Controls.Add(_status, 0, 0);
            actionLayout.Controls.Add(_startButton, 1, 0);
            actionPanel.Controls.Add(actionLayout);

            _log.Dock = DockStyle.Fill;
            _log.Multiline = true;
            _log.ReadOnly = true;
            _log.ScrollBars = ScrollBars.Both;
            _log.WordWrap = false;
            _log.Font = new Font("Consolas", ConsolasFontSize());

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

        private static float ConsolasFontSize()
        {
            return 8.5F;
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

        private void ReadPreview(ImportItem item)
        {
            item.PreviewLoaded = true;
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
                string label = FirstValue(part.Designation, part.PartNumber, part.TypeNumber,
                    part.OrderNumber, part.SourceXmlEntry);
                TreeNode partNode = new TreeNode(OrEmpty(label));
                partNode.Tag = part;
                node.Nodes.Add(partNode);
            }
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
                ReadProperty(part, "ARTICLE_MANUFACTURER_NAME")));
            AddDetail("Nhà cung cấp (Supplier)", CombineCodeAndName(
                ReadProperty(part, "ARTICLE_SUPPLIER"), ReadProperty(part, "ARTICLE_SUPPLIER_NAME")));
            AddDetail("Nhóm sản phẩm", part.ProductGroup);
            AddDetail("Tên 1 (Designation 1)", part.Designation);
            AddDetail("Tên 2 (Designation 2)", ReadProperty(part, "ARTICLE_DESCR2", "DESIGNATION2"));
            AddDetail("Tên 3 (Designation 3)", ReadProperty(part, "ARTICLE_DESCR3", "DESIGNATION3"));
            AddDetail("Mô tả (Description)", ReadProperty(part, "ARTICLE_NOTE", "NOTE"));
            AddDetail("Văn bản bổ sung", ReadProperty(part, "ARTICLE_ADDITIONALTEXT", "ADDITIONALTEXT"));
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
            try
            {
                if (_details.Columns.Count >= 2 && _details.ClientSize.Width > 0)
                    _details.Columns[1].Width = Math.Max(120, _details.ClientSize.Width - _details.Columns[0].Width - 26);
            }
            catch { /* giữ kích thước mặc định */ }
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
            _status.Text = String.Format("Đã nạp: {0} | Preview: {1} | Lỗi: {2}", _items.Count, ok, failed);
            _startButton.Enabled = _items.Count > 0;
        }

        private void OnStartClicked(object sender, EventArgs e)
        {
            if (_items.Count == 0)
            {
                MessageBox.Show(this, "Chưa có gói EDZ nào trong danh sách.", "Tool Oumijia",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult confirm = MessageBox.Show(this,
                "Nhập " + _items.Count + " gói EDZ vào Parts Database?\n\n" +
                "Mặc định: cập nhật part đã có + thêm part mới (theo mã hàng).\n\n" +
                "Bạn nên kiểm tra trên database test trước khi nhập vào database chính.",
                "Tool Oumijia", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            _startButton.Enabled = false;
            try
            {
                ArrayList files = new ArrayList(_items.Keys.ToArray());
                Log("Bắt đầu nhập " + files.Count + " gói (cập nhật + thêm mới, theo mã hàng) …");
                AddinLog.Write("Import: " + files.Count + " files, mode=UpdateAndAppend, identifier=PartNumber");

                MDPartsManagement management = new MDPartsManagement();
                string scheme = ImportPartsSupport.Import(management, files,
                    FixedImportMode, FixedAdditionalLanguage, FixedIdentifier);

                Log("EPLAN đã xử lý xong yêu cầu nhập (scheme: '" + scheme + "').");
                AddinLog.Write("Import finished OK (scheme='" + scheme + "')");
                AddinCsvLog.Append("import", "OK", FixedImportMode.ToString(), FixedIdentifier.ToString(),
                    files.Count.ToString(), scheme);
                MessageBox.Show(this,
                    "Đã gửi yêu cầu nhập " + files.Count + " gói cho EPLAN.\n" +
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
                _startButton.Enabled = _items.Count > 0;
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
