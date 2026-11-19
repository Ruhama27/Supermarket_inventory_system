using System;

namespace Stock_Sync.Models
{
    public class StockBatchModel
    {
        public int StockId { get; set; }
        public int BarcodeId { get; set; }
        public long Barcode { get; set; }
        public string ProductName { get; set; }
        public string StockCode { get; set; }
        public int? SupplierId { get; set; }
        public string SupplierName { get; set; }
        public int Quantity { get; set; }
        public int MinimumStock { get; set; } = 5;
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public DateTime ArrivalDate { get; set; } = DateTime.Today;
        public DateTime? ExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime DateAdded { get; set; } = DateTime.Now;

        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value.Date <= DateTime.Today;
        public bool IsLowStock => Quantity <= MinimumStock;
    }
}
