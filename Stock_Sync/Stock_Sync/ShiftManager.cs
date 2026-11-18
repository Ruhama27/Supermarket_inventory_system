using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Stock_Sync
{
    public class StaffShift
    {
        public int ShiftId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public DateTime ShiftStart { get; set; }
        public DateTime? ShiftEnd { get; set; }
        public int TotalSalesCount { get; set; }
        public decimal TotalSalesAmount { get; set; }
        public string Status { get; set; } // "Active" or "Completed"
    }

    public class StaffActivity
    {
        public int LogId { get; set; }
        public string Message { get; set; }
        public DateTime ActionTime { get; set; }
    }

    public static class ShiftManager
    {
        private static bool schemaInitialized = false;

        public static void EnsureSchema()
        {
            if (schemaInitialized) return;

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();

                    // 1. Create StaffShifts table if not exists
                    string createShiftsTable = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StaffShifts')
                    BEGIN
                        CREATE TABLE StaffShifts (
                            ShiftID INT IDENTITY(1,1) PRIMARY KEY,
                            UserID INT NOT NULL,
                            Username NVARCHAR(100) NOT NULL,
                            Role NVARCHAR(50) NOT NULL,
                            ShiftStart DATETIME DEFAULT GETDATE(),
                            ShiftEnd DATETIME NULL,
                            TotalSalesCount INT DEFAULT 0,
                            TotalSalesAmount DECIMAL(10,2) DEFAULT 0,
                            Status NVARCHAR(50) DEFAULT 'Active'
                        );
                    END";

                    using (SqlCommand cmd = new SqlCommand(createShiftsTable, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 2. Ensure Orders table has CashierName and ShiftID columns
                    string alterOrders = @"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'CashierName')
                    BEGIN
                        ALTER TABLE Orders ADD CashierName NVARCHAR(100) NULL;
                    END
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'ShiftID')
                    BEGIN
                        ALTER TABLE Orders ADD ShiftID INT NULL;
                    END";

                    using (SqlCommand cmd = new SqlCommand(alterOrders, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 3. Ensure AuditLog table exists
                    string createAudit = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLog')
                    BEGIN
                        CREATE TABLE AuditLog (
                            LogID INT PRIMARY KEY IDENTITY(1,1),
                            Message NVARCHAR(500),
                            ActionTime DATETIME DEFAULT GETDATE()
                        );
                    END";

                    using (SqlCommand cmd = new SqlCommand(createAudit, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    schemaInitialized = true;
                    Logger.LogInfo("ShiftManager database schema verified successfully.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to ensure ShiftManager database schema", ex);
            }
        }

        /// <summary>
        /// Starts a new shift when a staff member or cashier logs in.
        /// </summary>
        public static int StartShift(int userId, string username, string role)
        {
            EnsureSchema();

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();

                    // Close any lingering active shifts for this user from before
                    string closePrev = "UPDATE StaffShifts SET Status = 'Completed', ShiftEnd = GETDATE() WHERE UserID = @uid AND Status = 'Active'";
                    using (SqlCommand cmd = new SqlCommand(closePrev, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.ExecuteNonQuery();
                    }

                    // Insert new active shift
                    string insertQuery = @"
                    INSERT INTO StaffShifts (UserID, Username, Role, ShiftStart, TotalSalesCount, TotalSalesAmount, Status)
                    VALUES (@uid, @user, @role, GETDATE(), 0, 0, 'Active');
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.Parameters.AddWithValue("@user", username);
                        cmd.Parameters.AddWithValue("@role", role);
                        int shiftId = (int)cmd.ExecuteScalar();

                        LogActivity($"Staff '{username}' ({role}) started shift #{shiftId}");
                        return shiftId;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error starting shift for user {username}", ex);
                return 0;
            }
        }

        /// <summary>
        /// Records a sale against the currently active shift.
        /// </summary>
        public static void RecordSale(int shiftId, decimal amount)
        {
            if (shiftId <= 0) return;

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"
                    UPDATE StaffShifts 
                    SET TotalSalesCount = TotalSalesCount + 1,
                        TotalSalesAmount = TotalSalesAmount + @amt
                    WHERE ShiftID = @sid";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@amt", amount);
                        cmd.Parameters.AddWithValue("@sid", shiftId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error recording sale on shift {shiftId}", ex);
            }
        }

        /// <summary>
        /// Ends an active shift on logout or app exit.
        /// </summary>
        public static void EndShift(int shiftId)
        {
            if (shiftId <= 0) return;

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"
                    UPDATE StaffShifts 
                    SET ShiftEnd = GETDATE(), Status = 'Completed' 
                    WHERE ShiftID = @sid AND Status = 'Active'";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@sid", shiftId);
                        cmd.ExecuteNonQuery();
                    }

                    LogActivity($"Shift #{shiftId} completed.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error ending shift {shiftId}", ex);
            }
        }

        /// <summary>
        /// Returns all staff members currently on duty.
        /// </summary>
        public static List<StaffShift> GetActiveShifts()
        {
            EnsureSchema();
            var list = new List<StaffShift>();

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"
                    SELECT ShiftID, UserID, Username, Role, ShiftStart, TotalSalesCount, TotalSalesAmount, Status
                    FROM StaffShifts
                    WHERE Status = 'Active'
                    ORDER BY ShiftStart DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new StaffShift
                            {
                                ShiftId = Convert.ToInt32(r["ShiftID"]),
                                UserId = Convert.ToInt32(r["UserID"]),
                                Username = r["Username"].ToString(),
                                Role = r["Role"].ToString(),
                                ShiftStart = Convert.ToDateTime(r["ShiftStart"]),
                                TotalSalesCount = Convert.ToInt32(r["TotalSalesCount"]),
                                TotalSalesAmount = Convert.ToDecimal(r["TotalSalesAmount"]),
                                Status = r["Status"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error getting active shifts", ex);
            }

            return list;
        }

        /// <summary>
        /// Returns recent shifts (both active and completed) for admin audit.
        /// </summary>
        public static List<StaffShift> GetRecentShifts(int limit = 15)
        {
            EnsureSchema();
            var list = new List<StaffShift>();

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = $@"
                    SELECT TOP {limit} ShiftID, UserID, Username, Role, ShiftStart, ShiftEnd, TotalSalesCount, TotalSalesAmount, Status
                    FROM StaffShifts
                    ORDER BY ShiftStart DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new StaffShift
                            {
                                ShiftId = Convert.ToInt32(r["ShiftID"]),
                                UserId = Convert.ToInt32(r["UserID"]),
                                Username = r["Username"].ToString(),
                                Role = r["Role"].ToString(),
                                ShiftStart = Convert.ToDateTime(r["ShiftStart"]),
                                ShiftEnd = r["ShiftEnd"] != DBNull.Value ? Convert.ToDateTime(r["ShiftEnd"]) : (DateTime?)null,
                                TotalSalesCount = Convert.ToInt32(r["TotalSalesCount"]),
                                TotalSalesAmount = Convert.ToDecimal(r["TotalSalesAmount"]),
                                Status = r["Status"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error getting recent shifts", ex);
            }

            return list;
        }

        /// <summary>
        /// Logs a human-readable activity event into AuditLog table for the admin feed.
        /// </summary>
        public static void LogActivity(string message)
        {
            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = "INSERT INTO AuditLog (Message, ActionTime) VALUES (@msg, GETDATE())";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@msg", message);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Returns recent activity events showing who did what.
        /// </summary>
        public static List<StaffActivity> GetRecentActivities(int limit = 20)
        {
            EnsureSchema();
            var list = new List<StaffActivity>();

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = $"SELECT TOP {limit} LogID, Message, ActionTime FROM AuditLog ORDER BY ActionTime DESC";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new StaffActivity
                            {
                                LogId = Convert.ToInt32(r["LogID"]),
                                Message = r["Message"].ToString(),
                                ActionTime = Convert.ToDateTime(r["ActionTime"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error getting recent activities", ex);
            }

            return list;
        }
    }
}
