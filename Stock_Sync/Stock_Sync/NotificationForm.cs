using System;
using System.IO;
using System.Windows.Forms;

namespace Stock_Sync
{
    public partial class NotificationForm : Form
    {
        private ContextMenuStrip contextMenu = new ContextMenuStrip();

        public NotificationForm()
        {
            InitializeComponent();
            SetupExportContextMenu();
        }

        private void SetupExportContextMenu()
        {
            var exportItem = new ToolStripMenuItem("Export Alerts to File...", null, (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(richTextBox1.Text))
                {
                    MessageBox.Show("No notification text to export.", "Export Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                    sfd.FileName = $"Inventory_Alerts_{DateTime.Now:yyyyMMdd}.txt";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            File.WriteAllText(sfd.FileName, richTextBox1.Text);
                            MessageBox.Show($"Alerts report saved successfully to:\n{sfd.FileName}", "Export Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError("Error saving alerts report", ex);
                            MessageBox.Show("Failed to save alerts report: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            });

            contextMenu.Items.Add(exportItem);
            richTextBox1.ContextMenuStrip = contextMenu;
        }

        private void NotificationForm_Load(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            Notification.ShowLowStockAlerts(richTextBox1);
            Notification.ShowExpiredProductAlerts(richTextBox1);
            Notification.ShowNeverSoldItems(richTextBox1);
        }

        ////// Navigation Buttons ///////
        private void button3_Click(object sender, EventArgs e)
        {
            Buttons.OpenBillForm(this);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Buttons.OpenProductForm(this);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Buttons.OpenStockForm(this);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Buttons.OpenChartForm(this);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Buttons.OpenReportForm(this);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Buttons.OpenLoginForm(this);
        }
    }
}
