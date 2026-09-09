using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.ViewModels
{
    public class ProductCreateViewModel
    {
        [Required]
        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public int CategoryID { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        [Range(0, int.MaxValue)]
        public int LowStockThreshold { get; set; }
    }
}