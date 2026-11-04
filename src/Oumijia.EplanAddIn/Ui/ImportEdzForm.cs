using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Eplan.EplApi.MasterData;

namespace Oumijia.EplanAddIn.Ui
{
    /// <summary>Batch import of EDZ packages into the parts database (MDPartsManagement.ImportParts).</summary>
    internal sealed class ImportEdzForm : Form
    {
        private readonly ListBox _fileList = new ListBox();
        private readonly ComboBox _modeCombo = new ComboBox();
        private readonly ComboBox _identifierCombo = new ComboBox();
        private readonly CheckBox _additionalLanguage = new CheckBox();
        private readonly TextBox _log = new TextBox();
        private readonly Button _startButton = new Button();

        public ImportEdzForm()
        {
            Text = "Tool Oumijia — Nhập EDZ vào Parts Database";
            Width = 800;
            Height = 620;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);
            BuildUi();
        }

        private void BuildUi()
        {
            var top = new Panel { Dock = DockStyle.Top, Height = 106 };

            var label = new Label { Text = "Các gói EDZ cần nhập:", AutoSize = true, Location = new Point(10, 8) };

            var btnAddFiles = new Button { Text = "Thêm tệp…", Location = new Point(10, 30), Size = new Size(96, 26) };
            btnAddFiles.Click += OnAddFilesClicked;
            var btnAddFolder = new Button { Text = "Thêm thư mục…", Location = new Point(112, 30), Size = new Size(112, 26) };
            btnAddFolder.Click += OnAddFolderClicked;
            var btnRemove = new Button { Text = "Xóa mục chọn", Location = new Point(230, 30), Size = new Size(100, 26) };
            btnRemove.Click += (s, e) =>
            {
                foreach (var item in _fileList.SelectedItems.Cast<object>().ToList())
                {
                    _fileList.Items.Remove(item);
                }
            };
            var btnClear = new Button { Text = "Xóa hết", Location = new Point(336, 30), Size = new Size(80, 26) };
            btnClear.Click += (s, e) => _fileList.Items.Clear();

            var lblMode = new Label { Text = "Chế độ nhập:", AutoSize = true, Location = new Point(10, 70) };
            _modeCombo.Location = new Point(96, 66);
            _modeCombo.Width = 220;
            _modeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _modeCombo.Items.Add("Chỉ thêm mới (AppendNewRecords)");
            _modeCombo.Items.Add("Chỉ cập nhật (UpdateExistingRecords)");
            _modeCombo.Items.Add("Cập nhật + thêm mới (UpdateAndAppend)");
            _modeCombo.SelectedIndex = 0;

            var lblIdent = new Label { Text = "Nhận dạng part:", AutoSize = true, Location = new Point(330, 70) };
            _identifierCombo.Location = new Point(430, 66);
            _identifierCombo.Width = 180;
            _identifierCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _identifierCombo.Items.Add("Theo mã hàng (PartNumber)");
            _identifierCombo.Items.Add("Theo UniqueId");
            _identifierCombo.Items.Add("UniqueId + mã hàng");
            _identifierCombo.SelectedIndex = 0;

            _additionalLanguage.Text = "Nhập thêm ngôn ngữ phụ";
            _additionalLanguage.AutoSize = true;
            _additionalLanguage.Location = new Point(622, 68);

            top.Controls.Add(label);
            top.Controls.Add(btnAddFiles);
            top.Controls.Add(btnAddFolder);
            top.Controls.Add(btnRemove);
            top.Controls.Add(btnClear);
            top.Controls.Add(lblMode);
            top.Controls.Add(_modeCombo);
            top.Controls.Add(lblIdent);
            top.Controls.Add(_identifierCombo);
            top.Controls.Add(_additionalLanguage);

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 176 };

            var buttonPanel = new Panel { Dock = DockStyle.Top, Height = 42 };
            _startButton.Text = "NHẬP VÀO PARTS DATABASE ▸";
            _startButton.Location = new Point(10, 6);
            _startButton.Size = new Size(236, 30);
            _startButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _startButton.Click += OnStartClicked;

