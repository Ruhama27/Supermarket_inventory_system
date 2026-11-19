using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Stock_Sync.Models;

namespace Stock_Sync.Data
{
    public class StockMovementRepository : IStockMovementRepository
    {
        public void LogMovement(long barcode, int? stockId, string movementType, int qtyChange, int? balanceAfter, string referenceNo, string remarks, string performedBy)
        {
            string query = @"
                INSERT INTO StockMovements (Barcode, StockID, MovementType, QuantityChange, BalanceAfter, ReferenceNo, Remarks, PerformedBy, MovementDate)
                VALUES (@Barcode, @StockID, @Type, @Change, @Balance, @Ref, @Remarks, @User, GETDATE())";

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Barcode", barcode);
                        cmd.Parameters.AddWithValue("@StockID", (object)stockId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Type", movementType);
                        cmd.Parameters.AddWithValue("@Change", qtyChange);
                        cmd.Parameters.AddWithValue("@Balance", (object)balanceAfter ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Ref", (object)referenceNo ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Remarks", (object)remarks ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@User", (object)performedBy ?? (Session.Username ?? "System"));

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in StockMovementRepository.LogMovement", ex);
            }
        }

        public IEnumerable<StockMovementModel> GetRecentMovements(int count = 50)
        {
            var list = new List<StockMovementModel>();
            string query = $@"
                SELECT TOP ({count})
                    sm.MovementID, sm.Barcode, ISNULL(p.PName, 'Unknown') AS ProductName,
                    sm.StockID, sm.MovementType, sm.QuantityChange, sm.BalanceAfter,
                    sm.ReferenceNo, sm.Remarks, sm.PerformedBy, sm.MovementDate
                FROM StockMovements sm
                LEFT JOIN Products p ON sm.Barcode = p.Barcode
                ORDER BY sm.MovementDate DESC";

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
                                list.Add(MapMovement(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in StockMovementRepository.GetRecentMovements", ex);
            }
            return list;
        }

        public IEnumerable<StockMovementModel> GetMovementsByBarcode(long barcode)
        {
            var list = new List<StockMovementModel>();
            string query = @"
                SELECT 
                    sm.MovementID, sm.Barcode, ISNULL(p.PName, 'Unknown') AS ProductName,
                    sm.StockID, sm.MovementType, sm.QuantityChange, sm.BalanceAfter,
                    sm.ReferenceNo, sm.Remarks, sm.PerformedBy, sm.MovementDate
                FROM StockMovements sm
                LEFT JOIN Products p ON sm.Barcode = p.Barcode
                WHERE sm.Barcode = @Barcode
                ORDER BY sm.MovementDate DESC";

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
                            while (reader.Read())
                            {
                                list.Add(MapMovement(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in StockMovementRepository.GetMovementsByBarcode ({barcode})", ex);
            }
            return list;
        }

        private static StockMovementModel MapMovement(SqlDataReader reader)
        {
            return new StockMovementModel
            {
                MovementId = Convert.ToInt32(reader["MovementID"]),
                Barcode = Convert.ToInt64(reader["Barcode"]),
                ProductName = reader["ProductName"].ToString(),
                StockId = reader["StockID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["StockID"]),
                MovementType = reader["MovementType"].ToString(),
                QuantityChange = Convert.ToInt32(reader["QuantityChange"]),
                BalanceAfter = reader["BalanceAfter"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["BalanceAfter"]),
                ReferenceNo = reader["ReferenceNo"] == DBNull.Value ? "" : reader["ReferenceNo"].ToString(),
                Remarks = reader["Remarks"] == DBNull.Value ? "" : reader["Remarks"].ToString(),
                PerformedBy = reader["PerformedBy"] == DBNull.Value ? "System" : reader["PerformedBy"].ToString(),
                MovementDate = Convert.ToDateTime(reader["MovementDate"])
            };
        }
    }
}
