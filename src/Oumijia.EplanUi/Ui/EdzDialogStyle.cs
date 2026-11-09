using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace Oumijia.EplanUi
{
    /// <summary>
    /// One visual language for both EDZ dialogs (Nhập / Xuất):
    /// same header, toolbar, tree, details, log, primary button and status row.
    /// </summary>
    internal static class EdzDialogStyle
    {
        public static readonly Color HeaderColor = Color.FromArgb(23, 56, 88);
        public static readonly Color HintColor = Color.DimGray;

        public static readonly Font UiFont = new Font("Segoe UI", 9F);
        public static readonly Font HeaderFont = new Font("Segoe UI", 10F, FontStyle.Bold);
        public static readonly Font CaptionFont = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        public static readonly Font StatusFont = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        public static readonly Font PrimaryButtonFont = new Font("Segoe UI", 9F, FontStyle.Bold);
        public static readonly Font LogFont = new Font("Consolas", 8.5F);

        public const int HeaderHeight = 24;
        public const int ToolbarHeight = 34;
        public const int HintHeight = 20;
        public const int ToolbarButtonHeight = 28;
        public const int ActionRowHeight = 44;
        public const int PrimaryButtonHeight = 34;
        public const int LabelMaxLength = 140;

        public static void ApplyForm(Form form, string title)
        {
            form.Text = title;
            form.ClientSize = new Size(1120, 780);
            form.MinimumSize = new Size(940, 640);
            form.StartPosition = FormStartPosition.CenterScreen;
            form.Font = UiFont;
        }

        public static void ApplyHeader(Label label)
        {
            label.AutoSize = false;
            label.Dock = DockStyle.Top;
            label.Height = HeaderHeight;
            label.Font = HeaderFont;
            label.ForeColor = HeaderColor;
            label.Padding = new Padding(2, 0, 0, 0);
        }

        public static void ApplyCaption(Label label)
        {
            label.AutoSize = false;
            label.Dock = DockStyle.Top;
            label.Height = HeaderHeight;
            label.Font = CaptionFont;
            label.ForeColor = HeaderColor;
        }

        public static void ApplyHint(Label label)
        {
            label.AutoSize = false;
            label.Dock = DockStyle.Top;
            label.Height = HintHeight;
            label.ForeColor = HintColor;
            label.Padding = new Padding(2, 1, 0, 0);
        }

        public static void ApplyStatus(Label label)
        {
            label.Font = StatusFont;
            label.ForeColor = HeaderColor;
        }

        public static void ApplyToolbarButton(Button button)
        {
            button.Height = ToolbarButtonHeight;
            button.Margin = new Padding(0, 3, 4, 0);
        }

        public static void ApplyPrimaryButton(Button button)
        {
            button.Height = PrimaryButtonHeight;
            button.Font = PrimaryButtonFont;
        }

        public static void ApplyTree(TreeView tree)
        {
            tree.BorderStyle = BorderStyle.FixedSingle;
            tree.HideSelection = false;
            tree.ShowLines = true;
            tree.ShowRootLines = true;
            tree.ItemHeight = 22;
            tree.ShowNodeToolTips = true;
            EnableDoubleBuffering(tree);
        }

        public static void ApplyDetails(ListView details)
        {
            details.Dock = DockStyle.Fill;
            details.View = View.Details;
            details.FullRowSelect = true;
            details.MultiSelect = false;
            details.HideSelection = false;
            details.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            if (details.Columns.Count == 0)
            {
                details.Columns.Add("Thuộc tính", 190);
                details.Columns.Add("Giá trị", 320);
            }
        }

        public static void ApplyLog(TextBox log)
        {
            log.Dock = DockStyle.Fill;
            log.Multiline = true;
            log.ReadOnly = true;
            log.ScrollBars = ScrollBars.Both;
            log.WordWrap = false;
            log.Font = LogFont;
        }

        public static void AdjustDetailsColumns(ListView details)
        {
            try
            {
                if (details.Columns.Count >= 2 && details.ClientSize.Width > 0)
                {
                    details.Columns[1].Width = Math.Max(120,
                        details.ClientSize.Width - details.Columns[0].Width - 26);
                }
            }
            catch
            {
                // keep the default width while the control is not laid out yet
            }
        }

        /// <summary>Short tree-row text; the full value stays in the node tooltip.</summary>
        public static string Shorten(string text)
        {
            return Shorten(text, LabelMaxLength);
        }

        public static string Shorten(string text, int maxLength)
        {
            if (String.IsNullOrEmpty(text) || text.Length <= maxLength) return text ?? String.Empty;
            int cut = Math.Max(1, maxLength - 1);
            return text.Substring(0, cut).TrimEnd() + "…";
        }

        private static void EnableDoubleBuffering(Control control)
        {
            try
            {
                typeof(Control)
                    .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(control, true, null);
            }
            catch
            {
                // painting optimisation only
            }
        }
    }
}
