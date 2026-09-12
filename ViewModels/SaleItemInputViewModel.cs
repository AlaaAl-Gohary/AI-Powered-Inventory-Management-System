using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.ViewModels
{
    public class SaleItemInputViewModel
    {
        [Required(ErrorMessage = "اختار منتج")]
        public int ProductID { get; set; }

        [Required(ErrorMessage = "أدخل الكمية")]
        [Range(1, int.MaxValue, ErrorMessage = "الكمية لازم تكون رقم أكبر من صفر")]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }
    }
}
