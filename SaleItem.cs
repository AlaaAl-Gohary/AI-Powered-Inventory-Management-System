using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementSystem.Models
{
    public class SaleItem
    {
        [Key]
        public int SaleItemID { get; set; }

        [Required]
        public int SaleID { get; set; }

        [ForeignKey(nameof(SaleID))]
        public Sale Sale { get; set; }

        [Required]
        public int ProductID { get; set; }

        [ForeignKey(nameof(ProductID))]
        public Product Product { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "الكمية لازم تكون أكبر من صفر")]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [NotMapped]
        public decimal LineTotal => Quantity * UnitPrice;
    }
}
