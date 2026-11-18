using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;

namespace Stock_Sync
{
    internal class Bill
    {
        private List<BillItem> items = new List<BillItem>();

        public IReadOnlyList<BillItem> Items => items.AsReadOnly();
        public int LastSavedOrderId { get; private set; }

        public bool ScanBarcode(long barcode)
        {
            var product = GetProductByBarcode(barcode);
            if (product == null)
            {
                MessageBox.Show($"Product with barcode {barcode} was not found or is inactive.", "Product Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            int availableStock = GetAvailableStock(barcode);
            if (availableStock <= 0)
            {
                MessageBox.Show($"Product '{product.PName}' is completely out of stock!", "Out of Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            var existingItem = items.FirstOrDefault(i => i.Product.PBarcode == barcode);
            if (existingItem != null)
            {
                if (existingItem.Quantity + 1 > availableStock)
                {
                    MessageBox.Show($"Only {availableStock} units available in stock for '{product.PName}'.", "Stock Limit Reached", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                existingItem.Quantity++;
            }
            else
            {
                decimal price = GetSellingPriceByBarcode(barcode);
                if (price <= 0)
                {
                    MessageBox.Show($"No active stock price found for product '{product.PName}'.", "Pricing Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                items.Add(new BillItem
                {
                    Product = product,
                    Quantity = 1,
                    Price = price
                });
            }

            return true;
        }

        public int GetAvailableStock(long barcode)
        {
            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"
                    SELECT ISNULL(SUM(s.quantity), 0)
                    FROM Stocks s
                    INNER JOIN Products p ON p.ID = s.barcode_id
                    WHERE p.Barcode = @barcode AND s.IsActive = 1 AND p.IsActive = 1";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@barcode", barcode);
                        object result = cmd.ExecuteScalar();
                        if (result != DBNull.Value && result != null)
                        {
                            return Convert.ToInt32(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error getting available stock for barcode {barcode}", ex);
            }

            return 0;
        }

        private Product GetProductByBarcode(long barcode)
        {
            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"
                    SELECT Barcode, PName, Category, PStatus
                    FROM Products
                    WHERE Barcode = @barcode AND IsActive = 1";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@barcode", barcode);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new Product
                                {
                                    PBarcode = Convert.ToInt64(reader["Barcode"]),
                                    PName = reader["PName"].ToString(),
                                    PCategory = reader["Category"].ToString(),
                                    PStatus = reader["PStatus"].ToString()
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error retrieving product by barcode {barcode}", ex);
            }

            return null;
        }

        private decimal GetSellingPriceByBarcode(long barcode)
        {
            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"
                    SELECT TOP 1 s.sp_unit
                    FROM Stocks s
                    INNER JOIN Products p ON p.ID = s.barcode_id
                    WHERE p.Barcode = @barcode AND s.quantity > 0 AND s.IsActive = 1 AND p.IsActive = 1
                    ORDER BY s.expiry_date ASC, s.arrival_date ASC";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@barcode", barcode);
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
                Logger.LogError($"Error retrieving selling price for barcode {barcode}", ex);
            }

            return 0m;
        }

        public decimal CalculateSubtotal()
        {
            return items.Sum(i => i.Total);
        }

        public void Clear()
        {
            items.Clear();
        }

        public void RemoveItem(BillItem item)
        {
            items.Remove(item);
        }

        public bool SaveCart(decimal cashReceived, decimal changeReturned)
        {
            return SaveCart(cashReceived, changeReturned, "Cash", 0m, 0m, out _);
        }

        /// <summary>
        /// Completes the retail checkout with true multi-batch FIFO/FEFO stock depletion and transaction concurrency locks.
        /// </summary>
        public bool SaveCart(decimal cashReceived, decimal changeReturned, string paymentMethod, decimal discountAmount, decimal taxAmount, out int createdOrderId)
        {
            createdOrderId = 0;

            if (items.Count == 0)
            {
                MessageBox.Show("Cart is empty. Please scan items before checking out.", "Empty Cart", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            decimal subtotal = CalculateSubtotal();
            decimal totalPayable = Math.Max(0m, (subtotal - discountAmount) + taxAmount);

            using (SqlConnection conn = DBHelper.GetConnection())
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);

                try
                {
                    // 1. Insert master order record
                    ShiftManager.EnsureSchema();
                    string orderQuery = @"
                    INSERT INTO Orders (Date, Subtotal, TotalPayable, CashReceived, ChangeReturned, PaymentMethod, DiscountAmount, TaxAmount, CashierEmail, CashierName, ShiftID)
                    VALUES (@date, @subtotal, @total, @cash, @change, @payMethod, @discount, @tax, @cashier, @cashierName, @shiftId);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    using (SqlCommand cmd = new SqlCommand(orderQuery, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@date", DateTime.Now);
                        cmd.Parameters.AddWithValue("@subtotal", subtotal);
                        cmd.Parameters.AddWithValue("@total", totalPayable);
                        cmd.Parameters.AddWithValue("@cash", cashReceived);
                        cmd.Parameters.AddWithValue("@change", changeReturned);
                        cmd.Parameters.AddWithValue("@payMethod", paymentMethod ?? "Cash");
                        cmd.Parameters.AddWithValue("@discount", discountAmount);
                        cmd.Parameters.AddWithValue("@tax", taxAmount);
                        cmd.Parameters.AddWithValue("@cashier", (object)Session.Username ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@cashierName", (object)Session.Username ?? "Staff");
                        cmd.Parameters.AddWithValue("@shiftId", Session.CurrentShiftId > 0 ? (object)Session.CurrentShiftId : DBNull.Value);

                        createdOrderId = (int)cmd.ExecuteScalar();
                        LastSavedOrderId = createdOrderId;
                    }

                    // 2. Insert line items and execute multi-batch FIFO / FEFO deduction
                    foreach (var item in items)
                    {
                        // Insert line item into OrderDetails
                        string detailsQuery = @"
                        INSERT INTO OrderDetails (OrderID, Barcode, ProductName, Quantity, UnitPrice, TotalPrice)
                        VALUES (@orderId, @barcode, @name, @qty, @price, @total)";

                        using (SqlCommand cmd = new SqlCommand(detailsQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@orderId", createdOrderId);
                            cmd.Parameters.AddWithValue("@barcode", item.Product.PBarcode);
                            cmd.Parameters.AddWithValue("@name", item.Product.PName);
                            cmd.Parameters.AddWithValue("@qty", item.Quantity);
                            cmd.Parameters.AddWithValue("@price", item.Price);
                            cmd.Parameters.AddWithValue("@total", item.Total);

                            cmd.ExecuteNonQuery();
                        }

                        // Retrieve all batches for this product with UPDLOCK & ROWLOCK for concurrency protection
                        string fetchBatchesQuery = @"
                        SELECT s.stock_id, s.quantity
                        FROM Stocks s WITH (UPDLOCK, ROWLOCK)
                        INNER JOIN Products p ON p.ID = s.barcode_id
                        WHERE p.Barcode = @barcode 
                          AND s.quantity > 0 
                          AND s.IsActive = 1 
                          AND p.IsActive = 1
                        ORDER BY s.expiry_date ASC, s.arrival_date ASC, s.stock_id ASC";

                        var batchList = new List<Tuple<int, int>>();
                        using (SqlCommand cmd = new SqlCommand(fetchBatchesQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@barcode", item.Product.PBarcode);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    batchList.Add(Tuple.Create(Convert.ToInt32(reader["stock_id"]), Convert.ToInt32(reader["quantity"])));
                                }
                            }
                        }

                        // Deplete stock across available batches (FIFO)
                        int neededQty = item.Quantity;
                        foreach (var batch in batchList)
                        {
                            if (neededQty <= 0) break;

                            int stockId = batch.Item1;
                            int availableInBatch = batch.Item2;
                            int deduct = Math.Min(availableInBatch, neededQty);

                            string deductQuery = "UPDATE Stocks SET quantity = quantity - @deduct WHERE stock_id = @stockId";
                            using (SqlCommand cmd = new SqlCommand(deductQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@deduct", deduct);
                                cmd.Parameters.AddWithValue("@stockId", stockId);
                                cmd.ExecuteNonQuery();
                            }

                            neededQty -= deduct;
                        }

                        if (neededQty > 0)
                        {
                            throw new InvalidOperationException($"Insufficient inventory for '{item.Product.PName}'. Short by {neededQty} unit(s).");
                        }
                    }

                    transaction.Commit();
                    Logger.LogInfo($"Sale completed successfully: Order #{createdOrderId}, Total: {totalPayable:C}, Cashier: {Session.Username}");
                    
                    try
                    {
                        if (Session.CurrentShiftId > 0)
                        {
                            ShiftManager.RecordSale(Session.CurrentShiftId, totalPayable);
                        }
                        string staffRole = Session.IsAdmin ? "Admin" : "Staff";
                        ShiftManager.LogActivity($"{staffRole} '{Session.Username}' sold Order #{createdOrderId} for {LocalizationManager.FormatMoney(totalPayable)} ({paymentMethod})");
                    }
                    catch { }

                    Clear();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Logger.LogError("Transaction rollback during checkout", ex);
                    MessageBox.Show($"Checkout failed: {ex.Message}", "Sale Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
        }
    }
}
