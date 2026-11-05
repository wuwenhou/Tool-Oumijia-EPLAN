using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Eplan.EplApi.ApplicationFramework;

namespace Oumijia.EplanAddIn.Ui
{
    internal static class VeeIntegration
    {
        private const string ExportConnectionsAction = "VE_E_ExportConnections";
        private const string TableMakerAction = "VEECreateEplanTable";

        public static bool IsAvailable(string actionName)
        {
            try
            {
                var manager = new ActionManager();
                return manager.FindAction(actionName) != null;
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("VEE availability " + actionName, ex);
                return false;
            }
        }

        public static bool OpenExportConnections()
        {
            return OpenOrReport(null, ExportConnectionsAction, "VEE Export connections data",
                "https://t.me/EPLAN4UCHAT/13646",
                "Tính năng này hướng dẫn EPLAN Platform 2022+ / .NET Framework 4.8+.\n" +
                "Bản cài đặt & bản quyền thuộc VIET EPLAN & ELECTRIC; Tool Oumijia không phân phối lại DLL.\n\n" +
                "Sau khi cài, restart EPLAN và bấm nút này lần nữa.");
        }

        public static void OpenExportConnections(IWin32Window owner)
        {
            OpenExportConnections();
        }

        public static bool OpenTableMaker()
        {
            return OpenOrReport(null, TableMakerAction, "VEE Table Maker",
                "https://t.me/EPLAN4U/12788",
                "Tạo technical tables trực tiếp trên trang EPLAN.\n" +
                "Bản cài đặt & bản quyền thuộc VIET EPLAN & ELECTRIC; Tool Oumijia không phân phối lại DLL.\n\n" +
                "Sau khi cài, restart EPLAN và bấm nút này lần nữa.");
        }

        public static void OpenTableMaker(IWin32Window owner)
        {
            OpenTableMaker();
        }

        private static bool OpenOrReport(IWin32Window owner, string actionName, string displayName, string downloadUrl, string helpText)
        {
            try
            {
                var manager = new ActionManager();
                var action = manager.FindAction(actionName);
                if (action != null)
                {
                    bool ok = action.Execute(new ActionCallingContext());
                    AddinLog.Write("VEE: invoked " + actionName + " (ok=" + ok + ")");
                    return ok;
                }
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("VEE invoke " + actionName, ex);
                MessageBox.Show(owner,
                    "Không chạy được " + displayName + ":\n" + ex.Message,
                    "Tool Oumijia — VEE",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            var choice = MessageBox.Show(owner,
                displayName + " chưa được đăng ký/cài đặt trên máy này.\n\n" + helpText,
                "Tool Oumijia — VEE",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (choice == DialogResult.Yes)
            {
                try { Process.Start(downloadUrl); }
                catch (Exception ex)
                {
                    AddinLog.WriteError("Open VEE link", ex);
                    MessageBox.Show(owner,
                        "Không mở được link. Sao chép:\n" + downloadUrl,
                        "Tool Oumijia — VEE",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            return false;
        }
    }
}
