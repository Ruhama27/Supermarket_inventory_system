using System;
using System.Drawing;
using System.Windows.Forms;

namespace Stock_Sync
{
    public partial class LoginForm : Form
    {
        private Button btnLangToggle;

        public LoginForm()
        {
            InitializeComponent();
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            this.BackColor = UITheme.AppBg;
            this.button1.BackColor = UITheme.SidebarBg;
            this.button2.BackColor = UITheme.Primary;
            this.button1.ForeColor = Color.White;
            this.button2.ForeColor = Color.White;
            this.button1.FlatStyle = FlatStyle.Flat;
            this.button2.FlatStyle = FlatStyle.Flat;
            this.button1.FlatAppearance.BorderSize = 0;
            this.button2.FlatAppearance.BorderSize = 0;
            this.panel2.BackColor = UITheme.Primary;
            this.panel3.BackColor = UITheme.Primary;
            this.groupBox1.BackColor = UITheme.CardBg;

            // Add modern language switcher button (Top Right)
            btnLangToggle = new Button
            {
                Text = LocalizationManager.CurrentLanguage == AppLanguage.English ? "🌐 አማርኛ" : "🌐 English",
                Size = new Size(115, 34),
                Location = new Point(this.ClientSize.Width - 135, 16),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.SidebarActiveBg,
                ForeColor = UITheme.SidebarActiveText,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLangToggle.FlatAppearance.BorderSize = 0;
            btnLangToggle.Click += (s, ev) =>
            {
                LocalizationManager.ToggleLanguage();
                btnLangToggle.Text = LocalizationManager.CurrentLanguage == AppLanguage.English ? "🌐 አማርኛ" : "🌐 English";
                ApplyLocalization();
            };

            this.Controls.Add(btnLangToggle);
            btnLangToggle.BringToFront();

            ApplyLocalization();
        }

        private void ApplyLocalization()
        {
            this.Text = LocalizationManager.CurrentLanguage == AppLanguage.English ? "Stock Sync - Login" : "ስቶክ ሲንክ - መግቢያ";
            this.label6.Text = LocalizationManager.T("SignInTitle");
            this.button2.Text = LocalizationManager.T("SignInAdmin");
            this.button1.Text = LocalizationManager.T("SignInStaff");
            this.label8.Text = LocalizationManager.T("Or");
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
