using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace InventoryManagementSystem.ViewModels
{
    public class SaleCreateViewModel
    {
        [Required(ErrorMessage = "من فضلك أدخل بيانات العميل")]
        [Display(Name = "بيانات العميل")]
        public string CustomerInfo { get; set; }

        public List<SaleItemInputViewModel> Items { get; set; } = new List<SaleItemInputViewModel>();

        public decimal TotalAmount => Items?.Sum(i => i.Quantity * i.UnitPrice) ?? 0;
    }
}
