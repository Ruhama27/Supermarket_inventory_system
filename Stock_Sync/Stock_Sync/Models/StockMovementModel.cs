using System;

namespace Stock_Sync.Models
{
    public class StockMovementModel
    {
        public int MovementId { get; set; }
        public long Barcode { get; set; }
        public string ProductName { get; set; }
        public int? StockId { get; set; }
        public string MovementType { get; set; } // "Sale", "Restock", "Damage", "Return", "Adjustment"
        public int QuantityChange { get; set; }
        public int? BalanceAfter { get; set; }
        public string ReferenceNo { get; set; }
        public string Remarks { get; set; }
        public string PerformedBy { get; set; }
        public DateTime MovementDate { get; set; } = DateTime.Now;
    }
}
