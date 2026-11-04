using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Eplan.EplApi.MasterData;

namespace Oumijia.EplanAddIn.Ui
{
    /// <summary>Export parts from the parts database to EDZ (MDPartsDatabase.ExportParts + DataFormat.EDZ).</summary>
    internal sealed class ExportEdzForm : Form
    {
        private readonly RadioButton _scopeAll = new RadioButton();
        private readonly RadioButton _scopeSelected = new RadioButton();
        private readonly RadioButton _outputSingle = new RadioButton();
        private readonly RadioButton _outputPerPart = new RadioButton();
        private readonly TextBox _target = new TextBox();
        private readonly TextBox _log = new TextBox();
        private readonly Button _startButton = new Button();

        public ExportEdzForm()
        {
            Text = "Tool Oumijia — Xuất Parts Database ra EDZ";
            Width = 800;
            Height = 620;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);
            BuildUi();
        }

        private void BuildUi()
        {
            var top = new Panel { Dock = DockStyle.Top, Height = 178 };

            var scopeGroup = new GroupBox { Text = "Phạm vi part", Location = new Point(10, 6), Size = new Size(764, 68) };
            _scopeAll.Text = "Toàn bộ Parts Database đang mở";
            _scopeAll.AutoSize = true;
            _scopeAll.Location = new Point(14, 22);
            _scopeAll.Checked = true;
            _scopeSelected.Text = "Các part đang chọn trong cửa sổ Parts Management";
            _scopeSelected.AutoSize = true;
            _scopeSelected.Location = new Point(14, 44);
            scopeGroup.Controls.Add(_scopeAll);
            scopeGroup.Controls.Add(_scopeSelected);

            var outputGroup = new GroupBox { Text = "Cách xuất", Location = new Point(10, 80), Size = new Size(764, 92) };
            _outputSingle.Text = "Một file EDZ duy nhất";
            _outputSingle.AutoSize = true;
            _outputSingle.Location = new Point(14, 22);
            _outputSingle.Checked = true;
            _outputSingle.CheckedChanged += (s, e) => UpdateBrowseHint();
            _outputPerPart.Text = "Mỗi part một file EDZ (xuất vào thư mục)";
            _outputPerPart.AutoSize = true;
            _outputPerPart.Location = new Point(14, 44);
            _outputPerPart.CheckedChanged += (s, e) => UpdateBrowseHint();

            _target.Location = new Point(14, 66);
            _target.Width = 630;

            var browse = new Button { Text = "Chọn…", Location = new Point(652, 64), Size = new Size(96, 25) };
            browse.Click += OnBrowseClicked;

            outputGroup.Controls.Add(_outputSingle);
            outputGroup.Controls.Add(_outputPerPart);
            outputGroup.Controls.Add(_target);
            outputGroup.Controls.Add(browse);

            top.Controls.Add(scopeGroup);
            top.Controls.Add(outputGroup);

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 176 };

            var buttonPanel = new Panel { Dock = DockStyle.Top, Height = 42 };
            _startButton.Text = "XUẤT RA EDZ ▸";
            _startButton.Location = new Point(10, 6);
            _startButton.Size = new Size(160, 30);
            _startButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _startButton.Click += OnStartClicked;

            var hint = new Label
            {
                Text = "EDZ chứa part data + macro/tài liệu tham chiếu (do converter IXPartsImportExportEdz của EPLAN sinh ra).",
                AutoSize = true,
                Location = new Point(184, 14),
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

            Controls.Add(bottom);
            Controls.Add(top);
        }

        private void UpdateBrowseHint()
        {
            // Nothing dynamic needed; the browse dialog adapts to the selected mode.
        }

