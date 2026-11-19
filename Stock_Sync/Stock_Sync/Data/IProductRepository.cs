using System.Collections.Generic;
using Stock_Sync.Models;

namespace Stock_Sync.Data
{
    public interface IProductRepository
    {
        IEnumerable<ProductModel> GetAll(bool activeOnly = true);
        ProductModel GetById(int id);
        ProductModel GetByBarcode(long barcode);
        bool BarcodeExists(long barcode, int? excludeId = null);
        bool Add(ProductModel product, out string errorMessage);
        bool Update(ProductModel product, out string errorMessage);
        bool Delete(int id, out string errorMessage);
        IEnumerable<string> GetDistinctCategories();
    }
}
