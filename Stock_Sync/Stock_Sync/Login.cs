using System;
using System.Data.SqlClient;

namespace Stock_Sync
{
    public class Login
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }

        /// <summary>
        /// Authenticates the user with their plain text Username (or Email) and hashed Password.
        /// The username is queried as plain text and is NEVER hashed.
        /// Only the password is verified and hashed using PBKDF2.
        /// </summary>
        public static Login Authenticate(string usernameInput, string password)
        {
            if (string.IsNullOrWhiteSpace(usernameInput) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();

                    // Ensure Username column exists in the database
                    EnsureUsernameColumn(conn);

                    // Note: Username is matched as plain text (NOT hashed). Only the password is verified/hashed.
                    string query = @"
                        SELECT UserID, 
                               ISNULL(Username, Email) AS Username, 
                               Email, 
                               Password, 
                               Role 
                        FROM Users 
                        WHERE Username = @Username OR Email = @Username";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        // Username is strictly plain text, NEVER hashed
                        cmd.Parameters.AddWithValue("@Username", usernameInput.Trim());

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userId = Convert.ToInt32(reader["UserID"]);
                                string username = reader["Username"].ToString();
                                string email = reader["Email"].ToString();
                                string storedPassword = reader["Password"].ToString();
                                string role = reader["Role"].ToString();

                                // Verify ONLY the password (PBKDF2 hash or legacy plaintext upgrade)
                                bool isValid = SecurityHelper.VerifyPassword(password, storedPassword, out bool needsUpgrade);

                                if (isValid)
                                {
                                    reader.Close();

                                    // Automatic migration of plaintext passwords to PBKDF2 hash (passwords ONLY!)
                                    if (needsUpgrade)
                                    {
                                        UpgradePassword(conn, userId, password);
                                    }

                                    Logger.LogInfo($"User logged in successfully: {username} ({role})");

                                    return new Login
                                    {
                                        UserId = userId,
                                        Username = username,
                                        Email = email,
                                        Role = role
                                    };
                                }
                            }
                        }
                    }
                }

                Logger.LogWarning($"Failed login attempt for: {usernameInput}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Authentication exception for {usernameInput}", ex);
            }

            return null;
        }

        private static void EnsureUsernameColumn(SqlConnection conn)
        {
            try
            {
                string checkQuery = @"
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'Username')
                    BEGIN
                        ALTER TABLE Users ADD Username NVARCHAR(100) NULL;
                    END";
                using (SqlCommand cmd = new SqlCommand(checkQuery, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // Populate default usernames if null or empty
                string populateQuery = @"
                    UPDATE Users 
                    SET Username = CASE 
                        WHEN Email = 'admin@gmail.com' THEN 'admin'
                        WHEN Email = 'staff@gmail.com' THEN 'staff'
                        ELSE SUBSTRING(Email, 1, CHARINDEX('@', Email + '@') - 1)
                    END 
                    WHERE Username IS NULL OR Username = '';";
                using (SqlCommand cmd = new SqlCommand(populateQuery, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not verify/add Username column in Users table: {ex.Message}");
            }
        }

        private static void UpgradePassword(SqlConnection conn, int userId, string plaintextPassword)
        {
            try
            {
                // Only the password is hashed with PBKDF2
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
