using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace Stock_Sync
{
    public partial class ProductsForm : Form
    {
        private int selectedId = -1;
        private string input;
        private Product product = new Product();
        private ContextMenuStrip contextMenu = new ContextMenuStrip();

        public ProductsForm()
        {
            InitializeComponent();
            SetupExportContextMenu();
        }

        private void SetupExportContextMenu()
        {
            var exportItem = new ToolStripMenuItem("Export Products to CSV...", null, (s, e) =>
            {
                if (dataGridView1.DataSource is DataTable dt && dt.Rows.Count > 0)
                {
                    CsvExportHelper.ExportToCsv(dt, $"Products_Inventory_{DateTime.Now:yyyyMMdd}.csv");
                }
                else
                {
                    MessageBox.Show("No products available to export.", "Export Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            });

            contextMenu.Items.Add(exportItem);
            dataGridView1.ContextMenuStrip = contextMenu;
        }

        // Form Load
        private void ProductsForm_Load(object sender, EventArgs e)
        {
            if (Session.Role == "Staff")
            {
                button5.Enabled = false;
                button6.Enabled = false;
                button7.Enabled = false;
            }

            barcode.Focus();
            barcode.UseSystemPasswordChar = false;
            barcode.Multiline = false;
            textBox11.UseSystemPasswordChar = false;
            textBox11.Multiline = false;

            // Automatically populate product grid on load
            LoadProducts();
        }

        public void Clear()
        {
            barcode.Text = string.Empty;
            name.Text = string.Empty;
            category.Text = string.Empty;
            status.SelectedIndex = -1;
            selectedId = -1;
        }

        private void LoadProducts()
        {
            if (!string.IsNullOrEmpty(input))
            {
                dataGridView1.DataSource = Product.SearchProducts(input);
            }
            else
            {
                dataGridView1.DataSource = Product.ViewAll();
            }

            // Styling
            dataGridView1.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        // Add button
        private void insert_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(barcode.Text) ||
                string.IsNullOrWhiteSpace(name.Text) ||
                string.IsNullOrWhiteSpace(category.Text) ||
                string.IsNullOrWhiteSpace(status.Text))
            {
                MessageBox.Show("Please fill in all required product fields.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!long.TryParse(barcode.Text.Trim(), out long parsedBarcode))
            {
                MessageBox.Show("Please enter a valid numeric barcode.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            product.PBarcode = parsedBarcode;
            product.PName = name.Text.Trim();
            product.PCategory = category.Text.Trim();
            product.PStatus = status.Text.Trim();

            if (product.Add())
            {
                MessageBox.Show("Product added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Clear();
                LoadProducts();
                barcode.Focus();
            }
        }

        // Clear button
        private void button12_Click(object sender, EventArgs e)
        {
            Clear();
            input = string.Empty;
            textBox11.Clear();
            LoadProducts();
        }

        // Grid Click to populate fields
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow selectedRow = dataGridView1.Rows[e.RowIndex];

                if (selectedRow.Cells["ID"].Value != DBNull.Value)
                {
                    selectedId = Convert.ToInt32(selectedRow.Cells["ID"].Value);
                    product.FetchDetailId(selectedId);

                    barcode.Text = product.PBarcode.ToString();
                    name.Text = product.PName;
                    category.Text = product.PCategory;
                    status.SelectedItem = product.PStatus;
                }
            }
        }

        // Update button
        private void button9_Click(object sender, EventArgs e)
        {
            if (selectedId == -1)
            {
                MessageBox.Show("Please select a product from the list to update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!long.TryParse(barcode.Text.Trim(), out long parsedBarcode))
            {
                MessageBox.Show("Please enter a valid numeric barcode.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            product.PBarcode = parsedBarcode;
            product.PName = name.Text.Trim();
            product.PCategory = category.Text.Trim();
            product.PStatus = status.Text.Trim();

            if (product.Update(selectedId))
            {
                LoadProducts();
                MessageBox.Show("Product updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Clear();
            }
        }

        // Delete Button (Soft Delete)
        private void button10_Click(object sender, EventArgs e)
        {
            if (selectedId == -1)
            {
                MessageBox.Show("Please select a product from the list to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult confirm = MessageBox.Show(
                $"Are you sure you want to deactivate product '{name.Text}' (ID: {selectedId})?",
                "Confirm Deactivation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                if (product.Delete(selectedId))
                {
                    LoadProducts();
                    MessageBox.Show("Product deactivated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Clear();
                }
            }
        }

        // Search / View Button
        private void button11_Click(object sender, EventArgs e)
        {
            button13_Click(sender, e);
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            dataGridView1_CellClick(sender, e);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            input = textBox11.Text.Trim();
            LoadProducts();
        }

        ////// Navigation Buttons ///////
        private void button2_Click(object sender, EventArgs e)
        {
            Buttons.OpenStockForm(this);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Buttons.OpenBillForm(this);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Buttons.OpenChartForm(this);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Buttons.OpenReportForm(this);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Buttons.OpenNotificationForm(this);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Current form
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Buttons.OpenLoginForm(this);
        }
    }
}
