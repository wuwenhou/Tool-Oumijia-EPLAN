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
    /// <summary>
    /// Nhập batch gói EDZ vào Parts Database (MDPartsManagement.ImportParts).
    ///
    /// Giao diện ĐƠN GIẢN theo yêu cầu người dùng: chọn file/thư mục → NHẬP.
    /// Các tùy chọn kỹ thuật được cố định ở hậu trường (giá trị hợp lý nhất):
    ///   - Chế độ nhập      : Cập nhật + thêm mới (UpdateAndAppend)
    ///   - Nhận dạng part   : Theo mã hàng (PartNumber)
    ///   - Ngôn ngữ phụ     : không
    /// </summary>
    internal sealed class ImportEdzForm : Form
    {
        // ----- mặc định cố định (đơn giản hóa giao diện)
        private const MDPartsManagement.ImportMode FixedImportMode = MDPartsManagement.ImportMode.UpdateAndAppend;
        private const MDPartsManagement.ImportIdentifier FixedIdentifier = MDPartsManagement.ImportIdentifier.PartNumber;
        private const bool FixedAdditionalLanguage = false;

        private readonly ListBox _fileList = new ListBox();
        private readonly TextBox _log = new TextBox();
        private readonly Button _startButton = new Button();

        public ImportEdzForm()
        {
            Text = "Tool Oumijia — Nhập EDZ vào Parts Database";
            ClientSize = new Size(880, 620);
            MinimumSize = new Size(700, 480);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);
            BuildUi();
        }

        private void BuildUi()
        {
            // ----- trên: tiêu đề + nút thêm file + ghi chú mặc định
            var topPanel = new Panel { Dock = DockStyle.Top, Height = 94 };

            var label = new Label
            {
                Text = "Các gói EDZ cần nhập:",
                Dock = DockStyle.Top,
                Height = 24,
                Padding = new Padding(4, 4, 0, 0)
            };

            var buttonFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 34,
                WrapContents = false,
                Padding = new Padding(4, 2, 0, 0)
            };

            var btnAddFiles = new Button { Text = "Thêm tệp…", Width = 96, Height = 26 };
            btnAddFiles.Click += OnAddFilesClicked;
            var btnAddFolder = new Button { Text = "Thêm thư mục…", Width = 116, Height = 26 };
            btnAddFolder.Click += OnAddFolderClicked;
            var btnRemove = new Button { Text = "Xóa mục chọn", Width = 100, Height = 26 };
            btnRemove.Click += (s, e) =>
            {
                foreach (var item in _fileList.SelectedItems.Cast<object>().ToList())
                {
                    _fileList.Items.Remove(item);
                }
            };
            var btnClear = new Button { Text = "Xóa hết", Width = 80, Height = 26 };
            btnClear.Click += (s, e) => _fileList.Items.Clear();

            buttonFlow.Controls.Add(btnAddFiles);
            buttonFlow.Controls.Add(btnAddFolder);
            buttonFlow.Controls.Add(btnRemove);
            buttonFlow.Controls.Add(btnClear);

            var hint = new Label
            {
                Dock = DockStyle.Top,
                Height = 32,
                Padding = new Padding(4, 2, 0, 0),
                ForeColor = Color.DimGray,
                Text = "Mặc định khi nhập: cập nhật part đã có + thêm part mới (nhận dạng theo mã hàng).\r\n" +
                       "Khuyến nghị: thử trên database test trước khi nhập vào database chính."
            };

            topPanel.Controls.Add(hint);
            topPanel.Controls.Add(buttonFlow);
            topPanel.Controls.Add(label);

            // ----- đáy: nút nhập + log
            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 220 };

            var actionFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 44,
                WrapContents = false,
                Padding = new Padding(4, 6, 0, 0)
            };

            _startButton.Text = "NHẬP VÀO PARTS DATABASE ▸";
            _startButton.Size = new Size(220, 30);
            _startButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _startButton.Click += OnStartClicked;
            actionFlow.Controls.Add(_startButton);

            _log.Dock = DockStyle.Fill;
            _log.Multiline = true;
            _log.ReadOnly = true;
            _log.ScrollBars = ScrollBars.Both;
            _log.WordWrap = false;
            _log.Font = new Font("Consolas", 8.5F);

            bottomPanel.Controls.Add(_log);
            bottomPanel.Controls.Add(actionFlow);

            _fileList.Dock = DockStyle.Fill;
            _fileList.SelectionMode = SelectionMode.MultiExtended;

            Controls.Add(_fileList);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);
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

            var added = 0;
            foreach (var file in files)
            {
                if (string.IsNullOrWhiteSpace(file)) continue;
                if (existing.Add(file))
                {
                    _fileList.Items.Add(file);
                    added++;
                }
            }

            Log("Đã thêm " + added + " gói EDZ (tổng: " + _fileList.Items.Count + ").");
        }

        private void OnStartClicked(object sender, EventArgs e)
        {
            if (_fileList.Items.Count == 0)
            {
                MessageBox.Show(this, "Chưa có gói EDZ nào trong danh sách.", "Tool Oumijia",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(this,
                "Nhập " + _fileList.Items.Count + " gói EDZ vào Parts Database?\n\n" +
                "Mặc định: cập nhật part đã có + thêm part mới (theo mã hàng).\n\n" +
                "Bạn nên kiểm tra trên database test trước khi nhập vào database chính.",
                "Tool Oumijia", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            _startButton.Enabled = false;
            try
            {
                var files = new ArrayList();
                foreach (var item in _fileList.Items) files.Add(item.ToString());

                Log("Bắt đầu nhập " + files.Count + " gói (chế độ cập nhật + thêm mới, theo mã hàng) …");
                AddinLog.Write("Import: " + files.Count + " files, mode=UpdateAndAppend, identifier=PartNumber");

                var management = new MDPartsManagement();
                var scheme = ImportPartsSupport.Import(management, files,
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
                _startButton.Enabled = true;
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
    }
}
