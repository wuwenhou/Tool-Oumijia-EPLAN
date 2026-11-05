using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Eplan.EplApi.ApplicationFramework;

namespace Oumijia.EplanAddIn.Ui
{
    /// <summary>
    /// Cửa sổ "EPLAN Classic": danh mục các lệnh quen thuộc trong menu cũ,
    /// có thể tìm kiếm và gọi action EPLAN đã được xác nhận là có mặt.
    /// </summary>
    internal sealed class EplanClassicForm : Form
    {
        private const string HelpUrl = "https://github.com/wuwenhou/Tool-Oumijia-EPLAN/blob/main/docs/THIRD-PARTY-ADDINS.md";

        private readonly ListBox _categoryList = new ListBox();
        private readonly TextBox _searchBox = new TextBox();
        private readonly ListView _commands = new ListView();
        private readonly Label _statusLabel = new Label();
        private readonly Button _runButton = new Button();
        private readonly Button _helpButton = new Button();

        private readonly List<ClassicCommand> _catalog = ClassicCommand.BuildCatalog();
        private List<ClassicCommand> _filtered = new List<ClassicCommand>();

        public EplanClassicForm()
        {
            Text = "Tool Oumijia — EPLAN Classic";
            ClientSize = new Size(980, 560);
            MinimumSize = new Size(760, 420);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9F);

            BuildUi();
            PopulateCategories();
            ShowCategory(0);
        }

        private void BuildUi()
        {
            var leftPanel = new Panel { Dock = DockStyle.Left, Width = 185 };
            var separator = new Panel { Dock = DockStyle.Left, Width = 1, BackColor = Color.FromArgb(190, 190, 190) };
            var mainPanel = new Panel { Dock = DockStyle.Fill };

            var sourceLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 34,
                Text = "Danh mục",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(6, 7, 0, 0)
            };

            _categoryList.Dock = DockStyle.Fill;
            _categoryList.IntegralHeight = false;
            _categoryList.SelectionMode = SelectionMode.One;
            _categoryList.SelectedIndexChanged += CategorySelectionChanged;
            leftPanel.Controls.Add(_categoryList);
            leftPanel.Controls.Add(sourceLabel);

            var searchPanel = new Panel { Dock = DockStyle.Top, Height = 38, Padding = new Padding(8, 7, 8, 5) };
            var searchLabel = new Label
            {
                Dock = DockStyle.Left,
                Width = 30,
                Text = "Tìm:",
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            };
            _searchBox.Dock = DockStyle.Fill;
            _searchBox.KeyDown += SearchBoxKeyDown;
            searchPanel.Controls.Add(_searchBox);
            searchPanel.Controls.Add(searchLabel);

