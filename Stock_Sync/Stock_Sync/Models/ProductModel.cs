using System;

namespace Stock_Sync.Models
{
    public class ProductModel
    {
        public int Id { get; set; }
        public long Barcode { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public int? CategoryId { get; set; }
        public string Brand { get; set; }
        public string Unit { get; set; } = "Pcs";
        public string Status { get; set; } = "Available";
        public bool IsActive { get; set; } = true;
        public DateTime DateAdded { get; set; } = DateTime.Now;

        // Computed / Aggregated properties
        public decimal CurrentSellingPrice { get; set; }
        public decimal CurrentCostPrice { get; set; }
        public int TotalAvailableQuantity { get; set; }
        public int MinimumStockThreshold { get; set; } = 5;

        public bool IsOutOfStock => TotalAvailableQuantity <= 0;
        public bool IsLowStock => TotalAvailableQuantity > 0 && TotalAvailableQuantity <= MinimumStockThreshold;
    }
}
