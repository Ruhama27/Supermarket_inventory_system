using System;
using System.Configuration;
using System.Data.SqlClient;

namespace Stock_Sync
{
    public static class DBHelper
    {
        private static readonly string defaultConnectionString = 
            "Data Source=.\\SQLEXPRESS;Initial Catalog=InventoryDB;Integrated Security=True;TrustServerCertificate=True";

        public static string ConnectionString
        {
            get
            {
                try
                {
                    string configured = ConfigurationManager.ConnectionStrings["InventoryDB"]?.ConnectionString;
                    if (!string.IsNullOrWhiteSpace(configured))
                    {
                        return configured;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Failed to read connection string from App.config: {ex.Message}. Using default.");
                }

                return defaultConnectionString;
            }
        }

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }
    }
}