            var hint = new Label
            {
                Text = "Khuyến nghị: kiểm tra trên database test trước khi nhập vào database chính.",
                AutoSize = true,
                Location = new Point(258, 14),
                ForeColor = Color.DimGray
            };

            buttonPanel.Controls.Add(_startButton);
            buttonPanel.Controls.Add(hint);

            _log.Dock = DockStyle.Fill;
            _log.Multiline = true;
            _log.ReadOnly = true;
            _log.ScrollBars = ScrollBars.Both;
            _log.WordWrap = false;
            _log.Font = new Font("Consolas", 8.5F);

            bottom.Controls.Add(_log);
            bottom.Controls.Add(buttonPanel);

            _fileList.Dock = DockStyle.Fill;
            _fileList.SelectionMode = SelectionMode.MultiExtended;

            Controls.Add(_fileList);
            Controls.Add(bottom);
            Controls.Add(top);
        }

        private void OnAddFilesClicked(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
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
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Chọn thư mục chứa EDZ (bao gồm thư mục con)";
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
        }

        private void AddDistinct(IEnumerable<string> files)
        {
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in _fileList.Items)
            {
                if (item != null) existing.Add(item.ToString());
            }

            foreach (var file in files)
            {
                if (string.IsNullOrWhiteSpace(file)) continue;
                if (existing.Add(file)) _fileList.Items.Add(file);
            }
        }

        private void OnStartClicked(object sender, EventArgs e)
        {
            if (_fileList.Items.Count == 0)
            {
                MessageBox.Show(this, "Chưa có gói EDZ nào trong danh sách.", "Tool Oumijia",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var mode = ModeFromCombo();
            var identifier = IdentifierFromCombo();

            var confirm = MessageBox.Show(this,
                "Nhập " + _fileList.Items.Count + " gói EDZ vào Parts Database?\n\n" +
                "Chế độ: " + _modeCombo.Text + "\n" +
                "Nhận dạng: " + _identifierCombo.Text + "\n\n" +
                "Bạn nên kiểm tra trên database test trước khi nhập vào database chính.",
                "Tool Oumijia", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            _startButton.Enabled = false;
            try
            {
                var files = new ArrayList();
                foreach (var item in _fileList.Items) files.Add(item.ToString());

                Log("Bắt đầu nhập " + files.Count + " gói (mode=" + mode + ", identifier=" + identifier + ") …");
                AddinLog.Write("Import: " + files.Count + " files, mode=" + mode + ", identifier=" + identifier);

                var management = new MDPartsManagement();
                management.ImportParts(files, "IXPartsImportExportEdz", mode, _additionalLanguage.Checked, identifier, null);

                Log("EPLAN đã xử lý xong yêu cầu nhập.");
                AddinLog.Write("Import finished OK");
                AddinCsvLog.Append("import", "OK", mode.ToString(), identifier.ToString(), files.Count.ToString());

                MessageBox.Show(this,
                    "Đã gửi yêu cầu nhập " + files.Count + " gói cho EPLAN.\n" +
                    "Kiểm tra Parts Management để xác nhận kết quả.",
                    "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log("LỖI: " + ex.Message);
                AddinLog.WriteError("Import", ex);
                AddinCsvLog.Append("import", "ERROR", mode.ToString(), ex.Message);
                MessageBox.Show(this, "Nhập thất bại:\n" + ex.Message, "Tool Oumijia",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _startButton.Enabled = true;
            }
        }

        private MDPartsManagement.ImportMode ModeFromCombo()
        {
            switch (_modeCombo.SelectedIndex)
            {
                case 1: return MDPartsManagement.ImportMode.UpdateExistingRecords;
                case 2: return MDPartsManagement.ImportMode.UpdateAndAppend;
                default: return MDPartsManagement.ImportMode.AppendNewRecords;
            }
        }

        private MDPartsManagement.ImportIdentifier IdentifierFromCombo()
        {
            switch (_identifierCombo.SelectedIndex)
            {
                case 1: return MDPartsManagement.ImportIdentifier.UniqueId;
                case 2: return MDPartsManagement.ImportIdentifier.UniqueIdOptPartNumber;
                default: return MDPartsManagement.ImportIdentifier.PartNumber;
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
}