            var listPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 8, 0) };
            _commands.Dock = DockStyle.Fill;
            _commands.View = View.Details;
            _commands.FullRowSelect = true;
            _commands.HideSelection = false;
            _commands.GridLines = true;
            _commands.MultiSelect = false;
            _commands.Columns.Add("Lệnh", 330);
            _commands.Columns.Add("Action", 230);
            _commands.Columns.Add("Trạng thái", 180);
            _commands.DoubleClick += OnCommandsDoubleClick;
            _commands.SelectedIndexChanged += UpdateSelection;
            listPanel.Controls.Add(_commands);

            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(8, 8, 8, 8) };
            _statusLabel.Dock = DockStyle.Left;
            _statusLabel.Width = 540;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            _statusLabel.ForeColor = Color.DimGray;

            _helpButton.Dock = DockStyle.Right;
            _helpButton.Width = 92;
            _helpButton.Text = "Help";
            _helpButton.Click += OnHelpClicked;

            _runButton.Dock = DockStyle.Right;
            _runButton.Width = 110;
            _runButton.Text = "Chạy lệnh ▸";
            _runButton.Enabled = false;
            _runButton.Click += OnRunClicked;

            bottomPanel.Controls.Add(_statusLabel);
            bottomPanel.Controls.Add(_runButton);
            bottomPanel.Controls.Add(_helpButton);

            mainPanel.Controls.Add(listPanel);
            mainPanel.Controls.Add(searchPanel);
            mainPanel.Controls.Add(bottomPanel);

            Controls.Add(mainPanel);
            Controls.Add(separator);
            Controls.Add(leftPanel);
        }

        private void PopulateCategories()
        {
            foreach (var category in _catalog.Select(x => x.Category).Distinct())
            {
                _categoryList.Items.Add(category);
            }

            // Luôn hiển thị đúng 12 nhóm của menu cổ điển, kể cả khi nhóm chưa có action usable.
            foreach (string category in ClassicCommand.MenuCategories)
            {
                if (!_categoryList.Items.Contains(category))
                {
                    _categoryList.Items.Add(category);
                }
            }

            if (_categoryList.Items.Count > 0)
            {
                _categoryList.SelectedIndex = 0;
            }
        }

        private void CategorySelectionChanged(object sender, EventArgs e)
        {
            ShowCategory(_categoryList.SelectedIndex);
        }

        private void ShowCategory(int index)
        {
            if (index < 0) return;
            string category = _categoryList.Items[index].ToString();
            ApplyFilter(_searchBox.Text, category);
        }

        private void SearchBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            string category = CurrentCategory();
            ApplyFilter(_searchBox.Text, category);
        }

        private void ApplyFilter(string query, string category)
        {
            query = (query ?? string.Empty).Trim().ToLowerInvariant();
            _filtered = _catalog
                .Where(x => x.Category == category)
                .Where(x => query.Length == 0
                    || x.Name.ToLowerInvariant().Contains(query)
                    || x.ActionId.ToLowerInvariant().Contains(query)
                    || (x.Source ?? string.Empty).ToLowerInvariant().Contains(query))
                .OrderBy(x => x.MenuIndex)
                .ToList();

            _commands.BeginUpdate();
            try
            {
                _commands.Items.Clear();
                foreach (ClassicCommand command in _filtered)
                {
                    bool available = command.IsAvailable();
                    ListViewItem item = new ListViewItem(command.Name);
                    item.SubItems.Add(command.ActionId);
                    item.SubItems.Add(available ? "Sẵn sàng" : "Không có trong phiên này");
                    item.Tag = command;
                    item.BackColor = available ? Color.White : Color.FromArgb(245, 245, 245);
                    item.ForeColor = available ? Color.Black : Color.DimGray;
                    _commands.Items.Add(item);
                }
            }
            finally
            {
                _commands.EndUpdate();
            }

            if (_filtered.Count == 0)
            {
                _statusLabel.Text = query.Length == 0
                    ? "Chưa xác định được lệnh của nhóm này trên phiên EPLAN hiện tại."
                    : "Không tìm thấy lệnh phù hợp trong nhóm này.";
            }
            else
            {
                _statusLabel.Text = _filtered.Count + " lệnh; hành động không sẵn sàng sẽ bị khóa.";
            }

            UpdateSelection(this, EventArgs.Empty);
        }

        private void UpdateSelection(object sender, EventArgs e)
        {
            ClassicCommand selection = SelectedCommand();
            _runButton.Enabled = selection != null && selection.IsAvailable();
            _statusLabel.Text = selection == null
                ? "Chọn một lệnh để xem trạng thái."
                : (selection.IsAvailable() ? "Sẵn sàng chạy: " + selection.ActionId : "Không sẵn sàng: " + selection.Note);
        }

        private ClassicCommand SelectedCommand()
        {
            if (_commands.SelectedItems.Count == 0) return null;
            return _commands.SelectedItems[0].Tag as ClassicCommand;
        }

        private string CurrentCategory()
        {
            return _categoryList.SelectedIndex < 0 ? ClassicCommand.MenuCategories[0] : _categoryList.Items[_categoryList.SelectedIndex].ToString();
        }

        private void OnCommandsDoubleClick(object sender, EventArgs e)
        {
            RunSelected();
        }

        private void OnRunClicked(object sender, EventArgs e)
        {
            RunSelected();
        }

        private void RunSelected()
        {
            ClassicCommand command = SelectedCommand();
            if (command == null) return;

            if (!command.IsAvailable())
            {
                MessageBox.Show(this,
                    "Lệnh chưa sẵn sàng:\n" + command.Note,
                    "Tool Oumijia — EPLAN Classic",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                bool ok = command.Run();
                _statusLabel.Text = ok ? "Đã chạy: " + command.ActionId : "Action trả về lỗi: " + command.ActionId;
                AddinLog.Write("EPLAN Classic: ran " + command.ActionId + " (ok=" + ok + ")");
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("EPLAN Classic run " + command.ActionId, ex);
                MessageBox.Show(this,
                    "Không chạy được lệnh:\n" + ex.Message,
                    "Tool Oumijia — EPLAN Classic",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnHelpClicked(object sender, EventArgs e)
        {
            try
            {
                Process.Start(HelpUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "Không mở được help:\n" + ex.Message,
                    "Tool Oumijia — EPLAN Classic",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    /// <summary>
    /// Action dùng cho ribbon/script khi EPLAN Classic cần mở cửa sổ từ ngoài.
    /// </summary>
    internal static class EplanClassicLauncher
    {
        public static bool Open()
        {
            try
            {
                using (var form = new EplanClassicForm())
                {
                    form.ShowDialog();
                }
                return true;
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("EPLAN Classic open", ex);
                return false;
            }
        }
    }

    internal sealed class ClassicCommand
    {
        public static readonly string[] MenuCategories =
        {
            "Project", "Page", "Layout space", "Edit", "View", "Insert",
            "Project data", "Find", "Options", "Utilities", "Window", "Help"
        };

        public readonly string Category;
        public readonly string Name;
        public readonly string ActionId;
        public readonly string Note;
        public readonly string Source;
        public readonly int MenuIndex;

        private ClassicCommand(string category, string name, string actionId, string source, string note, int menuIndex)
        {
            Category = category;
            Name = name;
            ActionId = actionId;
            Source = source;
            Note = note;
            MenuIndex = menuIndex;
        }

        public static List<ClassicCommand> BuildCatalog()
        {
            var result = new List<ClassicCommand>();
            Add(result, "Project", "Mở project", "ProjectOpen", "regPlatform.xml", "Action mở hộp chọn project", 0);
            Add(result, "Project", "So sánh project", "PrjManagementGUIIGfWindPrjCompareProject", "regPlatform.xml", "Mở so sánh project", 1);
            Add(result, "Project", "Backup project", "BackupProjectDialogShow", "regPlatform.xml", "Mở hộp Backup project", 2);
            Add(result, "Project", "Nén project", "CompressProjectDialogShow", "regPlatform.xml", "Mở hộp nén project", 3);
            Add(result, "Project", "Đóng project hiện tại", "PrjManagementGUIIGfWindPrjCloseProject", "regPlatform.xml", "Đóng project hiện tại", 4);

            Add(result, "Page", "Thêm trang", "XPageNewDialogShow", "regPlatform.xml", "Mở hộp thêm trang", 0);
            Add(result, "Page", "Trang kế tiếp", "XGedNextPageAction", "regPlatform.xml", "Chuyển sang trang kế tiếp", 1);
            Add(result, "Page", "Trang trước", "XGedPrevPageAction", "regPlatform.xml", "Chuyển sang trang trước", 2);
            Add(result, "Page", "Đóng trang", "XGedClosePage", "regPlatform.xml", "Đóng trang hiện tại", 3);
            Add(result, "Page", "Thuộc tính trang", "XPagePropertyDialogShow", "regPlatform.xml", "Mở thuộc tính trang", 4);

            Add(result, "Edit", "Căn lề ngang", "XGedIaAlignHorizontally", "regPlatform.xml", "Căn lề theo chiều ngang", 0);
            Add(result, "Edit", "Căn lề dọc", "XGedIaAlignVertically", "regPlatform.xml", "Căn lề theo chiều dọc", 1);
            Add(result, "Edit", "Căn đặt lên lưới", "XGedIaArrangeOnGrid", "regPlatform.xml", "Cần selection phù hợp; EPLAN kiểm tra điều kiện", 2);
            Add(result, "Edit", "Tìm phần tử không có property", "XKBeFindDmObjectsWithoutProperties", "regPlatform.xml", "Danh sách phần tử thiếu property", 3);

            Add(result, "View", "Bật/Tắt Menu Bar", "MfToggleMainMenuAction", "regPlatform.xml", "Toggle thanh menu chính của EPLAN (MF)", 0);
            Add(result, "View", "Bật/Tắt bắt dính lưới", "XGedToggleGridSnapAction", "regPlatform.xml", "Chỉ áp dụng khi cửa sổ vẽ EPLAN đang mở", 1);
            Add(result, "View", "Bật/Tắt bắt dính đối tượng", "XGedToggleObjectSnapAction", "regPlatform.xml", "Chỉ áp dụng khi cửa sổ vẽ EPLAN đang mở", 2);
            Add(result, "View", "Bật/Tắt Design mode", "XGedToggleCursorDesignAction", "regPlatform.xml", "Cursor placement cho special interactions", 3);
            Add(result, "View", "Bật/Tắt hiển thị lưới", "XGedToggleGridVisibilityAction", "regPlatform.xml", "Chỉ áp dụng khi có cửa sổ EPLAN", 4);

            Add(result, "Project data", "Nhập Parts List", "ImportPartslistAction", "regPlatform.xml", "Nhập parts list theo converter EPLAN", 0);
            Add(result, "Project data", "Xuất Parts List", "ExportPartslistAction", "regPlatform.xml", "Xuất parts list theo converter EPLAN", 1);
            Add(result, "Project data", "Nhập Parts quản lý bằng GUI", "PrjManagementGUIIGfWindImportProject", "regPlatform.xml", "Nhập project theo thao tác EPLAN", 2);
            Add(result, "Project data", "Xuất project", "PrjManagementGUIIGfWindPrjExportProject", "regPlatform.xml", "Mở thao tác xuất project", 3);

            Add(result, "Find", "Tìm trong EPLAN", "XSGedFindAction", "regPlatform.xml", "Tìm đối tượng/dữ liệu trong dự án", 0);
            Add(result, "Find", "Tìm connection point ngoài lưới", "XSGedFindConnectionPointsOutOfRasterAction", "regPlatform.xml", "Tìm connection point nằm ngoài raster", 1);

            Add(result, "Options", "Import settings", "AfImportSettingsAction", "regPlatform.xml", "Nhập settings từ file EPLAN", 0);
            Add(result, "Options", "Export settings", "AfExportSettingsAction", "regPlatform.xml", "Xuất settings ra file EPLAN", 1);
            Add(result, "Options", "Thiết lập hiển thị lưới", "XGedToggleGridVisibilityAction", "regPlatform.xml", "Toggle hiển thị lưới", 2);

            Add(result, "Utilities", "Dịch database", "translate", "regPlatform.xml", "Chạy thao tác dịch của EPLAN (kiểm tra trước khi chạy hàng loạt)", 0);
            Add(result, "Utilities", "Backup project", "BackupProjectDialogShow", "regPlatform.xml", "Mở hộp Backup project", 1);

            return result;
        }

        private static void Add(List<ClassicCommand> list, string category, string name, string actionId, string source, string note, int menuIndex)
        {
            list.Add(new ClassicCommand(category, name, actionId, source, note, menuIndex));
        }

        public bool IsAvailable()
        {
            if (string.IsNullOrWhiteSpace(ActionId)) return false;
            try
            {
                var manager = new ActionManager();
                var action = manager.FindAction(ActionId);
                return action != null;
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("ClassicCommand IsAvailable " + ActionId, ex);
                return false;
            }
        }

        public bool Run()
        {
            if (!IsAvailable()) return false;

            var manager = new ActionManager();
            var action = manager.FindAction(ActionId);
            if (action == null) return false;

            var context = new ActionCallingContext();
            return action.Execute(context);
        }
    }
}
