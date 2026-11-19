using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Stock_Sync.Models;

namespace Stock_Sync.Data
{
    public class SupplierRepository : ISupplierRepository
    {
        public IEnumerable<SupplierModel> GetAll(bool activeOnly = true)
        {
            var list = new List<SupplierModel>();
            string query = @"
                SELECT SupplierID, SupplierName, ContactPerson, Phone, Email, Address, IsActive, DateAdded
                FROM Suppliers
                WHERE (@ActiveOnly = 0 OR IsActive = 1)
                ORDER BY SupplierName ASC";

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
                                list.Add(MapSupplier(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in SupplierRepository.GetAll", ex);
            }
            return list;
        }

        public SupplierModel GetById(int id)
        {
            string query = @"
                SELECT SupplierID, SupplierName, ContactPerson, Phone, Email, Address, IsActive, DateAdded
                FROM Suppliers
                WHERE SupplierID = @ID";

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", id);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return MapSupplier(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in SupplierRepository.GetById ({id})", ex);
            }
            return null;
        }

        public bool Add(SupplierModel supplier, out string errorMessage)
        {
            errorMessage = "";
            if (supplier == null || string.IsNullOrWhiteSpace(supplier.SupplierName))
            {
                errorMessage = "Supplier name is required.";
                return false;
            }

            string query = @"
                INSERT INTO Suppliers (SupplierName, ContactPerson, Phone, Email, Address, IsActive, DateAdded)
                OUTPUT INSERTED.SupplierID
                VALUES (@Name, @Contact, @Phone, @Email, @Address, 1, GETDATE())";

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", supplier.SupplierName.Trim());
                        cmd.Parameters.AddWithValue("@Contact", (object)supplier.ContactPerson ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Phone", (object)supplier.Phone ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", (object)supplier.Email ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Address", (object)supplier.Address ?? DBNull.Value);

                        supplier.SupplierId = Convert.ToInt32(cmd.ExecuteScalar());
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                Logger.LogError("Error in SupplierRepository.Add", ex);
                return false;
            }
        }

        public bool Update(SupplierModel supplier, out string errorMessage)
        {
            errorMessage = "";
            if (supplier == null || supplier.SupplierId <= 0)
            {
                errorMessage = "Invalid supplier ID.";
                return false;
            }

            string query = @"
                UPDATE Suppliers
                SET SupplierName = @Name,
                    ContactPerson = @Contact,
                    Phone = @Phone,
                    Email = @Email,
                    Address = @Address,
                    IsActive = @IsActive
                WHERE SupplierID = @ID";

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", supplier.SupplierId);
                        cmd.Parameters.AddWithValue("@Name", supplier.SupplierName.Trim());
                        cmd.Parameters.AddWithValue("@Contact", (object)supplier.ContactPerson ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Phone", (object)supplier.Phone ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", (object)supplier.Email ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Address", (object)supplier.Address ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", supplier.IsActive ? 1 : 0);

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                Logger.LogError($"Error in SupplierRepository.Update ({supplier.SupplierId})", ex);
                return false;
            }
        }

        public bool Delete(int id, out string errorMessage)
        {
            errorMessage = "";
            string query = "UPDATE Suppliers SET IsActive = 0 WHERE SupplierID = @ID";
            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", id);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                Logger.LogError($"Error in SupplierRepository.Delete ({id})", ex);
                return false;
            }
        }

        private static SupplierModel MapSupplier(SqlDataReader reader)
        {
            return new SupplierModel
            {
                SupplierId = Convert.ToInt32(reader["SupplierID"]),
                SupplierName = reader["SupplierName"].ToString(),
                ContactPerson = reader["ContactPerson"] == DBNull.Value ? "" : reader["ContactPerson"].ToString(),
                Phone = reader["Phone"] == DBNull.Value ? "" : reader["Phone"].ToString(),
                Email = reader["Email"] == DBNull.Value ? "" : reader["Email"].ToString(),
                Address = reader["Address"] == DBNull.Value ? "" : reader["Address"].ToString(),
                IsActive = Convert.ToBoolean(reader["IsActive"]),
                DateAdded = Convert.ToDateTime(reader["DateAdded"])
            };
        }
    }
}
