using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Stock_Sync
{
    public class StaffUserInfo
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public int TotalShifts { get; set; }
        public decimal TotalSalesRevenue { get; set; }
    }

    /// <summary>
    /// Manages supermarket staff accounts — strictly controlled by Admin.
    /// Admin creates, resets, and removes staff credentials.
    /// </summary>
    public static class StaffUserManager
    {
        /// <summary>
        /// Creates a new Staff user account. Hashes password with PBKDF2.
        /// </summary>
        public static bool CreateStaff(string username, string password, out string errorMessage)
        {
            errorMessage = "";

            if (string.IsNullOrWhiteSpace(username) || username.Trim().Length < 3)
            {
                errorMessage = "Username must be at least 3 characters.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(password) || password.Trim().Length < 3)
            {
                errorMessage = "Password must be at least 3 characters.";
                return false;
            }

            string cleanUser = username.Trim().ToLowerInvariant();

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();

                    // Check if username already exists
                    string checkQuery = @"
                        SELECT COUNT(*) 
                        FROM Users 
                        WHERE LOWER(Username) = @user OR LOWER(Email) = @user OR LOWER(Email) = @email";

                    string syntheticEmail = $"{cleanUser}@supermarket.local";

                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@user", cleanUser);
                        checkCmd.Parameters.AddWithValue("@email", syntheticEmail);
                        int count = (int)checkCmd.ExecuteScalar();
                        if (count > 0)
                        {
                            errorMessage = $"Username '{cleanUser}' already exists. Please choose a different name.";
                            return false;
                        }
                    }

                    // Hash password with PBKDF2
                    string passwordHash = SecurityHelper.HashPassword(password.Trim());

                    string insertQuery = @"
                        INSERT INTO Users (Username, Email, Password, Role)
                        VALUES (@user, @email, @pass, 'Staff');
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@user", cleanUser);
                        insertCmd.Parameters.AddWithValue("@email", syntheticEmail);
                        insertCmd.Parameters.AddWithValue("@pass", passwordHash);
                        int newUserId = (int)insertCmd.ExecuteScalar();

                        ShiftManager.LogActivity($"Admin created new Staff account: '{cleanUser}' (User ID #{newUserId})");
                        Logger.LogInfo($"New Staff account created: {cleanUser} (ID #{newUserId})");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error creating staff account '{cleanUser}'", ex);
                errorMessage = "Database error: " + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Retrieves all registered Staff accounts along with their shift summary.
        /// </summary>
        public static List<StaffUserInfo> GetAllStaffUsers()
        {
            var list = new List<StaffUserInfo>();

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            u.UserID, 
                            ISNULL(u.Username, u.Email) AS Username, 
                            u.Email, 
                            u.Role,
                            (SELECT COUNT(*) FROM StaffShifts s WHERE s.UserID = u.UserID) AS TotalShifts,
                            (SELECT ISNULL(SUM(s.TotalSalesAmount), 0) FROM StaffShifts s WHERE s.UserID = u.UserID) AS TotalSales
                        FROM Users u
                        WHERE u.Role = 'Staff'
                        ORDER BY u.UserID DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new StaffUserInfo
                            {
                                UserId = Convert.ToInt32(r["UserID"]),
                                Username = r["Username"].ToString(),
                                Email = r["Email"].ToString(),
                                Role = r["Role"].ToString(),
                                TotalShifts = Convert.ToInt32(r["TotalShifts"]),
                                TotalSalesRevenue = Convert.ToDecimal(r["TotalSales"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error getting staff users", ex);
            }

            return list;
        }

        /// <summary>
        /// Resets the password for an existing Staff member.
        /// </summary>
        public static bool ResetStaffPassword(int userId, string newPassword, out string errorMessage)
        {
            errorMessage = "";
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Trim().Length < 3)
            {
                errorMessage = "New password must be at least 3 characters.";
                return false;
            }

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string hash = SecurityHelper.HashPassword(newPassword.Trim());
                    string updateQuery = "UPDATE Users SET Password = @hash WHERE UserID = @uid AND Role = 'Staff'";

                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@hash", hash);
                        cmd.Parameters.AddWithValue("@uid", userId);
                        int rows = cmd.ExecuteNonQuery();
                        if (rows > 0)
                        {
                            ShiftManager.LogActivity($"Admin reset password for Staff User #{userId}");
                            return true;
                        }
                        else
                        {
                            errorMessage = "Staff user not found or is an Admin account.";
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error resetting password for user {userId}", ex);
                errorMessage = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Deletes a Staff account. (Admin accounts cannot be deleted).
        /// </summary>
        public static bool DeleteStaff(int userId, out string errorMessage)
        {
            errorMessage = "";

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = "DELETE FROM Users WHERE UserID = @uid AND Role = 'Staff'";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        int rows = cmd.ExecuteNonQuery();
                        if (rows > 0)
                        {
                            ShiftManager.LogActivity($"Admin deleted Staff User ID #{userId}");
                            return true;
                        }
                        else
                        {
                            errorMessage = "Staff user not found or cannot delete Admin.";
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error deleting staff user {userId}", ex);
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}