        private void OnBrowseClicked(object sender, EventArgs e)
        {
            if (_outputSingle.Checked)
            {
                using (var dialog = new SaveFileDialog())
                {
                    dialog.Title = "Chọn file EDZ đích";
                    dialog.Filter = "Gói EPLAN (*.edz)|*.edz";
                    dialog.FileName = "PartsDatabase.edz";
                    if (dialog.ShowDialog(this) == DialogResult.OK) _target.Text = dialog.FileName;
                }
            }
            else
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.Description = "Chọn thư mục đích (mỗi part một file .edz)";
                    if (dialog.ShowDialog(this) == DialogResult.OK) _target.Text = dialog.SelectedPath;
                }
            }
        }

        private void OnStartClicked(object sender, EventArgs e)
        {
            var management = new MDPartsManagement();

            MDPartsDatabase database = null;
            try
            {
                database = management.SelectedPartsDatabase;
            }
            catch (Exception ex)
            {
                AddinLog.Write("Export: probe SelectedPartsDatabase failed: " + ex.Message);
            }

            if (database == null)
            {
                MessageBox.Show(this,
                    "Chưa có Parts Database nào đang mở trong phiên EPLAN này.\n\n" +
                    "Hãy mở: Master data → Parts → Management (chọn database) rồi thực hiện lại.",
                    "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<MDPart> parts;
            if (_scopeAll.Checked)
            {
                parts = new List<MDPart>(database.Parts);
            }
            else
            {
                parts = new List<MDPart>();
                try
                {
                    foreach (var item in management.GetSelectedPartDatabaseItems(false))
                    {
                        var part = item as MDPart;
                        if (part != null) parts.Add(part);
                    }
                }
                catch (Exception ex)
                {
                    Log("Không đọc được danh sách part đang chọn: " + ex.Message);
                }
            }

            if (parts.Count == 0)
            {
                MessageBox.Show(this,
                    _scopeAll.Checked
                        ? "Parts Database không có part nào."
                        : "Không có part nào đang được chọn trong Parts Management.\nHãy chọn part trong cửa sổ đó rồi thực hiện lại.",
                    "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var target = (_target.Text ?? "").Trim();
            if (target.Length == 0)
            {
                MessageBox.Show(this, "Chưa chọn đích xuất (file hoặc thư mục).", "Tool Oumijia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(this,
                "Xuất " + parts.Count + " part ra EDZ?\n\n" +
                "Đích: " + target + "\n" +
                "Cách xuất: " + (_outputSingle.Checked ? "1 file duy nhất" : "mỗi part 1 file"),
                "Tool Oumijia", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            _startButton.Enabled = false;
            try
            {
                if (_outputSingle.Checked)
                {
                    if (!target.EndsWith(".edz", StringComparison.OrdinalIgnoreCase)) target += ".edz";

                    Log("Xuất " + parts.Count + " part → " + target);
                    database.ExportParts(target, MDPartsDatabase.DataFormat.EDZ, parts);
                    Log("Hoàn tất.");
                    AddinLog.Write("Export single OK: " + parts.Count + " parts -> " + target);
                    AddinCsvLog.Append("export", "OK-single", target, parts.Count.ToString());

                    MessageBox.Show(this, "Đã xuất " + parts.Count + " part ra:\n" + target, "Tool Oumijia",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    Directory.CreateDirectory(target);
                    int ok = 0, failed = 0;

                    foreach (var part in parts)
                    {
                        var partNumber = part.PartNr ?? "part";
                        try
                        {
                            var filePath = Path.Combine(target, SanitizeFileName(partNumber) + ".edz");
                            database.ExportParts(filePath, MDPartsDatabase.DataFormat.EDZ, new[] { part });
                            ok++;
                            Log("OK: " + Path.GetFileName(filePath));
                            AddinCsvLog.Append("export", "OK", filePath);
                        }
                        catch (Exception exPart)
                        {
                            failed++;
                            Log("LỖI " + partNumber + ": " + exPart.Message);
                            AddinCsvLog.Append("export", "ERROR", partNumber, exPart.Message);
                        }
                    }

                    AddinLog.Write("Export per-part done: ok=" + ok + ", failed=" + failed + ", dir=" + target);
                    MessageBox.Show(this,
                        "Hoàn tất xuất từng part:\n• Thành công: " + ok + "\n• Lỗi: " + failed + "\n\nThư mục: " + target,
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
                _startButton.Enabled = true;
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
