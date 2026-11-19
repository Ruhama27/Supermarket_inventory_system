using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace Stock_Sync
{
    public partial class StockForm : Form
    {
        private int selectedId = -1;
        private string input;
        private Stock stock = new Stock();
        private ContextMenuStrip contextMenu = new ContextMenuStrip();

        public StockForm()
        {
            InitializeComponent();
            SetupExportContextMenu();
        }

        private void SetupExportContextMenu()
        {
            var exportItem = new ToolStripMenuItem("Export Stocks to CSV...", null, (s, e) =>
            {
                if (dataGridView1.DataSource is DataTable dt && dt.Rows.Count > 0)
                {
                    CsvExportHelper.ExportToCsv(dt, $"Stock_Inventory_{DateTime.Now:yyyyMMdd}.csv");
                }
                else
                {
                    MessageBox.Show("No stock data available to export.", "Export Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            });

            contextMenu.Items.Add(exportItem);
            dataGridView1.ContextMenuStrip = contextMenu;
        }

        // Form Load
        private void StockForm_Load(object sender, EventArgs e)
        {
            if (Session.Role == "Staff")
            {
                button5.Enabled = false;
                button6.Enabled = false;
                button7.Enabled = false;
            }

            id.Focus();

            // Automatically load stock data on form load
            LoadStocks();
        }

        public void Clear()
        {
            id.Text = string.Empty;
            code.Text = string.Empty;
            quantity.Text = string.Empty;
            textBox1.Text = string.Empty;
            textBox3.Text = string.Empty;
            textBox5.Text = string.Empty;
            dateTimePicker1.Value = DateTime.Now;
            dateTimePicker2.Checked = false;
            selectedId = -1;
        }

        private void LoadStocks()
        {
            if (!string.IsNullOrEmpty(input))
            {
                dataGridView1.DataSource = Stock.SearchStocks(input);
            }
            else
            {
                dataGridView1.DataSource = Stock.ViewAll();
            }

            // Styling
            dataGridView1.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        // Add button
        private void insert_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(id.Text) ||
                string.IsNullOrWhiteSpace(code.Text) ||
                string.IsNullOrWhiteSpace(quantity.Text) ||
                string.IsNullOrWhiteSpace(textBox1.Text) ||
                string.IsNullOrWhiteSpace(textBox3.Text) ||
                string.IsNullOrWhiteSpace(textBox5.Text))
            {
                MessageBox.Show("Please fill in all required fields!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(id.Text, out _) ||
                !int.TryParse(quantity.Text, out _) ||
                !int.TryParse(textBox1.Text, out _) ||
                !double.TryParse(textBox3.Text, out _) ||
                !double.TryParse(textBox5.Text, out _))
            {
                MessageBox.Show("Invalid input detected. Please enter only numeric values in numeric fields.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            stock.Barcode_id = int.Parse(id.Text);
            stock.Stock_code = code.Text.Trim();
            stock.Quantity = int.Parse(quantity.Text);
            stock.Minimum_stock = int.Parse(textBox1.Text);
            stock.Cp_unit = decimal.Parse(textBox3.Text);
            stock.Sp_unit = decimal.Parse(textBox5.Text);
            stock.Arrival_date = dateTimePicker1.Value;

            if (dateTimePicker2.Checked)
            {
                stock.Expirey_Date = dateTimePicker2.Value;
            }
            else
            {
                stock.Expirey_Date = null;
            }

            if (stock.Add())
            {
                MessageBox.Show("Stock record saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Clear();
                LoadStocks();
                id.Focus();
            }
        }

        // Clear button
        private void button12_Click(object sender, EventArgs e)
        {
            Clear();
            input = string.Empty;
            textBox11.Clear();
            LoadStocks();
        }

        // Grid row selection
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow selectedRow = dataGridView1.Rows[e.RowIndex];

                if (selectedRow.Cells["Stock Id"].Value != DBNull.Value)
                {
                    selectedId = Convert.ToInt32(selectedRow.Cells["Stock Id"].Value);
                    stock.FetchDetailId(selectedId);

                    id.Text = stock.Barcode_id.ToString();
                    code.Text = stock.Stock_code;
                    quantity.Text = stock.Quantity.ToString();
                    textBox1.Text = stock.Minimum_stock.ToString();
                    textBox3.Text = stock.Cp_unit.ToString();
                    textBox5.Text = stock.Sp_unit.ToString();
                    dateTimePicker1.Value = stock.Arrival_date;

                    if (stock.Expirey_Date.HasValue)
                    {
                        dateTimePicker2.Checked = true;
                        dateTimePicker2.Value = stock.Expirey_Date.Value;
                    }
                    else
                    {
                        dateTimePicker2.Checked = false;
                    }
                }
            }
        }

        // Update button
        private void button9_Click(object sender, EventArgs e)
        {
            if (selectedId == -1)
            {
                MessageBox.Show("Please select a stock entry to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(id.Text, out _) ||
                !int.TryParse(quantity.Text, out _) ||
                !int.TryParse(textBox1.Text, out _) ||
                !double.TryParse(textBox3.Text, out _) ||
                !double.TryParse(textBox5.Text, out _))
            {
                MessageBox.Show("Invalid input. Please enter valid numeric values in numeric fields.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            stock.Barcode_id = int.Parse(id.Text);
            stock.Stock_code = code.Text.Trim();
            stock.Quantity = int.Parse(quantity.Text);
            stock.Minimum_stock = int.Parse(textBox1.Text);
            stock.Cp_unit = decimal.Parse(textBox3.Text);
            stock.Sp_unit = decimal.Parse(textBox5.Text);
            stock.Arrival_date = dateTimePicker1.Value;

            if (dateTimePicker2.Checked)
            {
                stock.Expirey_Date = dateTimePicker2.Value;
            }
            else
            {
                stock.Expirey_Date = null;
            }

            if (stock.Update(selectedId))
            {
                LoadStocks();
                MessageBox.Show("Stock record updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Clear();
            }
        }

        // Delete Button (Soft Delete)
        private void button10_Click(object sender, EventArgs e)
        {
            if (selectedId == -1)
            {
                MessageBox.Show("Please select a stock entry to deactivate.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult confirm = MessageBox.Show(
                $"Are you sure you want to deactivate stock batch ID #{selectedId}?",
                "Confirm Deactivation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                if (stock.Delete(selectedId))
                {
                    LoadStocks();
                    MessageBox.Show("Stock batch deactivated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Clear();
                }
            }
        }

        // View / Search Button
        private void button11_Click(object sender, EventArgs e)
        {
            button13_Click(sender, e);
        }

        private void dataGridView1_CellDoubleClick_1(object sender, DataGridViewCellEventArgs e)
        {
            dataGridView1_CellClick(sender, e);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            input = textBox11.Text.Trim();
            LoadStocks();
        }

        ////// Navigation Buttons ///////
        private void button1_Click(object sender, EventArgs e)
        {
            Buttons.OpenProductForm(this);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Buttons.OpenBillForm(this);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Buttons.OpenLoginForm(this);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Buttons.OpenNotificationForm(this);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Buttons.OpenChartForm(this);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Buttons.OpenReportForm(this);
        }
    }
}
