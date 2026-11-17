using System;

namespace Stock_Sync
{
    public static class Session
    {
        public static int UserId { get; set; }
        public static string Email { get; set; }
        public static string Role { get; set; }
        public static DateTime LoginTime { get; set; }
        public static DateTime LastActivityTime { get; set; }

        public static bool IsLoggedIn => !string.IsNullOrEmpty(Email);
        public static bool IsAdmin => string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);
        public static bool IsStaff => string.Equals(Role, "Staff", StringComparison.OrdinalIgnoreCase);

        public static void StartSession(int userId, string email, string role)
        {
            UserId = userId;
            Email = email;
            Role = role;
            LoginTime = DateTime.Now;
            LastActivityTime = DateTime.Now;
        }

        public static void RecordActivity()
        {
            LastActivityTime = DateTime.Now;
        }

        public static bool HasTimedOut(int timeoutMinutes = 30)
        {
            if (!IsLoggedIn) return false;
            return (DateTime.Now - LastActivityTime).TotalMinutes >= timeoutMinutes;
        }

        public static void Clear()
        {
            UserId = 0;
            Email = null;
            Role = null;
            LoginTime = DateTime.MinValue;
            LastActivityTime = DateTime.MinValue;
        }
    }
}
