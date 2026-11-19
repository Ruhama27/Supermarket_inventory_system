using System.Collections.Generic;
using Stock_Sync.Models;

namespace Stock_Sync.Data
{
    public interface IStockMovementRepository
    {
        void LogMovement(long barcode, int? stockId, string movementType, int qtyChange, int? balanceAfter, string referenceNo, string remarks, string performedBy);
        IEnumerable<StockMovementModel> GetRecentMovements(int count = 50);
        IEnumerable<StockMovementModel> GetMovementsByBarcode(long barcode);
    }
}
