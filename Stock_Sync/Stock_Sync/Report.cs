using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Stock_Sync
{
    internal class Report
    {
        private static DateTime date = DateTime.Today;

        public DateTime Date
        {
            get { return date; }
            set { date = value; }
        }

        public static DataTable ViewOrdersByDate()
        {
            DataTable dt = new DataTable();

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();

                    string query = @"
                    SELECT 
                        OrderID AS [Order ID],
                        Date,
                        TotalPayable AS [Total Payable],
                        CashReceived AS [Cash Received],
                        ChangeReturned AS [Change Returned],
                        PaymentMethod AS [Payment Method],
                        ISNULL(CashierEmail, 'Admin') AS [Cashier]
                    FROM Orders
                    WHERE CAST(Date AS DATE) = @Date
                    ORDER BY OrderID DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Date", date.Date);
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error loading orders for report", ex);
                MessageBox.Show("Error loading orders: " + ex.Message, "Report Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return dt;
        }

        /// <summary>
        /// Retrieves the comprehensive order history styled for the Orders Screen.
        /// </summary>
        public static DataTable FetchOrderHistory(string filter = "All")
        {
            DataTable dt = new DataTable();

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();

                    string query = @"
                    SELECT 
                        ROW_NUMBER() OVER (ORDER BY o.OrderID DESC) AS [S No],
                        '#ORD-' + CAST(o.OrderID AS VARCHAR(10)) AS [Order ID],
                        ISNULL((
                            SELECT TOP 1 ProductName + 
                                CASE WHEN (SELECT COUNT(*) FROM OrderDetails WHERE OrderID = o.OrderID) > 1 
                                     THEN ' (+' + CAST((SELECT COUNT(*) - 1 FROM OrderDetails WHERE OrderID = o.OrderID) AS VARCHAR(10)) + ' more)' 
                                     ELSE '' 
                                END 
                            FROM OrderDetails WHERE OrderID = o.OrderID
                        ), 'General Checkout') AS [Order Name],
                        o.TotalPayable AS [Stock Value],
                        ISNULL((SELECT SUM(Quantity) FROM OrderDetails WHERE OrderID = o.OrderID), 1) AS [Quantity],
                        o.PaymentMethod AS [Payment],
                        'Delivered' AS [Status]
                    FROM Orders o
                    ORDER BY o.OrderID DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error fetching order history", ex);
            }

            return dt;
        }

        /// <summary>
        /// Retrieves live KPI metrics for the Dashboard cards.
        /// </summary>
        public static (decimal TodaySales, int TodayOrders, int TotalProducts, int LowStockItems) GetDashboardKpis()
        {
            decimal sales = 0m;
            int orders = 0;
            int products = 0;
            int lowStock = 0;

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();

                    string query = @"
                    SELECT 
                        ISNULL((SELECT SUM(TotalPayable) FROM Orders WHERE CAST(Date AS DATE) = CAST(GETDATE() AS DATE)), 0) AS TodaySales,
                        ISNULL((SELECT COUNT(*) FROM Orders WHERE CAST(Date AS DATE) = CAST(GETDATE() AS DATE)), 0) AS TodayOrders,
                        ISNULL((SELECT COUNT(*) FROM Products WHERE IsActive = 1), 0) AS TotalProducts,
                        ISNULL((
                            SELECT COUNT(DISTINCT p.ID) 
                            FROM Products p 
                            INNER JOIN Stocks s ON p.ID = s.barcode_id 
                            WHERE p.IsActive = 1 AND s.IsActive = 1 
                            GROUP BY p.ID, s.minimum_stock 
                            HAVING SUM(s.quantity) < s.minimum_stock
                        ), 0) AS LowStockCount";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            sales = Convert.ToDecimal(reader["TodaySales"]);
                            orders = Convert.ToInt32(reader["TodayOrders"]);
                            products = Convert.ToInt32(reader["TotalProducts"]);
                            lowStock = Convert.ToInt32(reader["LowStockCount"]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error fetching dashboard KPIs", ex);
            }

            return (sales, orders, products, lowStock);
        }
    }
}
