using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Stock_Sync
{
    public static class UITheme
    {
        // Colors from inspiration image
        public static readonly Color SidebarBg = Color.FromArgb(20, 41, 54);          // #142936 Dark Slate Navy
        public static readonly Color SidebarText = Color.FromArgb(143, 167, 181);     // #8FA7B5 Soft Muted Slate
        public static readonly Color SidebarActiveBg = Color.FromArgb(227, 237, 243); // #E3EDF3 Light Pill
        public static readonly Color SidebarActiveText = Color.FromArgb(20, 41, 54);  // #142936 Active Dark
        public static readonly Color SidebarHover = Color.FromArgb(30, 58, 76);       // #1E3A4C Hover

        public static readonly Color AppBg = Color.FromArgb(238, 243, 246);           // #EEF3F6 Soft Blue-Grey
        public static readonly Color CardBg = Color.White;
        public static readonly Color CardBorder = Color.FromArgb(226, 232, 240);      // #E2E8F0
        
        public static readonly Color Primary = Color.FromArgb(2, 132, 199);           // #0284C7 Sky Blue
        public static readonly Color PrimaryDark = Color.FromArgb(3, 105, 161);
        public static readonly Color Success = Color.FromArgb(16, 185, 129);          // #10B981 Emerald
        public static readonly Color Danger = Color.FromArgb(239, 68, 68);            // #EF4444 Rose Red
        public static readonly Color Warning = Color.FromArgb(245, 158, 11);          // #F59E0B Amber
        
        public static readonly Color TextMain = Color.FromArgb(30, 41, 59);           // #1E293B
        public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);       // #64748B

        // Standard Fonts
        public static readonly Font FontTitle = new Font("Segoe UI", 16F, FontStyle.Bold);
        public static readonly Font FontSubtitle = new Font("Segoe UI", 12F, FontStyle.Bold);
        public static readonly Font FontBold = new Font("Segoe UI", 10F, FontStyle.Bold);
        public static readonly Font FontRegular = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        public static readonly Font FontSmall = new Font("Segoe UI", 8.5F, FontStyle.Regular);

        /// <summary>
        /// Styles a DataGridView to match the modern aesthetic from the inspiration mockup.
        /// </summary>
        public static void StyleDataGrid(DataGridView dgv)
        {
            if (dgv == null) return;

            dgv.BackgroundColor = CardBg;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor = Color.FromArgb(238, 242, 246);
            
            dgv.RowHeadersVisible = false;
            dgv.EnableHeadersVisualStyles = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.AllowUserToResizeRows = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Header styling
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 248);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(12, 0, 0, 0);
            dgv.ColumnHeadersHeight = 44;

            // Row styling
            dgv.RowTemplate.Height = 40;
            dgv.DefaultCellStyle.BackColor = CardBg;
            dgv.DefaultCellStyle.ForeColor = TextMain;
            dgv.DefaultCellStyle.Font = FontRegular;
            dgv.DefaultCellStyle.Padding = new Padding(12, 0, 0, 0);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(226, 238, 247);
            dgv.DefaultCellStyle.SelectionForeColor = TextMain;

            // Alternating rows
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 252);
            dgv.AlternatingRowsDefaultCellStyle.ForeColor = TextMain;
            dgv.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(226, 238, 247);
            dgv.AlternatingRowsDefaultCellStyle.SelectionForeColor = TextMain;
        }

        /// <summary>
        /// Creates a rounded graphics path.
        /// </summary>
        public static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
