using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace Stock_Sync
{
    public partial class ReportForm : System.Windows.Forms.Form
    {
        private Report report = new Report();
        private ContextMenuStrip contextMenu = new ContextMenuStrip();

        public ReportForm()
        {
            InitializeComponent();
            SetupExportContextMenu();
        }

        private void SetupExportContextMenu()
        {
            var exportItem = new ToolStripMenuItem("Export Report to CSV...", null, (s, e) =>
            {
                if (dataGridView1.DataSource is DataTable dt && dt.Rows.Count > 0)
                {
                    CsvExportHelper.ExportToCsv(dt, $"DailySalesReport_{dateTimePicker1.Value:yyyy-MM-dd}.csv");
                }
                else
                {
                    MessageBox.Show("Please generate a report with data before exporting.", "No Data", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            });

            contextMenu.Items.Add(exportItem);
            dataGridView1.ContextMenuStrip = contextMenu;
        }

        private void insert_Click(object sender, EventArgs e)
        {   
            report.Date = dateTimePicker1.Value;
            var dt = Report.ViewOrdersByDate();
            dataGridView1.DataSource = dt;

            // Styling
            dataGridView1.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            if (dt == null || dt.Rows.Count == 0)
            {
                MessageBox.Show($"No sales records found for {dateTimePicker1.Value:yyyy-MM-dd}.", "Report Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        ////// Navigation Buttons ///////
        private void button1_Click(object sender, EventArgs e)
        {
            Buttons.OpenProductForm(this);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Buttons.OpenStockForm(this);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Buttons.OpenBillForm(this);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Buttons.OpenNotificationForm(this);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Buttons.OpenChartForm(this);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Buttons.OpenLoginForm(this);
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }
    }
}
