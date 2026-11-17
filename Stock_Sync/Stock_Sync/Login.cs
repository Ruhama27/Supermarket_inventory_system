using System;
using System.Data.SqlClient;

namespace Stock_Sync
{
    public class Login
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }

        public static Login Authenticate(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    string query = "SELECT UserID, Email, Password, Role FROM Users WHERE Email = @Email";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", email.Trim());
                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userId = Convert.ToInt32(reader["UserID"]);
                                string userEmail = reader["Email"].ToString();
                                string storedPassword = reader["Password"].ToString();
                                string role = reader["Role"].ToString();

                                bool isValid = SecurityHelper.VerifyPassword(password, storedPassword, out bool needsUpgrade);

                                if (isValid)
                                {
                                    reader.Close();

                                    // Automatic migration of plaintext passwords to PBKDF2 hash
                                    if (needsUpgrade)
                                    {
                                        UpgradePassword(conn, userId, password);
                                    }

                                    Logger.LogInfo($"User logged in successfully: {userEmail} ({role})");

                                    return new Login
                                    {
                                        UserId = userId,
                                        Email = userEmail,
                                        Role = role
                                    };
                                }
                            }
                        }
                    }
                }

                Logger.LogWarning($"Failed login attempt for: {email}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Authentication exception for {email}", ex);
            }

            return null;
        }

        private static void UpgradePassword(SqlConnection conn, int userId, string plaintextPassword)
        {
            try
            {
                string newHash = SecurityHelper.HashPassword(plaintextPassword);
                string updateQuery = "UPDATE Users SET Password = @NewHash WHERE UserID = @UserID";

                using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                {
                    updateCmd.Parameters.AddWithValue("@NewHash", newHash);
                    updateCmd.Parameters.AddWithValue("@UserID", userId);
                    updateCmd.ExecuteNonQuery();
                    Logger.LogInfo($"Upgraded password to PBKDF2 hash for UserID: {userId}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to upgrade password for UserID: {userId}", ex);
            }
        }
    }
}
