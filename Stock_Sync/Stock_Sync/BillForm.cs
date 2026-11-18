using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Stock_Sync
{
    public partial class BillForm : Form
    {
        private Bill bill = new Bill();
        private decimal subtotal;
        private PrintDocument printDocument = new PrintDocument();
        private string printText = "";

        // Track last completed order for receipt printing after cart checkout
        private List<BillItem> lastReceiptItems = new List<BillItem>();
        private int lastOrderId = 0;
        private decimal lastSubtotal = 0m;
        private decimal lastCashReceived = 0m;
        private decimal lastChangeReturned = 0m;
        private DateTime lastOrderDate = DateTime.Now;

        public BillForm()
        {
            InitializeComponent();
            // Attach print event handler once to prevent event accumulation leaks
            printDocument.PrintPage += PrintDocument_PrintPage;
        }

        private void BillForm_Load(object sender, EventArgs e)
        {
            if (Session.Role == "Staff")
            {
                button5.Enabled = false;
                button6.Enabled = false;
                button7.Enabled = false;
            }

            textBox11.Focus();
            textBox11.UseSystemPasswordChar = false;
            textBox11.Multiline = false;
            textBox1.UseSystemPasswordChar = false;
            textBox1.Multiline = false;
        }

        private void textBox11_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true; // prevent ding sound
                e.SuppressKeyPress = true;

                if (long.TryParse(textBox11.Text.Trim(), out long barcode))
                {
                    bool success = bill.ScanBarcode(barcode);
                    if (success)
                    {
                        RefreshGrid();
                        UpdateSubtotal();
                    }
                }
                else
                {
                    MessageBox.Show("Please enter or scan a valid numeric barcode.", "Invalid Barcode", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                textBox11.Clear();
                textBox11.Focus();
            }
        }

        private void RefreshGrid()
        {
            dataGridView1.DataSource = null;
            dataGridView1.DataSource = bill.Items.Select(i => new
            {
                Barcode = i.Product.PBarcode,
                Name = i.Product.PName,
                Quantity = i.Quantity,
                Price = i.Price,
                Total = i.Total
            }).ToList();

            // Styling
            dataGridView1.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void UpdateSubtotal()
        {
            subtotal = bill.CalculateSubtotal();
            total.Text = LocalizationManager.FormatMoney(subtotal);
            sub.Text = LocalizationManager.FormatMoney(subtotal);

            // Recalculate change if cash received is already entered
            if (decimal.TryParse(textBox1.Text, out decimal cashReceived))
            {
                textBox2.Text = LocalizationManager.FormatMoney(Math.Max(0m, cashReceived - subtotal));
            }
        }

        public decimal Change(decimal amount)
        {
            return amount - subtotal;
        }

        ////////// Cash Received and Change //////////////
        private void textBox1_KeyDown_1(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                if (decimal.TryParse(textBox1.Text, out decimal cashReceived))
                {
                    textBox2.Text = LocalizationManager.FormatMoney(cashReceived - subtotal);
                }
                else
                {
                    MessageBox.Show("Please enter a valid numeric amount for cash received.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        ////////// Save Button (Complete Sale) //////////////
        private void button10_Click(object sender, EventArgs e)
        {
            try
            {
                if (bill.Items.Count == 0)
                {
                    MessageBox.Show("Cannot complete sale. The cart is currently empty.", "Empty Cart", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!decimal.TryParse(textBox1.Text, out decimal cashReceived))
                {
                    MessageBox.Show("Please enter the amount of cash received.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox1.Focus();
                    return;
                }

                decimal totalPayable = bill.CalculateSubtotal();
                decimal changeReturned = cashReceived - totalPayable;

                if (cashReceived < totalPayable)
                {
                    MessageBox.Show($"Cash received ({LocalizationManager.FormatMoney(cashReceived)}) is less than total payable ({LocalizationManager.FormatMoney(totalPayable)})!", "Payment Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Preserve items for receipt printing
                var itemsForReceipt = bill.Items.Select(x => new BillItem
                {
                    Product = x.Product,
                    Quantity = x.Quantity,
                    Price = x.Price
                }).ToList();

                bool saved = bill.SaveCart(cashReceived, changeReturned, "Cash", 0m, 0m, out int orderId);
                if (saved)
                {
                    lastOrderId = orderId;
                    lastReceiptItems = itemsForReceipt;
                    lastSubtotal = totalPayable;
                    lastCashReceived = cashReceived;
                    lastChangeReturned = changeReturned;
                    lastOrderDate = DateTime.Now;

                    DialogResult printPrompt = MessageBox.Show(
                        $"Sale #{orderId} completed successfully!\nChange returned: {LocalizationManager.FormatMoney(changeReturned)}\n\nWould you like to print the receipt now?",
                        "Checkout Success",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    ClearCartUI();

                    if (printPrompt == DialogResult.Yes)
                    {
                        PrintCurrentOrLastReceipt();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error completing sale in BillForm", ex);
                MessageBox.Show("Unexpected error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        ////////// Remove Item Button //////////////
        private void button11_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null)
            {
                MessageBox.Show("Please select an item to remove.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            long barcode = Convert.ToInt64(dataGridView1.CurrentRow.Cells["Barcode"].Value);
            var item = bill.Items.FirstOrDefault(i => i.Product.PBarcode == barcode);

            if (item == null)
            {
                MessageBox.Show("Invalid item selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (item.Quantity > 1)
            {
                item.Quantity--;
            }
            else
            {
                bill.RemoveItem(item);
            }

            RefreshGrid();
            UpdateSubtotal();
        }

        ////////// Clear Button //////////////
        private void button9_Click(object sender, EventArgs e)
        {
            button12_Click(sender, e);
        }

        private void button12_Click(object sender, EventArgs e)
        {
            bill.Clear();
            ClearCartUI();
        }

        private void ClearCartUI()
        {
            RefreshGrid();
            subtotal = 0m;
            total.Text = LocalizationManager.FormatMoney(0m);
            sub.Text = LocalizationManager.FormatMoney(0m);
            textBox1.Clear();    // Cash received textbox
            textBox2.Clear();    // Change textbox
            textBox11.Clear();   // Barcode input textbox
            textBox11.Focus();
        }

        ////////// Print Receipt Button //////////////
        private void insert_Click(object sender, EventArgs e)
        {
            PrintCurrentOrLastReceipt();
        }

        private void PrintCurrentOrLastReceipt()
        {
            List<BillItem> itemsToPrint = bill.Items.Count > 0 ? bill.Items.ToList() : lastReceiptItems;

            if (itemsToPrint == null || itemsToPrint.Count == 0)
            {
                MessageBox.Show("No active or recent receipt found to print.", "No Receipt Data", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int orderNumber = bill.Items.Count > 0 ? (lastOrderId + 1) : lastOrderId;
            decimal printSub = bill.Items.Count > 0 ? bill.CalculateSubtotal() : lastSubtotal;
            decimal printCash = bill.Items.Count > 0 ? (decimal.TryParse(textBox1.Text, out decimal c) ? c : printSub) : lastCashReceived;
            decimal printChange = bill.Items.Count > 0 ? (printCash - printSub) : lastChangeReturned;
            DateTime printDate = bill.Items.Count > 0 ? DateTime.Now : lastOrderDate;

            // Build standard 80mm thermal receipt format
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("           STOCK SYNC RETAIL POS           ");
            sb.AppendLine("         Supermarket Inventory System      ");
            sb.AppendLine("-------------------------------------------");
            sb.AppendLine($"Invoice #: {orderNumber,-12} Date: {printDate:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"Cashier:   {(string.IsNullOrEmpty(Session.Email) ? "Admin" : Session.Email)}");
            sb.AppendLine("===========================================");
            sb.AppendLine("Item                  Qty    Price    Total");
            sb.AppendLine("-------------------------------------------");

            foreach (var item in itemsToPrint)
            {
                string name = item.Product.PName;
                if (name.Length > 16) name = name.Substring(0, 16);
                sb.AppendLine($"{name,-17} {item.Quantity,3}  {LocalizationManager.FormatMoney(item.Price),10}  {LocalizationManager.FormatMoney(item.Total),10}");
            }

            sb.AppendLine("===========================================");
            sb.AppendLine($"Subtotal:                       {LocalizationManager.FormatMoney(printSub),15}");
            sb.AppendLine($"Tax (0%):                       {LocalizationManager.FormatMoney(0m),15}");
            sb.AppendLine($"TOTAL PAYABLE:                  {LocalizationManager.FormatMoney(printSub),15}");
            sb.AppendLine("-------------------------------------------");
            sb.AppendLine($"Cash Received:                  {LocalizationManager.FormatMoney(printCash),15}");
            sb.AppendLine($"Change Returned:                {LocalizationManager.FormatMoney(printChange),15}");
            sb.AppendLine("===========================================");
            sb.AppendLine("        THANK YOU FOR SHOPPING WITH US!    ");
            sb.AppendLine("             አመሰግናለሁ! እንኳን ደህና መጡ!         ");
            sb.AppendLine();

            printText = sb.ToString();

            PrintDialog dialog = new PrintDialog();
            dialog.Document = printDocument;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    printDocument.Print();
                }
                catch (Exception ex)
                {
                    Logger.LogError("Error printing receipt", ex);
                    MessageBox.Show("Printing failed: " + ex.Message, "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            Font font = new Font("Courier New", 9, FontStyle.Regular);
            e.Graphics.DrawString(printText, font, Brushes.Black, new PointF(10, 10));
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
