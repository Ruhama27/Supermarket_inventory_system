using System.Collections.Generic;
using Stock_Sync.Models;

namespace Stock_Sync.Data
{
    public interface IStockRepository
    {
        IEnumerable<StockBatchModel> GetAllBatches(bool activeOnly = true);
        IEnumerable<StockBatchModel> GetBatchesByProductId(int productId);
        StockBatchModel GetBatchById(int stockId);
        bool AddBatch(StockBatchModel batch, out string errorMessage);
        bool UpdateBatch(StockBatchModel batch, out string errorMessage);
        bool DeleteBatch(int stockId, out string errorMessage);
        int GetTotalStockByBarcode(long barcode);
        decimal GetActiveSellingPrice(long barcode);
        IEnumerable<StockBatchModel> GetLowStockBatches();
        IEnumerable<StockBatchModel> GetExpiredBatches();
        bool Restock(long barcode, int additionalQuantity, string stockCode, out string errorMessage);
    }
}
