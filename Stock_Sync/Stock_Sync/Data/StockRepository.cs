using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Stock_Sync.Models;

namespace Stock_Sync.Data
{
    public class StockRepository : IStockRepository
    {
        public IEnumerable<StockBatchModel> GetAllBatches(bool activeOnly = true)
        {
            var list = new List<StockBatchModel>();
            string query = @"
                SELECT 
                    s.stock_id,
                    s.barcode_id,
                    p.Barcode,
                    p.PName,
                    s.stock_code,
                    s.supplier_id,
                    ISNULL(sup.SupplierName, '') AS SupplierName,
                    s.quantity,
                    s.minimum_stock,
                    s.cp_unit,
                    s.sp_unit,
                    s.arrival_date,
                    s.expiry_date,
                    s.IsActive,
                    s.date_added
                FROM Stocks s
                INNER JOIN Products p ON s.barcode_id = p.ID
                LEFT JOIN Suppliers sup ON s.supplier_id = sup.SupplierID
                WHERE (@ActiveOnly = 0 OR (s.IsActive = 1 AND p.IsActive = 1))
                ORDER BY s.stock_id DESC";

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
                                list.Add(MapBatch(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in StockRepository.GetAllBatches", ex);
            }
            return list;
        }

        public IEnumerable<StockBatchModel> GetBatchesByProductId(int productId)
        {
            var list = new List<StockBatchModel>();
            string query = @"
                SELECT 
                    s.stock_id, s.barcode_id, p.Barcode, p.PName, s.stock_code, s.supplier_id,
                    ISNULL(sup.SupplierName, '') AS SupplierName, s.quantity, s.minimum_stock,
                    s.cp_unit, s.sp_unit, s.arrival_date, s.expiry_date, s.IsActive, s.date_added
                FROM Stocks s
                INNER JOIN Products p ON s.barcode_id = p.ID
                LEFT JOIN Suppliers sup ON s.supplier_id = sup.SupplierID
                WHERE s.barcode_id = @ProductId AND s.IsActive = 1
                ORDER BY s.expiry_date ASC, s.stock_id ASC";

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ProductId", productId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(MapBatch(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in StockRepository.GetBatchesByProductId ({productId})", ex);
            }
            return list;
        }

