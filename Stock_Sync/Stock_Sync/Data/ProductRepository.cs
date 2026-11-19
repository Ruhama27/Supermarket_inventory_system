using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Stock_Sync.Models;

namespace Stock_Sync.Data
{
    public class ProductRepository : IProductRepository
    {
        public IEnumerable<ProductModel> GetAll(bool activeOnly = true)
        {
            var list = new List<ProductModel>();
            string query = @"
                SELECT 
                    p.ID, 
                    p.Barcode, 
                    p.PName, 
                    p.Category, 
                    p.CategoryID, 
                    ISNULL(p.Brand, '') AS Brand, 
                    ISNULL(p.Unit, 'Pcs') AS Unit, 
                    p.PStatus, 
                    p.IsActive, 
                    p.DateAdded,
                    ISNULL(SUM(s.quantity), 0) AS TotalQuantity,
                    ISNULL(MAX(s.sp_unit), 0.00) AS SellingPrice,
                    ISNULL(MAX(s.cp_unit), 0.00) AS CostPrice,
                    ISNULL(MAX(s.minimum_stock), 5) AS MinStock
                FROM Products p
                LEFT JOIN Stocks s ON p.ID = s.barcode_id AND s.IsActive = 1
                WHERE (@ActiveOnly = 0 OR p.IsActive = 1)
                GROUP BY p.ID, p.Barcode, p.PName, p.Category, p.CategoryID, p.Brand, p.Unit, p.PStatus, p.IsActive, p.DateAdded
                ORDER BY p.PName ASC";

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ActiveOnly", activeOnly ? 1 : 0);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(MapProduct(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in ProductRepository.GetAll", ex);
            }

