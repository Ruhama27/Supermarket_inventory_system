using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Stock_Sync
{
    internal class Product : IDataOperations
    {
        private long pBarcode;
        private string pName;
        private string pCategory;
        private string pStatus;

        // Start getter setter
        public long PBarcode
        {
            get { return pBarcode; }
            set { pBarcode = value; }
        }

        public string PName
        {
            get { return pName; }
            set { pName = value; }
        }

        public string PCategory
        {
            get { return pCategory; }
            set { pCategory = value; }
        }

        public string PStatus
        {
            get { return pStatus; }
            set { pStatus = value; }
        }
        // End getter setter

        // Start Add Method
        public bool Add()
        {
            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = "INSERT INTO Products (Barcode, PName, Category, PStatus, IsActive) VALUES (@Barcode, @Name, @Category, @Status, 1)";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Barcode", pBarcode);
                        cmd.Parameters.AddWithValue("@Name", pName);
                        cmd.Parameters.AddWithValue("@Category", pCategory ?? "");
                        cmd.Parameters.AddWithValue("@Status", pStatus ?? "Available");
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error adding product", ex);
                MessageBox.Show($"Failed to add product: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    string query = @"
                    SELECT 
                        ID, 
                        Barcode, 
                        PName AS [Product Name], 
                        Category AS [Product Category], 
                        PStatus AS [Product Status], 
                        DateAdded AS [Date Added]
                    FROM Products
                    WHERE IsActive = 1
                    ORDER BY ID DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error loading products", ex);
                MessageBox.Show("Error loading products: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    string query = "SELECT * FROM Products WHERE ID = @Id AND IsActive = 1";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", Id);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                PBarcode = Convert.ToInt64(reader["Barcode"]);
                                PName = reader["PName"].ToString();
                                PCategory = reader["Category"].ToString();
                                PStatus = reader["PStatus"].ToString();
                            }
                            else
                            {
                                MessageBox.Show("Product not found.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching product details for ID: {Id}", ex);
                MessageBox.Show("Error fetching product details: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    string query = "UPDATE Products SET Barcode = @Barcode, PName = @Name, Category = @Category, PStatus = @Status WHERE ID = @Id";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@Barcode", pBarcode);
                        cmd.Parameters.AddWithValue("@Name", pName);
                        cmd.Parameters.AddWithValue("@Category", pCategory ?? "");
                        cmd.Parameters.AddWithValue("@Status", pStatus ?? "Available");
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error updating product ID: {id}", ex);
                MessageBox.Show($"Failed to update product: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    // Soft delete preserves historical invoices and audit logs
                    string query = "UPDATE Products SET IsActive = 0 WHERE ID = @Id";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error soft-deleting product ID: {id}", ex);
                MessageBox.Show($"Failed to delete product: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        // End Delete Method

        // Start Search method
        public static DataTable SearchProducts(string keyword)
        {
            DataTable dt = new DataTable();

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"
                    SELECT 
                        ID, 
                        Barcode, 
                        PName AS [Product Name], 
                        Category AS [Product Category], 
                        PStatus AS [Product Status], 
                        DateAdded AS [Date Added]
                    FROM Products
                    WHERE IsActive = 1
                      AND (PName LIKE @keyword OR CAST(Barcode AS NVARCHAR) LIKE @keyword)
                    ORDER BY ID DESC";

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
                Logger.LogError($"Error during product search with keyword '{keyword}'", ex);
                MessageBox.Show("Error during product search: " + ex.Message, "Search Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return dt;
        }
        // End Search method
    }
}