        public StockBatchModel GetBatchById(int stockId)
        {
            string query = @"
                SELECT 
                    s.stock_id, s.barcode_id, p.Barcode, p.PName, s.stock_code, s.supplier_id,
                    ISNULL(sup.SupplierName, '') AS SupplierName, s.quantity, s.minimum_stock,
                    s.cp_unit, s.sp_unit, s.arrival_date, s.expiry_date, s.IsActive, s.date_added
                FROM Stocks s
                INNER JOIN Products p ON s.barcode_id = p.ID
                LEFT JOIN Suppliers sup ON s.supplier_id = sup.SupplierID
                WHERE s.stock_id = @StockId";

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@StockId", stockId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return MapBatch(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in StockRepository.GetBatchById ({stockId})", ex);
            }
            return null;
        }

        public bool AddBatch(StockBatchModel batch, out string errorMessage)
        {
            errorMessage = "";
            if (batch == null)
            {
                errorMessage = "Batch model cannot be null.";
                return false;
            }

            if (batch.Quantity < 0)
            {
                errorMessage = "Stock quantity cannot be negative.";
                return false;
            }

            string query = @"
                INSERT INTO Stocks (barcode_id, stock_code, supplier_id, quantity, minimum_stock, cp_unit, sp_unit, arrival_date, expiry_date, IsActive, date_added)
                OUTPUT INSERTED.stock_id
                VALUES (@BarcodeId, @StockCode, @SupplierId, @Qty, @MinStock, @Cp, @Sp, @Arrival, @Expiry, 1, GETDATE())";

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@BarcodeId", batch.BarcodeId);
                        cmd.Parameters.AddWithValue("@StockCode", (object)batch.StockCode ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SupplierId", (object)batch.SupplierId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Qty", batch.Quantity);
                        cmd.Parameters.AddWithValue("@MinStock", batch.MinimumStock);
                        cmd.Parameters.AddWithValue("@Cp", batch.CostPrice);
                        cmd.Parameters.AddWithValue("@Sp", batch.SellingPrice);
                        cmd.Parameters.AddWithValue("@Arrival", batch.ArrivalDate);
                        cmd.Parameters.AddWithValue("@Expiry", (object)batch.ExpiryDate ?? DBNull.Value);

                        batch.StockId = Convert.ToInt32(cmd.ExecuteScalar());
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                Logger.LogError("Error in StockRepository.AddBatch", ex);
                return false;
            }
        }

        public bool UpdateBatch(StockBatchModel batch, out string errorMessage)
        {
            errorMessage = "";
            if (batch == null || batch.StockId <= 0)
            {
                errorMessage = "Invalid stock batch ID.";
                return false;
            }

            if (batch.Quantity < 0)
            {
                errorMessage = "Stock quantity cannot be negative.";
                return false;
            }

            string query = @"
                UPDATE Stocks 
                SET barcode_id = @BarcodeId, 
                    stock_code = @StockCode, 
                    supplier_id = @SupplierId,
                    quantity = @Qty, 
                    minimum_stock = @MinStock, 
                    cp_unit = @Cp, 
                    sp_unit = @Sp, 
                    arrival_date = @Arrival, 
                    expiry_date = @Expiry,
                    IsActive = @IsActive
                WHERE stock_id = @StockId";

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@StockId", batch.StockId);
                        cmd.Parameters.AddWithValue("@BarcodeId", batch.BarcodeId);
                        cmd.Parameters.AddWithValue("@StockCode", (object)batch.StockCode ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SupplierId", (object)batch.SupplierId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Qty", batch.Quantity);
                        cmd.Parameters.AddWithValue("@MinStock", batch.MinimumStock);
                        cmd.Parameters.AddWithValue("@Cp", batch.CostPrice);
                        cmd.Parameters.AddWithValue("@Sp", batch.SellingPrice);
                        cmd.Parameters.AddWithValue("@Arrival", batch.ArrivalDate);
                        cmd.Parameters.AddWithValue("@Expiry", (object)batch.ExpiryDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", batch.IsActive ? 1 : 0);

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                Logger.LogError($"Error in StockRepository.UpdateBatch ({batch.StockId})", ex);
                return false;
            }
        }

        public bool DeleteBatch(int stockId, out string errorMessage)
        {
            errorMessage = "";
            string query = "UPDATE Stocks SET IsActive = 0 WHERE stock_id = @StockId";
            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@StockId", stockId);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                Logger.LogError($"Error in StockRepository.DeleteBatch ({stockId})", ex);
                return false;
            }
        }

        public int GetTotalStockByBarcode(long barcode)
        {
            string query = @"
                SELECT ISNULL(SUM(s.quantity), 0)
                FROM Stocks s
                INNER JOIN Products p ON s.barcode_id = p.ID
                WHERE p.Barcode = @Barcode AND s.IsActive = 1 AND p.IsActive = 1";

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Barcode", barcode);
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in StockRepository.GetTotalStockByBarcode ({barcode})", ex);
                return 0;
            }
        }

        public decimal GetActiveSellingPrice(long barcode)
        {
            string query = @"
                SELECT TOP 1 s.sp_unit
                FROM Stocks s
                INNER JOIN Products p ON s.barcode_id = p.ID
                WHERE p.Barcode = @Barcode AND s.IsActive = 1 AND p.IsActive = 1 AND s.quantity > 0
                ORDER BY 
                    CASE WHEN s.expiry_date IS NOT NULL THEN 0 ELSE 1 END,
                    s.expiry_date ASC,
                    s.stock_id ASC";

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Barcode", barcode);
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            return Convert.ToDecimal(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in StockRepository.GetActiveSellingPrice ({barcode})", ex);
            }
            return 0.00m;
        }

        public IEnumerable<StockBatchModel> GetLowStockBatches()
        {
            var list = new List<StockBatchModel>();
            string query = @"
                SELECT 
                    s.stock_id, s.barcode_id, p.Barcode, p.PName, s.stock_code, s.supplier_id,
                    ISNULL(sup.SupplierName, '') AS SupplierName, s.quantity, s.minimum_stock,
                    s.cp_unit, s.sp_unit, s.arrival_date, s.expiry_date, s.IsActive, s.date_added
                FROM Stocks s
                INNER JOIN Products p ON s.barcode_id = p.ID
                LEFT JOIN Suppliers sup ON s.supplier_id = sup.SupplierID
                WHERE s.IsActive = 1 AND p.IsActive = 1 AND s.quantity <= s.minimum_stock
                ORDER BY s.quantity ASC";

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
                                list.Add(MapBatch(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in StockRepository.GetLowStockBatches", ex);
            }
            return list;
        }

        public IEnumerable<StockBatchModel> GetExpiredBatches()
        {
            var list = new List<StockBatchModel>();
            string query = @"
                SELECT 
                    s.stock_id, s.barcode_id, p.Barcode, p.PName, s.stock_code, s.supplier_id,
                    ISNULL(sup.SupplierName, '') AS SupplierName, s.quantity, s.minimum_stock,
                    s.cp_unit, s.sp_unit, s.arrival_date, s.expiry_date, s.IsActive, s.date_added
                FROM Stocks s
                INNER JOIN Products p ON s.barcode_id = p.ID
                LEFT JOIN Suppliers sup ON s.supplier_id = sup.SupplierID
                WHERE s.IsActive = 1 AND p.IsActive = 1 AND s.expiry_date <= CAST(GETDATE() AS DATE)
                ORDER BY s.expiry_date ASC";

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
                                list.Add(MapBatch(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in StockRepository.GetExpiredBatches", ex);
            }
            return list;
        }

        public bool Restock(long barcode, int additionalQuantity, string stockCode, out string errorMessage)
        {
            errorMessage = "";
            if (additionalQuantity <= 0)
            {
                errorMessage = "Restock quantity must be greater than zero.";
                return false;
            }

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"
                        DECLARE @Pid INT = (SELECT TOP 1 ID FROM Products WHERE Barcode = @Barcode AND IsActive = 1);
                        IF @Pid IS NULL
                        BEGIN
                            RAISERROR('Product not found or inactive', 16, 1);
                            RETURN;
                        END

                        IF EXISTS (SELECT 1 FROM Stocks WHERE barcode_id = @Pid AND IsActive = 1)
                        BEGIN
                            UPDATE Stocks 
                            SET quantity = quantity + @AddQty, arrival_date = CAST(GETDATE() AS DATE)
                            WHERE stock_id = (
                                SELECT TOP 1 stock_id 
                                FROM Stocks 
                                WHERE barcode_id = @Pid AND IsActive = 1 
                                ORDER BY stock_id DESC
                            );
                        END
                        ELSE
                        BEGIN
                            INSERT INTO Stocks (barcode_id, stock_code, quantity, minimum_stock, cp_unit, sp_unit, arrival_date)
                            VALUES (@Pid, @StockCode, @AddQty, 5, 0.00, 0.00, CAST(GETDATE() AS DATE));
                        END";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Barcode", barcode);
                        cmd.Parameters.AddWithValue("@AddQty", additionalQuantity);
                        cmd.Parameters.AddWithValue("@StockCode", string.IsNullOrWhiteSpace(stockCode) ? $"RESTOCK-{DateTime.Now:yyyyMMdd}" : stockCode);

                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                Logger.LogError($"Error in StockRepository.Restock ({barcode})", ex);
                return false;
            }
        }

        private static StockBatchModel MapBatch(SqlDataReader reader)
        {
            return new StockBatchModel
            {
                StockId = Convert.ToInt32(reader["stock_id"]),
                BarcodeId = Convert.ToInt32(reader["barcode_id"]),
                Barcode = Convert.ToInt64(reader["Barcode"]),
                ProductName = reader["PName"].ToString(),
                StockCode = reader["stock_code"] == DBNull.Value ? "" : reader["stock_code"].ToString(),
                SupplierId = reader["supplier_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["supplier_id"]),
                SupplierName = reader["SupplierName"].ToString(),
                Quantity = Convert.ToInt32(reader["quantity"]),
                MinimumStock = Convert.ToInt32(reader["minimum_stock"]),
                CostPrice = Convert.ToDecimal(reader["cp_unit"]),
                SellingPrice = Convert.ToDecimal(reader["sp_unit"]),
                ArrivalDate = Convert.ToDateTime(reader["arrival_date"]),
                ExpiryDate = reader["expiry_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["expiry_date"]),
                IsActive = Convert.ToBoolean(reader["IsActive"]),
                DateAdded = Convert.ToDateTime(reader["date_added"])
            };
        }
    }
}
