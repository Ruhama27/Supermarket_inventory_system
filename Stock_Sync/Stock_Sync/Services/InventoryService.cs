using System;
using System.Collections.Generic;
using System.Linq;
using Stock_Sync.Data;
using Stock_Sync.Models;

namespace Stock_Sync.Services
{
    public class DashboardSummary
    {
        public int TotalActiveProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int ExpiredBatches { get; set; }
        public decimal TotalStockInventoryValue { get; set; }
    }

    public class InventoryService
    {
        private readonly IProductRepository _productRepo;
        private readonly IStockRepository _stockRepo;
        private readonly IStockMovementRepository _movementRepo;
        private readonly ISupplierRepository _supplierRepo;

        public InventoryService(
            IProductRepository productRepo = null,
            IStockRepository stockRepo = null,
            IStockMovementRepository movementRepo = null,
            ISupplierRepository supplierRepo = null)
        {
            _productRepo = productRepo ?? new ProductRepository();
            _stockRepo = stockRepo ?? new StockRepository();
            _movementRepo = movementRepo ?? new StockMovementRepository();
            _supplierRepo = supplierRepo ?? new SupplierRepository();
        }

        public IProductRepository Products => _productRepo;
        public IStockRepository Stocks => _stockRepo;
        public IStockMovementRepository Movements => _movementRepo;
        public ISupplierRepository Suppliers => _supplierRepo;

        public DashboardSummary GetDashboardSummary()
        {
            var summary = new DashboardSummary();
            try
            {
                var products = _productRepo.GetAll(activeOnly: true).ToList();
                summary.TotalActiveProducts = products.Count;
                summary.OutOfStockProducts = products.Count(p => p.IsOutOfStock);
                summary.LowStockProducts = products.Count(p => p.IsLowStock);

                var expiredBatches = _stockRepo.GetExpiredBatches().ToList();
                summary.ExpiredBatches = expiredBatches.Count;

                var batches = _stockRepo.GetAllBatches(activeOnly: true);
                summary.TotalStockInventoryValue = batches.Sum(b => b.Quantity * b.SellingPrice);
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in InventoryService.GetDashboardSummary", ex);
            }
            return summary;
        }

        public bool RecordRestock(long barcode, int quantity, string stockCode, string performedBy, out string error)
        {
            error = "";
            if (quantity <= 0)
            {
                error = "Restock quantity must be positive.";
                return false;
            }

            var prod = _productRepo.GetByBarcode(barcode);
            if (prod == null)
            {
                error = $"Product with barcode {barcode} does not exist.";
                return false;
            }

            bool success = _stockRepo.Restock(barcode, quantity, stockCode, out error);
            if (success)
            {
                int newBalance = _stockRepo.GetTotalStockByBarcode(barcode);
                _movementRepo.LogMovement(
                    barcode, 
                    null, 
                    "Restock", 
                    quantity, 
                    newBalance, 
                    stockCode ?? "RESTOCK", 
                    $"Restocked +{quantity} units", 
                    performedBy);
            }
            return success;
        }

        public bool RecordDamageOrLoss(long barcode, int stockId, int quantity, string remarks, string performedBy, out string error)
        {
            error = "";
            if (quantity <= 0)
            {
                error = "Quantity must be positive.";
                return false;
            }

            var batch = _stockRepo.GetBatchById(stockId);
            if (batch == null)
            {
                error = "Stock batch not found.";
                return false;
            }

            if (batch.Quantity < quantity)
            {
                error = $"Cannot deduct {quantity} units. Batch only has {batch.Quantity} units.";
                return false;
            }

            batch.Quantity -= quantity;
            bool success = _stockRepo.UpdateBatch(batch, out error);
            if (success)
            {
                int newBalance = _stockRepo.GetTotalStockByBarcode(barcode);
                _movementRepo.LogMovement(
                    barcode, 
                    stockId, 
                    "Damage", 
                    -quantity, 
                    newBalance, 
                    "DAMAGE-LOSS", 
                    remarks ?? "Damaged / Expired stock write-off", 
                    performedBy);
            }
            return success;
        }
    }
}
