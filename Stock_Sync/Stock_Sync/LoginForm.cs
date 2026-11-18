using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Stock_Sync
{
    public partial class LoginForm : Form
    {
        private Button btnLangToggle;
        private Label lblCardBadge;
        private Label lblEmailTag;
        private Label lblPasswordTag;
        private Label lblSubtitle;

        public LoginForm()
        {
            InitializeComponent();
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Paint Warm Vibrant Orange Gradient (matching Mockup 2)
            using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                this.ClientRectangle,
                UITheme.LoginBg,
                UITheme.LoginBgDark,
                LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(bgBrush, this.ClientRectangle);
            }

            // Paint Soft Ambient Spheres
            UITheme.PaintAmbientSpheres(e.Graphics, this.ClientRectangle);
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            this.Size = new Size(880, 720);
            this.MinimumSize = new Size(800, 680);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Hide old dark top banner and old external label
            if (this.panel1 != null) this.panel1.Visible = false;
            if (this.label5 != null) this.label5.Visible = false;

            // Setup Language Toggle (Top Right of Form)
            btnLangToggle = new Button
            {
                Text = LocalizationManager.CurrentLanguage == AppLanguage.English ? "🌐 አማርኛ" : "🌐 English",
                Size = new Size(125, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = UITheme.Primary,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLangToggle.FlatAppearance.BorderSize = 0;
            btnLangToggle.Location = new Point(this.ClientSize.Width - 145, 18);
            btnLangToggle.Click += (s, ev) =>
            {
                LocalizationManager.ToggleLanguage();
                btnLangToggle.Text = LocalizationManager.CurrentLanguage == AppLanguage.English ? "🌐 አማርኛ" : "🌐 English";
                ApplyLocalization();
            };
            this.Controls.Add(btnLangToggle);
            btnLangToggle.BringToFront();

            // Style Center White Elevated Card (groupBox1)
            this.groupBox1.Text = "";
            this.groupBox1.BackColor = Color.White;
            this.groupBox1.Size = new Size(540, 520);
            CenterCard();

            // Custom Paint on Card for rounded shadow edge
            this.groupBox1.Paint += (s, pe) =>
            {
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, groupBox1.Width - 1, groupBox1.Height - 1);
                using (Pen borderPen = new Pen(Color.FromArgb(220, 255, 255, 255), 1.5f))
                {
                    pe.Graphics.DrawRectangle(borderPen, rect);
                }
            };

            // Remove/hide old accent indicator bars & icon images
            if (this.panel2 != null) this.panel2.Visible = false;
            if (this.panel3 != null) this.panel3.Visible = false;
            if (this.pictureBox2 != null) this.pictureBox2.Visible = false;
            if (this.pictureBox3 != null) this.pictureBox3.Visible = false;

            // 1. Top Orange Circular Icon Badge [📦]
            lblCardBadge = new Label
            {
                Size = new Size(58, 58),
                Location = new Point((groupBox1.Width - 58) / 2, 24),
                BackColor = UITheme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                Text = "📦",
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblCardBadge.Paint += (s, pe) =>
            {
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddEllipse(0, 0, lblCardBadge.Width - 1, lblCardBadge.Height - 1);
                    lblCardBadge.Region = new Region(path);
                }
            };
            groupBox1.Controls.Add(lblCardBadge);

            // 2. Title & Subtitle inside Card
            this.label6.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            this.label6.ForeColor = UITheme.TextMain;
            this.label6.TextAlign = ContentAlignment.MiddleCenter;
            this.label6.Size = new Size(groupBox1.Width - 40, 30);
            this.label6.Location = new Point(20, 90);

            lblSubtitle = new Label
            {
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(groupBox1.Width - 40, 22),
                Location = new Point(20, 122),
                Text = "Track . Manage . Grow — Supermarket Inventory"
            };
            groupBox1.Controls.Add(lblSubtitle);

            // 3. Email Input & Label
            lblEmailTag = new Label
            {
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(40, 155),
                Size = new Size(460, 20),
                Text = "Email Address / መለያ ኢሜይል"
            };
            groupBox1.Controls.Add(lblEmailTag);

            this.email.Location = new Point(40, 178);
            this.email.Size = new Size(460, 38);
            this.email.Font = new Font("Segoe UI", 11.5F);
            this.email.BackColor = UITheme.InputBg;
            this.email.BorderStyle = BorderStyle.FixedSingle;
            this.email.ForeColor = UITheme.TextMain;
            this.email.UseSystemPasswordChar = false;

            // 4. Password Input & Label
            lblPasswordTag = new Label
            {
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(40, 226),
                Size = new Size(460, 20),
                Text = "Password / የይለፍ ቃል"
            };
            groupBox1.Controls.Add(lblPasswordTag);

            this.password.Location = new Point(40, 248);
            this.password.Size = new Size(460, 38);
            this.password.Font = new Font("Segoe UI", 11.5F);
            this.password.BackColor = UITheme.InputBg;
            this.password.BorderStyle = BorderStyle.FixedSingle;
            this.password.ForeColor = UITheme.TextMain;
            this.password.UseSystemPasswordChar = true;

            // 5. Admin Sign In Button (Vibrant Coral Orange Pill - Mockup 2)
            this.button2.Location = new Point(40, 310);
            this.button2.Size = new Size(460, 44);
            this.button2.FlatStyle = FlatStyle.Flat;
            this.button2.FlatAppearance.BorderSize = 0;
            this.button2.BackColor = UITheme.Primary;
            this.button2.ForeColor = Color.White;
            this.button2.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            this.button2.Cursor = Cursors.Hand;

            // 6. Separator ("- OR -")
            this.label8.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.label8.ForeColor = UITheme.TextLight;
            this.label8.TextAlign = ContentAlignment.MiddleCenter;
            this.label8.Size = new Size(120, 20);
            this.label8.Location = new Point((groupBox1.Width - 120) / 2, 366);

            // 7. Staff Sign In Button (Soft Peach / Outline Pill)
            this.button1.Location = new Point(40, 396);
            this.button1.Size = new Size(460, 44);
            this.button1.FlatStyle = FlatStyle.Flat;
            this.button1.FlatAppearance.BorderSize = 0;
            this.button1.BackColor = UITheme.PrimaryLight;
            this.button1.ForeColor = UITheme.Primary;
            this.button1.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            this.button1.Cursor = Cursors.Hand;

            this.Resize += (s, re) =>
            {
                CenterCard();
                if (btnLangToggle != null)
                {
                    btnLangToggle.Location = new Point(this.ClientSize.Width - 145, 18);
                }
                this.Invalidate();
            };

            ApplyLocalization();
        }

        private void CenterCard()
        {
            if (this.groupBox1 != null)
            {
                int x = (this.ClientSize.Width - this.groupBox1.Width) / 2;
                int y = (this.ClientSize.Height - this.groupBox1.Height) / 2 + 10;
                this.groupBox1.Location = new Point(Math.Max(20, x), Math.Max(20, y));
            }
        }

        private void ApplyLocalization()
        {
            bool isEng = LocalizationManager.CurrentLanguage == AppLanguage.English;
            this.Text = isEng ? "Stock Sync — Modern Inventory & POS" : "ስቶክ ሲንክ — ዘመናዊ የክምችት እና ሽያጭ ሲስተም";
            this.label6.Text = LocalizationManager.T("SignInTitle");
            this.button2.Text = LocalizationManager.T("SignInAdmin");
            this.button1.Text = LocalizationManager.T("SignInStaff");
            this.label8.Text = isEng ? "—  OR  —" : "—  ወይም  —";
            
            if (lblSubtitle != null)
            {
                lblSubtitle.Text = isEng 
                    ? "Track . Manage . Grow — Supermarket System" 
                    : "ይከታተሉ . ያስተዳድሩ . ያሳድጉ — የሱፐርማርኬት ሲስተም";
            }

            if (lblEmailTag != null)
            {
                lblEmailTag.Text = isEng ? "Email Address" : "የኢሜይል አድራሻ";
            }

            if (lblPasswordTag != null)
            {
                lblPasswordTag.Text = isEng ? "Password" : "የይለፍ ቃል";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string emailInput = email.Text.Trim();
            string passwordInput = password.Text.Trim();

            if (string.IsNullOrEmpty(emailInput) || string.IsNullOrEmpty(passwordInput))
            {
                string msg = LocalizationManager.CurrentLanguage == AppLanguage.English 
                    ? "Please enter both email and password." 
                    : "እባክዎ ኢሜይል እና የይለፍ ቃል ያስገቡ።";
                MessageBox.Show(msg, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var user = Login.Authenticate(emailInput, passwordInput);

            if (user != null && user.Role == "Staff")
            {
                Session.StartSession(user.UserId, user.Email, user.Role);
                MainDashboardForm dashboard = new MainDashboardForm();
                this.Hide();
                dashboard.Show();
            }
            else
            {
                string errorMsg = LocalizationManager.CurrentLanguage == AppLanguage.English
                    ? "Access denied! Invalid credentials or not a Staff account."
                    : "መግባት አልተፈቀደም! የተሳሳተ መለያ ወይም የሠራተኛ አካውንት አይደለም።";
                MessageBox.Show(errorMsg, "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                password.Clear();
                password.Focus();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string emailInput = email.Text.Trim();
            string passwordInput = password.Text.Trim();

            if (string.IsNullOrEmpty(emailInput) || string.IsNullOrEmpty(passwordInput))
            {
                string msg = LocalizationManager.CurrentLanguage == AppLanguage.English 
                    ? "Please enter both email and password." 
                    : "እባክዎ ኢሜይል እና የይለፍ ቃል ያስገቡ።";
                MessageBox.Show(msg, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var user = Login.Authenticate(emailInput, passwordInput);

            if (user != null && user.Role == "Admin")
            {
                Session.StartSession(user.UserId, user.Email, user.Role);
                MainDashboardForm dashboard = new MainDashboardForm();
                this.Hide();
                dashboard.Show();
            }
            else
            {
                string errorMsg = LocalizationManager.CurrentLanguage == AppLanguage.English
                    ? "Access denied! Invalid credentials or not an Admin account."
                    : "መግባት አልተፈቀደም! የተሳሳተ መለያ ወይም የአስተዳዳሪ አካውንት አይደለም።";
                MessageBox.Show(errorMsg, "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                password.Clear();
                password.Focus();
            }
        }
    }
}
