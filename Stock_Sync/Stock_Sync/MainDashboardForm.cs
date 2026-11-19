using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using WinChart = System.Windows.Forms.DataVisualization.Charting.Chart;

namespace Stock_Sync
{
    public class MainDashboardForm : Form
    {
        private enum ScreenType
        {
            Dashboard,
            Pos,
            Products,
            Stock,
            Orders,
            Suppliers,
            Reports,
            Alerts
        }

        private ScreenType currentScreen = ScreenType.Dashboard;

        // UI Layout Panels
        private Panel pnlSidebar;
        private Panel pnlHeader;
        private Panel pnlContentContainer;

        // Profile controls
        private Label lblProfileName;
        private Label lblProfileSubtitle;
        private Label lblAvatar;

        // Header controls
        private Label lblScreenTitle;
        private Label lblBreadcrumb;
        private Label lblClock;
        private Button btnNotificationBadge;
        private Button btnLangToggle;
        private Button btnMobileAccess;
        private Button btnStaffAccounts;
        private Timer clockTimer;

        // Sidebar Navigation Buttons
        private Button btnNavDashboard;
        private Button btnNavPos;
        private Button btnNavProducts;
        private Button btnNavStock;
        private Button btnNavOrders;
        private Button btnNavSuppliers;
        private Button btnNavReports;
        private Button btnNavAlerts;
        private Button btnNavLogout;
        private List<Button> navButtons = new List<Button>();

        // Views (Sub-panels)
        private Panel pnlViewDashboard;
        private Panel pnlViewPos;
        private Panel pnlViewProducts;
        private Panel pnlViewStock;
        private Panel pnlViewOrders;
        private Panel pnlViewSuppliers;
        private Panel pnlViewReports;
        private Panel pnlViewAlerts;

        // View: Suppliers Controls
        private DataGridView dgvSuppliers;
        private TextBox txtSupplierSearch;
        private Button btnSupplierSearch;
        private Button btnSupplierExport;
        private Label lblSupplierFormTitle;
        private TextBox txtSupplierName;
        private TextBox txtSupplierContact;
        private TextBox txtSupplierPhone;
        private TextBox txtSupplierEmail;
        private TextBox txtSupplierAddress;
        private Button btnSupplierAdd;
        private Button btnSupplierUpdate;
        private Button btnSupplierDelete;
        private Button btnSupplierClear;
        private int selectedSupplierId = -1;

        // Dashboard Sub-navigation & Analytics Tabs
        private Button btnDashTabCharts;
        private Button btnDashTabRestock;
        private Button btnDashTabOrders;
        private Button btnDashTabExpiry;
        private Button btnDashTabShifts;
        private List<Button> dashTabButtons = new List<Button>();
        private Panel pnlDashTabContent;
        private Panel pnlDashCharts;
        private Panel pnlDashRestock;
        private Panel pnlDashOrders;
        private Panel pnlDashExpiry;
        private Panel pnlDashShifts;
        private DataGridView dgvDashRestock;
        private DataGridView dgvDashOrders;
        private DataGridView dgvDashExpiry;
        private DataGridView dgvDashShifts;

        // POS Payment Method
        private ComboBox cmbPosPaymentMethod;

        // Enhanced Products Controls
        private Label lblProdBrand;
        private Label lblProdUnit;
        private TextBox txtProdBrand;
        private ComboBox cmbProdUnit;
        private ComboBox cmbProductFilterCategory;
        private Button btnProductClear;

        // Enhanced Stock Controls
        private Label lblStockSupplier;
        private ComboBox cmbStockProduct;
        private ComboBox cmbStockSupplier;
        private Button btnStockUpdate;
        private Button btnStockDelete;
        private Button btnStockClear;

        // View 1: Orders Screen Controls (Matching Mockup 1)
        private Label lblOrdersKpiTotalTitle;
        private Label lblOrdersKpiTotalVal;
        private Label lblOrdersKpiSalesTitle;
        private Label lblOrdersKpiSalesVal;
        private Label lblOrdersKpiDoneTitle;
        private Label lblOrdersKpiDoneVal;
        private Label lblOrdersKpiPendingTitle;
        private Label lblOrdersKpiPendingVal;

        private Label lblOrdersCardTitle;
        private Button btnOrdersTabAll;
        private Button btnOrdersTabCompleted;
        private Button btnOrdersTabPending;
        private TextBox txtOrdersSearch;
        private Button btnExportOrders;
        private DataGridView dgvOrders;
        private DateTimePicker dtpOrdersFilter;
        private string activeOrderTab = "All";

        // View 2: POS Controls
        private Bill activeBill = new Bill();
        private DataGridView dgvCart;
        private Label lblScanTitle;
        private TextBox txtPosBarcode;
        private Button btnClearCart;
        private Label lblSummaryTitle;
        private Label lblSubTitle;
        private Label lblTaxTitle;
        private Label lblPayableTitle;
        private Label lblCashTitle;
        private Label lblChangeTitle;
        private Label lblPosSubtotal;
        private Label lblPosTotal;
        private TextBox txtPosCashReceived;
        private Label lblPosChange;
        private Button btnCompleteSale;
        private Button btnPrintReceipt;
        private PrintDocument printDoc = new PrintDocument();
        private string receiptText = "";
        private List<BillItem> lastCheckoutItems = new List<BillItem>();
        private int lastCheckoutOrderId = 0;
        private decimal lastCheckoutTotal = 0m;
        private decimal lastCheckoutCash = 0m;
        private decimal lastCheckoutChange = 0m;

        // View 3: Products Controls
        private DataGridView dgvProducts;
        private TextBox txtProductSearch;
        private Button btnProductSearch;
        private Button btnProductExport;
        private Label lblProductFormTitle;
        private Label lblProdBarcode;
        private Label lblProdName;
        private Label lblProdCategory;
        private Label lblProdStatus;
        private TextBox txtProdBarcode;
        private TextBox txtProdName;
        private ComboBox cmbProdCategory;
        private ComboBox cmbProdStatus;
        private Button btnProductAdd;
        private Button btnProductUpdate;
        private Button btnProductDeactivate;
        private int selectedProductId = -1;

        // View 4: Stock Controls
        private DataGridView dgvStock;
        private TextBox txtStockSearch;
        private Button btnStockSearch;
        private Button btnStockExport;
        private Label lblStockFormTitle;
        private Label lblStockPid;
        private Label lblStockCode;
        private Label lblStockQty;
        private Label lblStockMin;
        private Label lblStockCp;
        private Label lblStockSp;
        private Label lblStockArrival;
        private TextBox txtStockCode;
        private TextBox txtStockQty;
        private TextBox txtStockMin;
        private TextBox txtStockCp;
        private TextBox txtStockSp;
        private DateTimePicker dtpStockArrival;
        private DateTimePicker dtpStockExpiry;
        private CheckBox chkStockHasExpiry;
        private Button btnStockAdd;
        private int selectedStockId = -1;

        // View 5: Dashboard Analytics
        private Label lblKpiTitle1;
        private Label lblKpiTitle2;
        private Label lblKpiTitle3;
        private Label lblKpiTitle4;
        private Label lblKpiTodaySales;
        private Label lblKpiTodayOrders;
        private Label lblKpiActiveProducts;
        private Label lblKpiLowStock;
        private WinChart chartDailySales;
        private WinChart chartTopSold;

        // View 6: Reports Controls
        private Label lblReportPick;
        private Button btnReportFilter;
        private Button btnReportExport;
        private DataGridView dgvReports;
        private DateTimePicker dtpReportDate;

        // View 7: Alerts Controls
        private Label lblAlertLowStock;
        private Label lblAlertExpired;
        private Label lblAlertNeverSold;
        private RichTextBox rtbLowStock;
        private RichTextBox rtbExpired;
        private RichTextBox rtbNeverSold;

        public MainDashboardForm()
        {
            InitializeModernLayout();
            SetupClock();
            printDoc.PrintPage += PrintDoc_PrintPage;

            // Subscribe to language switcher events
            LocalizationManager.OnLanguageChanged += ApplyLanguage;

            // Load initial view: Dashboard for Admin, POS for Staff
            SwitchView(Session.IsAdmin ? ScreenType.Dashboard : ScreenType.Pos);

            // Apply active localization
            ApplyLanguage();

            // Start mobile micro-server for phone dashboard
            try
            {
                MobileServer.Start();
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to start MobileServer", ex);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LocalizationManager.OnLanguageChanged -= ApplyLanguage;
                clockTimer?.Dispose();
                printDoc?.Dispose();
                // Note: MobileServer is kept running in background for Admin mobile access
            }
            base.Dispose(disposing);
        }

        private void InitializeModernLayout()
        {
            this.Text = "Stock-Sync — Supermarket Inventory & POS System";
            this.Size = new Size(1320, 840);
            this.MinimumSize = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = UITheme.AppBg;
            this.Font = UITheme.FontRegular;

            // 1. Sidebar (Left) - Pure White matching Mockup 1
            pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 240,
                BackColor = UITheme.SidebarBg,
                Padding = new Padding(0)
            };
            pnlSidebar.Paint += (s, e) =>
            {
                // Draw subtle right border line
                using (Pen borderPen = new Pen(UITheme.SidebarBorder, 1))
                {
                    e.Graphics.DrawLine(borderPen, pnlSidebar.Width - 1, 0, pnlSidebar.Width - 1, pnlSidebar.Height);
                }
            };

            BuildSidebar();

            // 2. Header (Top) - Pure White matching Mockup 1
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = UITheme.CardBg,
                Padding = new Padding(24, 12, 24, 12)
            };
            pnlHeader.Paint += (s, e) =>
            {
                // Draw subtle bottom border line
                using (Pen borderPen = new Pen(UITheme.CardBorder, 1))
                {
                    e.Graphics.DrawLine(borderPen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
                }
            };

            BuildHeader();

            // 3. Content Container (Fill)
            pnlContentContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.AppBg,
                Padding = new Padding(24, 16, 24, 20)
            };

            // Build all sub-views
            BuildDashboardView();
            BuildPosView();
            BuildProductsView();
            BuildStockView();
            BuildOrdersView();
            BuildSuppliersView();
            BuildReportsView();
            BuildAlertsView();

