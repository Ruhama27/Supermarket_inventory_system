using System.Collections.Generic;
using Stock_Sync.Models;

namespace Stock_Sync.Data
{
    public interface ISupplierRepository
    {
        IEnumerable<SupplierModel> GetAll(bool activeOnly = true);
        SupplierModel GetById(int id);
        bool Add(SupplierModel supplier, out string errorMessage);
        bool Update(SupplierModel supplier, out string errorMessage);
        bool Delete(int id, out string errorMessage);
    }
}
