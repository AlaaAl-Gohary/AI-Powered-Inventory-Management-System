using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementSystem.Models
{
    
    public class PurchaseItem
    {
        [Key]
        public int PurchaseItemID { get; set; }

        [Required]
        public int PurchaseID { get; set; }

        [ForeignKey(nameof(PurchaseID))]
        public Purchase? Purchase { get; set; }

        [Required(ErrorMessage = "Please select a product.")]
        [Display(Name = "Product")]
        public int ProductID { get; set; }

        [ForeignKey(nameof(ProductID))]
        public Product? Product { get; set; }

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Unit cost is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit cost must be greater than 0.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Unit Cost")]
        public decimal UnitCost { get; set; }

        [NotMapped]
        public decimal LineTotal => Quantity * UnitCost;
    }
}
