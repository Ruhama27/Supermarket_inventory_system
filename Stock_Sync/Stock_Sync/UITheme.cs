using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Stock_Sync
{
    public static class UITheme
    {
        // =========================================================================
        // MODERN STORE MANAGEMENT & POS COLOR PALETTE (From Inspiration Mockups)
        // =========================================================================

        // Primary Coral Accent (#FF5B26 / #FA602D)
        public static readonly Color Primary = Color.FromArgb(255, 91, 38);          // #FF5B26 Vibrant Coral Orange
        public static readonly Color PrimaryHover = Color.FromArgb(224, 72, 22);     // #E04816
        public static readonly Color PrimaryPressed = Color.FromArgb(199, 61, 16);   // #C73D10
        public static readonly Color PrimaryLight = Color.FromArgb(255, 240, 235);   // #FFF0EB Warm Peach
        public static readonly Color PrimaryDark = Color.FromArgb(219, 68, 18);

        // App Backdrop (Ultra-clean soft light grey)
        public static readonly Color AppBg = Color.FromArgb(246, 248, 250);          // #F6F8FA Soft Canvas

        // Sidebar Styling (Pure White matching Mockup 1)
        public static readonly Color SidebarBg = Color.White;                         // #FFFFFF Pure White
        public static readonly Color SidebarBorder = Color.FromArgb(235, 239, 243);  // #EBEFF3
        public static readonly Color SidebarText = Color.FromArgb(100, 116, 139);    // #64748B Slate Grey
        public static readonly Color SidebarSection = Color.FromArgb(148, 163, 184); // #94A3B8 Muted Section
        public static readonly Color SidebarHover = Color.FromArgb(255, 245, 242);   // #FFF5F2 Soft Coral Tint
        public static readonly Color SidebarActiveBg = Color.FromArgb(255, 91, 38); // #FF5B26 Vibrant Coral Pill
        public static readonly Color SidebarActiveText = Color.White;

        // Cards & Surfaces
        public static readonly Color CardBg = Color.White;
        public static readonly Color CardBorder = Color.FromArgb(235, 239, 243);     // #EBEFF3
        public static readonly Color CardHover = Color.FromArgb(250, 252, 254);

        // Inputs & Form Controls
        public static readonly Color InputBg = Color.FromArgb(244, 246, 249);         // #F4F6F9
        public static readonly Color InputBorder = Color.FromArgb(226, 232, 240);     // #E2E8F0
        public static readonly Color InputFocusBorder = Color.FromArgb(255, 91, 38); // Coral highlight

        // Login Backdrop (Mockup 2 Warm Orange & Spheres)
        public static readonly Color LoginBg = Color.FromArgb(255, 117, 24);         // #FF7518
        public static readonly Color LoginBgDark = Color.FromArgb(255, 85, 0);       // #FF5500
        public static readonly Color LoginSphere = Color.FromArgb(255, 175, 102);    // Soft Sphere Accent

        // Status Badge / Pill Colors (Matching Mockup 1)
        // 1. Soft Mint Green (Available / Completed / Delivered)
        public static readonly Color PillGreenBg = Color.FromArgb(230, 247, 237);    // #E6F7ED
        public static readonly Color PillGreenText = Color.FromArgb(13, 137, 79);    // #0D894F

        // 2. Soft Peach / Coral (Occupied / Low Stock / Cancelled)
        public static readonly Color PillOrangeBg = Color.FromArgb(255, 240, 235);   // #FFF0EB
        public static readonly Color PillOrangeText = Color.FromArgb(224, 75, 22);   // #E04B16

        // 3. Soft Lavender / Blue (Reserved / Pending / Discontinued)
        public static readonly Color PillBlueBg = Color.FromArgb(238, 242, 255);     // #EEF2FF
        public static readonly Color PillBlueText = Color.FromArgb(79, 70, 229);     // #4F46E5

        // General Functional Semantic Colors
        public static readonly Color Success = Color.FromArgb(13, 137, 79);          // #0D894F Emerald Green
        public static readonly Color Danger = Color.FromArgb(239, 68, 68);           // #EF4444 Rose Red
        public static readonly Color Warning = Color.FromArgb(245, 158, 11);         // #F59E0B Amber Yellow

        // Typography Colors
        public static readonly Color TextMain = Color.FromArgb(30, 41, 59);          // #1E293B Dark Slate
        public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);      // #64748B Slate Grey
        public static readonly Color TextLight = Color.FromArgb(148, 163, 184);      // #94A3B8 Light Slate

        // Standard Fonts
        public static readonly Font FontDisplay = new Font("Segoe UI", 18F, FontStyle.Bold);
        public static readonly Font FontTitle = new Font("Segoe UI", 14F, FontStyle.Bold);
        public static readonly Font FontSubtitle = new Font("Segoe UI", 11F, FontStyle.Bold);
        public static readonly Font FontBold = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        public static readonly Font FontRegular = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        public static readonly Font FontSmall = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        public static readonly Font FontTiny = new Font("Segoe UI", 7.5F, FontStyle.Bold);

        /// <summary>
        /// Styles a DataGridView to match the clean coral & white aesthetic from Mockup 1.
        /// </summary>
        public static void StyleDataGrid(DataGridView dgv)
        {
            if (dgv == null) return;

            dgv.BackgroundColor = CardBg;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor = Color.FromArgb(241, 245, 249);
            
            dgv.RowHeadersVisible = false;
            dgv.EnableHeadersVisualStyles = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.AllowUserToResizeRows = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Header styling
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(71, 85, 105);
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(14, 0, 0, 0);
            dgv.ColumnHeadersHeight = 44;

            // Row styling
            dgv.RowTemplate.Height = 42;
            dgv.DefaultCellStyle.BackColor = CardBg;
            dgv.DefaultCellStyle.ForeColor = TextMain;
            dgv.DefaultCellStyle.Font = FontRegular;
            dgv.DefaultCellStyle.Padding = new Padding(14, 0, 0, 0);
            dgv.DefaultCellStyle.SelectionBackColor = PrimaryLight;
            dgv.DefaultCellStyle.SelectionForeColor = TextMain;

            // Alternating rows
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(252, 253, 255);
            dgv.AlternatingRowsDefaultCellStyle.ForeColor = TextMain;
            dgv.AlternatingRowsDefaultCellStyle.SelectionBackColor = PrimaryLight;
            dgv.AlternatingRowsDefaultCellStyle.SelectionForeColor = TextMain;

            // Custom Cell Painting for rounded Status pill badges
            dgv.CellPainting += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    string colName = dgv.Columns[e.ColumnIndex].Name;
                    if ((colName == "Status" || colName == "Product Status") && e.Value != null)
                    {
                        e.PaintBackground(e.CellBounds, true);
                        string status = e.Value.ToString().Trim();

                        Color pillBg = PillGreenBg;
                        Color pillText = PillGreenText;

                        if (status.Equals("Delivered", StringComparison.OrdinalIgnoreCase) ||
                            status.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                            status.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
                            status.Equals("Available", StringComparison.OrdinalIgnoreCase))
                        {
                            pillBg = PillGreenBg;
                            pillText = PillGreenText;
                        }
                        else if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) ||
                                 status.Equals("Inactive", StringComparison.OrdinalIgnoreCase) ||
                                 status.Equals("Occupied", StringComparison.OrdinalIgnoreCase) ||
                                 status.Equals("Low Stock", StringComparison.OrdinalIgnoreCase))
                        {
                            pillBg = PillOrangeBg;
                            pillText = PillOrangeText;
                        }
                        else if (status.Equals("Pending", StringComparison.OrdinalIgnoreCase) ||
                                 status.Equals("Reserved", StringComparison.OrdinalIgnoreCase) ||
                                 status.Equals("Discontinued", StringComparison.OrdinalIgnoreCase))
                        {
                            pillBg = PillBlueBg;
                            pillText = PillBlueText;
                        }

                        // Measure pill
                        using (Font pillFont = new Font("Segoe UI", 8.5F, FontStyle.Bold))
                        {
                            Size textSize = TextRenderer.MeasureText(status, pillFont);
                            int pillWidth = Math.Max(textSize.Width + 18, 70);
                            int pillHeight = 24;
                            int pillX = e.CellBounds.X + 10;
                            int pillY = e.CellBounds.Y + (e.CellBounds.Height - pillHeight) / 2;
                            Rectangle pillRect = new Rectangle(pillX, pillY, pillWidth, pillHeight);

                            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                            using (GraphicsPath path = GetRoundedPath(pillRect, 12))
                            {
                                using (SolidBrush brush = new SolidBrush(pillBg))
                                {
                                    e.Graphics.FillPath(brush, path);
                                }
                            }

                            TextRenderer.DrawText(e.Graphics, status, pillFont, pillRect, pillText,
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        }

                        e.Handled = true;
                    }
                }
            };
        }

        /// <summary>
        /// Creates a rounded graphics path.
        /// </summary>
        public static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }
            int diameter = radius * 2;
            if (diameter > rect.Width) diameter = rect.Width;
            if (diameter > rect.Height) diameter = rect.Height;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// Draws soft ambient spheres for login/hero backgrounds (matching Mockup 2).
        /// </summary>
        public static void PaintAmbientSpheres(Graphics g, Rectangle bounds)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 1. Top-center sphere
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(140, 255, 175, 102)))
            {
                g.FillEllipse(brush, bounds.Width / 2 - 120, -100, 260, 260);
            }

            // 2. Mid-left sphere
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(120, 255, 185, 120)))
            {
                g.FillEllipse(brush, -60, bounds.Height / 2 - 70, 160, 160);
            }

            // 3. Mid-right sphere
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(130, 255, 175, 102)))
            {
                g.FillEllipse(brush, bounds.Width - 120, bounds.Height / 2 - 40, 190, 190);
            }

            // 4. Bottom-left sphere
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(140, 255, 195, 135)))
            {
                g.FillEllipse(brush, 80, bounds.Height - 110, 150, 150);
            }

            // 5. Bottom-right sphere
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(150, 255, 190, 130)))
            {
                g.FillEllipse(brush, bounds.Width - 90, bounds.Height - 90, 130, 130);
            }
        }
    }
}
