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

        private readonly ListBox _fileList = new ListBox();
        private readonly TextBox _log = new TextBox();
        private readonly Button _startButton = new Button();
        private readonly SplitContainer _split = new SplitContainer();
        private readonly Label _detailTitle = new Label();
        private readonly Label _detailFile = new Label();
        private readonly Label _detailSummary = new Label();
        private readonly Label _detailStatus = new Label();
        private readonly ListView _parts = new ListView();
        private readonly Label _selectedPart = new Label();
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
            Button btnRemove = new Button { Text = "Xóa mục chọn", Width = 112, Height = 28 };
            btnRemove.Click += OnRemoveClicked;
            Button btnClear = new Button { Text = "Xóa hết", Width = 82, Height = 28 };
            btnClear.Click += delegate
            {
                _fileList.Items.Clear();
                _items.Clear();
                RefreshDetails();
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

            _fileList.Dock = DockStyle.Fill;
            _fileList.SelectionMode = SelectionMode.MultiExtended;
            _fileList.HorizontalScrollbar = true;
            _fileList.BorderStyle = BorderStyle.FixedSingle;
            _fileList.SelectedIndexChanged += delegate { RefreshDetails(); };

            Panel listPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 8, 4, 8) };
            Label listCaption = new Label
            {
                Text = "Danh sách gói EDZ",
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(23, 56, 88)
            };
            listPanel.Controls.Add(_fileList);
            listPanel.Controls.Add(listCaption);

            Panel detailPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 8, 10, 8) };
            _detailTitle.Dock = DockStyle.Top;
            _detailTitle.Height = 24;
            _detailTitle.Padding = new Padding(2, 3, 0, 0);
            _detailTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _detailTitle.Text = "Chi tiết gói EDZ";

            _detailFile.Dock = DockStyle.Top;
            _detailFile.Height = 42;
            _detailFile.Padding = new Padding(2, 1, 0, 0);
            _detailFile.AutoEllipsis = true;
            _detailFile.ForeColor = Color.FromArgb(23, 56, 88);

            _detailSummary.Dock = DockStyle.Top;
            _detailSummary.Height = 24;
            _detailSummary.Padding = new Padding(2, 1, 0, 0);
            _detailSummary.ForeColor = Color.DimGray;

            _detailStatus.Dock = DockStyle.Top;
            _detailStatus.Height = 24;
            _detailStatus.Padding = new Padding(2, 2, 0, 0);
            _detailStatus.ForeColor = Color.FromArgb(23, 56, 88);

            Label gridCaption = new Label
            {
                Text = "Part trong EDZ · Tên | Hãng | Mã hàng",
                Dock = DockStyle.Top,
                Height = 24,
                Padding = new Padding(2, 4, 0, 0),
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };

            _parts.Dock = DockStyle.Fill;
            _parts.View = View.Details;
            _parts.FullRowSelect = true;
            _parts.GridLines = true;
            _parts.MultiSelect = false;
            _parts.HideSelection = false;
            _parts.Columns.Add("Tên thiết bị", 190);
            _parts.Columns.Add("Hãng", 100);
            _parts.Columns.Add("Mã hàng", 150);
            _parts.SelectedIndexChanged += OnSelectedPartChanged;
            _parts.Resize += delegate { AdjustGridColumns(); };

            _selectedPart.Dock = DockStyle.Bottom;
            _selectedPart.Height = 22;
            _selectedPart.Padding = new Padding(2, 2, 0, 0);
            _selectedPart.ForeColor = Color.DimGray;
            _selectedPart.Text = "Chọn một dòng để xem part.";

            detailPanel.Controls.Add(_parts);
            detailPanel.Controls.Add(_selectedPart);
            detailPanel.Controls.Add(gridCaption);
            detailPanel.Controls.Add(_detailStatus);
            detailPanel.Controls.Add(_detailSummary);
            detailPanel.Controls.Add(_detailFile);
            detailPanel.Controls.Add(_detailTitle);

            _split.Dock = DockStyle.Fill;
            _split.Panel1.Controls.Add(listPanel);
            _split.Panel2.Controls.Add(detailPanel);
            _split.Orientation = Orientation.Vertical;
            _split.FixedPanel = FixedPanel.None;

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
                _split.SplitterDistance = Math.Max(450, (int)(_split.Width * 0.58));
            }
            catch { /* giữ mặc định */ }
            AdjustGridColumns();
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
            List<string> remove = new List<string>();
            foreach (object item in _fileList.SelectedItems)
            {
                string path = item == null ? null : item.ToString();
                if (!String.IsNullOrWhiteSpace(path)) remove.Add(path);
            }
            foreach (string path in remove) _items.Remove(path);
            foreach (string path in remove) _fileList.Items.Remove(path);
            Log("Đã xóa " + remove.Count + " mục.");
            RefreshDetails();
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
                    _fileList.Items.Add(file);
                    added++;
                }
                else failed++;
            }

            Log("Đã thêm " + added + " gói EDZ" + (failed > 0 ? "; bỏ qua " + failed + " mục trùng" : "") + ".");
            UseWaitCursor = true;
            _startButton.Enabled = false;
            try
            {
                foreach (object selected in _fileList.Items)
                {
                    if (!(selected is string selectedPath) || !_items.ContainsKey(selectedPath) || _items[selectedPath].PreviewLoaded) continue;
                    ReadPreview(_items[selectedPath]);
                }
            }
            finally
            {
                UseWaitCursor = false;
                UpdateCount();
                RefreshDetails();
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
        }

        private void RefreshDetails()
        {
            string selectedPath = _fileList.SelectedItem == null ? null : _fileList.SelectedItem.ToString();
            ImportItem item;
            _parts.BeginUpdate();
            try
            {
                _parts.Items.Clear();
                if (selectedPath == null || !_items.TryGetValue(selectedPath, out item))
                {
                    _detailTitle.Text = "Chi tiết gói EDZ";
                    _detailFile.Text = "Chọn một file trong danh sách.";
                    _detailSummary.Text = "Tên · Hãng · Mã hàng được lấy cục bộ từ EDZ.";
                    _detailStatus.Text = "Chưa có file được chọn.";
                    _detailStatus.ForeColor = Color.DimGray;
                    _selectedPart.Text = "Chọn một dòng để xem part.";
                    return;
                }

                _detailTitle.Text = "Chi tiết: " + Path.GetFileName(item.Path);
                _detailFile.Text = item.Path;
                string size = ByteSize(item.FileSize);
                _detailSummary.Text = size + " · Sửa: " + item.LastWrite.ToString("dd/MM/yyyy HH:mm");
                if (item.Info != null)
                {
                    _detailStatus.Text = String.Format("Sẵn sàng · {0} part · {1}", item.Info.Parts.Count, item.Info.ContainerKind.ToUpperInvariant());
                    _detailStatus.ForeColor = Color.FromArgb(20, 104, 73);
                    foreach (EdzPartInfo part in item.Info.Parts.Take(500))
                    {
                        ListViewItem line = new ListViewItem(OrEmpty(part.Designation ?? part.SourceXmlEntry));
                        line.SubItems.Add(OrEmpty(part.Manufacturer));
                        line.SubItems.Add(OrEmpty(part.PartNumber ?? part.TypeNumber ?? part.OrderNumber));
                        _parts.Items.Add(line);
                    }
                }
                else
                {
                    _detailStatus.Text = "Không đọc được preview";
                    _detailStatus.ForeColor = Color.FromArgb(167, 73, 31);
                    if (!String.IsNullOrWhiteSpace(item.Error))
                        _detailSummary.Text += "\r\nLỗi: " + item.Error;
                }
                AdjustGridColumns();
            }
            finally
            {
                _parts.EndUpdate();
            }
        }

        private void AdjustGridColumns()
        {
            if (_parts.Columns.Count < 3 || _parts.ClientSize.Width <= 0) return;
            int third = Math.Max(90, (_parts.ClientSize.Width - 22) / 3);
            _parts.Columns[0].Width = third + 40;
            _parts.Columns[1].Width = Math.Max(96, third - 18);
            _parts.Columns[2].Width = Math.Max(120, _parts.ClientSize.Width - _parts.Columns[0].Width - _parts.Columns[1].Width - 26);
        }

        private void OnSelectedPartChanged(object sender, EventArgs e)
        {
            if (_parts.SelectedItems.Count != 1) return;
            ListViewItem line = _parts.SelectedItems[0];
            _selectedPart.Text = String.Format("{0} — {1} — {2}",
                line.SubItems.Count > 0 ? line.SubItems[0].Text : String.Empty,
                line.SubItems.Count > 1 ? line.SubItems[1].Text : String.Empty,
                line.SubItems.Count > 2 ? line.SubItems[2].Text : String.Empty);
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
