using System;
using System.Windows.Forms;
using Eplan.EplApi.ApplicationFramework;
using Oumijia.EplanUi;

namespace Oumijia.EplanAddIn.Actions
{
    /// <summary>
    /// Action tương thích cũ. Nút ribbon mới sao chép command native bằng ID;
    /// không được gọi DataPortalTabView riêng lẻ vì đó chỉ là phần nội dung.
    /// </summary>
    public class DataPortalAction : IEplAction
    {
        public bool Execute(ActionCallingContext oActionCallingContext)
        {
            AddinLog.Write("Action OumijiaDataPortal: native ribbon command unavailable; no content-only dialog opened");
            ShowUnavailable("Lệnh native của EPLAN chưa sẵn sàng trên ribbon.");
            return false;
        }

        private static void ShowUnavailable(string detail)
        {
            MessageBox.Show(
                "Không mở được Data Portal native của EPLAN.\n\n" + detail +
                "\n\nHãy mở trực tiếp: tab Eplan Cloud → Data Portal.\n\n" +
                "Sau khi tải .edz bằng EPLAN, dùng nút Nhập EDZ của Tool Oumijia để nhập.\n\n" +
                "Chi tiết đã ghi vào addin.log.",
                "Tool Oumijia — Data Portal",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
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
