using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Oumijia.PortalHost
{
    /// <summary>
    /// Cửa sổ EPLAN Data Portal nhúng bằng Microsoft Edge WebView2.
    ///
    /// Người dùng tự đăng nhập trên trang của EPLAN; Tool không lưu tài khoản,
    /// không đọc/ghi credential. Mọi tệp tải về được đặt vào thư mục người dùng chọn
    /// và ghi một dòng vào CSV (URL đã bỏ query/fragment).
    /// </summary>
    public sealed class PortalForm : Form
    {
        public const string HomeUrl = "https://dataportal.eplan.com";
        private const string RuntimeDownloadUrl = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";

        private readonly string _userDataFolder;
        private readonly string _logDirectory;
        private readonly List<string> _downloadedFiles = new List<string>();
        private bool _webViewReady;

        private readonly WebView2 _webView = new WebView2();
        private readonly Panel _webHost = new Panel();
        private readonly Panel _fallbackPanel = new Panel();
        private readonly Label _fallbackMessage = new Label();
        private readonly TextBox _address = new TextBox();
        private readonly Button _back = new Button();
        private readonly Button _forward = new Button();
        private readonly Button _reload = new Button();
        private readonly Button _external = new Button();
        private readonly TextBox _downloadFolder = new TextBox();
        private readonly Button _chooseFolder = new Button();
        private readonly Button _openFolder = new Button();
        private readonly Button _clearData = new Button();
        private readonly Label _status = new Label();
        private readonly ProgressBar _progress = new ProgressBar();
        private readonly Button _importButton = new Button();

        static PortalForm()
        {
            PortalEnvironment.Ensure();
        }

        public PortalForm()
            : this(null, null, null)
        {
        }

        public PortalForm(string userDataFolder, string downloadFolder, string logDirectory)
        {
            _userDataFolder = string.IsNullOrWhiteSpace(userDataFolder)
                ? PortalPaths.DefaultUserDataFolder
                : userDataFolder.Trim();
            _logDirectory = string.IsNullOrWhiteSpace(logDirectory)
                ? PortalPaths.LogDirectory
                : logDirectory.Trim();

            string initialFolder = string.IsNullOrWhiteSpace(downloadFolder)
                ? PortalPaths.LoadDownloadFolder()
                : downloadFolder.Trim();

            if (string.IsNullOrWhiteSpace(initialFolder))
            {
                initialFolder = PortalPaths.DefaultDownloadFolder;
            }

            Text = "Tool Oumijia — EPLAN Data Portal";
            ClientSize = new Size(1200, 800);
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);
            Icon = SystemIcons.Application;

            BuildUi(initialFolder);
            Shown += OnShown;
            FormClosing += OnFormClosing;
        }

        /// <summary>Các tệp đã tải trong phiên này (đường dẫn tuyệt đối).</summary>
        public List<string> DownloadedFiles
        {
            get { return new List<string>(_downloadedFiles); }
        }

        /// <summary>Người dùng bấm "Nhập EDZ vừa tải" để đóng cửa sổ và chuyển sang hộp Nhập.</summary>
        public bool RequestImport { get; private set; }

        /// <summary>Dùng cho kiểm thử: WebView2 đã khởi tạo xong chưa.</summary>
        public bool IsWebViewReady
        {
            get { return _webViewReady && _webView.CoreWebView2 != null; }
        }

        public string DownloadFolder
        {
            get { return (_downloadFolder.Text ?? string.Empty).Trim(); }
        }

        public string UserDataFolder
        {
            get { return _userDataFolder; }
        }

        // ================================================================ UI

        private void BuildUi(string initialFolder)
        {
            // ----- thanh địa chỉ
            var nav = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 38,
                ColumnCount = 5,
                RowCount = 1,
                Padding = new Padding(6, 4, 6, 4)
            };
            nav.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36));
            nav.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36));
            nav.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36));
            nav.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            nav.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));

            ConfigureNavButton(_back, "←", "Quay lại");
            ConfigureNavButton(_forward, "→", "Tiến tới");
            ConfigureNavButton(_reload, "⟳", "Tải lại");
            _back.Click += OnBackClicked;
            _forward.Click += OnForwardClicked;
            _reload.Click += OnReloadClicked;

            _address.Dock = DockStyle.Fill;
            _address.Margin = new Padding(0, 0, 8, 0);
            _address.KeyDown += OnAddressKeyDown;

            _external.Text = "Mở ngoài";
            _external.Dock = DockStyle.Fill;
            _external.Margin = new Padding(0, 0, 0, 0);
            _external.Click += delegate { OpenExternal(CurrentUrl()); };

            nav.Controls.Add(_back, 0, 0);
            nav.Controls.Add(_forward, 1, 0);
            nav.Controls.Add(_reload, 2, 0);
            nav.Controls.Add(_address, 3, 0);
            nav.Controls.Add(_external, 4, 0);

            // ----- hàng thư mục tải
            var downloadBar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 36,
                ColumnCount = 6,
                RowCount = 1,
                Padding = new Padding(6, 3, 6, 3)
            };
            downloadBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            downloadBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            downloadBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
            downloadBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
            downloadBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 138));
            downloadBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 12));

            var folderLabel = new Label
            {
                Text = "Thư mục tải:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _downloadFolder.Dock = DockStyle.Fill;
            _downloadFolder.Margin = new Padding(0, 0, 6, 0);

            _chooseFolder.Text = "Chọn…";
            _chooseFolder.Dock = DockStyle.Fill;
            _chooseFolder.Margin = new Padding(0, 0, 6, 0);
            _chooseFolder.Click += OnChooseFolderClicked;

            _openFolder.Text = "Mở thư mục";
            _openFolder.Dock = DockStyle.Fill;
            _openFolder.Margin = new Padding(0, 0, 6, 0);
            _openFolder.Click += OnOpenFolderClicked;

            _clearData.Text = "Xóa dữ liệu phiên";
            _clearData.Dock = DockStyle.Fill;
            _clearData.Margin = new Padding(0, 0, 0, 0);
            _clearData.Click += OnClearDataClicked;

            downloadBar.Controls.Add(folderLabel, 0, 0);
            downloadBar.Controls.Add(_downloadFolder, 1, 0);
            downloadBar.Controls.Add(_chooseFolder, 2, 0);
            downloadBar.Controls.Add(_openFolder, 3, 0);
            downloadBar.Controls.Add(_clearData, 4, 0);

            // ----- vùng WebView
            _webHost.Dock = DockStyle.Fill;
            _webHost.BackColor = Color.White;
            _webView.Dock = DockStyle.Fill;
            _webHost.Controls.Add(_webView);
            _webHost.Controls.Add(_fallbackPanel);
            BuildFallbackPanel();

            // ----- thanh trạng thái + nút nhập
            var bottom = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(6, 5, 6, 5)
            };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));

            _status.Dock = DockStyle.Fill;
            _status.TextAlign = ContentAlignment.MiddleLeft;
            _status.ForeColor = Color.DimGray;
            _status.Text = "Sẵn sàng.";

            _progress.Dock = DockStyle.Fill;
            _progress.Margin = new Padding(0, 9, 12, 9);
            _progress.Style = ProgressBarStyle.Continuous;
            _progress.Minimum = 0;
            _progress.Maximum = 100;
            _progress.Visible = false;

            _importButton.Text = "Nhập EDZ vừa tải";
            _importButton.Dock = DockStyle.Fill;
            _importButton.Enabled = false;
            _importButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _importButton.Click += OnImportClicked;

            bottom.Controls.Add(_status, 0, 0);
            bottom.Controls.Add(_progress, 1, 0);
            bottom.Controls.Add(_importButton, 2, 0);

            Controls.Add(_webHost);
            Controls.Add(bottom);
            Controls.Add(downloadBar);
            Controls.Add(nav);

            _downloadFolder.Text = initialFolder;
        }

        private static void ConfigureNavButton(Button button, string text, string tip)
        {
            button.Text = text;
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(0, 0, 4, 0);
            button.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            var toolTip = new ToolTip();
            toolTip.SetToolTip(button, tip);
        }

        private void BuildFallbackPanel()
        {
            _fallbackPanel.Dock = DockStyle.Fill;
            _fallbackPanel.BackColor = Color.White;
            _fallbackPanel.Visible = false;

            _fallbackMessage.Dock = DockStyle.Top;
            _fallbackMessage.Height = 130;
            _fallbackMessage.TextAlign = ContentAlignment.MiddleCenter;
            _fallbackMessage.Padding = new Padding(30, 10, 30, 10);
            _fallbackMessage.ForeColor = Color.FromArgb(60, 60, 60);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(30, 4, 30, 4)
            };

            var openBrowser = new Button
            {
                Text = "Mở trong trình duyệt",
                Width = 180,
                Height = 30
            };
            openBrowser.Click += delegate { OpenExternal(HomeUrl); };

            var installRuntime = new Button
            {
                Text = "Tải WebView2 Runtime",
                Width = 180,
                Height = 30
            };
            installRuntime.Click += delegate { OpenExternal(RuntimeDownloadUrl); };

            buttons.Controls.Add(openBrowser);
            buttons.Controls.Add(installRuntime);

            _fallbackPanel.Controls.Add(buttons);
            _fallbackPanel.Controls.Add(_fallbackMessage);
        }

        // ================================================================ khởi tạo WebView2

        private async void OnShown(object sender, EventArgs e)
        {
            if (_webViewReady) return;
            _webViewReady = true;

            string browserVersion;
            try
            {
                browserVersion = CoreWebView2Environment.GetAvailableBrowserVersionString();
            }
            catch (Exception ex)
            {
                ShowFallback(
                    "Không tìm thấy Microsoft Edge WebView2 Runtime trên máy.\n\n" +
                    "Tool vẫn có thể mở EPLAN Data Portal bằng trình duyệt ngoài.\n\n" +
                    "Chi tiết: " + ex.Message);
                return;
            }

            if (string.IsNullOrWhiteSpace(browserVersion))
            {
                ShowFallback(
                    "Không tìm thấy Microsoft Edge WebView2 Runtime trên máy.\n\n" +
                    "Tool vẫn có thể mở EPLAN Data Portal bằng trình duyệt ngoài.");
                return;
            }

            try
            {
                Directory.CreateDirectory(_userDataFolder);
                CoreWebView2Environment environment =
                    await CoreWebView2Environment.CreateAsync(null, _userDataFolder, null);
                await _webView.EnsureCoreWebView2Async(environment);

                ConfigureWebView();
                _webView.CoreWebView2.Navigate(HomeUrl);
                _address.Text = HomeUrl;
                SetStatus("Đang mở EPLAN Data Portal…");
            }
            catch (Exception ex)
            {
                ShowFallback(
                    "Không khởi tạo được cửa sổ EPLAN Data Portal.\n\n" +
                    "Bạn vẫn có thể mở portal bằng trình duyệt ngoài.\n\n" +
                    "Chi tiết: " + ex.Message);
            }
        }

        private void ConfigureWebView()
        {
            CoreWebView2 core = _webView.CoreWebView2;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.IsPasswordAutosaveEnabled = false;
            core.Settings.IsGeneralAutofillEnabled = false;
            core.Settings.AreBrowserAcceleratorKeysEnabled = true;
            core.Settings.IsZoomControlEnabled = true;

            core.DownloadStarting += OnDownloadStarting;
            core.NavigationStarting += OnNavigationStarting;
            core.NavigationCompleted += OnNavigationCompleted;
            core.SourceChanged += OnSourceChanged;
            core.ProcessFailed += OnProcessFailed;
            core.NewWindowRequested += OnNewWindowRequested;
        }

        private void ShowFallback(string message)
        {
            _fallbackMessage.Text = message;
            _fallbackPanel.Visible = true;
            _fallbackPanel.BringToFront();
            _webView.Visible = false;
            SetStatus("WebView2 không khả dụng — dùng nút “Mở ngoài”.");
        }

        // ================================================================ điều hướng

        private void OnAddressKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            Navigate(_address.Text);
        }

        private void Navigate(string url)
        {
            if (_webView.CoreWebView2 == null)
            {
                OpenExternal(string.IsNullOrWhiteSpace(url) ? HomeUrl : url);
                return;
            }

            string value = (url ?? string.Empty).Trim();
            if (value.Length == 0) value = HomeUrl;
            if (!value.Contains("://")) value = "https://" + value;

            try
            {
                _webView.CoreWebView2.Navigate(value);
            }
            catch (Exception ex)
            {
                SetStatus("Địa chỉ không hợp lệ: " + ex.Message);
            }
        }

        private void OnBackClicked(object sender, EventArgs e)
        {
            try
            {
                if (_webView.CoreWebView2 != null && _webView.CoreWebView2.CanGoBack)
                {
                    _webView.CoreWebView2.GoBack();
                }
            }
            catch
            {
                // bỏ qua
            }
        }

        private void OnForwardClicked(object sender, EventArgs e)
        {
            try
            {
                if (_webView.CoreWebView2 != null && _webView.CoreWebView2.CanGoForward)
                {
                    _webView.CoreWebView2.GoForward();
                }
            }
            catch
            {
                // bỏ qua
            }
        }

        private void OnReloadClicked(object sender, EventArgs e)
        {
            try
            {
                if (_webView.CoreWebView2 != null)
                {
                    _webView.CoreWebView2.Reload();
                }
            }
            catch
            {
                // bỏ qua
            }
        }

        private string CurrentUrl()
        {
            try
            {
                if (_webView.CoreWebView2 != null && _webView.CoreWebView2.Source != null)
                {
                    return _webView.CoreWebView2.Source;
                }
            }
            catch
            {
                // bỏ qua
            }
            return HomeUrl;
        }

        private void OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            SetStatus("Đang mở: " + PortalPaths.SanitizeUrl(e.Uri));
            UpdateNavigationButtons();
        }

        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                SetStatus("Đã mở trang.");
            }
            else
            {
                SetStatus("Không tải được trang (" + e.WebErrorStatus + ").");
            }

            try
            {
                _address.Text = CurrentUrl();
            }
            catch
            {
                // bỏ qua
            }

            UpdateNavigationButtons();
        }

        private void OnSourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            try
            {
                _address.Text = CurrentUrl();
            }
            catch
            {
                // bỏ qua
            }
            UpdateNavigationButtons();
        }

        private void OnProcessFailed(object sender, CoreWebView2ProcessFailedEventArgs e)
        {
            SetStatus("WebView2 gặp sự cố (" + e.ProcessFailedKind + "). Hãy thử tải lại hoặc mở ngoài.");
        }

        private void OnNewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            try
            {
                Uri uri = new Uri(e.Uri);
                string host = uri.Host;
                bool eplanHost = host.Equals("eplan.com", StringComparison.OrdinalIgnoreCase) ||
                                 host.EndsWith(".eplan.com", StringComparison.OrdinalIgnoreCase);

                if (eplanHost && _webView.CoreWebView2 != null)
                {
                    // Giữ trong cùng cửa sổ để download handler vẫn hoạt động.
                    e.Handled = true;
                    _webView.CoreWebView2.Navigate(e.Uri);
                }

                // Host khác (đăng nhập SSO, Microsoft, ...) để WebView2 mở cửa sổ riêng.
            }
            catch
            {
                // bỏ qua
            }
        }

        private void UpdateNavigationButtons()
        {
            try
            {
                CoreWebView2 core = _webView.CoreWebView2;
                _back.Enabled = core != null && core.CanGoBack;
                _forward.Enabled = core != null && core.CanGoForward;
            }
            catch
            {
                // bỏ qua
            }
        }

        // ================================================================ tải file

        private void OnDownloadStarting(object sender, CoreWebView2DownloadStartingEventArgs e)
        {
            CoreWebView2Deferral deferral = null;
            try
            {
                deferral = e.GetDeferral();
            }
            catch
            {
                // bỏ qua
            }

            try
            {
                string folder = DownloadFolder;
                if (string.IsNullOrWhiteSpace(folder))
                {
                    folder = PortalPaths.DefaultDownloadFolder;
                }

                Directory.CreateDirectory(folder);

                string fileName = SuggestedFileName(e);
                string target = Path.Combine(folder, fileName);

                if (File.Exists(target))
                {
                    DialogResult answer = MessageBox.Show(
                        this,
                        "Tệp đã tồn tại:\n" + target + "\n\n" +
                        "Yes = Ghi đè\nNo = Lưu với tên khác\nCancel = Hủy tải",
                        "Tool Oumijia — Tệp đã tồn tại",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);

                    if (answer == DialogResult.Cancel)
                    {
                        e.Cancel = true;
                        SetStatus("Đã hủy tải: " + fileName);
                        return;
                    }

                    if (answer == DialogResult.No)
                    {
                        target = PortalPaths.MakeUniquePath(folder, fileName);
                    }
                }

                e.ResultFilePath = target;
                e.Handled = true;

                CoreWebView2DownloadOperation operation = e.DownloadOperation;
                if (operation != null)
                {
                    operation.StateChanged += delegate { OnDownloadStateChanged(operation, target); };
                    operation.BytesReceivedChanged += delegate { OnDownloadProgress(operation); };
                }

                _progress.Value = 0;
                _progress.Visible = true;
                SetStatus("Đang tải: " + Path.GetFileName(target));
            }
            catch (Exception ex)
            {
                SetStatus("Lỗi chuẩn bị tải: " + ex.Message);
            }
            finally
            {
                if (deferral != null)
                {
                    try
                    {
                        deferral.Complete();
                    }
                    catch
                    {
                        // bỏ qua
                    }
                }
            }
        }

        private static string SuggestedFileName(CoreWebView2DownloadStartingEventArgs e)
        {
            try
            {
                string fromResult = Path.GetFileName(e.ResultFilePath);
                if (!string.IsNullOrWhiteSpace(fromResult))
                {
                    return PortalPaths.SanitizeFileName(fromResult);
                }
            }
            catch
            {
                // bỏ qua
            }

            try
            {
                string uri = e.DownloadOperation != null ? e.DownloadOperation.Uri : null;
                if (!string.IsNullOrWhiteSpace(uri))
                {
                    Uri parsed = new Uri(uri);
                    string fromUri = Path.GetFileName(parsed.AbsolutePath);
                    if (!string.IsNullOrWhiteSpace(fromUri))
                    {
                        return PortalPaths.SanitizeFileName(Uri.UnescapeDataString(fromUri));
                    }
                }
            }
            catch
            {
                // bỏ qua
            }

            return "download.edz";
        }

        private void OnDownloadProgress(CoreWebView2DownloadOperation operation)
        {
            try
            {
                ulong? totalValue = operation.TotalBytesToReceive;
                long received = operation.BytesReceived;
                if (!totalValue.HasValue || totalValue.Value == 0 || received < 0) return;

                ulong total = totalValue.Value;
                int percent = (int)Math.Min(100UL, ((ulong)received * 100UL) / total);
                OnUi(delegate
                {
                    _progress.Visible = true;
                    _progress.Value = Math.Max(0, Math.Min(100, percent));
                });
            }
            catch
            {
                // bỏ qua
            }
        }

        private void OnDownloadStateChanged(CoreWebView2DownloadOperation operation, string target)
        {
            try
            {
                if (operation.State == CoreWebView2DownloadState.Completed)
                {
                    bool isEdz = string.Equals(Path.GetExtension(target), ".edz", StringComparison.OrdinalIgnoreCase);
                    if (isEdz && !_downloadedFiles.Exists(
                            delegate(string existing)
                            {
                                return string.Equals(existing, target, StringComparison.OrdinalIgnoreCase);
                            }))
                    {
                        _downloadedFiles.Add(target);
                    }

                    PortalPaths.AppendDownloadLog(_logDirectory, operation.Uri, target, "Completed", string.Empty);
                    OnUi(delegate
                    {
                        _progress.Visible = false;
                        _importButton.Enabled = _downloadedFiles.Count > 0;
                        SetStatus(isEdz
                            ? "Đã tải EDZ: " + target
                            : "Đã tải tệp (không phải EDZ): " + target);
                    });
                }
                else if (operation.State == CoreWebView2DownloadState.Interrupted)
                {
                    PortalPaths.AppendDownloadLog(
                        _logDirectory,
                        operation.Uri,
                        target,
                        "Interrupted",
                        operation.InterruptReason.ToString());
                    OnUi(delegate
                    {
                        _progress.Visible = false;
                        SetStatus("Tải thất bại: " + operation.InterruptReason);
                    });
                }
            }
            catch
            {
                // bỏ qua
            }
        }

        private void OnImportClicked(object sender, EventArgs e)
        {
            RequestImport = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        // ================================================================ thư mục / dữ liệu

        private void OnChooseFolderClicked(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Chọn thư mục lưu các gói EDZ tải về";
                string current = DownloadFolder;
                if (!string.IsNullOrWhiteSpace(current) && Directory.Exists(current))
                {
                    dialog.SelectedPath = current;
                }

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _downloadFolder.Text = dialog.SelectedPath;
                    PortalPaths.SaveDownloadFolder(dialog.SelectedPath);
                    SetStatus("Thư mục tải: " + dialog.SelectedPath);
                }
            }
        }

        private void OnOpenFolderClicked(object sender, EventArgs e)
        {
            try
            {
                string folder = DownloadFolder;
                if (string.IsNullOrWhiteSpace(folder)) folder = PortalPaths.DefaultDownloadFolder;
                Directory.CreateDirectory(folder);
                Process.Start(new ProcessStartInfo("explorer.exe", "\"" + folder + "\"")
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Không mở được thư mục:\n" + ex.Message,
                    "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void OnClearDataClicked(object sender, EventArgs e)
        {
            DialogResult answer = MessageBox.Show(
                this,
                "Xóa toàn bộ cookie, cache và dữ liệu đăng nhập của cửa sổ Data Portal?\n\n" +
                "Bạn sẽ phải đăng nhập lại EPLAN Data Portal.",
                "Tool Oumijia — Xóa dữ liệu phiên",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;

            try
            {
                CoreWebView2 core = _webView.CoreWebView2;
                if (core == null)
                {
                    ShowFallback("WebView2 chưa sẵn sàng — không có dữ liệu để xóa.");
                    return;
                }

                await core.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.AllProfile);
                core.Navigate(HomeUrl);
                SetStatus("Đã xóa dữ liệu phiên.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Không xóa được dữ liệu phiên:\n" + ex.Message,
                    "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                PortalPaths.SaveDownloadFolder(DownloadFolder);
            }
            catch
            {
                // bỏ qua
            }
        }

        // ================================================================ tiện ích

        private void SetStatus(string message)
        {
            try
            {
                _status.Text = message ?? string.Empty;
            }
            catch
            {
                // bỏ qua
            }
        }

        private void OnUi(Action action)
        {
            try
            {
                if (IsDisposed) return;
                if (InvokeRequired)
                {
                    BeginInvoke(action);
                }
                else
                {
                    action();
                }
            }
            catch
            {
                // bỏ qua
            }
        }

        private void OpenExternal(string url)
        {
            try
            {
                string value = string.IsNullOrWhiteSpace(url) ? HomeUrl : url;
                Process.Start(new ProcessStartInfo(value) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Không mở được trình duyệt:\n" + ex.Message,
                    "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    _webView.Dispose();
                }
                catch
                {
                    // bỏ qua
                }
            }

            base.Dispose(disposing);
        }
    }
}
