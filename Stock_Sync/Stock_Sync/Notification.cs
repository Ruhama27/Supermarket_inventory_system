using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace Stock_Sync
{
    internal class Notification
    {
        public static void ShowLowStockAlerts(RichTextBox rtb)
        {
            rtb.SelectionAlignment = HorizontalAlignment.Center;
            rtb.SelectionFont = new Font("Arial", 16, FontStyle.Bold | FontStyle.Underline);
            rtb.SelectionColor = Color.FromArgb(25, 45, 55);
            rtb.AppendText("\nLOW STOCK ALERTS\n\n");

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"
                    SELECT p.PName, SUM(s.quantity) AS TotalQty, MAX(s.minimum_stock) AS MinStock
                    FROM Products p 
                    INNER JOIN Stocks s ON p.ID = s.barcode_id 
                    WHERE p.IsActive = 1 AND s.IsActive = 1
                    GROUP BY p.PName
                    HAVING SUM(s.quantity) < MAX(s.minimum_stock)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            rtb.SelectionAlignment = HorizontalAlignment.Left;
                            rtb.SelectionFont = new Font("Arial", 12, FontStyle.Italic);
                            rtb.SelectionColor = Color.Gray;
                            rtb.AppendText("  All active products have sufficient stock.\n\n");
                            return;
                        }

                        rtb.SelectionAlignment = HorizontalAlignment.Left;
                        while (reader.Read())
                        {
                            string name = reader["PName"].ToString();
                            int qty = Convert.ToInt32(reader["TotalQty"]);
                            int min = Convert.ToInt32(reader["MinStock"]);
                            rtb.SelectionFont = new Font("Arial", 13, FontStyle.Bold);
                            rtb.SelectionColor = Color.DarkRed;
                            rtb.AppendText($"   - {name}: ");
                            rtb.SelectionFont = new Font("Arial", 13, FontStyle.Regular);
                            rtb.SelectionColor = Color.Black;
                            rtb.AppendText($"Only {qty} left in stock (Min Threshold: {min})\n\n");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error loading low stock alerts", ex);
            }
        }

        public static void ShowExpiredProductAlerts(RichTextBox rtb)
        {
            rtb.SelectionAlignment = HorizontalAlignment.Center;
            rtb.SelectionFont = new Font("Arial", 16, FontStyle.Bold | FontStyle.Underline);
            rtb.SelectionColor = Color.FromArgb(37, 63, 75);
            rtb.AppendText("\nEXPIRED PRODUCT BATCHES\n\n");

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"
                    SELECT p.PName AS ProductName, s.stock_code, s.quantity, s.expiry_date 
                    FROM Products p 
                    INNER JOIN Stocks s ON p.ID = s.barcode_id 
                    WHERE s.expiry_date <= CAST(GETDATE() AS DATE) 
                      AND s.quantity > 0
                      AND p.IsActive = 1 AND s.IsActive = 1
                    ORDER BY s.expiry_date ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            rtb.SelectionAlignment = HorizontalAlignment.Left;
                            rtb.SelectionFont = new Font("Arial", 12, FontStyle.Italic);
                            rtb.SelectionColor = Color.Gray;
                            rtb.AppendText("  No active stock batches are expired.\n\n");
                            return;
                        }

                        rtb.SelectionAlignment = HorizontalAlignment.Left;
                        while (reader.Read())
                        {
                            string name = reader["ProductName"].ToString();
                            string code = reader["stock_code"].ToString();
                            int qty = Convert.ToInt32(reader["quantity"]);
                            DateTime expiry = Convert.ToDateTime(reader["expiry_date"]);

                            rtb.SelectionFont = new Font("Arial", 13, FontStyle.Bold);
                            rtb.SelectionColor = Color.Red;
                            rtb.AppendText($"   - {name} ");
                            rtb.SelectionFont = new Font("Arial", 13, FontStyle.Regular);
                            rtb.SelectionColor = Color.Black;
                            rtb.AppendText($"[Batch: {code}, Qty: {qty}] expired on {expiry:dd MMM yyyy}\n\n");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error loading expired product alerts", ex);
            }
        }

        public static void ShowNeverSoldItems(RichTextBox rtb)
        {
            rtb.SelectionAlignment = HorizontalAlignment.Center;
            rtb.SelectionFont = new Font("Arial", 16, FontStyle.Bold | FontStyle.Underline);
            rtb.SelectionColor = Color.FromArgb(25, 45, 55);
            rtb.AppendText("\nNEVER SOLD PRODUCTS\n\n");

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"
                    SELECT p.PName, p.Barcode, p.Category
                    FROM Products p
                    WHERE p.IsActive = 1
                      AND p.Barcode NOT IN (SELECT DISTINCT Barcode FROM OrderDetails)
                    ORDER BY p.PName";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            rtb.SelectionAlignment = HorizontalAlignment.Left;
                            rtb.SelectionFont = new Font("Segoe UI", 12, FontStyle.Italic);
                            rtb.SelectionColor = Color.Gray;
                            rtb.AppendText("  All active products have been sold at least once.\n\n");
                            return;
                        }

                        rtb.SelectionAlignment = HorizontalAlignment.Left;
                        while (reader.Read())
                        {
                            string name = reader["PName"].ToString();
                            long barcode = Convert.ToInt64(reader["Barcode"]);
                            string cat = reader["Category"].ToString();

                            rtb.SelectionFont = new Font("Arial", 13, FontStyle.Italic);
                            rtb.SelectionColor = Color.FromArgb(50, 50, 50);
                            rtb.AppendText($"   - {name} (Barcode: {barcode}, Category: {cat}) has never been sold\n\n");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error loading never sold items", ex);
            }
        }
    }
}
