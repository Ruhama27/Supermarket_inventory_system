using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Stock_Sync
{
    internal class Stock
    {
        private int barcode_id;
        private string stock_code;
        private int quantity;
        private int minimum_stock;
        private decimal cp_uint;
        private decimal sp_unit;
        private DateTime arrival_date;
        private DateTime? expiry_date;

        // Start getter setter
        public int Barcode_id
        {
            get { return barcode_id; }
            set { barcode_id = value; }
        }

        public string Stock_code
        {
            get { return stock_code; }
            set { stock_code = value; }
        }

        public int Quantity
        {
            get { return quantity; }
            set { quantity = value; }
        }

        public int Minimum_stock
        {
            get { return minimum_stock; }
            set { minimum_stock = value; }
        }

        public decimal Cp_unit
        {
            get { return cp_uint; }
            set { cp_uint = value; }
        }

        public decimal Sp_unit
        {
            get { return sp_unit; }
            set { sp_unit = value; }
        }

        public DateTime Arrival_date
        {
            get { return arrival_date; }
            set { arrival_date = value; }
        }

        public DateTime? Expirey_Date
        {
            get { return expiry_date; }
            set { expiry_date = value; }
        }
        // end getter setter

        // Start Add Method
        public bool Add()
        {
            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = "INSERT INTO Stocks (barcode_id, stock_code, quantity, minimum_stock, cp_unit, sp_unit, arrival_date, expiry_date, IsActive) " +
                                   "VALUES (@barcode_id, @stock_code, @quantity, @minimum_stock, @cp_unit, @sp_unit, @arrival_date, @ExpiryDate, 1)";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@barcode_id", barcode_id);
                        cmd.Parameters.AddWithValue("@stock_code", stock_code ?? "");
                        cmd.Parameters.AddWithValue("@quantity", quantity);
                        cmd.Parameters.AddWithValue("@minimum_stock", minimum_stock);
                        cmd.Parameters.AddWithValue("@cp_unit", cp_uint);
                        cmd.Parameters.AddWithValue("@sp_unit", sp_unit);
                        cmd.Parameters.AddWithValue("@arrival_date", arrival_date);

                        if (expiry_date.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@ExpiryDate", expiry_date.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@ExpiryDate", DBNull.Value);
                        }

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error adding stock record", ex);
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        // End Add Method

        // Start View Method
        public static DataTable ViewAll()
        {
            DataTable dt = new DataTable();

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT 
                        s.stock_id AS [Stock Id], 
                        p.Barcode, 
                        p.PName AS [Product Name], 
                        s.stock_code AS [Stock Code], 
                        s.quantity AS Quantity, 
                        s.cp_unit AS [Cost Price/Unit], 
                        s.sp_unit AS [Selling Price/Unit], 
                        s.expiry_date AS [Expiry Date],
                        s.date_added AS [Date Added]
                    FROM 
                        Stocks s
                    INNER JOIN 
                        Products p ON s.barcode_id = p.ID
                    WHERE
                        s.IsActive = 1 AND p.IsActive = 1
                    ORDER BY s.stock_id DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error loading stock view", ex);
                MessageBox.Show("Error loading stock records: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return dt;
        }
        // End View Method

        // Start FetchDetailId
        public void FetchDetailId(int Id)
        {
            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT * FROM Stocks WHERE stock_id = @Id AND IsActive = 1";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", Id);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                barcode_id = Convert.ToInt32(reader["barcode_id"]);
                                stock_code = reader["stock_code"].ToString();
                                quantity = Convert.ToInt32(reader["quantity"]);
                                minimum_stock = Convert.ToInt32(reader["minimum_stock"]);
                                cp_uint = Convert.ToDecimal(reader["cp_unit"]);
                                sp_unit = Convert.ToDecimal(reader["sp_unit"]);
                                arrival_date = Convert.ToDateTime(reader["arrival_date"]);
                                if (reader["expiry_date"] != DBNull.Value)
                                {
                                    expiry_date = Convert.ToDateTime(reader["expiry_date"]);
                                }
                                else
                                {
                                    expiry_date = null;
                                }
                            }
                            else
                            {
                                MessageBox.Show("Stock record not found.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching stock details for ID: {Id}", ex);
                MessageBox.Show("Error fetching stock details: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // End FetchDetailId

        // Start Update Method
        public bool Update(int id)
        {
            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"UPDATE Stocks 
                             SET 
                                 barcode_id = @BarcodeId,
                                 stock_code = @StockCode,
                                 quantity = @Quantity,
                                 minimum_stock = @MinimumStock,
                                 cp_unit = @CPUnit,
                                 sp_unit = @SPUnit,
                                 arrival_date = @ArrivalDate,
                                 expiry_date = @ExpiryDate
                             WHERE stock_id = @Id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@BarcodeId", barcode_id);
                        cmd.Parameters.AddWithValue("@StockCode", stock_code ?? "");
                        cmd.Parameters.AddWithValue("@Quantity", quantity);
                        cmd.Parameters.AddWithValue("@MinimumStock", minimum_stock);
                        cmd.Parameters.AddWithValue("@CPUnit", cp_uint);
                        cmd.Parameters.AddWithValue("@SPUnit", sp_unit);
                        cmd.Parameters.AddWithValue("@ArrivalDate", arrival_date);

                        if (expiry_date.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@ExpiryDate", expiry_date.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@ExpiryDate", DBNull.Value);
                        }

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error updating stock ID: {id}", ex);
                MessageBox.Show($"Failed to update stock: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        // End Update Method

        // Start Delete Method (Soft Delete)
        public bool Delete(int id)
        {
            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = "UPDATE Stocks SET IsActive = 0 WHERE stock_id = @Id";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error soft-deleting stock ID: {id}", ex);
                MessageBox.Show($"Failed to delete stock: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        // End Delete Method

        // Start Search method
        public static DataTable SearchStocks(string keyword)
        {
            DataTable dt = new DataTable();

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"
                    SELECT 
                        s.stock_id AS [Stock Id], 
                        p.Barcode, 
                        p.PName AS [Product Name], 
                        s.stock_code AS [Stock Code], 
                        s.quantity AS Quantity, 
                        s.cp_unit AS [Cost Price/Unit], 
                        s.sp_unit AS [Selling Price/Unit], 
                        s.expiry_date AS [Expiry Date],
                        s.date_added AS [Date Added]
                    FROM Stocks s
                    INNER JOIN Products p ON s.barcode_id = p.ID
                    WHERE 
                        s.IsActive = 1 AND p.IsActive = 1
                        AND (p.PName LIKE @keyword OR CAST(p.Barcode AS NVARCHAR) LIKE @keyword)
                    ORDER BY s.stock_id DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@keyword", "%" + keyword + "%");

                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during stock search with keyword '{keyword}'", ex);
                MessageBox.Show("Error during stock search: " + ex.Message, "Search Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return dt;
        }
        // End Search method
    }
}
