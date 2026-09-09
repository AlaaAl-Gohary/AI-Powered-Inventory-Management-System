using System.Collections.Generic;

namespace InventoryManagementSystem.Models
{
    
    public class Product
    {
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;

        public int CategoryID { get; set; }
        public Category? Category { get; set; }

        public decimal UnitPrice { get; set; }
        public int StockQuantity { get; set; }
        public int LowStockThreshold { get; set; }

        public ICollection<PurchaseItem> PurchaseItems { get; set; } = new List<PurchaseItem>();
    }
}