            // Add panels to form
            this.Controls.Add(pnlContentContainer);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlSidebar);
        }

        #region Sidebar & Header Construction (Matching Mockup 1)
        private void BuildSidebar()
        {
            // Brand Logo Header (Top of Sidebar - Mockup 1 [📦] D.CC / Stock-Sync)
            Panel pnlBrand = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = UITheme.SidebarBg,
                Padding = new Padding(18, 16, 18, 10)
            };

            // Coral rounded square icon badge [📦]
            Label lblBrandBadge = new Label
            {
                Size = new Size(38, 38),
                Location = new Point(18, 16),
                BackColor = UITheme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Text = "📦",
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblBrandBadge.Paint += (s, e) =>
            {
                using (GraphicsPath path = UITheme.GetRoundedPath(new Rectangle(0, 0, lblBrandBadge.Width, lblBrandBadge.Height), 8))
                {
                    lblBrandBadge.Region = new Region(path);
                }
            };

            Label lblBrandTitle = new Label
            {
                Text = "Stock-Sync",
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                ForeColor = UITheme.TextMain,
                Location = new Point(64, 16),
                AutoSize = true
            };

            Label lblBrandSub = new Label
            {
                Text = "← Store Management",
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                Location = new Point(65, 38),
                AutoSize = true
            };

            pnlBrand.Controls.Add(lblBrandBadge);
            pnlBrand.Controls.Add(lblBrandTitle);
            pnlBrand.Controls.Add(lblBrandSub);
            pnlSidebar.Controls.Add(pnlBrand);

            // Bottom Profile Card (Mockup 1 John Holland / Admin)
            Panel pnlProfileBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                BackColor = UITheme.SidebarBg,
                Padding = new Padding(16, 12, 16, 12)
            };
            pnlProfileBottom.Paint += (s, e) =>
            {
                using (Pen p = new Pen(UITheme.SidebarBorder, 1))
                {
                    e.Graphics.DrawLine(p, 0, 0, pnlProfileBottom.Width, 0);
                }
            };

            lblAvatar = new Label
            {
                Size = new Size(40, 40),
                Location = new Point(16, 15),
                BackColor = UITheme.PrimaryLight,
                ForeColor = UITheme.Primary,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Text = string.IsNullOrEmpty(Session.Role) ? "AD" : Session.Role.Substring(0, Math.Min(2, Session.Role.Length)).ToUpper()
            };
            lblAvatar.Paint += (s, e) =>
            {
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddEllipse(0, 0, lblAvatar.Width - 1, lblAvatar.Height - 1);
                    lblAvatar.Region = new Region(path);
                }
            };

            lblProfileName = new Label
            {
                Text = Session.IsAdmin ? LocalizationManager.T("AdminRole") : LocalizationManager.T("StaffRole"),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = UITheme.TextMain,
                Location = new Point(64, 16),
                Size = new Size(125, 18)
            };

            lblProfileSubtitle = new Label
            {
                Text = string.IsNullOrEmpty(Session.Email) ? "Admin User" : Session.Email,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                Location = new Point(64, 35),
                Size = new Size(125, 16)
            };

            btnNavLogout = new Button
            {
                Text = "🚪",
                Size = new Size(32, 32),
                Location = new Point(195, 18),
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.PrimaryLight,
                ForeColor = UITheme.Primary,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnNavLogout.FlatAppearance.BorderSize = 0;
            btnNavLogout.Click += (s, e) =>
            {
                Session.Clear();
                LoginForm login = new LoginForm();
                login.StartPosition = FormStartPosition.CenterScreen;
                login.Show();
                this.Dispose();
            };

            pnlProfileBottom.Controls.Add(lblAvatar);
            pnlProfileBottom.Controls.Add(lblProfileName);
            pnlProfileBottom.Controls.Add(lblProfileSubtitle);
            pnlProfileBottom.Controls.Add(btnNavLogout);
            pnlSidebar.Controls.Add(pnlProfileBottom);

            // Navigation Container (Middle of Sidebar)
            Panel pnlNavContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.SidebarBg,
                Padding = new Padding(12, 10, 12, 10),
                AutoScroll = true
            };

            // Section 1: Menu / Store Management
            Label lblSectionMenu = new Label
            {
                Text = "MENU",
                Font = UITheme.FontTiny,
                ForeColor = UITheme.SidebarSection,
                Dock = DockStyle.Top,
                Height = 24,
                TextAlign = ContentAlignment.BottomLeft,
                Padding = new Padding(8, 0, 0, 4)
            };

            btnNavDashboard = CreateNavButton("⊞   " + LocalizationManager.T("NavDashboard"));
            btnNavPos = CreateNavButton("🛒   " + LocalizationManager.T("NavPos"));
            btnNavProducts = CreateNavButton("📦   " + LocalizationManager.T("NavProducts"));
            btnNavStock = CreateNavButton("📋   " + LocalizationManager.T("NavStock"));
            btnNavOrders = CreateNavButton("🧾   " + LocalizationManager.T("NavOrders"));
            btnNavSuppliers = CreateNavButton("🚚   " + LocalizationManager.T("NavSuppliers"));
            btnNavReports = CreateNavButton("📈   " + LocalizationManager.T("NavReports"));

            // Section 2: Others
            Label lblSectionOthers = new Label
            {
                Text = "OTHERS",
                Font = UITheme.FontTiny,
                ForeColor = UITheme.SidebarSection,
                Dock = DockStyle.Top,
                Height = 32,
                TextAlign = ContentAlignment.BottomLeft,
                Padding = new Padding(8, 10, 0, 4)
            };

            btnNavAlerts = CreateNavButton("🔔   " + LocalizationManager.T("NavAlerts"));

            // Navigation Click Handlers
            btnNavDashboard.Click += (s, e) => SwitchView(ScreenType.Dashboard);
            btnNavPos.Click += (s, e) => SwitchView(ScreenType.Pos);
            btnNavProducts.Click += (s, e) => SwitchView(ScreenType.Products);
            btnNavStock.Click += (s, e) => SwitchView(ScreenType.Stock);
            btnNavOrders.Click += (s, e) => SwitchView(ScreenType.Orders);
            btnNavSuppliers.Click += (s, e) => SwitchView(ScreenType.Suppliers);
            btnNavReports.Click += (s, e) => SwitchView(ScreenType.Reports);
            btnNavAlerts.Click += (s, e) => SwitchView(ScreenType.Alerts);

            // Hide Admin-only navigation buttons from Cashier Staff
            if (!Session.IsAdmin)
            {
                btnNavDashboard.Visible = false;
                btnNavStock.Visible = false;
                btnNavSuppliers.Visible = false;
                btnNavReports.Visible = false;
            }

            // Add to navigation panel in reverse order for Dock = Top
            pnlNavContainer.Controls.Add(btnNavAlerts);
            pnlNavContainer.Controls.Add(lblSectionOthers);
            pnlNavContainer.Controls.Add(btnNavReports);
            pnlNavContainer.Controls.Add(btnNavSuppliers);
            pnlNavContainer.Controls.Add(btnNavOrders);
            pnlNavContainer.Controls.Add(btnNavStock);
            pnlNavContainer.Controls.Add(btnNavProducts);
            pnlNavContainer.Controls.Add(btnNavPos);
            pnlNavContainer.Controls.Add(btnNavDashboard);
            pnlNavContainer.Controls.Add(lblSectionMenu);

            pnlSidebar.Controls.Add(pnlNavContainer);
        }

        private Button CreateNavButton(string text)
        {
            Button btn = new Button
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 42,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = UITheme.SidebarText,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                Margin = new Padding(0, 2, 0, 2),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) =>
            {
                if (btn.BackColor != UITheme.SidebarActiveBg)
                {
                    btn.BackColor = UITheme.SidebarHover;
                }
            };
            btn.MouseLeave += (s, e) =>
            {
                if (btn.BackColor != UITheme.SidebarActiveBg)
                {
                    btn.BackColor = Color.White;
                }
            };

            navButtons.Add(btn);
            return btn;
        }

        private void BuildHeader()
        {
            lblScreenTitle = new Label
            {
                Text = LocalizationManager.T("TitleOrders"),
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = UITheme.TextMain,
                AutoSize = true,
                Location = new Point(20, 10)
            };

            lblBreadcrumb = new Label
            {
                Text = LocalizationManager.T("BreadcrumbOrders"),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Location = new Point(22, 38)
            };

            // Notification Pill Button with badge (Mockup 1 🔔 8+)
            btnNotificationBadge = new Button
            {
                Text = "🔔 8+",
                Size = new Size(68, 34),
                Location = new Point(pnlHeader.Width - 215, 16),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.PrimaryLight,
                ForeColor = UITheme.Primary,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnNotificationBadge.FlatAppearance.BorderSize = 0;
            btnNotificationBadge.Click += (s, e) => SwitchView(ScreenType.Alerts);

            // Phone Live Access Button (Mobile Web Dashboard - Strictly restricted to Admin)
            btnMobileAccess = new Button
            {
                Text = LocalizationManager.CurrentLanguage == AppLanguage.Amharic ? "📱 የስልክ መዳረሻ" : "📱 Phone Live",
                Size = new Size(130, 34),
                Location = new Point(pnlHeader.Width - 355, 16),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(238, 242, 255),
                ForeColor = Color.FromArgb(79, 70, 229),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Visible = Session.IsAdmin
            };
            btnMobileAccess.FlatAppearance.BorderSize = 0;
            btnMobileAccess.Click += (s, e) => ShowMobileAccessDialog();

            // Staff Accounts Management Button (Admin Only)
            btnStaffAccounts = new Button
            {
                Text = LocalizationManager.CurrentLanguage == AppLanguage.Amharic ? "👥 ሰራተኞች" : "👥 Staff Access",
                Size = new Size(135, 34),
                Location = new Point(pnlHeader.Width - 500, 16),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 253, 244),
                ForeColor = Color.FromArgb(22, 163, 74),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Visible = Session.IsAdmin
            };
            btnStaffAccounts.FlatAppearance.BorderSize = 0;
            btnStaffAccounts.Click += (s, e) => ShowStaffManagementDialog();

            // Language Toggle Button (Pill Styled - Mockup 1)
            btnLangToggle = new Button
            {
                Text = LocalizationManager.CurrentLanguage == AppLanguage.English ? "🌐 አማርኛ" : "🌐 English",
                Size = new Size(125, 34),
                Location = new Point(pnlHeader.Width - 140, 16),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.PrimaryLight,
                ForeColor = UITheme.Primary,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLangToggle.FlatAppearance.BorderSize = 0;
            btnLangToggle.Click += (s, e) =>
            {
                LocalizationManager.ToggleLanguage();
            };

            // Live Clock
            lblClock = new Label
            {
                Text = DateTime.Now.ToString("dd-MM-yyyy | HH:mm"),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlHeader.Width - 680, 24)
            };

            pnlHeader.Controls.Add(lblScreenTitle);
            pnlHeader.Controls.Add(lblBreadcrumb);
            pnlHeader.Controls.Add(lblClock);
            pnlHeader.Controls.Add(btnStaffAccounts);
            pnlHeader.Controls.Add(btnMobileAccess);
            pnlHeader.Controls.Add(btnNotificationBadge);
            pnlHeader.Controls.Add(btnLangToggle);
        }

        private void SetupClock()
        {
            clockTimer = new Timer { Interval = 30000 };
            clockTimer.Tick += (s, e) =>
            {
                lblClock.Text = DateTime.Now.ToString("dd-MM-yyyy | HH:mm");
            };
            clockTimer.Start();
        }

        private void ShowMobileAccessDialog()
        {
            if (!Session.IsAdmin)
            {
                string msg = LocalizationManager.CurrentLanguage == AppLanguage.Amharic
                    ? "ይቅርታ! የስልክ መዳረሻ የተፈቀደው ለአስተዳዳሪ (Admin) ብቻ ነው።"
                    : "Access Denied: Mobile Live Dashboard is restricted to Admin accounts only.";
                MessageBox.Show(msg, "Admin Only", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool isAmh = LocalizationManager.CurrentLanguage == AppLanguage.Amharic;
            string mobileUrl = MobileServer.GetMobileUrl();
            string localhostUrl = MobileServer.GetLocalhostUrl();

            using (Form dlg = new Form())
            {
                dlg.Text = isAmh ? "የስልክ መዳረሻ ዳሽቦርድ" : "Stock-Sync Mobile Dashboard";
                dlg.Size = new Size(560, 440);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = Color.White;

                // Top banner
                Panel topBanner = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 75,
                    BackColor = Color.FromArgb(254, 242, 237),
                    Padding = new Padding(24, 16, 24, 16)
                };
                Label lblDlgTitle = new Label
                {
                    Text = isAmh ? "📱 የስልክ መዳረሻ ዳሽቦርድ (ቀጥታ)" : "📱 Live Mobile Dashboard Access",
                    Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                    ForeColor = UITheme.Primary,
                    AutoSize = true,
                    Location = new Point(20, 14)
                };
                Label lblDlgSub = new Label
                {
                    Text = isAmh 
                        ? "በሱቁ ዋይፋይ (Wi-Fi) አማካኝነት ስልክዎ ላይ ሽያጭ እና ክምችት በቅጽበት ይከታተሉ" 
                        : "View live sales, completed orders, and low-stock alerts from your mobile phone.",
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                    ForeColor = UITheme.TextMuted,
                    AutoSize = true,
                    Location = new Point(22, 42)
                };
                topBanner.Controls.Add(lblDlgTitle);
                topBanner.Controls.Add(lblDlgSub);

                Label lblStep1 = new Label
                {
                    Text = isAmh ? "በስልክዎ ብሮውዘር (Chrome / Safari) ይህንን አድራሻ ይክፈቱ፦" : "Open this URL in your phone's browser (Chrome or Safari):",
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = UITheme.TextMain,
                    AutoSize = true,
                    Location = new Point(24, 95)
                };

                // URL display box
                TextBox txtUrl = new TextBox
                {
                    Text = mobileUrl,
                    Font = new Font("Consolas", 12F, FontStyle.Bold),
                    ForeColor = UITheme.Primary,
                    BackColor = Color.FromArgb(248, 250, 252),
                    ReadOnly = true,
                    Location = new Point(24, 125),
                    Size = new Size(350, 30)
                };

                Button btnCopy = new Button
                {
                    Text = isAmh ? "📋 ኮፒ አድርግ" : "📋 Copy Link",
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    BackColor = UITheme.Primary,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(130, 30),
                    Location = new Point(385, 124),
                    Cursor = Cursors.Hand
                };
                btnCopy.FlatAppearance.BorderSize = 0;
                btnCopy.Click += (s, e) =>
                {
                    try
                    {
                        Clipboard.SetText(mobileUrl);
                        btnCopy.Text = isAmh ? "✓ ተገልብጧል!" : "✓ Copied!";
                    }
                    catch { }
                };

                // Instructions
                Label lblInstructions = new Label
                {
                    Text = isAmh
                        ? "💡 መመሪያ፦\n" +
                          "1. ስልክዎ እና ይህ ኮምፒውተር በአንድ የዋይፋይ (Wi-Fi) ኔትወርክ ላይ መሆናቸውን ያረጋግጡ።\n" +
                          "2. ከላይ ያለውን አድራሻ በስልክዎ ላይ ይክፈቱ። በየ 15 ሰከንዱ ራሱ ይታደሳል።\n" +
                          "3. በስልክዎ 'Add to Home screen' በማድረግ እንደ መተግበሪያ መጠቀም ይችላሉ!"
                        : "💡 Quick Tips:\n" +
                          "1. Ensure your phone is connected to the same Wi-Fi / Hotspot as this computer.\n" +
                          "2. Type or scan the address above. The dashboard auto-refreshes every 15s.\n" +
                          "3. Tap 'Add to Home Screen' in Safari/Chrome for 1-tap app experience!",
                    Font = new Font("Segoe UI", 8.8F, FontStyle.Regular),
                    ForeColor = Color.FromArgb(71, 85, 105),
                    Location = new Point(24, 175),
                    Size = new Size(490, 110)
                };

                // Bottom action buttons
                Button btnTestBrowser = new Button
                {
                    Text = isAmh ? "🌐 በኮምፒውተርዎ ይሞክሩት" : "🌐 Test in PC Browser",
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    BackColor = Color.FromArgb(241, 245, 249),
                    ForeColor = UITheme.TextMain,
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(170, 36),
                    Location = new Point(24, 320),
                    Cursor = Cursors.Hand
                };
                btnTestBrowser.FlatAppearance.BorderSize = 0;
                btnTestBrowser.Click += (s, e) =>
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = localhostUrl,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not open browser: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                };

                Button btnClose = new Button
                {
                    Text = isAmh ? "ዝጋ" : "Close",
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    BackColor = Color.FromArgb(226, 232, 240),
                    ForeColor = UITheme.TextMain,
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(100, 36),
                    Location = new Point(415, 320),
                    Cursor = Cursors.Hand,
                    DialogResult = DialogResult.OK
                };
                btnClose.FlatAppearance.BorderSize = 0;

                dlg.Controls.Add(lblStep1);
                dlg.Controls.Add(txtUrl);
                dlg.Controls.Add(btnCopy);
                dlg.Controls.Add(lblInstructions);
                dlg.Controls.Add(btnTestBrowser);
                dlg.Controls.Add(btnClose);
                dlg.Controls.Add(topBanner);
                dlg.AcceptButton = btnClose;

                dlg.ShowDialog(this);
            }
        }

        private void ShowStaffManagementDialog()
        {
            if (!Session.IsAdmin)
            {
                MessageBox.Show("Access Denied: Staff accounts can only be managed by Admin.", "Admin Only", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool isAmh = LocalizationManager.CurrentLanguage == AppLanguage.Amharic;

            using (Form dlg = new Form())
            {
                dlg.Text = isAmh ? "የሱፐርማርኬት ሰራተኞች መለያ አስተዳደር" : "Supermarket Staff & Cashier Management";
                dlg.Size = new Size(680, 520);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = Color.White;

                // Top Banner
                Panel topBanner = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 70,
                    BackColor = Color.FromArgb(240, 253, 244),
                    Padding = new Padding(24, 14, 24, 14)
                };
                Label lblDlgTitle = new Label
                {
                    Text = isAmh ? "👥 የሱፐርማርኬት ሰራተኞች መለያ አስተዳደር" : "👥 Staff & Cashier Account Access",
                    Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(22, 163, 74),
                    AutoSize = true,
                    Location = new Point(20, 12)
                };
                Label lblDlgSub = new Label
                {
                    Text = isAmh 
                        ? "አዲስ ሰራተኛ ሲቀጠሩ ስም እና የይለፍ ቃል በመስጠት በሽያጭ (POS) ላይ እንዲሰሩ ያድርጉ" 
                        : "Create credentials for cashiers. Give staff their username and password so they can log in and start selling.",
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                    ForeColor = UITheme.TextMuted,
                    AutoSize = true,
                    Location = new Point(22, 38)
                };
                topBanner.Controls.Add(lblDlgTitle);
                topBanner.Controls.Add(lblDlgSub);

                // Add Staff Input Strip
                GroupBox gbAdd = new GroupBox
                {
                    Text = isAmh ? " ➕ አዲስ ሰራተኛ መመዝገቢያ " : " ➕ Register New Staff / Cashier ",
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = UITheme.TextMain,
                    Location = new Point(24, 85),
                    Size = new Size(615, 80)
                };

                Label lblU = new Label { Text = isAmh ? "የመለያ ስም፦" : "Username:", Location = new Point(16, 32), AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
                TextBox txtUser = new TextBox { Location = new Point(90, 28), Size = new Size(130, 25), Font = new Font("Segoe UI", 9.5F) };

                Label lblP = new Label { Text = isAmh ? "የይለፍ ቃል፦" : "Password:", Location = new Point(235, 32), AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
                TextBox txtPass = new TextBox { Location = new Point(305, 28), Size = new Size(130, 25), Font = new Font("Segoe UI", 9.5F) };

                Button btnCreate = new Button
                {
                    Text = isAmh ? "መለያ ፍጠር" : "Create Staff",
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    BackColor = UITheme.Primary,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Location = new Point(450, 26),
                    Size = new Size(150, 30),
                    Cursor = Cursors.Hand
                };
                btnCreate.FlatAppearance.BorderSize = 0;

                gbAdd.Controls.Add(lblU);
                gbAdd.Controls.Add(txtUser);
                gbAdd.Controls.Add(lblP);
                gbAdd.Controls.Add(txtPass);
                gbAdd.Controls.Add(btnCreate);

                // DataGrid for existing staff
                DataGridView dgv = new DataGridView
                {
                    Location = new Point(24, 180),
                    Size = new Size(615, 225),
                    ReadOnly = true,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    MultiSelect = false,
                    AllowUserToAddRows = false,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
                };
                UITheme.StyleDataGrid(dgv);

                Action loadStaff = () =>
                {
                    var staffList = StaffUserManager.GetAllStaffUsers();
                    dgv.DataSource = null;
                    dgv.DataSource = staffList;
                };

                loadStaff();

                btnCreate.Click += (s, e) =>
                {
                    if (StaffUserManager.CreateStaff(txtUser.Text, txtPass.Text, out string err))
                    {
                        MessageBox.Show(isAmh ? "ሰራተኛው በተሳካ ሁኔታ ተመዝግቧል! አሁን በ POS ላይ መስራት ይችላሉ።" : "Staff account created successfully! They can now log in on the POS.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        txtUser.Clear();
                        txtPass.Clear();
                        loadStaff();
                    }
                    else
                    {
                        MessageBox.Show(err, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                };

                // Bottom actions
                Button btnReset = new Button
                {
                    Text = isAmh ? "🔑 የይለፍ ቃል ቀይር" : "🔑 Reset Password",
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    BackColor = Color.FromArgb(241, 245, 249),
                    ForeColor = UITheme.TextMain,
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(140, 34),
                    Location = new Point(24, 420),
                    Cursor = Cursors.Hand
                };
                btnReset.FlatAppearance.BorderSize = 0;
                btnReset.Click += (s, e) =>
                {
                    if (dgv.SelectedRows.Count > 0 && dgv.SelectedRows[0].DataBoundItem is StaffUserInfo selected)
                    {
                        string newP = PromptInput("Enter new password for " + selected.Username + ":", "Reset Staff Password", "1234");
                        if (!string.IsNullOrWhiteSpace(newP))
                        {
                            if (StaffUserManager.ResetStaffPassword(selected.UserId, newP, out string err))
                            {
                                MessageBox.Show("Password updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                MessageBox.Show(err, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                };

                Button btnDelete = new Button
                {
                    Text = isAmh ? "🗑️ ሰርዝ" : "🗑️ Delete Staff",
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    BackColor = Color.FromArgb(254, 226, 226),
                    ForeColor = Color.FromArgb(220, 38, 38),
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(120, 34),
                    Location = new Point(175, 420),
                    Cursor = Cursors.Hand
                };
                btnDelete.FlatAppearance.BorderSize = 0;
                btnDelete.Click += (s, e) =>
                {
                    if (dgv.SelectedRows.Count > 0 && dgv.SelectedRows[0].DataBoundItem is StaffUserInfo selected)
                    {
                        if (MessageBox.Show($"Are you sure you want to remove staff account '{selected.Username}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            if (StaffUserManager.DeleteStaff(selected.UserId, out string err))
                            {
                                loadStaff();
                            }
                            else
                            {
                                MessageBox.Show(err, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                };

                Button btnCloseDlg = new Button
                {
                    Text = isAmh ? "ዝጋ" : "Close",
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    BackColor = Color.FromArgb(226, 232, 240),
                    ForeColor = UITheme.TextMain,
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(100, 34),
                    Location = new Point(539, 420),
                    Cursor = Cursors.Hand,
                    DialogResult = DialogResult.OK
                };
                btnCloseDlg.FlatAppearance.BorderSize = 0;

                dlg.Controls.Add(topBanner);
                dlg.Controls.Add(gbAdd);
                dlg.Controls.Add(dgv);
                dlg.Controls.Add(btnReset);
                dlg.Controls.Add(btnDelete);
                dlg.Controls.Add(btnCloseDlg);
                dlg.AcceptButton = btnCloseDlg;

                dlg.ShowDialog(this);
            }
        }

        private static string PromptInput(string prompt, string title, string defaultValue = "")
        {
            using (Form f = new Form())
            {
                f.Text = title;
                f.Size = new Size(380, 160);
                f.StartPosition = FormStartPosition.CenterParent;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.MaximizeBox = false;
                f.MinimizeBox = false;
                f.BackColor = Color.White;

                Label lbl = new Label { Text = prompt, Location = new Point(20, 15), AutoSize = true, Font = new Font("Segoe UI", 9F) };
                TextBox tb = new TextBox { Text = defaultValue, Location = new Point(20, 42), Size = new Size(320, 25), Font = new Font("Segoe UI", 10F) };
                Button btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(170, 80), Size = new Size(80, 28), BackColor = UITheme.Primary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                btnOk.FlatAppearance.BorderSize = 0;
                Button btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(260, 80), Size = new Size(80, 28) };

                f.Controls.AddRange(new Control[] { lbl, tb, btnOk, btnCancel });
                f.AcceptButton = btnOk;
                f.CancelButton = btnCancel;

                return f.ShowDialog() == DialogResult.OK ? tb.Text.Trim() : null;
            }
        }

        private void SwitchView(ScreenType screen)
        {
            currentScreen = screen;

            // Hide all views
            pnlViewOrders.Visible = false;
            pnlViewPos.Visible = false;
            pnlViewProducts.Visible = false;
            pnlViewStock.Visible = false;
            if (pnlViewSuppliers != null) pnlViewSuppliers.Visible = false;
            pnlViewDashboard.Visible = false;
            pnlViewReports.Visible = false;
            pnlViewAlerts.Visible = false;

            // Reset navigation button pill styling
            foreach (var b in navButtons)
            {
                b.BackColor = Color.White;
                b.ForeColor = UITheme.SidebarText;
                b.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            }

            Panel targetView = null;
            Button activeBtn = null;
            string titleKey = "";
            string breadcrumbKey = "";

            switch (screen)
            {
                case ScreenType.Orders:
                    targetView = pnlViewOrders;
                    activeBtn = btnNavOrders;
                    titleKey = "TitleOrders";
                    breadcrumbKey = "BreadcrumbOrders";
                    LoadOrdersData();
                    break;
                case ScreenType.Pos:
                    targetView = pnlViewPos;
                    activeBtn = btnNavPos;
                    titleKey = "TitlePos";
                    breadcrumbKey = "BreadcrumbPos";
                    break;
                case ScreenType.Products:
                    targetView = pnlViewProducts;
                    activeBtn = btnNavProducts;
                    titleKey = "TitleProducts";
                    breadcrumbKey = "BreadcrumbProducts";
                    LoadProductsData();
                    break;
                case ScreenType.Stock:
                    targetView = pnlViewStock;
                    activeBtn = btnNavStock;
                    titleKey = "TitleStock";
                    breadcrumbKey = "BreadcrumbStock";
                    LoadStockData();
                    break;
                case ScreenType.Suppliers:
                    targetView = pnlViewSuppliers;
                    activeBtn = btnNavSuppliers;
                    titleKey = "TitleSuppliers";
                    breadcrumbKey = "BreadcrumbSuppliers";
                    LoadSuppliersData();
                    break;
                case ScreenType.Dashboard:
                    targetView = pnlViewDashboard;
                    activeBtn = btnNavDashboard;
                    titleKey = "TitleDashboard";
                    breadcrumbKey = "BreadcrumbDashboard";
                    LoadDashboardData();
                    break;
                case ScreenType.Reports:
                    targetView = pnlViewReports;
                    activeBtn = btnNavReports;
                    titleKey = "TitleReports";
                    breadcrumbKey = "BreadcrumbReports";
                    LoadReportsData();
                    break;
                case ScreenType.Alerts:
                    targetView = pnlViewAlerts;
                    activeBtn = btnNavAlerts;
                    titleKey = "TitleAlerts";
                    breadcrumbKey = "BreadcrumbAlerts";
                    LoadAlertsData();
                    break;
            }

            // Highlight active button with vibrant coral pill (Mockup 1)
            if (activeBtn != null)
            {
                activeBtn.BackColor = UITheme.SidebarActiveBg;
                activeBtn.ForeColor = UITheme.SidebarActiveText;
                activeBtn.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            }

            // Update header titles
            lblScreenTitle.Text = LocalizationManager.T(titleKey);
            lblBreadcrumb.Text = LocalizationManager.T(breadcrumbKey);

            // Show active view
            if (targetView != null)
            {
                targetView.Visible = true;
                targetView.BringToFront();
            }
        }

        private void ApplyLanguage()
        {
            // Form Title
            this.Text = LocalizationManager.CurrentLanguage == AppLanguage.English
                ? "Stock-Sync — Supermarket Inventory & POS System"
                : "ስቶክ-ሲንክ — የሱፐርማርኬት ክምችት እና የሽያጭ ሲስተም";

            // Language Toggle Button
            if (btnLangToggle != null)
            {
                btnLangToggle.Text = LocalizationManager.CurrentLanguage == AppLanguage.English ? "🌐 አማርኛ" : "🌐 English";
            }

            // Mobile Access Button (Visible strictly to Admin)
            if (btnMobileAccess != null)
            {
                btnMobileAccess.Visible = Session.IsAdmin;
                btnMobileAccess.Text = LocalizationManager.CurrentLanguage == AppLanguage.Amharic ? "📱 የስልክ መዳረሻ" : "📱 Phone Live";
            }

            // Staff Accounts Button (Visible strictly to Admin)
            if (btnStaffAccounts != null)
            {
                btnStaffAccounts.Visible = Session.IsAdmin;
                btnStaffAccounts.Text = LocalizationManager.CurrentLanguage == AppLanguage.Amharic ? "👥 ሰራተኞች" : "👥 Staff Access";
            }

            // Profile
            if (lblProfileName != null)
            {
                lblProfileName.Text = Session.IsAdmin ? LocalizationManager.T("AdminRole") : LocalizationManager.T("StaffRole");
            }

            // Sidebar Nav
            if (btnNavDashboard != null) btnNavDashboard.Text = "⊞   " + LocalizationManager.T("NavDashboard");
            if (btnNavPos != null) btnNavPos.Text = "🛒   " + LocalizationManager.T("NavPos");
            if (btnNavProducts != null) btnNavProducts.Text = "📦   " + LocalizationManager.T("NavProducts");
            if (btnNavStock != null) btnNavStock.Text = "📋   " + LocalizationManager.T("NavStock");
            if (btnNavOrders != null) btnNavOrders.Text = "🧾   " + LocalizationManager.T("NavOrders");
            if (btnNavSuppliers != null) btnNavSuppliers.Text = "🚚   " + LocalizationManager.T("NavSuppliers");
            if (btnNavReports != null) btnNavReports.Text = "📈   " + LocalizationManager.T("NavReports");
            if (btnNavAlerts != null) btnNavAlerts.Text = "🔔   " + LocalizationManager.T("NavAlerts");

            // Header Titles
            string titleKey = "Title" + currentScreen.ToString();
            string breadcrumbKey = "Breadcrumb" + currentScreen.ToString();
            if (lblScreenTitle != null) lblScreenTitle.Text = LocalizationManager.T(titleKey);
            if (lblBreadcrumb != null) lblBreadcrumb.Text = LocalizationManager.T(breadcrumbKey);

            // View 1: Orders KPI Strip & Tabs
            if (lblOrdersKpiTotalTitle != null) lblOrdersKpiTotalTitle.Text = LocalizationManager.T("ColTotal") + " " + LocalizationManager.T("TitleOrders");
            if (lblOrdersKpiSalesTitle != null) lblOrdersKpiSalesTitle.Text = LocalizationManager.T("KpiTodaySales");
            if (lblOrdersKpiDoneTitle != null) lblOrdersKpiDoneTitle.Text = LocalizationManager.T("Completed");
            if (lblOrdersKpiPendingTitle != null) lblOrdersKpiPendingTitle.Text = LocalizationManager.T("Pending");

            if (lblOrdersCardTitle != null) lblOrdersCardTitle.Text = LocalizationManager.T("OrderHistory");
            if (btnOrdersTabAll != null) btnOrdersTabAll.Text = LocalizationManager.T("AllOrders");
            if (btnOrdersTabCompleted != null) btnOrdersTabCompleted.Text = LocalizationManager.T("Completed");
            if (btnOrdersTabPending != null) btnOrdersTabPending.Text = LocalizationManager.T("Pending");
            if (btnExportOrders != null) btnExportOrders.Text = LocalizationManager.T("ExportCsv");

            // View 2: POS
            if (lblScanTitle != null) lblScanTitle.Text = LocalizationManager.T("ScanPrompt");
            if (btnClearCart != null) btnClearCart.Text = LocalizationManager.T("ClearCart");
            if (lblSummaryTitle != null) lblSummaryTitle.Text = LocalizationManager.T("PaymentSummary");
            if (lblSubTitle != null) lblSubTitle.Text = LocalizationManager.T("Subtotal");
            if (lblTaxTitle != null) lblTaxTitle.Text = LocalizationManager.T("Tax");
            if (lblPayableTitle != null) lblPayableTitle.Text = LocalizationManager.T("TotalPayable");
            if (lblCashTitle != null) lblCashTitle.Text = LocalizationManager.T("CashReceived");
            if (lblChangeTitle != null) lblChangeTitle.Text = LocalizationManager.T("ChangeDue");
            if (btnCompleteSale != null) btnCompleteSale.Text = LocalizationManager.T("CompleteSale");
            if (btnPrintReceipt != null) btnPrintReceipt.Text = LocalizationManager.T("PrintReceipt");

            // Re-render POS amounts with active currency notation
            decimal curTotal = activeBill.CalculateSubtotal();
            if (lblPosSubtotal != null) lblPosSubtotal.Text = LocalizationManager.FormatMoney(curTotal);
            if (lblPosTotal != null) lblPosTotal.Text = LocalizationManager.FormatMoney(curTotal);
            if (txtPosCashReceived != null && decimal.TryParse(txtPosCashReceived.Text, out decimal cReceived))
            {
                if (lblPosChange != null) lblPosChange.Text = LocalizationManager.FormatMoney(cReceived - curTotal);
            }
            else
            {
                if (lblPosChange != null) lblPosChange.Text = LocalizationManager.FormatMoney(0m);
            }

            // View 3: Products
            if (btnProductSearch != null) btnProductSearch.Text = LocalizationManager.T("Search");
            if (btnProductExport != null) btnProductExport.Text = LocalizationManager.T("ExportCsv");
            if (lblProductFormTitle != null) lblProductFormTitle.Text = LocalizationManager.T("ManageProduct");
            if (lblProdBarcode != null) lblProdBarcode.Text = LocalizationManager.T("Barcode");
            if (lblProdName != null) lblProdName.Text = LocalizationManager.T("ProductName");
            if (lblProdCategory != null) lblProdCategory.Text = LocalizationManager.T("Category");
            if (lblProdStatus != null) lblProdStatus.Text = LocalizationManager.T("Status");
            if (btnProductAdd != null) btnProductAdd.Text = LocalizationManager.T("AddNewProduct");
            if (btnProductUpdate != null) btnProductUpdate.Text = LocalizationManager.T("UpdateSelected");
            if (btnProductDeactivate != null) btnProductDeactivate.Text = LocalizationManager.T("DeactivateProduct");

            // View 4: Stock
            if (btnStockSearch != null) btnStockSearch.Text = LocalizationManager.T("Search");
            if (btnStockExport != null) btnStockExport.Text = LocalizationManager.T("ExportCsv");
            if (lblStockFormTitle != null) lblStockFormTitle.Text = LocalizationManager.T("ManageStockBatch");
            if (lblStockPid != null) lblStockPid.Text = LocalizationManager.T("ProductId");
            if (lblStockCode != null) lblStockCode.Text = LocalizationManager.T("BatchCode");
            if (lblStockQty != null) lblStockQty.Text = LocalizationManager.T("ColQuantity") + ":";
            if (lblStockMin != null) lblStockMin.Text = LocalizationManager.T("MinThreshold");
            if (lblStockCp != null) lblStockCp.Text = LocalizationManager.T("CostPrice");
            if (lblStockSp != null) lblStockSp.Text = LocalizationManager.T("SellingPrice");
            if (lblStockArrival != null) lblStockArrival.Text = LocalizationManager.T("ArrivalDate");
            if (chkStockHasExpiry != null) chkStockHasExpiry.Text = LocalizationManager.T("HasExpiryDate");
            if (btnStockAdd != null) btnStockAdd.Text = LocalizationManager.T("SaveStockBatch");

            // View 5: Dashboard
            if (lblKpiTitle1 != null) lblKpiTitle1.Text = LocalizationManager.T("KpiTodaySales");
            if (lblKpiTitle2 != null) lblKpiTitle2.Text = LocalizationManager.T("KpiTodayOrders");
            if (lblKpiTitle3 != null) lblKpiTitle3.Text = LocalizationManager.T("KpiActiveProducts");
            if (lblKpiTitle4 != null) lblKpiTitle4.Text = LocalizationManager.T("KpiLowStock");

            // View 6: Reports
            if (lblReportPick != null) lblReportPick.Text = LocalizationManager.T("SelectDate");
            if (btnReportFilter != null) btnReportFilter.Text = LocalizationManager.T("GenerateReport");
            if (btnReportExport != null) btnReportExport.Text = LocalizationManager.T("ExportCsv");

            // View 7: Alerts
            if (lblAlertLowStock != null) lblAlertLowStock.Text = LocalizationManager.T("LowStockAlerts");
            if (lblAlertExpired != null) lblAlertExpired.Text = LocalizationManager.T("ExpiredBatches");
            if (lblAlertNeverSold != null) lblAlertNeverSold.Text = LocalizationManager.T("NeverSoldItems");

            // Localize column headers in DataGridViews
            LocalizeGridHeaders(dgvOrders);
            LocalizeGridHeaders(dgvCart);
            LocalizeGridHeaders(dgvProducts);
            LocalizeGridHeaders(dgvStock);
            LocalizeGridHeaders(dgvReports);

            // Invalidate grids so custom cell formatting re-renders in active currency notation
            dgvOrders?.Invalidate();
            dgvCart?.Invalidate();
            dgvStock?.Invalidate();
            dgvReports?.Invalidate();
        }

        private void LocalizeGridHeaders(DataGridView dgv)
        {
            if (dgv == null || dgv.Columns.Count == 0) return;
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                string name = col.Name;
                switch (name)
                {
                    case "S No": col.HeaderText = LocalizationManager.T("ColSNo"); break;
                    case "Order ID": col.HeaderText = LocalizationManager.T("ColOrderId"); break;
                    case "Order Name": col.HeaderText = LocalizationManager.T("ColOrderName"); break;
                    case "Stock Value": col.HeaderText = LocalizationManager.T("ColStockValue"); break;
                    case "Quantity": col.HeaderText = LocalizationManager.T("ColQuantity"); break;
                    case "Payment Method": col.HeaderText = LocalizationManager.T("ColPayment"); break;
                    case "Status": col.HeaderText = LocalizationManager.T("ColStatus"); break;
                    case "Barcode": col.HeaderText = LocalizationManager.T("Barcode").TrimEnd(':'); break;
                    case "Name": col.HeaderText = LocalizationManager.T("ColName"); break;
                    case "Price": col.HeaderText = LocalizationManager.T("ColPrice"); break;
                    case "Total": col.HeaderText = LocalizationManager.T("ColTotal"); break;
                    case "ID": col.HeaderText = LocalizationManager.T("ColId"); break;
                    case "Product Name": col.HeaderText = LocalizationManager.T("ColName"); break;
                    case "Product Category": col.HeaderText = LocalizationManager.T("ColCategory"); break;
                    case "Product Status": col.HeaderText = LocalizationManager.T("Status").TrimEnd(':'); break;
                    case "Stock Id": col.HeaderText = LocalizationManager.T("ColId"); break;
                    case "Stock Code": col.HeaderText = LocalizationManager.T("ColBatchCode"); break;
                    case "Cost Price/Unit": col.HeaderText = LocalizationManager.T("ColCostPrice"); break;
                    case "Selling Price/Unit": col.HeaderText = LocalizationManager.T("ColSellingPrice"); break;
                    case "Arrival Date": col.HeaderText = LocalizationManager.T("ColArrivalDate"); break;
                    case "Expirey Date":
                    case "Expiry Date": col.HeaderText = LocalizationManager.T("ColExpiryDate"); break;
                    case "Minimum Stock": col.HeaderText = LocalizationManager.T("ColMinStock"); break;
                    case "Cashier": col.HeaderText = LocalizationManager.T("ColCashier"); break;
                    case "Date": col.HeaderText = LocalizationManager.T("ColDate"); break;
                    case "Total Amount": col.HeaderText = LocalizationManager.T("ColTotal"); break;
                    case "Total Payable": col.HeaderText = LocalizationManager.T("TotalPayable").TrimEnd(':'); break;
                    case "Cash Received": col.HeaderText = LocalizationManager.T("ColCashReceived"); break;
                    case "Change Returned": col.HeaderText = LocalizationManager.T("ColChangeReturned"); break;
                }
            }
        }
        #endregion

        #region View 1: Orders Screen (Matching Mockup 1: Top Strip + Modern Filter Card)
        private void BuildOrdersView()
        {
            pnlViewOrders = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.AppBg,
                Visible = false
            };

            // 1. Top KPI Summary Strip (Mockup 1 Signature: Coral Card 1 + White Metric Cards)
            TableLayoutPanel tlpOrdersKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 88,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = UITheme.AppBg,
                Margin = new Padding(0, 0, 0, 14)
            };
            tlpOrdersKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpOrdersKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpOrdersKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpOrdersKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            // Card 1: Solid Vibrant Coral-Orange (Mockup 1 Total Orders)
            Panel pnlCoralCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.Primary,
                Margin = new Padding(0, 0, 10, 12),
                Padding = new Padding(16, 12, 16, 12)
            };
            lblOrdersKpiTotalTitle = new Label
            {
                Text = "Total Orders",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 20
            };
            lblOrdersKpiTotalVal = new Label
            {
                Text = "12",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill
            };
            pnlCoralCard.Controls.Add(lblOrdersKpiTotalVal);
            pnlCoralCard.Controls.Add(lblOrdersKpiTotalTitle);

            // Card 2: Occupied / Today's Sales (White Card)
            Panel pnlCard2 = CreateKpiMetricCard("Occupied / Today's Sales", LocalizationManager.FormatMoney(0m), out lblOrdersKpiSalesTitle, out lblOrdersKpiSalesVal);
            // Card 3: Reserved / Completed (White Card)
            Panel pnlCard3 = CreateKpiMetricCard("Reserved / Completed", "0", out lblOrdersKpiDoneTitle, out lblOrdersKpiDoneVal);
            // Card 4: Available / Pending (White Card)
            Panel pnlCard4 = CreateKpiMetricCard("Available / Pending", "0", out lblOrdersKpiPendingTitle, out lblOrdersKpiPendingVal);

            tlpOrdersKpis.Controls.Add(pnlCoralCard, 0, 0);
            tlpOrdersKpis.Controls.Add(pnlCard2, 1, 0);
            tlpOrdersKpis.Controls.Add(pnlCard3, 2, 0);
            tlpOrdersKpis.Controls.Add(pnlCard4, 3, 0);

            // 2. Main Order History White Card Container
            Panel pnlCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.CardBg,
                Padding = new Padding(20)
            };

            // Card Header (Tabs on left, Search & Date & Export on right)
            Panel pnlCardHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = UITheme.CardBg
            };

            lblOrdersCardTitle = new Label
            {
                Text = LocalizationManager.T("OrderHistory"),
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                ForeColor = UITheme.TextMain,
                Location = new Point(0, 0),
                AutoSize = true
            };

            // Tab Pills (Mockup 1 Analytics / Subscription style)
            FlowLayoutPanel pnlFilterTabs = new FlowLayoutPanel
            {
                Location = new Point(0, 30),
                Height = 32,
                Width = 420,
                BackColor = UITheme.CardBg
            };

            btnOrdersTabAll = new Button
            {
                Text = LocalizationManager.T("AllOrders"),
                AutoSize = true,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.PrimaryLight,
                ForeColor = UITheme.Primary,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            btnOrdersTabAll.FlatAppearance.BorderSize = 0;

            btnOrdersTabCompleted = new Button
            {
                Text = LocalizationManager.T("Completed"),
                AutoSize = true,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = UITheme.TextMuted,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            btnOrdersTabCompleted.FlatAppearance.BorderSize = 0;

            btnOrdersTabPending = new Button
            {
                Text = LocalizationManager.T("Pending"),
                AutoSize = true,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = UITheme.TextMuted,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            btnOrdersTabPending.FlatAppearance.BorderSize = 0;

            Action<Button, string> selectTab = (selected, tabKey) =>
            {
                activeOrderTab = tabKey;
                foreach (Control c in pnlFilterTabs.Controls)
                {
                    if (c is Button b)
                    {
                        b.BackColor = Color.Transparent;
                        b.ForeColor = UITheme.TextMuted;
                        b.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                    }
                }
                selected.BackColor = UITheme.PrimaryLight;
                selected.ForeColor = UITheme.Primary;
                selected.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                FilterOrdersGrid();
            };

            btnOrdersTabAll.Click += (s, e) => selectTab(btnOrdersTabAll, "All");
            btnOrdersTabCompleted.Click += (s, e) => selectTab(btnOrdersTabCompleted, "Completed");
            btnOrdersTabPending.Click += (s, e) => selectTab(btnOrdersTabPending, "Pending");

            pnlFilterTabs.Controls.Add(btnOrdersTabAll);
            pnlFilterTabs.Controls.Add(btnOrdersTabCompleted);
            pnlFilterTabs.Controls.Add(btnOrdersTabPending);

            // Right-aligned Search, Date Filter & Export Button
            txtOrdersSearch = new TextBox
            {
                Text = "",
                Width = 160,
                Font = UITheme.FontRegular,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlCard.Width - 460, 28),
                BackColor = UITheme.InputBg
            };
            txtOrdersSearch.TextChanged += (s, e) => FilterOrdersGrid();

            Label lblSearchIcon = new Label
            {
                Text = "🔍",
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlCard.Width - 485, 30),
                Font = new Font("Segoe UI", 9F)
            };

            dtpOrdersFilter = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                Width = 115,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlCard.Width - 280, 27),
                Font = UITheme.FontRegular
            };

            btnExportOrders = new Button
            {
                Text = LocalizationManager.T("ExportCsv"),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlCard.Width - 150, 25),
                Size = new Size(130, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnExportOrders.FlatAppearance.BorderSize = 0;
            btnExportOrders.Click += (s, e) =>
            {
                if (dgvOrders.DataSource is DataTable dt && dt.Rows.Count > 0)
                {
                    CsvExportHelper.ExportToCsv(dt, $"OrderHistory_{DateTime.Now:yyyyMMdd}.csv");
                }
            };

            pnlCardHeader.Controls.Add(lblOrdersCardTitle);
            pnlCardHeader.Controls.Add(pnlFilterTabs);
            pnlCardHeader.Controls.Add(lblSearchIcon);
            pnlCardHeader.Controls.Add(txtOrdersSearch);
            pnlCardHeader.Controls.Add(dtpOrdersFilter);
            pnlCardHeader.Controls.Add(btnExportOrders);

            // DataGridView matching Mockup 1
            dgvOrders = new DataGridView
            {
                Dock = DockStyle.Fill
            };
            UITheme.StyleDataGrid(dgvOrders);

            // Numeric & Currency formatting for Ethiopian Birr
            dgvOrders.CellFormatting += (s, e) =>
            {
                if ((dgvOrders.Columns[e.ColumnIndex].Name == "Stock Value" || 
                     dgvOrders.Columns[e.ColumnIndex].Name == "Total Payable") && e.Value != null)
                {
                    if (decimal.TryParse(e.Value.ToString(), out decimal val))
                    {
                        e.Value = LocalizationManager.FormatMoney(val);
                    }
                }
            };

            // Bottom Pagination Footer (Mockup 1 ‹ 1 2 3 4 ... 12 ›)
            Panel pnlPagination = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = UITheme.CardBg
            };

            Label lblPagination = new Label
            {
                Text = "‹   [ 1 ]   2   3   4   ...   12   ›",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = UITheme.Primary,
                AutoSize = true,
                Location = new Point(pnlCard.Width / 2 - 90, 12),
                Cursor = Cursors.Hand
            };
            pnlPagination.Controls.Add(lblPagination);

            pnlCard.Controls.Add(dgvOrders);
            pnlCard.Controls.Add(pnlPagination);
            pnlCard.Controls.Add(pnlCardHeader);

            pnlViewOrders.Controls.Add(pnlCard);
            pnlViewOrders.Controls.Add(tlpOrdersKpis);

            pnlContentContainer.Controls.Add(pnlViewOrders);
        }

        private Panel CreateKpiMetricCard(string title, string initialValue, out Label titleLabel, out Label valueLabel)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.CardBg,
                Margin = new Padding(0, 0, 10, 12),
                Padding = new Padding(16, 12, 16, 12)
            };
            card.Paint += (s, e) =>
            {
                using (Pen p = new Pen(UITheme.CardBorder, 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
                }
            };

            titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                Dock = DockStyle.Top,
                Height = 20
            };

            valueLabel = new Label
            {
                Text = initialValue,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = UITheme.TextMain,
                Dock = DockStyle.Fill
            };

            card.Controls.Add(valueLabel);
            card.Controls.Add(titleLabel);
            return card;
        }

        private void LoadOrdersData()
        {
            DataTable dt = Report.FetchOrderHistory();
            dgvOrders.DataSource = dt;
            LocalizeGridHeaders(dgvOrders);

            // Update KPI Strip metrics
            if (dt != null)
            {
                int totalCount = dt.Rows.Count;
                lblOrdersKpiTotalVal.Text = totalCount.ToString();

                int completed = 0;
                int pending = 0;
                decimal totalSales = 0m;

                foreach (DataRow row in dt.Rows)
                {
                    string status = row["Status"]?.ToString() ?? "";
                    if (status.Equals("Delivered", StringComparison.OrdinalIgnoreCase) ||
                        status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                    {
                        completed++;
                    }
                    else
                    {
                        pending++;
                    }

                    if (row.Table.Columns.Contains("Stock Value") && decimal.TryParse(row["Stock Value"]?.ToString(), out decimal sv))
                    {
                        totalSales += sv;
                    }
                }

                lblOrdersKpiSalesVal.Text = LocalizationManager.FormatMoney(totalSales);
                lblOrdersKpiDoneVal.Text = completed.ToString();
                lblOrdersKpiPendingVal.Text = pending.ToString();
            }
        }

        private void FilterOrdersGrid()
        {
            if (dgvOrders.DataSource is DataTable dt)
            {
                string search = txtOrdersSearch?.Text?.Trim().Replace("'", "''") ?? "";
                string filter = "";

                if (activeOrderTab == "Completed")
                {
                    filter = "[Status] = 'Delivered' OR [Status] = 'Completed'";
                }
                else if (activeOrderTab == "Pending")
                {
                    filter = "[Status] = 'Pending' OR [Status] = 'Processing'";
                }

                if (!string.IsNullOrEmpty(search))
                {
                    string searchFilter = $"[Order Name] LIKE '%{search}%' OR CONVERT([Order ID], 'System.String') LIKE '%{search}%'";
                    filter = string.IsNullOrEmpty(filter) ? searchFilter : $"({filter}) AND ({searchFilter})";
                }

                dt.DefaultView.RowFilter = filter;
            }
        }
        #endregion

        #region View 2: Point of Sale (POS)
        private void BuildPosView()
        {
            pnlViewPos = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.AppBg,
                Visible = false
            };

            TableLayoutPanel tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = UITheme.AppBg
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));

            // Left Panel: Barcode input + Cart Grid
            Panel pnlCartCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.CardBg,
                Padding = new Padding(20)
            };

            Panel pnlScanBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = UITheme.CardBg
            };

            lblScanTitle = new Label
            {
                Text = LocalizationManager.T("ScanPrompt"),
                Font = UITheme.FontBold,
                ForeColor = UITheme.TextMain,
                Location = new Point(0, 0),
                AutoSize = true
            };

            txtPosBarcode = new TextBox
            {
                Location = new Point(0, 24),
                Width = 400,
                Font = new Font("Segoe UI", 12F),
                BackColor = UITheme.InputBg
            };
            txtPosBarcode.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    string scanned = txtPosBarcode.Text.Trim();
                    if (!string.IsNullOrEmpty(scanned))
                    {
                        long bCode = 0;
                        if (!long.TryParse(scanned, out bCode))
                        {
                            // Resilient QR / 2D barcode parser: extract numbers from QR payload
                            var match = System.Text.RegularExpressions.Regex.Match(scanned, @"\d+");
                            if (match.Success)
                            {
                                long.TryParse(match.Value, out bCode);
                            }
                        }

                        if (bCode > 0)
                        {
                            if (activeBill.ScanBarcode(bCode))
                            {
                                RefreshPosCart();
                            }
                        }
                    }
                    txtPosBarcode.Clear();
                    txtPosBarcode.Focus();
                }
            };

            btnClearCart = new Button
            {
                Text = LocalizationManager.T("ClearCart"),
                Location = new Point(415, 22),
                Size = new Size(110, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.PrimaryLight,
                ForeColor = UITheme.Primary,
                Font = UITheme.FontRegular,
                Cursor = Cursors.Hand
            };
            btnClearCart.FlatAppearance.BorderSize = 0;
            btnClearCart.Click += (s, e) =>
            {
                activeBill.Clear();
                RefreshPosCart();
            };

            pnlScanBar.Controls.Add(lblScanTitle);
            pnlScanBar.Controls.Add(txtPosBarcode);
            pnlScanBar.Controls.Add(btnClearCart);

            dgvCart = new DataGridView { Dock = DockStyle.Fill };
            UITheme.StyleDataGrid(dgvCart);

            pnlCartCard.Controls.Add(dgvCart);
            pnlCartCard.Controls.Add(pnlScanBar);

            // Right Panel: Checkout Summary Card
            Panel pnlSummaryCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.CardBg,
                Padding = new Padding(24),
                Margin = new Padding(16, 0, 0, 0)
            };

            lblSummaryTitle = new Label
            {
                Text = LocalizationManager.T("PaymentSummary"),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = UITheme.TextMain,
                Dock = DockStyle.Top,
                Height = 35
            };

            Panel pnlAmounts = new Panel { Dock = DockStyle.Top, Height = 310, BackColor = UITheme.CardBg };

            lblSubTitle = new Label { Text = LocalizationManager.T("Subtotal"), Location = new Point(0, 15), Font = UITheme.FontRegular, ForeColor = UITheme.TextMuted, AutoSize = true };
            lblPosSubtotal = new Label { Text = LocalizationManager.FormatMoney(0m), Location = new Point(180, 15), Font = UITheme.FontBold, ForeColor = UITheme.TextMain, AutoSize = true };

            lblTaxTitle = new Label { Text = LocalizationManager.T("Tax"), Location = new Point(0, 45), Font = UITheme.FontRegular, ForeColor = UITheme.TextMuted, AutoSize = true };
            Label lblTaxVal = new Label { Text = LocalizationManager.FormatMoney(0m), Location = new Point(180, 45), Font = UITheme.FontBold, ForeColor = UITheme.TextMain, AutoSize = true };

            lblPayableTitle = new Label { Text = LocalizationManager.T("TotalPayable"), Location = new Point(0, 80), Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = UITheme.Primary, AutoSize = true };
            lblPosTotal = new Label { Text = LocalizationManager.FormatMoney(0m), Location = new Point(180, 80), Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = UITheme.Primary, AutoSize = true };

            Label lblPaymentMethodTitle = new Label { Text = LocalizationManager.T("ColPayment") + ":", Location = new Point(0, 115), Font = UITheme.FontBold, ForeColor = UITheme.TextMain, AutoSize = true };
            cmbPosPaymentMethod = new ComboBox { Location = new Point(0, 138), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F) };
            cmbPosPaymentMethod.Items.AddRange(new object[] { "Cash", "Telebirr", "CBE Birr / Card" });
            cmbPosPaymentMethod.SelectedIndex = 0;
            cmbPosPaymentMethod.SelectedIndexChanged += (s, e) =>
            {
                if (cmbPosPaymentMethod.SelectedIndex > 0)
                {
                    decimal sub = activeBill.CalculateSubtotal();
                    txtPosCashReceived.Text = sub.ToString("0.00");
                }
            };

            lblCashTitle = new Label { Text = LocalizationManager.T("CashReceived"), Location = new Point(0, 175), Font = UITheme.FontBold, ForeColor = UITheme.TextMain, AutoSize = true };
            txtPosCashReceived = new TextBox { Location = new Point(0, 198), Width = 260, Font = new Font("Segoe UI", 12F), BackColor = UITheme.InputBg };
            txtPosCashReceived.TextChanged += (s, e) =>
            {
                if (decimal.TryParse(txtPosCashReceived.Text, out decimal cash))
                {
                    decimal sub = activeBill.CalculateSubtotal();
                    lblPosChange.Text = LocalizationManager.FormatMoney(cash - sub);
                }
                else
                {
                    lblPosChange.Text = LocalizationManager.FormatMoney(0m);
                }
            };

            lblChangeTitle = new Label { Text = LocalizationManager.T("ChangeDue"), Location = new Point(0, 245), Font = UITheme.FontBold, ForeColor = UITheme.TextMain, AutoSize = true };
            lblPosChange = new Label { Text = LocalizationManager.FormatMoney(0m), Location = new Point(180, 245), Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = UITheme.Success, AutoSize = true };

            pnlAmounts.Controls.AddRange(new Control[] { lblSubTitle, lblPosSubtotal, lblTaxTitle, lblTaxVal, lblPayableTitle, lblPosTotal, lblPaymentMethodTitle, cmbPosPaymentMethod, lblCashTitle, txtPosCashReceived, lblChangeTitle, lblPosChange });

            btnCompleteSale = new Button
            {
                Text = LocalizationManager.T("CompleteSale"),
                Dock = DockStyle.Top,
                Height = 46,
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 10, 0, 10)
            };
            btnCompleteSale.FlatAppearance.BorderSize = 0;
            btnCompleteSale.Click += (s, e) =>
            {
                if (activeBill.Items.Count == 0)
                {
                    MessageBox.Show(LocalizationManager.T("MsgEmptyCart"), LocalizationManager.T("MsgWarningTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!decimal.TryParse(txtPosCashReceived.Text, out decimal cashReceived))
                {
                    MessageBox.Show(LocalizationManager.T("MsgEnterCash"), LocalizationManager.T("MsgInputErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPosCashReceived.Focus();
                    return;
                }

                decimal payable = activeBill.CalculateSubtotal();
                if (cashReceived < payable)
                {
                    MessageBox.Show($"{LocalizationManager.T("MsgInsufficientCash")} ({LocalizationManager.FormatMoney(cashReceived)} < {LocalizationManager.FormatMoney(payable)})", LocalizationManager.T("MsgPaymentErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                decimal changeDue = cashReceived - payable;
                var itemsCopy = activeBill.Items.Select(x => new BillItem { Product = x.Product, Quantity = x.Quantity, Price = x.Price }).ToList();

                string chosenPayment = cmbPosPaymentMethod?.Text ?? "Cash";
                if (activeBill.SaveCart(cashReceived, changeDue, chosenPayment, 0m, 0m, out int orderId))
                {
                    lastCheckoutItems = itemsCopy;
                    lastCheckoutOrderId = orderId;
                    lastCheckoutTotal = payable;
                    lastCheckoutCash = cashReceived;
                    lastCheckoutChange = changeDue;

                    string promptText = string.Format(LocalizationManager.T("MsgSaleCompleted"), orderId, LocalizationManager.FormatMoney(changeDue));
                    DialogResult res = MessageBox.Show(promptText, LocalizationManager.T("MsgCheckoutSuccessTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    
                    activeBill.Clear();
                    txtPosCashReceived.Clear();
                    RefreshPosCart();

                    if (res == DialogResult.Yes)
                    {
                        PrintThermalReceipt();
                    }
                }
            };

            btnPrintReceipt = new Button
            {
                Text = LocalizationManager.T("PrintReceipt"),
                Dock = DockStyle.Top,
                Height = 42,
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.PrimaryLight,
                ForeColor = UITheme.Primary,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 10, 0, 0)
            };
            btnPrintReceipt.FlatAppearance.BorderSize = 0;
            btnPrintReceipt.Click += (s, e) => PrintThermalReceipt();

            pnlSummaryCard.Controls.Add(btnPrintReceipt);
            pnlSummaryCard.Controls.Add(btnCompleteSale);
            pnlSummaryCard.Controls.Add(pnlAmounts);
            pnlSummaryCard.Controls.Add(lblSummaryTitle);

            tlp.Controls.Add(pnlCartCard, 0, 0);
            tlp.Controls.Add(pnlSummaryCard, 1, 0);

            pnlViewPos.Controls.Add(tlp);
            pnlContentContainer.Controls.Add(pnlViewPos);
        }

        private void RefreshPosCart()
        {
            dgvCart.DataSource = null;
            dgvCart.DataSource = activeBill.Items.Select(i => new
            {
                Barcode = i.Product.PBarcode,
                Name = i.Product.PName,
                Quantity = i.Quantity,
                Price = LocalizationManager.FormatMoney(i.Price),
                Total = LocalizationManager.FormatMoney(i.Total)
            }).ToList();

            LocalizeGridHeaders(dgvCart);

            decimal total = activeBill.CalculateSubtotal();
            lblPosSubtotal.Text = LocalizationManager.FormatMoney(total);
            lblPosTotal.Text = LocalizationManager.FormatMoney(total);

            if (decimal.TryParse(txtPosCashReceived.Text, out decimal cash))
            {
                lblPosChange.Text = LocalizationManager.FormatMoney(cash - total);
            }
            else
            {
                lblPosChange.Text = LocalizationManager.FormatMoney(0m);
            }
        }

        private void PrintThermalReceipt()
        {
            var items = lastCheckoutItems.Count > 0 ? lastCheckoutItems : activeBill.Items.ToList();
            if (items.Count == 0)
            {
                MessageBox.Show("No recent order items found to print.", "Print Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            decimal total = lastCheckoutTotal > 0 ? lastCheckoutTotal : activeBill.CalculateSubtotal();
            decimal cash = lastCheckoutCash > 0 ? lastCheckoutCash : total;
            decimal change = lastCheckoutChange >= 0 ? lastCheckoutChange : 0m;
            int orderId = lastCheckoutOrderId > 0 ? lastCheckoutOrderId : 1;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("           STOCK SYNC RETAIL POS           ");
            sb.AppendLine("         Supermarket Inventory System      ");
            sb.AppendLine("-------------------------------------------");
            sb.AppendLine($"Invoice: #{orderId,-12} Date: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"Cashier: {(string.IsNullOrEmpty(Session.Email) ? "Admin" : Session.Email)}");
            sb.AppendLine("===========================================");
            sb.AppendLine("Item                  Qty    Price    Total");
            sb.AppendLine("-------------------------------------------");
            foreach (var item in items)
            {
                string name = item.Product.PName;
                if (name.Length > 16) name = name.Substring(0, 16);
                sb.AppendLine($"{name,-17} {item.Quantity,3}  {LocalizationManager.FormatMoney(item.Price),10}  {LocalizationManager.FormatMoney(item.Total),10}");
            }
            sb.AppendLine("===========================================");
            sb.AppendLine($"Subtotal:                       {LocalizationManager.FormatMoney(total),15}");
            sb.AppendLine($"Tax (0%):                       {LocalizationManager.FormatMoney(0m),15}");
            sb.AppendLine($"TOTAL PAYABLE:                  {LocalizationManager.FormatMoney(total),15}");
            sb.AppendLine("-------------------------------------------");
            sb.AppendLine($"Cash Received:                  {LocalizationManager.FormatMoney(cash),15}");
            sb.AppendLine($"Change Due:                     {LocalizationManager.FormatMoney(change),15}");
            sb.AppendLine("===========================================");
            sb.AppendLine("        THANK YOU FOR SHOPPING WITH US!    ");
            sb.AppendLine("             አመሰግናለሁ! እንኳን ደህና መጡ!         ");
            sb.AppendLine();

            receiptText = sb.ToString();

            PrintDialog dlg = new PrintDialog { Document = printDoc };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                printDoc.Print();
            }
        }

        private void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            e.Graphics.DrawString(receiptText, new Font("Courier New", 9F), Brushes.Black, new PointF(10, 10));
        }
        #endregion

        #region View 3: Products Catalog
        private void BuildProductsView()
        {
            pnlViewProducts = new Panel { Dock = DockStyle.Fill, Visible = false };

            TableLayoutPanel tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));

            // Left: Grid Card
            Panel pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.CardBg,
                Padding = new Padding(20)
            };

            Panel pnlSearch = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = UITheme.CardBg };
            txtProductSearch = new TextBox { Location = new Point(0, 10), Width = 220, Font = UITheme.FontRegular, BackColor = UITheme.InputBg };
            btnProductSearch = new Button
            {
                Text = LocalizationManager.T("Search"),
                Location = new Point(225, 8),
                Size = new Size(80, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.Primary,
                ForeColor = Color.White,
                Font = UITheme.FontSmall,
                Cursor = Cursors.Hand
            };
            btnProductSearch.FlatAppearance.BorderSize = 0;
            btnProductSearch.Click += (s, e) => LoadProductsData(txtProductSearch.Text.Trim(), cmbProductFilterCategory?.Text);

            cmbProductFilterCategory = new ComboBox { Location = new Point(315, 9), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.FontSmall };
            cmbProductFilterCategory.Items.Add("All Categories");
            cmbProductFilterCategory.SelectedIndex = 0;
            cmbProductFilterCategory.SelectedIndexChanged += (s, e) => LoadProductsData(txtProductSearch.Text.Trim(), cmbProductFilterCategory.Text);

            btnProductExport = new Button
            {
                Text = LocalizationManager.T("ExportCsv"),
                Location = new Point(475, 8),
                Size = new Size(100, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.PrimaryLight,
                ForeColor = UITheme.Primary,
                Font = UITheme.FontSmall,
                Cursor = Cursors.Hand
            };
            btnProductExport.FlatAppearance.BorderSize = 0;
            btnProductExport.Click += (s, e) =>
            {
                if (dgvProducts.DataSource is DataTable dt)
                    CsvExportHelper.ExportToCsv(dt, "Products_Catalog.csv");
            };

            pnlSearch.Controls.AddRange(new Control[] { txtProductSearch, btnProductSearch, cmbProductFilterCategory, btnProductExport });

            dgvProducts = new DataGridView { Dock = DockStyle.Fill };
            UITheme.StyleDataGrid(dgvProducts);
            dgvProducts.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    var row = dgvProducts.Rows[e.RowIndex];
                    if (row.Cells["ID"].Value != DBNull.Value)
                    {
                        selectedProductId = Convert.ToInt32(row.Cells["ID"].Value);
                        txtProdBarcode.Text = row.Cells["Barcode"].Value?.ToString() ?? "";
                        txtProdName.Text = row.Cells["Product Name"].Value?.ToString() ?? "";
                        cmbProdCategory.Text = row.Cells["Category"].Value?.ToString() ?? "";
                        txtProdBrand.Text = row.Cells["Brand"].Value?.ToString() ?? "";
                        cmbProdUnit.Text = row.Cells["Unit"].Value?.ToString() ?? "Pcs";
                        cmbProdStatus.Text = row.Cells["Status"].Value?.ToString() ?? "Available";
                    }
                }
            };

            pnlGridCard.Controls.Add(dgvProducts);
            pnlGridCard.Controls.Add(pnlSearch);

            // Right: Form Card
            Panel pnlFormCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.CardBg,
                Padding = new Padding(20),
                Margin = new Padding(16, 0, 0, 0)
            };

            lblProductFormTitle = new Label { Text = LocalizationManager.T("ManageProduct"), Font = UITheme.FontSubtitle, ForeColor = UITheme.TextMain, Dock = DockStyle.Top, Height = 35 };

            Panel pnlFields = new Panel { Dock = DockStyle.Top, Height = 350, BackColor = UITheme.CardBg };

            lblProdBarcode = new Label { Text = LocalizationManager.T("Barcode"), Location = new Point(0, 5), Font = UITheme.FontBold, AutoSize = true };
            txtProdBarcode = new TextBox { Location = new Point(0, 25), Width = 260, BackColor = UITheme.InputBg };

            lblProdName = new Label { Text = LocalizationManager.T("ProductName"), Location = new Point(0, 60), Font = UITheme.FontBold, AutoSize = true };
            txtProdName = new TextBox { Location = new Point(0, 80), Width = 260, BackColor = UITheme.InputBg };

            lblProdCategory = new Label { Text = LocalizationManager.T("Category"), Location = new Point(0, 115), Font = UITheme.FontBold, AutoSize = true };
            cmbProdCategory = new ComboBox { Location = new Point(0, 135), Width = 260, DropDownStyle = ComboBoxStyle.DropDown };
            cmbProdCategory.Items.AddRange(new object[] { "Dairy", "Bakery", "Beverages", "Snacks", "Produce", "Canned Goods", "Household", "Personal Care", "Meat & Poultry" });

            lblProdBrand = new Label { Text = LocalizationManager.T("Brand"), Location = new Point(0, 170), Font = UITheme.FontBold, AutoSize = true };
            txtProdBrand = new TextBox { Location = new Point(0, 190), Width = 260, BackColor = UITheme.InputBg };

            lblProdUnit = new Label { Text = LocalizationManager.T("Unit"), Location = new Point(0, 225), Font = UITheme.FontBold, AutoSize = true };
            cmbProdUnit = new ComboBox { Location = new Point(0, 245), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbProdUnit.Items.AddRange(new object[] { "Pcs", "Kg", "Litre", "Pack", "Box", "Bottle", "Loaf", "Bag", "Can" });
            cmbProdUnit.SelectedIndex = 0;

            lblProdStatus = new Label { Text = LocalizationManager.T("Status"), Location = new Point(0, 280), Font = UITheme.FontBold, AutoSize = true };
            cmbProdStatus = new ComboBox { Location = new Point(0, 300), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbProdStatus.Items.AddRange(new object[] { "Available", "Inactive" });
            cmbProdStatus.SelectedIndex = 0;

            pnlFields.Controls.AddRange(new Control[] { 
                lblProdBarcode, txtProdBarcode, 
                lblProdName, txtProdName, 
                lblProdCategory, cmbProdCategory, 
                lblProdBrand, txtProdBrand, 
                lblProdUnit, cmbProdUnit, 
                lblProdStatus, cmbProdStatus 
            });

            Panel pnlBtnGroup = new Panel { Dock = DockStyle.Top, Height = 175, BackColor = UITheme.CardBg };

            btnProductAdd = new Button { Text = LocalizationManager.T("AddNewProduct"), Location = new Point(0, 5), Size = new Size(260, 36), FlatStyle = FlatStyle.Flat, BackColor = UITheme.Primary, ForeColor = Color.White, Font = UITheme.FontBold, Cursor = Cursors.Hand };
            btnProductAdd.FlatAppearance.BorderSize = 0;
            btnProductAdd.Click += (s, e) =>
            {
                if (long.TryParse(txtProdBarcode.Text, out long barcode) && !string.IsNullOrEmpty(txtProdName.Text))
                {
                    var prod = new Models.ProductModel
                    {
                        Barcode = barcode,
                        Name = txtProdName.Text.Trim(),
                        Category = cmbProdCategory.Text.Trim(),
                        Brand = txtProdBrand.Text.Trim(),
                        Unit = cmbProdUnit.Text.Trim(),
                        Status = cmbProdStatus.Text
                    };
                    var repo = new Data.ProductRepository();
                    if (repo.Add(prod, out string err))
                    {
                        MessageBox.Show(LocalizationManager.T("MsgProductAdded"), LocalizationManager.T("MsgSuccessTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearProductForm();
                        LoadProductsData();
                        PopulateStockFormDropdowns();
                    }
                    else
                    {
                        MessageBox.Show(err, "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Please enter valid numeric barcode and product name.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            btnProductUpdate = new Button { Text = LocalizationManager.T("UpdateSelected"), Location = new Point(0, 46), Size = new Size(260, 36), FlatStyle = FlatStyle.Flat, BackColor = UITheme.PrimaryLight, ForeColor = UITheme.Primary, Font = UITheme.FontBold, Cursor = Cursors.Hand };
            btnProductUpdate.FlatAppearance.BorderSize = 0;
            btnProductUpdate.Click += (s, e) =>
            {
                if (selectedProductId > 0 && long.TryParse(txtProdBarcode.Text, out long barcode))
                {
                    var prod = new Models.ProductModel
                    {
                        Id = selectedProductId,
                        Barcode = barcode,
                        Name = txtProdName.Text.Trim(),
                        Category = cmbProdCategory.Text.Trim(),
                        Brand = txtProdBrand.Text.Trim(),
                        Unit = cmbProdUnit.Text.Trim(),
                        Status = cmbProdStatus.Text,
                        IsActive = (cmbProdStatus.Text != "Inactive")
                    };
                    var repo = new Data.ProductRepository();
                    if (repo.Update(prod, out string err))
                    {
                        MessageBox.Show(LocalizationManager.T("MsgProductUpdated"), LocalizationManager.T("MsgSuccessTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadProductsData();
                        PopulateStockFormDropdowns();
                    }
                    else
                    {
                        MessageBox.Show(err, "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            };

            btnProductDeactivate = new Button { Text = LocalizationManager.T("DeactivateProduct"), Location = new Point(0, 87), Size = new Size(260, 36), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(254, 242, 242), ForeColor = UITheme.Danger, Font = UITheme.FontBold, Cursor = Cursors.Hand };
            btnProductDeactivate.FlatAppearance.BorderSize = 0;
            btnProductDeactivate.Click += (s, e) =>
            {
                if (selectedProductId > 0)
                {
                    var repo = new Data.ProductRepository();
                    if (repo.Delete(selectedProductId, out string err))
                    {
                        MessageBox.Show(LocalizationManager.T("MsgProductDeactivated"), LocalizationManager.T("MsgSuccessTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearProductForm();
                        LoadProductsData();
                        PopulateStockFormDropdowns();
                    }
                }
            };

            btnProductClear = new Button { Text = "🔄 Clear", Location = new Point(0, 128), Size = new Size(260, 32), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(241, 245, 249), ForeColor = UITheme.TextMuted, Font = UITheme.FontBold, Cursor = Cursors.Hand };
            btnProductClear.FlatAppearance.BorderSize = 0;
            btnProductClear.Click += (s, e) => ClearProductForm();

            pnlBtnGroup.Controls.AddRange(new Control[] { btnProductAdd, btnProductUpdate, btnProductDeactivate, btnProductClear });

            pnlFormCard.Controls.AddRange(new Control[] { pnlBtnGroup, pnlFields, lblProductFormTitle });

            tlp.Controls.Add(pnlGridCard, 0, 0);
            tlp.Controls.Add(pnlFormCard, 1, 0);

            pnlViewProducts.Controls.Add(tlp);
            pnlContentContainer.Controls.Add(pnlViewProducts);
        }

        private void ClearProductForm()
        {
            selectedProductId = -1;
            txtProdBarcode?.Clear();
            txtProdName?.Clear();
            if (cmbProdCategory != null) cmbProdCategory.Text = "";
            txtProdBrand?.Clear();
            if (cmbProdUnit != null && cmbProdUnit.Items.Count > 0) cmbProdUnit.SelectedIndex = 0;
            if (cmbProdStatus != null && cmbProdStatus.Items.Count > 0) cmbProdStatus.SelectedIndex = 0;
        }

        private void LoadProductsData(string search = "", string categoryFilter = "")
        {
            DataTable dt = new DataTable();
            try
            {
                using (var conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string q = @"
                    SELECT 
                        p.ID, 
                        p.Barcode, 
                        p.PName AS [Product Name], 
                        ISNULL(p.Category, 'Uncategorized') AS [Category], 
                        ISNULL(p.Brand, '-') AS [Brand],
                        ISNULL(p.Unit, 'Pcs') AS [Unit],
                        p.PStatus AS [Status], 
                        ISNULL(SUM(s.quantity), 0) AS [Stock Quantity],
                        ISNULL(MAX(s.sp_unit), 0.00) AS [Price],
                        p.DateAdded AS [Date Added]
                    FROM Products p
                    LEFT JOIN Stocks s ON p.ID = s.barcode_id AND s.IsActive = 1
                    WHERE p.IsActive = 1";

                    if (!string.IsNullOrEmpty(search))
                    {
                        q += " AND (p.PName LIKE @Search OR CAST(p.Barcode AS NVARCHAR) LIKE @Search)";
                    }
                    if (!string.IsNullOrEmpty(categoryFilter) && categoryFilter != "All Categories")
                    {
                        q += " AND p.Category = @Cat";
                    }

                    q += @" GROUP BY p.ID, p.Barcode, p.PName, p.Category, p.Brand, p.Unit, p.PStatus, p.DateAdded
                           ORDER BY p.ID DESC";

                    using (var cmd = new System.Data.SqlClient.SqlCommand(q, conn))
                    {
                        if (!string.IsNullOrEmpty(search)) cmd.Parameters.AddWithValue("@Search", $"%{search}%");
                        if (!string.IsNullOrEmpty(categoryFilter) && categoryFilter != "All Categories") cmd.Parameters.AddWithValue("@Cat", categoryFilter);
                        using (var da = new System.Data.SqlClient.SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }

                dgvProducts.DataSource = dt;
                LocalizeGridHeaders(dgvProducts);

                // Populate category filter dropdown if empty
                if (cmbProductFilterCategory != null && cmbProductFilterCategory.Items.Count <= 1)
                {
                    var catList = new Data.ProductRepository().GetDistinctCategories();
                    foreach (var c in catList)
                    {
                        if (!cmbProductFilterCategory.Items.Contains(c))
                            cmbProductFilterCategory.Items.Add(c);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error loading products", ex);
            }
        }
        #endregion

        #region View 4: Stock Batches
        private void BuildStockView()
        {
            pnlViewStock = new Panel { Dock = DockStyle.Fill, Visible = false };

            TableLayoutPanel tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));

            // Left: Grid Card
            Panel pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.CardBg,
                Padding = new Padding(20)
            };

            Panel pnlSearch = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = UITheme.CardBg };
            txtStockSearch = new TextBox { Location = new Point(0, 10), Width = 300, Font = UITheme.FontRegular, BackColor = UITheme.InputBg };
            btnStockSearch = new Button
            {
                Text = LocalizationManager.T("Search"),
                Location = new Point(310, 8),
                Size = new Size(100, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.Primary,
                ForeColor = Color.White,
                Font = UITheme.FontSmall,
                Cursor = Cursors.Hand
            };
            btnStockSearch.FlatAppearance.BorderSize = 0;
            btnStockSearch.Click += (s, e) => LoadStockData(txtStockSearch.Text.Trim());

            btnStockExport = new Button
            {
                Text = LocalizationManager.T("ExportCsv"),
                Location = new Point(420, 8),
                Size = new Size(110, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.PrimaryLight,
                ForeColor = UITheme.Primary,
                Font = UITheme.FontSmall,
                Cursor = Cursors.Hand
            };
            btnStockExport.FlatAppearance.BorderSize = 0;
            btnStockExport.Click += (s, e) =>
            {
                if (dgvStock.DataSource is DataTable dt)
                    CsvExportHelper.ExportToCsv(dt, "Stock_Batches.csv");
            };

            pnlSearch.Controls.AddRange(new Control[] { txtStockSearch, btnStockSearch, btnStockExport });

            dgvStock = new DataGridView { Dock = DockStyle.Fill };
            UITheme.StyleDataGrid(dgvStock);
            dgvStock.CellFormatting += (s, e) =>
            {
                if ((dgvStock.Columns[e.ColumnIndex].Name == "Cost Price/Unit" || dgvStock.Columns[e.ColumnIndex].Name == "Selling Price/Unit") && e.Value != null)
                {
                    if (decimal.TryParse(e.Value.ToString(), out decimal val))
                    {
                        e.Value = LocalizationManager.FormatMoney(val);
                    }
                }
            };
            dgvStock.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    var row = dgvStock.Rows[e.RowIndex];
                    if (row.Cells["Stock Id"].Value != DBNull.Value)
                    {
                        selectedStockId = Convert.ToInt32(row.Cells["Stock Id"].Value);
                        txtStockCode.Text = row.Cells["Stock Code"].Value?.ToString() ?? "";
                        txtStockQty.Text = row.Cells["Quantity"].Value?.ToString() ?? "";
                        txtStockCp.Text = row.Cells["Cost Price/Unit"].Value?.ToString() ?? "";
                        txtStockSp.Text = row.Cells["Selling Price/Unit"].Value?.ToString() ?? "";

                        // Select corresponding product in combo
                        long barcode = row.Cells["Barcode"].Value != DBNull.Value ? Convert.ToInt64(row.Cells["Barcode"].Value) : 0;
                        for (int i = 0; i < cmbStockProduct.Items.Count; i++)
                        {
                            if (cmbStockProduct.Items[i] is ComboBoxItem item && item.Text.Contains(barcode.ToString()))
                            {
                                cmbStockProduct.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                }
            };

            pnlGridCard.Controls.Add(dgvStock);
            pnlGridCard.Controls.Add(pnlSearch);

            // Right: Add / Manage Stock Batch
            Panel pnlFormCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.CardBg,
                Padding = new Padding(20),
                Margin = new Padding(16, 0, 0, 0)
            };

            lblStockFormTitle = new Label { Text = LocalizationManager.T("ManageStockBatch"), Font = UITheme.FontSubtitle, ForeColor = UITheme.TextMain, Dock = DockStyle.Top, Height = 35 };

            Panel pnlFields = new Panel { Dock = DockStyle.Top, Height = 380, BackColor = UITheme.CardBg };

            lblStockPid = new Label { Text = LocalizationManager.T("NavProducts") + ":", Location = new Point(0, 5), Font = UITheme.FontBold, AutoSize = true };
            cmbStockProduct = new ComboBox { Location = new Point(0, 25), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };

            lblStockCode = new Label { Text = LocalizationManager.T("BatchCode"), Location = new Point(0, 55), Font = UITheme.FontBold, AutoSize = true };
            txtStockCode = new TextBox { Location = new Point(0, 75), Width = 260, BackColor = UITheme.InputBg };

            lblStockSupplier = new Label { Text = LocalizationManager.T("Supplier") + ":", Location = new Point(0, 105), Font = UITheme.FontBold, AutoSize = true };
            cmbStockSupplier = new ComboBox { Location = new Point(0, 125), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };

            lblStockQty = new Label { Text = LocalizationManager.T("ColQuantity") + ":", Location = new Point(0, 155), Font = UITheme.FontBold, AutoSize = true };
            txtStockQty = new TextBox { Location = new Point(0, 175), Width = 120, BackColor = UITheme.InputBg };

            lblStockMin = new Label { Text = LocalizationManager.T("MinThreshold"), Location = new Point(140, 155), Font = UITheme.FontBold, AutoSize = true };
            txtStockMin = new TextBox { Location = new Point(140, 175), Width = 120, BackColor = UITheme.InputBg, Text = "5" };

            lblStockCp = new Label { Text = LocalizationManager.T("CostPrice"), Location = new Point(0, 205), Font = UITheme.FontBold, AutoSize = true };
            txtStockCp = new TextBox { Location = new Point(0, 225), Width = 120, BackColor = UITheme.InputBg, Text = "0.00" };

            lblStockSp = new Label { Text = LocalizationManager.T("SellingPrice"), Location = new Point(140, 205), Font = UITheme.FontBold, AutoSize = true };
            txtStockSp = new TextBox { Location = new Point(140, 225), Width = 120, BackColor = UITheme.InputBg, Text = "0.00" };

            lblStockArrival = new Label { Text = LocalizationManager.T("ArrivalDate"), Location = new Point(0, 255), Font = UITheme.FontBold, AutoSize = true };
            dtpStockArrival = new DateTimePicker { Location = new Point(0, 275), Width = 260, Format = DateTimePickerFormat.Short };

            chkStockHasExpiry = new CheckBox { Text = LocalizationManager.T("HasExpiryDate"), Location = new Point(0, 305), Font = UITheme.FontBold, AutoSize = true };
            dtpStockExpiry = new DateTimePicker { Location = new Point(0, 330), Width = 260, Format = DateTimePickerFormat.Short };

            pnlFields.Controls.AddRange(new Control[] { 
                lblStockPid, cmbStockProduct, 
                lblStockCode, txtStockCode, 
                lblStockSupplier, cmbStockSupplier,
                lblStockQty, txtStockQty, 
                lblStockMin, txtStockMin, 
                lblStockCp, txtStockCp, 
                lblStockSp, txtStockSp, 
                lblStockArrival, dtpStockArrival, 
                chkStockHasExpiry, dtpStockExpiry 
            });

            Panel pnlBtnGroup = new Panel { Dock = DockStyle.Top, Height = 175, BackColor = UITheme.CardBg };

            btnStockAdd = new Button { Text = LocalizationManager.T("SaveStockBatch"), Location = new Point(0, 5), Size = new Size(260, 36), FlatStyle = FlatStyle.Flat, BackColor = UITheme.Primary, ForeColor = Color.White, Font = UITheme.FontBold, Cursor = Cursors.Hand };
            btnStockAdd.FlatAppearance.BorderSize = 0;
            btnStockAdd.Click += (s, e) =>
            {
                if (cmbStockProduct.SelectedItem is ComboBoxItem prodItem && int.TryParse(txtStockQty.Text, out int qty) && decimal.TryParse(txtStockSp.Text, out decimal sp))
                {
                    int pid = (int)prodItem.Value;
                    int? supId = cmbStockSupplier.SelectedItem is ComboBoxItem sItem ? (int?)sItem.Value : null;

                    var batch = new Models.StockBatchModel
                    {
                        BarcodeId = pid,
                        StockCode = txtStockCode.Text.Trim(),
                        SupplierId = supId,
                        Quantity = qty,
                        MinimumStock = int.TryParse(txtStockMin.Text, out int min) ? min : 5,
                        CostPrice = decimal.TryParse(txtStockCp.Text, out decimal cp) ? cp : 0m,
                        SellingPrice = sp,
                        ArrivalDate = dtpStockArrival.Value,
                        ExpiryDate = chkStockHasExpiry.Checked ? (DateTime?)dtpStockExpiry.Value : null
                    };

                    var repo = new Data.StockRepository();
                    if (repo.AddBatch(batch, out string err))
                    {
                        MessageBox.Show(LocalizationManager.T("MsgStockAdded"), LocalizationManager.T("MsgSuccessTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearStockForm();
                        LoadStockData();
                    }
                    else
                    {
                        MessageBox.Show(err, "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Please select a product and enter valid numeric quantity and price.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            btnStockUpdate = new Button { Text = "✏️ Update Batch", Location = new Point(0, 46), Size = new Size(260, 36), FlatStyle = FlatStyle.Flat, BackColor = UITheme.PrimaryLight, ForeColor = UITheme.Primary, Font = UITheme.FontBold, Cursor = Cursors.Hand };
            btnStockUpdate.FlatAppearance.BorderSize = 0;
            btnStockUpdate.Click += (s, e) =>
            {
                if (selectedStockId > 0 && cmbStockProduct.SelectedItem is ComboBoxItem prodItem && int.TryParse(txtStockQty.Text, out int qty) && decimal.TryParse(txtStockSp.Text, out decimal sp))
                {
                    int pid = (int)prodItem.Value;
                    int? supId = cmbStockSupplier.SelectedItem is ComboBoxItem sItem ? (int?)sItem.Value : null;

                    var batch = new Models.StockBatchModel
                    {
                        StockId = selectedStockId,
                        BarcodeId = pid,
                        StockCode = txtStockCode.Text.Trim(),
                        SupplierId = supId,
                        Quantity = qty,
                        MinimumStock = int.TryParse(txtStockMin.Text, out int min) ? min : 5,
                        CostPrice = decimal.TryParse(txtStockCp.Text, out decimal cp) ? cp : 0m,
                        SellingPrice = sp,
                        ArrivalDate = dtpStockArrival.Value,
                        ExpiryDate = chkStockHasExpiry.Checked ? (DateTime?)dtpStockExpiry.Value : null
                    };

                    var repo = new Data.StockRepository();
                    if (repo.UpdateBatch(batch, out string err))
                    {
                        MessageBox.Show("Stock batch updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadStockData();
                    }
                    else
                    {
                        MessageBox.Show(err, "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            };

            btnStockDelete = new Button { Text = "🗑️ Delete Batch", Location = new Point(0, 87), Size = new Size(260, 36), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(254, 242, 242), ForeColor = UITheme.Danger, Font = UITheme.FontBold, Cursor = Cursors.Hand };
            btnStockDelete.FlatAppearance.BorderSize = 0;
            btnStockDelete.Click += (s, e) =>
            {
                if (selectedStockId > 0)
                {
                    if (MessageBox.Show("Are you sure you want to delete this batch?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        var repo = new Data.StockRepository();
                        if (repo.DeleteBatch(selectedStockId, out string err))
                        {
                            ClearStockForm();
                            LoadStockData();
                        }
                    }
                }
            };

            btnStockClear = new Button { Text = "🔄 Clear", Location = new Point(0, 128), Size = new Size(260, 32), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(241, 245, 249), ForeColor = UITheme.TextMuted, Font = UITheme.FontBold, Cursor = Cursors.Hand };
            btnStockClear.FlatAppearance.BorderSize = 0;
            btnStockClear.Click += (s, e) => ClearStockForm();

            pnlBtnGroup.Controls.AddRange(new Control[] { btnStockAdd, btnStockUpdate, btnStockDelete, btnStockClear });

            pnlFormCard.Controls.AddRange(new Control[] { pnlBtnGroup, pnlFields, lblStockFormTitle });

            tlp.Controls.Add(pnlGridCard, 0, 0);
            tlp.Controls.Add(pnlFormCard, 1, 0);

            pnlViewStock.Controls.Add(tlp);
            pnlContentContainer.Controls.Add(pnlViewStock);
        }

        private void ClearStockForm()
        {
            selectedStockId = -1;
            txtStockCode?.Clear();
            txtStockQty?.Clear();
            if (txtStockMin != null) txtStockMin.Text = "5";
            if (txtStockCp != null) txtStockCp.Text = "0.00";
            if (txtStockSp != null) txtStockSp.Text = "0.00";
            if (chkStockHasExpiry != null) chkStockHasExpiry.Checked = false;
        }

        private void PopulateStockFormDropdowns()
        {
            try
            {
                if (cmbStockProduct != null)
                {
                    cmbStockProduct.Items.Clear();
                    var prods = new Data.ProductRepository().GetAll(activeOnly: true);
                    foreach (var p in prods)
                    {
                        cmbStockProduct.Items.Add(new ComboBoxItem { Text = $"{p.Name} [{p.Barcode}]", Value = p.Id });
                    }
                    if (cmbStockProduct.Items.Count > 0) cmbStockProduct.SelectedIndex = 0;
                }

                if (cmbStockSupplier != null)
                {
                    cmbStockSupplier.Items.Clear();
                    var sups = new Data.SupplierRepository().GetAll(activeOnly: true);
                    foreach (var s in sups)
                    {
                        cmbStockSupplier.Items.Add(new ComboBoxItem { Text = s.SupplierName, Value = s.SupplierId });
                    }
                    if (cmbStockSupplier.Items.Count > 0) cmbStockSupplier.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error populating stock dropdowns", ex);
            }
        }

        private void LoadStockData(string filter = "")
        {
            DataTable dt = new DataTable();
            try
            {
                using (var conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string q = @"
                    SELECT 
                        s.stock_id AS [Stock Id], 
                        p.Barcode, 
                        p.PName AS [Product Name], 
                        s.stock_code AS [Stock Code], 
                        ISNULL(sup.SupplierName, 'Direct') AS [Supplier],
                        s.quantity AS Quantity, 
                        s.cp_unit AS [Cost Price/Unit], 
                        s.sp_unit AS [Selling Price/Unit], 
                        s.arrival_date AS [Arrival Date],
                        s.expiry_date AS [Expiry Date],
                        s.date_added AS [Date Added]
                    FROM Stocks s
                    INNER JOIN Products p ON s.barcode_id = p.ID
                    LEFT JOIN Suppliers sup ON s.supplier_id = sup.SupplierID
                    WHERE s.IsActive = 1 AND p.IsActive = 1";

                    if (!string.IsNullOrEmpty(filter))
                    {
                        q += " AND (p.PName LIKE @Filter OR s.stock_code LIKE @Filter OR CAST(p.Barcode AS NVARCHAR) LIKE @Filter)";
                    }
                    q += " ORDER BY s.stock_id DESC";

                    using (var cmd = new System.Data.SqlClient.SqlCommand(q, conn))
                    {
                        if (!string.IsNullOrEmpty(filter)) cmd.Parameters.AddWithValue("@Filter", $"%{filter}%");
                        using (var da = new System.Data.SqlClient.SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }
                dgvStock.DataSource = dt;
                LocalizeGridHeaders(dgvStock);
            }
            catch (Exception ex)
            {
                Logger.LogError("Error loading stock view", ex);
            }
        }
        #endregion

        #region View: Suppliers Directory
        private void BuildSuppliersView()
        {
            pnlViewSuppliers = new Panel { Dock = DockStyle.Fill, Visible = false };

            TableLayoutPanel tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));

            // Left: Grid Card
            Panel pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.CardBg,
                Padding = new Padding(20)
            };

            Panel pnlSearch = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = UITheme.CardBg };
            txtSupplierSearch = new TextBox { Location = new Point(0, 10), Width = 300, Font = UITheme.FontRegular, BackColor = UITheme.InputBg };
            btnSupplierSearch = new Button
            {
                Text = LocalizationManager.T("Search"),
                Location = new Point(310, 8),
                Size = new Size(100, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.Primary,
                ForeColor = Color.White,
                Font = UITheme.FontSmall,
                Cursor = Cursors.Hand
            };
            btnSupplierSearch.FlatAppearance.BorderSize = 0;
            btnSupplierSearch.Click += (s, e) => LoadSuppliersData(txtSupplierSearch.Text.Trim());

            btnSupplierExport = new Button
            {
                Text = LocalizationManager.T("ExportCsv"),
                Location = new Point(420, 8),
                Size = new Size(110, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.PrimaryLight,
                ForeColor = UITheme.Primary,
                Font = UITheme.FontSmall,
                Cursor = Cursors.Hand
            };
            btnSupplierExport.FlatAppearance.BorderSize = 0;
            btnSupplierExport.Click += (s, e) =>
            {
                if (dgvSuppliers.DataSource is DataTable dt)
                    CsvExportHelper.ExportToCsv(dt, "Suppliers_Directory.csv");
            };

            pnlSearch.Controls.AddRange(new Control[] { txtSupplierSearch, btnSupplierSearch, btnSupplierExport });

            dgvSuppliers = new DataGridView { Dock = DockStyle.Fill };
            UITheme.StyleDataGrid(dgvSuppliers);
            dgvSuppliers.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    var row = dgvSuppliers.Rows[e.RowIndex];
                    if (row.Cells["SupplierID"].Value != DBNull.Value)
                    {
                        selectedSupplierId = Convert.ToInt32(row.Cells["SupplierID"].Value);
                        txtSupplierName.Text = row.Cells["Supplier Name"].Value?.ToString() ?? "";
                        txtSupplierContact.Text = row.Cells["Contact Person"].Value?.ToString() ?? "";
                        txtSupplierPhone.Text = row.Cells["Phone"].Value?.ToString() ?? "";
                        txtSupplierEmail.Text = row.Cells["Email"].Value?.ToString() ?? "";
                        txtSupplierAddress.Text = row.Cells["Address"].Value?.ToString() ?? "";
                    }
                }
            };

            pnlGridCard.Controls.Add(dgvSuppliers);
            pnlGridCard.Controls.Add(pnlSearch);

            // Right: Form Card
            Panel pnlFormCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.CardBg,
                Padding = new Padding(20),
                Margin = new Padding(16, 0, 0, 0)
            };

            lblSupplierFormTitle = new Label { Text = LocalizationManager.T("TitleSuppliers"), Font = UITheme.FontSubtitle, ForeColor = UITheme.TextMain, Dock = DockStyle.Top, Height = 35 };

            Panel pnlFields = new Panel { Dock = DockStyle.Top, Height = 310, BackColor = UITheme.CardBg };

            Label lblName = new Label { Text = LocalizationManager.T("Supplier") + ":", Location = new Point(0, 5), Font = UITheme.FontBold, AutoSize = true };
            txtSupplierName = new TextBox { Location = new Point(0, 25), Width = 260, BackColor = UITheme.InputBg };

            Label lblContact = new Label { Text = LocalizationManager.T("ContactPerson") + ":", Location = new Point(0, 65), Font = UITheme.FontBold, AutoSize = true };
            txtSupplierContact = new TextBox { Location = new Point(0, 85), Width = 260, BackColor = UITheme.InputBg };

            Label lblPhone = new Label { Text = LocalizationManager.T("Phone") + ":", Location = new Point(0, 125), Font = UITheme.FontBold, AutoSize = true };
            txtSupplierPhone = new TextBox { Location = new Point(0, 145), Width = 260, BackColor = UITheme.InputBg };

            Label lblEmail = new Label { Text = LocalizationManager.T("Email") + ":", Location = new Point(0, 185), Font = UITheme.FontBold, AutoSize = true };
            txtSupplierEmail = new TextBox { Location = new Point(0, 205), Width = 260, BackColor = UITheme.InputBg };

            Label lblAddr = new Label { Text = LocalizationManager.T("Address") + ":", Location = new Point(0, 245), Font = UITheme.FontBold, AutoSize = true };
            txtSupplierAddress = new TextBox { Location = new Point(0, 265), Width = 260, BackColor = UITheme.InputBg };

            pnlFields.Controls.AddRange(new Control[] { lblName, txtSupplierName, lblContact, txtSupplierContact, lblPhone, txtSupplierPhone, lblEmail, txtSupplierEmail, lblAddr, txtSupplierAddress });

            Panel pnlBtnGroup = new Panel { Dock = DockStyle.Top, Height = 170, BackColor = UITheme.CardBg };

            btnSupplierAdd = new Button { Text = LocalizationManager.T("AddSupplier"), Location = new Point(0, 5), Size = new Size(260, 36), FlatStyle = FlatStyle.Flat, BackColor = UITheme.Primary, ForeColor = Color.White, Font = UITheme.FontBold, Cursor = Cursors.Hand };
            btnSupplierAdd.FlatAppearance.BorderSize = 0;
            btnSupplierAdd.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtSupplierName.Text))
                {
                    MessageBox.Show("Please enter supplier name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var sup = new Models.SupplierModel
                {
                    SupplierName = txtSupplierName.Text.Trim(),
                    ContactPerson = txtSupplierContact.Text.Trim(),
                    Phone = txtSupplierPhone.Text.Trim(),
                    Email = txtSupplierEmail.Text.Trim(),
                    Address = txtSupplierAddress.Text.Trim()
                };
                var repo = new Data.SupplierRepository();
                if (repo.Add(sup, out string err))
                {
                    MessageBox.Show("Supplier added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearSupplierForm();
                    LoadSuppliersData();
                    PopulateStockFormDropdowns();
                }
                else
                {
                    MessageBox.Show(err, "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            btnSupplierUpdate = new Button { Text = LocalizationManager.T("UpdateSupplier"), Location = new Point(0, 46), Size = new Size(260, 36), FlatStyle = FlatStyle.Flat, BackColor = UITheme.PrimaryLight, ForeColor = UITheme.Primary, Font = UITheme.FontBold, Cursor = Cursors.Hand };
            btnSupplierUpdate.FlatAppearance.BorderSize = 0;
            btnSupplierUpdate.Click += (s, e) =>
            {
                if (selectedSupplierId <= 0)
                {
                    MessageBox.Show("Please select a supplier from the table first.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var sup = new Models.SupplierModel
                {
                    SupplierId = selectedSupplierId,
                    SupplierName = txtSupplierName.Text.Trim(),
                    ContactPerson = txtSupplierContact.Text.Trim(),
                    Phone = txtSupplierPhone.Text.Trim(),
                    Email = txtSupplierEmail.Text.Trim(),
                    Address = txtSupplierAddress.Text.Trim()
                };
                var repo = new Data.SupplierRepository();
                if (repo.Update(sup, out string err))
                {
                    MessageBox.Show("Supplier updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadSuppliersData();
                    PopulateStockFormDropdowns();
                }
                else
                {
                    MessageBox.Show(err, "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            btnSupplierDelete = new Button { Text = LocalizationManager.T("DeleteSupplier"), Location = new Point(0, 87), Size = new Size(260, 36), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(254, 242, 242), ForeColor = UITheme.Danger, Font = UITheme.FontBold, Cursor = Cursors.Hand };
            btnSupplierDelete.FlatAppearance.BorderSize = 0;
            btnSupplierDelete.Click += (s, e) =>
            {
                if (selectedSupplierId <= 0) return;
                if (MessageBox.Show("Are you sure you want to delete this supplier?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    var repo = new Data.SupplierRepository();
                    if (repo.Delete(selectedSupplierId, out string err))
                    {
                        ClearSupplierForm();
                        LoadSuppliersData();
                        PopulateStockFormDropdowns();
                    }
                }
            };

            btnSupplierClear = new Button { Text = "🔄 Clear", Location = new Point(0, 128), Size = new Size(260, 32), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(241, 245, 249), ForeColor = UITheme.TextMuted, Font = UITheme.FontBold, Cursor = Cursors.Hand };
            btnSupplierClear.FlatAppearance.BorderSize = 0;
            btnSupplierClear.Click += (s, e) => ClearSupplierForm();

            pnlBtnGroup.Controls.AddRange(new Control[] { btnSupplierAdd, btnSupplierUpdate, btnSupplierDelete, btnSupplierClear });

            pnlFormCard.Controls.AddRange(new Control[] { pnlBtnGroup, pnlFields, lblSupplierFormTitle });

            tlp.Controls.Add(pnlGridCard, 0, 0);
            tlp.Controls.Add(pnlFormCard, 1, 0);

            pnlViewSuppliers.Controls.Add(tlp);
            pnlContentContainer.Controls.Add(pnlViewSuppliers);
        }

        private void ClearSupplierForm()
        {
            selectedSupplierId = -1;
            txtSupplierName?.Clear();
            txtSupplierContact?.Clear();
            txtSupplierPhone?.Clear();
            txtSupplierEmail?.Clear();
            txtSupplierAddress?.Clear();
        }

        private void LoadSuppliersData(string filter = "")
        {
            DataTable dt = new DataTable();
            try
            {
                using (var conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string q = @"SELECT SupplierID, SupplierName AS [Supplier Name], 
                                        ContactPerson AS [Contact Person], Phone, Email, Address, 
                                        DateAdded AS [Date Registered]
                                 FROM Suppliers 
                                 WHERE IsActive = 1";
                    if (!string.IsNullOrEmpty(filter))
                    {
                        q += " AND (SupplierName LIKE @Filter OR ContactPerson LIKE @Filter OR Phone LIKE @Filter)";
                    }
                    q += " ORDER BY SupplierName ASC";
                    using (var cmd = new System.Data.SqlClient.SqlCommand(q, conn))
                    {
                        if (!string.IsNullOrEmpty(filter)) cmd.Parameters.AddWithValue("@Filter", $"%{filter}%");
                        using (var da = new System.Data.SqlClient.SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }
                dgvSuppliers.DataSource = dt;
                LocalizeGridHeaders(dgvSuppliers);
            }
            catch (Exception ex)
            {
                Logger.LogError("Error loading suppliers", ex);
            }
        }
        #endregion

        #region View 5: Dashboard Analytics & Operational Hub
        private void BuildDashboardView()
        {
            pnlViewDashboard = new Panel { Dock = DockStyle.Fill, Visible = false, AutoScroll = true };

            // Top: 4 Stat Cards
            TableLayoutPanel tlpKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 100,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = UITheme.AppBg,
                Margin = new Padding(0, 0, 0, 16)
            };
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            lblKpiTodaySales = new Label();
            lblKpiTodayOrders = new Label();
            lblKpiActiveProducts = new Label();
            lblKpiLowStock = new Label();

            lblKpiTitle1 = new Label();
            lblKpiTitle2 = new Label();
            lblKpiTitle3 = new Label();
            lblKpiTitle4 = new Label();

            // Card 1: Vibrant Coral Highlight Card
            Panel pnlSalesCoralCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.Primary,
                Margin = new Padding(0, 0, 10, 8),
                Padding = new Padding(16, 12, 16, 12)
            };
            lblKpiTitle1.Text = LocalizationManager.T("KpiTodaySales");
            lblKpiTitle1.Font = UITheme.FontRegular;
            lblKpiTitle1.ForeColor = Color.White;
            lblKpiTitle1.Dock = DockStyle.Top;
            lblKpiTitle1.Height = 22;

            lblKpiTodaySales.Text = LocalizationManager.FormatMoney(0m);
            lblKpiTodaySales.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblKpiTodaySales.ForeColor = Color.White;
            lblKpiTodaySales.Dock = DockStyle.Fill;

            pnlSalesCoralCard.Controls.Add(lblKpiTodaySales);
            pnlSalesCoralCard.Controls.Add(lblKpiTitle1);

            tlpKpis.Controls.Add(pnlSalesCoralCard, 0, 0);
            tlpKpis.Controls.Add(CreateKpiCard(LocalizationManager.T("KpiTodayOrders"), "0", lblKpiTodayOrders, lblKpiTitle2, UITheme.TextMain), 1, 0);
            tlpKpis.Controls.Add(CreateKpiCard(LocalizationManager.T("KpiActiveProducts"), "0", lblKpiActiveProducts, lblKpiTitle3, Color.FromArgb(79, 70, 229)), 2, 0);
            tlpKpis.Controls.Add(CreateKpiCard(LocalizationManager.T("KpiLowStock"), "0", lblKpiLowStock, lblKpiTitle4, UITheme.Danger), 3, 0);

            pnlViewDashboard.Controls.Add(tlpKpis);

            // Middle: Interactive Dashboard Sub-navigation Tabs (Pill style)
            Panel pnlDashTabBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = UITheme.AppBg,
                Padding = new Padding(0, 6, 0, 6)
            };

            btnDashTabCharts = CreateDashTabButton(LocalizationManager.T("TabCharts"), "charts");
            btnDashTabRestock = CreateDashTabButton(LocalizationManager.T("TabRestockAlerts"), "restock");
            btnDashTabOrders = CreateDashTabButton(LocalizationManager.T("TabRecentOrders"), "orders");
            btnDashTabExpiry = CreateDashTabButton(LocalizationManager.T("TabExpiryAlerts"), "expiry");
            btnDashTabShifts = CreateDashTabButton(LocalizationManager.T("TabCashierShifts"), "shifts");

            pnlDashTabBar.Controls.Add(btnDashTabShifts);
            pnlDashTabBar.Controls.Add(btnDashTabExpiry);
            pnlDashTabBar.Controls.Add(btnDashTabOrders);
            pnlDashTabBar.Controls.Add(btnDashTabRestock);
            pnlDashTabBar.Controls.Add(btnDashTabCharts);

            pnlViewDashboard.Controls.Add(pnlDashTabBar);

            // Bottom: Tab Content Container
            pnlDashTabContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.AppBg,
                Padding = new Padding(0, 10, 0, 0)
            };

            // 1. Charts Tab
            pnlDashCharts = new Panel { Dock = DockStyle.Fill, Visible = true, BackColor = UITheme.AppBg };
            TableLayoutPanel tlpCharts = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            tlpCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            chartDailySales = new WinChart { Dock = DockStyle.Fill, BackColor = UITheme.CardBg };
            chartDailySales.ChartAreas.Add(new ChartArea("MainArea") { BackColor = UITheme.CardBg });
            
            chartTopSold = new WinChart { Dock = DockStyle.Fill, BackColor = UITheme.CardBg };
            chartTopSold.ChartAreas.Add(new ChartArea("MainArea") { BackColor = UITheme.CardBg });

            tlpCharts.Controls.Add(chartDailySales, 0, 0);
            tlpCharts.Controls.Add(chartTopSold, 1, 0);
            pnlDashCharts.Controls.Add(tlpCharts);

            // 2. Restock Alerts Tab
            pnlDashRestock = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = UITheme.CardBg, Padding = new Padding(16) };
            Panel pnlRestockTop = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = UITheme.CardBg };
            Label lblRestockHead = new Label { Text = "Products Needing Immediate Restocking (0 Remaining or Low Stock)", Font = UITheme.FontBold, ForeColor = UITheme.Danger, Location = new Point(0, 10), AutoSize = true };
            Button btnFastRestock = new Button
            {
                Text = "➕ " + LocalizationManager.T("RestockNow"),
                Size = new Size(130, 30),
                Location = new Point(pnlDashRestock.Width - 145, 5),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.Primary,
                ForeColor = Color.White,
                Font = UITheme.FontBold,
                Cursor = Cursors.Hand
            };
            btnFastRestock.FlatAppearance.BorderSize = 0;
            btnFastRestock.Click += (s, e) => TriggerFastRestockFromGrid();
            pnlRestockTop.Controls.Add(lblRestockHead);
            pnlRestockTop.Controls.Add(btnFastRestock);

            dgvDashRestock = new DataGridView { Dock = DockStyle.Fill };
            UITheme.StyleDataGrid(dgvDashRestock);
            pnlDashRestock.Controls.Add(dgvDashRestock);
            pnlDashRestock.Controls.Add(pnlRestockTop);

            // 3. Recent Orders Tab
            pnlDashOrders = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = UITheme.CardBg, Padding = new Padding(16) };
            Panel pnlOrdersTop = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = UITheme.CardBg };
            Label lblOrdersHead = new Label { Text = "Recently Completed Customer Transactions", Font = UITheme.FontBold, ForeColor = UITheme.TextMain, Location = new Point(0, 10), AutoSize = true };
            pnlOrdersTop.Controls.Add(lblOrdersHead);

            dgvDashOrders = new DataGridView { Dock = DockStyle.Fill };
            UITheme.StyleDataGrid(dgvDashOrders);
            pnlDashOrders.Controls.Add(dgvDashOrders);
            pnlDashOrders.Controls.Add(pnlOrdersTop);

            // 4. Expiring Batches Tab
            pnlDashExpiry = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = UITheme.CardBg, Padding = new Padding(16) };
            Panel pnlExpiryTop = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = UITheme.CardBg };
            Label lblExpiryHead = new Label { Text = "Batches Expired or Expiring Within 14 Days", Font = UITheme.FontBold, ForeColor = UITheme.Danger, Location = new Point(0, 10), AutoSize = true };
            pnlExpiryTop.Controls.Add(lblExpiryHead);

            dgvDashExpiry = new DataGridView { Dock = DockStyle.Fill };
            UITheme.StyleDataGrid(dgvDashExpiry);
            pnlDashExpiry.Controls.Add(dgvDashExpiry);
            pnlDashExpiry.Controls.Add(pnlExpiryTop);

            // 5. Staff Shifts Tab
            pnlDashShifts = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = UITheme.CardBg, Padding = new Padding(16) };
            Panel pnlShiftsTop = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = UITheme.CardBg };
            Label lblShiftsHead = new Label { Text = "Cashier Staff Shift Log & Sales Accountability", Font = UITheme.FontBold, ForeColor = UITheme.TextMain, Location = new Point(0, 10), AutoSize = true };
            pnlShiftsTop.Controls.Add(lblShiftsHead);

            dgvDashShifts = new DataGridView { Dock = DockStyle.Fill };
            UITheme.StyleDataGrid(dgvDashShifts);
            pnlDashShifts.Controls.Add(dgvDashShifts);
            pnlDashShifts.Controls.Add(pnlShiftsTop);

            pnlDashTabContent.Controls.Add(pnlDashShifts);
            pnlDashTabContent.Controls.Add(pnlDashExpiry);
            pnlDashTabContent.Controls.Add(pnlDashOrders);
            pnlDashTabContent.Controls.Add(pnlDashRestock);
            pnlDashTabContent.Controls.Add(pnlDashCharts);

            pnlViewDashboard.Controls.Add(pnlDashTabContent);
            pnlContentContainer.Controls.Add(pnlViewDashboard);

            // Select charts tab initially
            SwitchDashTab("charts");
        }

        private Button CreateDashTabButton(string text, string tabKey)
        {
            Button btn = new Button
            {
                Text = text,
                Dock = DockStyle.Left,
                Width = 145,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = UITheme.SidebarText,
                Font = UITheme.FontRegular,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => SwitchDashTab(tabKey);
            dashTabButtons.Add(btn);
            return btn;
        }

        private void SwitchDashTab(string tabKey)
        {
            pnlDashCharts.Visible = (tabKey == "charts");
            pnlDashRestock.Visible = (tabKey == "restock");
            pnlDashOrders.Visible = (tabKey == "orders");
            pnlDashExpiry.Visible = (tabKey == "expiry");
            pnlDashShifts.Visible = (tabKey == "shifts");

            foreach (var b in dashTabButtons)
            {
                b.BackColor = Color.White;
                b.ForeColor = UITheme.SidebarText;
                b.Font = UITheme.FontRegular;
            }

            Button active = null;
            if (tabKey == "charts") active = btnDashTabCharts;
            else if (tabKey == "restock") active = btnDashTabRestock;
            else if (tabKey == "orders") active = btnDashTabOrders;
            else if (tabKey == "expiry") active = btnDashTabExpiry;
            else if (tabKey == "shifts") active = btnDashTabShifts;

            if (active != null)
            {
                active.BackColor = UITheme.PrimaryLight;
                active.ForeColor = UITheme.Primary;
                active.Font = UITheme.FontBold;
            }
        }

        private void TriggerFastRestockFromGrid()
        {
            if (dgvDashRestock?.CurrentRow != null && dgvDashRestock.CurrentRow.Index >= 0)
            {
                var row = dgvDashRestock.CurrentRow;
                long barcode = Convert.ToInt64(row.Cells["Barcode"].Value);
                string prodName = row.Cells["Product Name"].Value.ToString();

                string input = PromptInput($"Enter units to restock for '{prodName}':", "Restock Product", "50");
                if (!string.IsNullOrEmpty(input) && int.TryParse(input, out int addQty) && addQty > 0)
                {
                    var inv = new Services.InventoryService();
                    if (inv.RecordRestock(barcode, addQty, $"RESTOCK-{DateTime.Now:yyyyMMdd}", Session.Username, out string err))
                    {
                        MessageBox.Show($"Successfully restocked +{addQty} units for '{prodName}'!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadDashboardData();
                        LoadStockData();
                        LoadProductsData();
                    }
                    else
                    {
                        MessageBox.Show(err, "Restock Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a product from the table first.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private Panel CreateKpiCard(string title, string initialValue, Label valueLabel, Label titleLabel, Color accent)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.CardBg,
                Margin = new Padding(0, 0, 10, 8),
                Padding = new Padding(16, 12, 16, 12)
            };
            card.Paint += (s, e) =>
            {
                using (Pen p = new Pen(UITheme.CardBorder, 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
                }
            };

            titleLabel.Text = title;
            titleLabel.Font = UITheme.FontRegular;
            titleLabel.ForeColor = UITheme.TextMuted;
            titleLabel.Dock = DockStyle.Top;
            titleLabel.Height = 22;

            valueLabel.Text = initialValue;
            valueLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            valueLabel.ForeColor = accent;
            valueLabel.Dock = DockStyle.Fill;

            card.Controls.Add(valueLabel);
            card.Controls.Add(titleLabel);
            return card;
        }

        private void LoadDashboardData()
        {
            var kpis = Report.GetDashboardKpis();
            lblKpiTodaySales.Text = LocalizationManager.FormatMoney(kpis.TodaySales);
            lblKpiTodayOrders.Text = kpis.TodayOrders.ToString();
            lblKpiActiveProducts.Text = kpis.TotalProducts.ToString();
            lblKpiLowStock.Text = kpis.LowStockItems.ToString();

            // 1. Populate Charts
            try
            {
                Chart chartLogic = new Chart();
                var dailySales = chartLogic.GetDailySales();
                chartDailySales.Series.Clear();
                var s1 = chartDailySales.Series.Add("Daily Sales");
                s1.ChartType = SeriesChartType.Column;
                s1.Color = UITheme.Primary;
                foreach (var kvp in dailySales)
                {
                    s1.Points.AddXY(kvp.Key.ToString("MM-dd"), kvp.Value);
                }

                var topSold = chartLogic.GetTop7SoldProducts();
                chartTopSold.Series.Clear();
                var s2 = chartTopSold.Series.Add("Top Sold");
                s2.ChartType = SeriesChartType.Bar;
                s2.Color = UITheme.Success;
                foreach (var kvp in topSold)
                {
                    s2.Points.AddXY(kvp.Key, kvp.Value);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error rendering dashboard charts", ex);
            }

            // 2. Populate Restock Alerts Tab
            try
            {
                DataTable dtRestock = new DataTable();
                using (var conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string q = @"
                    SELECT 
                        p.Barcode, 
                        p.PName AS [Product Name], 
                        ISNULL(p.Category, 'Uncategorized') AS [Category],
                        ISNULL(SUM(s.quantity), 0) AS [Available Qty],
                        ISNULL(MAX(s.minimum_stock), 5) AS [Min Stock],
                        ISNULL(MAX(s.sp_unit), 0.00) AS [Price],
                        CASE WHEN ISNULL(SUM(s.quantity), 0) = 0 THEN 'OUT OF STOCK' ELSE 'LOW STOCK' END AS [Status]
                    FROM Products p
                    LEFT JOIN Stocks s ON p.ID = s.barcode_id AND s.IsActive = 1
                    WHERE p.IsActive = 1
                    GROUP BY p.Barcode, p.PName, p.Category
                    HAVING ISNULL(SUM(s.quantity), 0) <= ISNULL(MAX(s.minimum_stock), 5)
                    ORDER BY [Available Qty] ASC, p.PName ASC";
                    using (var da = new System.Data.SqlClient.SqlDataAdapter(q, conn))
                    {
                        da.Fill(dtRestock);
                    }
                }
                dgvDashRestock.DataSource = dtRestock;
                LocalizeGridHeaders(dgvDashRestock);
            }
            catch (Exception ex) { Logger.LogError("Error loading restock tab", ex); }

            // 3. Populate Recent Orders Tab
            try
            {
                DataTable dtOrders = new DataTable();
                using (var conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string q = @"
                    SELECT TOP 15
                        o.OrderID AS [Order #],
                        CONVERT(VARCHAR(19), o.OrderTime, 120) AS [Date & Time],
                        ISNULL(o.CashierName, 'Cashier') AS [Cashier],
                        o.TotalPayable AS [Total Amount (ETB)],
                        o.PaymentMethod AS [Payment Method]
                    FROM Orders o
                    ORDER BY o.OrderID DESC";
                    using (var da = new System.Data.SqlClient.SqlDataAdapter(q, conn))
                    {
                        da.Fill(dtOrders);
                    }
                }
                dgvDashOrders.DataSource = dtOrders;
                LocalizeGridHeaders(dgvDashOrders);
            }
            catch (Exception ex) { Logger.LogError("Error loading recent orders tab", ex); }

            // 4. Populate Expiring Batches Tab
            try
            {
                DataTable dtExpiry = new DataTable();
                using (var conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string q = @"
                    SELECT TOP 20
                        s.stock_code AS [Batch Code],
                        p.PName AS [Product Name],
                        p.Barcode,
                        s.quantity AS [Quantity],
                        CONVERT(VARCHAR(10), s.expiry_date, 120) AS [Expiry Date],
                        DATEDIFF(day, GETDATE(), s.expiry_date) AS [Days Left],
                        CASE WHEN s.expiry_date <= CAST(GETDATE() AS DATE) THEN 'EXPIRED' ELSE 'EXPIRING SOON' END AS [Status]
                    FROM Stocks s
                    INNER JOIN Products p ON s.barcode_id = p.ID
                    WHERE s.IsActive = 1 AND p.IsActive = 1 AND s.expiry_date IS NOT NULL 
                      AND s.expiry_date <= DATEADD(day, 14, CAST(GETDATE() AS DATE))
                    ORDER BY s.expiry_date ASC";
                    using (var da = new System.Data.SqlClient.SqlDataAdapter(q, conn))
                    {
                        da.Fill(dtExpiry);
                    }
                }
                dgvDashExpiry.DataSource = dtExpiry;
                LocalizeGridHeaders(dgvDashExpiry);
            }
            catch (Exception ex) { Logger.LogError("Error loading expiry tab", ex); }

            // 5. Populate Shifts Tab
            try
            {
                DataTable dtShifts = new DataTable();
                using (var conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string q = @"
                    SELECT TOP 20
                        ShiftID AS [Shift #],
                        Username AS [Cashier],
                        Role,
                        CONVERT(VARCHAR(19), ShiftStart, 120) AS [Shift Started],
                        ISNULL(CONVERT(VARCHAR(19), ShiftEnd, 120), 'In Progress') AS [Shift Ended],
                        TotalSalesCount AS [Orders Processed],
                        TotalSalesAmount AS [Revenue (ETB)],
                        Status
                    FROM StaffShifts
                    ORDER BY ShiftID DESC";
                    using (var da = new System.Data.SqlClient.SqlDataAdapter(q, conn))
                    {
                        da.Fill(dtShifts);
                    }
                }
                dgvDashShifts.DataSource = dtShifts;
                LocalizeGridHeaders(dgvDashShifts);
            }
            catch (Exception ex) { Logger.LogError("Error loading shifts tab", ex); }
        }
        #endregion

        #region View 6: Sales Reports
        private void BuildReportsView()
        {
            pnlViewReports = new Panel { Dock = DockStyle.Fill, Visible = false };

            Panel pnlCard = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.CardBg, Padding = new Padding(20) };

            Panel pnlFilter = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = UITheme.CardBg };
            lblReportPick = new Label { Text = LocalizationManager.T("SelectDate"), Location = new Point(0, 15), Font = UITheme.FontBold, AutoSize = true };
            dtpReportDate = new DateTimePicker { Location = new Point(110, 12), Width = 140, Format = DateTimePickerFormat.Short };

            btnReportFilter = new Button
            {
                Text = LocalizationManager.T("GenerateReport"),
                Location = new Point(265, 10),
                Size = new Size(140, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.Primary,
                ForeColor = Color.White,
                Font = UITheme.FontSmall,
                Cursor = Cursors.Hand
            };
            btnReportFilter.FlatAppearance.BorderSize = 0;
            btnReportFilter.Click += (s, e) =>
            {
                Report r = new Report { Date = dtpReportDate.Value };
                dgvReports.DataSource = Report.ViewOrdersByDate();
                LocalizeGridHeaders(dgvReports);
            };

            btnReportExport = new Button
            {
                Text = LocalizationManager.T("ExportCsv"),
                Location = new Point(415, 10),
                Size = new Size(110, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.PrimaryLight,
                ForeColor = UITheme.Primary,
                Font = UITheme.FontSmall,
                Cursor = Cursors.Hand
            };
            btnReportExport.FlatAppearance.BorderSize = 0;
            btnReportExport.Click += (s, e) =>
            {
                if (dgvReports.DataSource is DataTable dt)
                    CsvExportHelper.ExportToCsv(dt, $"SalesReport_{dtpReportDate.Value:yyyyMMdd}.csv");
            };

            pnlFilter.Controls.AddRange(new Control[] { lblReportPick, dtpReportDate, btnReportFilter, btnReportExport });

            dgvReports = new DataGridView { Dock = DockStyle.Fill };
            UITheme.StyleDataGrid(dgvReports);
            dgvReports.CellFormatting += (s, e) =>
            {
                if ((dgvReports.Columns[e.ColumnIndex].Name == "Total Payable" || 
                     dgvReports.Columns[e.ColumnIndex].Name == "Cash Received" || 
                     dgvReports.Columns[e.ColumnIndex].Name == "Change Returned" || 
                     dgvReports.Columns[e.ColumnIndex].Name == "Total Amount") && e.Value != null)
                {
                    if (decimal.TryParse(e.Value.ToString(), out decimal val))
                    {
                        e.Value = LocalizationManager.FormatMoney(val);
                    }
                }
            };

            pnlCard.Controls.Add(dgvReports);
            pnlCard.Controls.Add(pnlFilter);

            pnlViewReports.Controls.Add(pnlCard);
            pnlContentContainer.Controls.Add(pnlViewReports);
        }

        private void LoadReportsData()
        {
            Report r = new Report { Date = dtpReportDate.Value };
            dgvReports.DataSource = Report.ViewOrdersByDate();
            LocalizeGridHeaders(dgvReports);
        }
        #endregion

        #region View 7: Alerts & Notifications
        private void BuildAlertsView()
        {
            pnlViewAlerts = new Panel { Dock = DockStyle.Fill, Visible = false };

            TableLayoutPanel tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));

            rtbLowStock = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(255, 245, 242) };
            rtbExpired = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(254, 242, 242) };
            rtbNeverSold = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(248, 250, 252) };

            lblAlertLowStock = new Label();
            lblAlertExpired = new Label();
            lblAlertNeverSold = new Label();

            tlp.Controls.Add(CreateAlertCard(LocalizationManager.T("LowStockAlerts"), rtbLowStock, lblAlertLowStock), 0, 0);
            tlp.Controls.Add(CreateAlertCard(LocalizationManager.T("ExpiredBatches"), rtbExpired, lblAlertExpired), 1, 0);
            tlp.Controls.Add(CreateAlertCard(LocalizationManager.T("NeverSoldItems"), rtbNeverSold, lblAlertNeverSold), 2, 0);

            pnlViewAlerts.Controls.Add(tlp);
            pnlContentContainer.Controls.Add(pnlViewAlerts);
        }

        private Panel CreateAlertCard(string title, RichTextBox rtb, Label titleLabel)
        {
            Panel card = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.CardBg, Padding = new Padding(16), Margin = new Padding(8) };
            card.Paint += (s, e) =>
            {
                using (Pen p = new Pen(UITheme.CardBorder, 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
                }
            };
            titleLabel.Text = title;
            titleLabel.Font = UITheme.FontBold;
            titleLabel.ForeColor = UITheme.TextMain;
            titleLabel.Dock = DockStyle.Top;
            titleLabel.Height = 30;
            card.Controls.Add(rtb);
            card.Controls.Add(titleLabel);
            return card;
        }

        private void LoadAlertsData()
        {
            rtbLowStock.Clear();
            rtbExpired.Clear();
            rtbNeverSold.Clear();
            Notification.ShowLowStockAlerts(rtbLowStock);
            Notification.ShowExpiredProductAlerts(rtbExpired);
            Notification.ShowNeverSoldItems(rtbNeverSold);
        }
        #endregion
    }
}