            return list;
        }

        public ProductModel GetById(int id)
        {
            string query = @"
                SELECT 
                    p.ID, p.Barcode, p.PName, p.Category, p.CategoryID, 
                    ISNULL(p.Brand, '') AS Brand, ISNULL(p.Unit, 'Pcs') AS Unit, 
                    p.PStatus, p.IsActive, p.DateAdded,
                    ISNULL(SUM(s.quantity), 0) AS TotalQuantity,
                    ISNULL(MAX(s.sp_unit), 0.00) AS SellingPrice,
                    ISNULL(MAX(s.cp_unit), 0.00) AS CostPrice,
                    ISNULL(MAX(s.minimum_stock), 5) AS MinStock
                FROM Products p
                LEFT JOIN Stocks s ON p.ID = s.barcode_id AND s.IsActive = 1
                WHERE p.ID = @ID
                GROUP BY p.ID, p.Barcode, p.PName, p.Category, p.CategoryID, p.Brand, p.Unit, p.PStatus, p.IsActive, p.DateAdded";

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", id);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return MapProduct(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in ProductRepository.GetById ({id})", ex);
            }

            return null;
        }

        public ProductModel GetByBarcode(long barcode)
        {
            string query = @"
                SELECT 
                    p.ID, p.Barcode, p.PName, p.Category, p.CategoryID, 
                    ISNULL(p.Brand, '') AS Brand, ISNULL(p.Unit, 'Pcs') AS Unit, 
                    p.PStatus, p.IsActive, p.DateAdded,
                    ISNULL(SUM(s.quantity), 0) AS TotalQuantity,
                    ISNULL(MAX(s.sp_unit), 0.00) AS SellingPrice,
                    ISNULL(MAX(s.cp_unit), 0.00) AS CostPrice,
                    ISNULL(MAX(s.minimum_stock), 5) AS MinStock
                FROM Products p
                LEFT JOIN Stocks s ON p.ID = s.barcode_id AND s.IsActive = 1
                WHERE p.Barcode = @Barcode AND p.IsActive = 1
                GROUP BY p.ID, p.Barcode, p.PName, p.Category, p.CategoryID, p.Brand, p.Unit, p.PStatus, p.IsActive, p.DateAdded";

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Barcode", barcode);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return MapProduct(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in ProductRepository.GetByBarcode ({barcode})", ex);
            }

            return null;
        }

        public bool BarcodeExists(long barcode, int? excludeId = null)
        {
            string query = "SELECT COUNT(*) FROM Products WHERE Barcode = @Barcode AND (@ExcludeId IS NULL OR ID <> @ExcludeId)";
            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Barcode", barcode);
                        cmd.Parameters.AddWithValue("@ExcludeId", (object)excludeId ?? DBNull.Value);
                        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in ProductRepository.BarcodeExists", ex);
                return false;
            }
        }

        public bool Add(ProductModel product, out string errorMessage)
        {
            errorMessage = "";
            if (product == null)
            {
                errorMessage = "Product model cannot be null.";
                return false;
            }

            if (BarcodeExists(product.Barcode))
            {
                errorMessage = $"Barcode {product.Barcode} already exists.";
                return false;
            }

            string query = @"
                INSERT INTO Products (Barcode, PName, Category, CategoryID, Brand, Unit, PStatus, IsActive, DateAdded)
                OUTPUT INSERTED.ID
                VALUES (@Barcode, @Name, @Category, @CategoryID, @Brand, @Unit, @Status, 1, GETDATE())";

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Barcode", product.Barcode);
                        cmd.Parameters.AddWithValue("@Name", product.Name.Trim());
                        cmd.Parameters.AddWithValue("@Category", (object)product.Category ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CategoryID", (object)product.CategoryId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Brand", (object)product.Brand ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Unit", string.IsNullOrWhiteSpace(product.Unit) ? "Pcs" : product.Unit);
                        cmd.Parameters.AddWithValue("@Status", string.IsNullOrWhiteSpace(product.Status) ? "Available" : product.Status);

                        product.Id = Convert.ToInt32(cmd.ExecuteScalar());
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                Logger.LogError("Error in ProductRepository.Add", ex);
                return false;
            }
        }

        public bool Update(ProductModel product, out string errorMessage)
        {
            errorMessage = "";
            if (product == null || product.Id <= 0)
            {
                errorMessage = "Invalid product ID.";
                return false;
            }

            if (BarcodeExists(product.Barcode, product.Id))
            {
                errorMessage = $"Barcode {product.Barcode} is already registered to another product.";
                return false;
            }

            string query = @"
                UPDATE Products 
                SET Barcode = @Barcode, 
                    PName = @Name, 
                    Category = @Category, 
                    CategoryID = @CategoryID,
                    Brand = @Brand,
                    Unit = @Unit,
                    PStatus = @Status,
                    IsActive = @IsActive
                WHERE ID = @ID";

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", product.Id);
                        cmd.Parameters.AddWithValue("@Barcode", product.Barcode);
                        cmd.Parameters.AddWithValue("@Name", product.Name.Trim());
                        cmd.Parameters.AddWithValue("@Category", (object)product.Category ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CategoryID", (object)product.CategoryId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Brand", (object)product.Brand ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Unit", string.IsNullOrWhiteSpace(product.Unit) ? "Pcs" : product.Unit);
                        cmd.Parameters.AddWithValue("@Status", string.IsNullOrWhiteSpace(product.Status) ? "Available" : product.Status);
                        cmd.Parameters.AddWithValue("@IsActive", product.IsActive ? 1 : 0);

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                Logger.LogError($"Error in ProductRepository.Update ({product.Id})", ex);
                return false;
            }
        }

        public bool Delete(int id, out string errorMessage)
        {
            errorMessage = "";
            string query = "UPDATE Products SET IsActive = 0 WHERE ID = @ID";
            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", id);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                Logger.LogError($"Error in ProductRepository.Delete ({id})", ex);
                return false;
            }
        }

        public IEnumerable<string> GetDistinctCategories()
        {
            var list = new List<string>();
            string query = "SELECT DISTINCT Category FROM Products WHERE Category IS NOT NULL AND Category <> '' ORDER BY Category";
            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(reader["Category"].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in ProductRepository.GetDistinctCategories", ex);
            }
            return list;
        }

        private static ProductModel MapProduct(SqlDataReader reader)
        {
            return new ProductModel
            {
                Id = Convert.ToInt32(reader["ID"]),
                Barcode = Convert.ToInt64(reader["Barcode"]),
                Name = reader["PName"].ToString(),
                Category = reader["Category"] == DBNull.Value ? "" : reader["Category"].ToString(),
                CategoryId = reader["CategoryID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["CategoryID"]),
                Brand = reader["Brand"].ToString(),
                Unit = reader["Unit"].ToString(),
                Status = reader["PStatus"].ToString(),
                IsActive = Convert.ToBoolean(reader["IsActive"]),
                DateAdded = Convert.ToDateTime(reader["DateAdded"]),
                TotalAvailableQuantity = Convert.ToInt32(reader["TotalQuantity"]),
                CurrentSellingPrice = Convert.ToDecimal(reader["SellingPrice"]),
                CurrentCostPrice = Convert.ToDecimal(reader["CostPrice"]),
                MinimumStockThreshold = Convert.ToInt32(reader["MinStock"])
            };
        }
    }
}
