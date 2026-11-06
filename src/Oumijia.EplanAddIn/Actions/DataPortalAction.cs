using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Eplan.EplApi.ApplicationFramework;
using Oumijia.EplanUi;
using Oumijia.PortalHost;

namespace Oumijia.EplanAddIn.Actions
{
    /// <summary>
    /// Action "OumijiaDataPortal": mở EPLAN Data Portal trong WebView2.
    ///
    /// Người dùng tự đăng nhập tài khoản hợp lệ; Tool không lưu credential.
    /// Sau khi tải, có thể chuyển ngay các file vừa tải sang hộp Nhập EDZ.
    /// </summary>
    public class DataPortalAction : IEplAction
    {
        public bool Execute(ActionCallingContext oActionCallingContext)
        {
            try
            {
                string dataRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Tool Oumijia");

                string webViewData = Path.Combine(dataRoot, "WebView2");
                string logDirectory = Path.Combine(dataRoot, "logs");

                List<string> downloaded;
                bool requestImport;

                using (var form = new PortalForm(webViewData, null, logDirectory))
                {
                    form.ShowDialog();
                    downloaded = form.DownloadedFiles;
                    requestImport = form.RequestImport;
                }

                AddinLog.Write("DataPortal closed: downloaded=" + downloaded.Count +
                               ", requestImport=" + requestImport);

                if (downloaded.Count == 0)
                {
                    return true;
                }

                bool openImport = requestImport;
                if (!openImport)
                {
                    DialogResult answer = MessageBox.Show(
                        "Đã tải " + downloaded.Count + " tệp từ EPLAN Data Portal.\n\n" +
                        "Mở hộp Nhập EDZ với các tệp này?",
                        "Tool Oumijia",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    openImport = answer == DialogResult.Yes;
                }

                if (openImport)
                {
                    using (var import = new ImportEdzForm(downloaded))
                    {
                        import.ShowDialog();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("DataPortal", ex);
                MessageBox.Show(
                    "Không mở được EPLAN Data Portal:\n" + ex.Message,
                    "Tool Oumijia",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        public bool OnRegister(ref string Name, ref int Ordinal)
        {
            Name = "OumijiaDataPortal";
            Ordinal = 25;
            return true;
        }

        public void GetActionProperties(ref ActionProperties actionProperties)
        {
            // EPLAN 2022: ActionProperties exposes no writable public members.
        }
    }
}
