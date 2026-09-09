using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.ViewModels
{
    public class PurchaseCreateViewModel
    {
        [Required(ErrorMessage = "Please select a supplier.")]
        [Display(Name = "Supplier")]
        public int SupplierID { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Purchase Date")]
        public DateTime PurchaseDate { get; set; } = DateTime.Today;

        [MinLength(1, ErrorMessage = "Add at least one purchase item.")]
        public List<PurchaseItemInputModel> Items { get; set; } = new List<PurchaseItemInputModel>
        {
            new PurchaseItemInputModel()
        };

        public List<SelectOption> SupplierOptions { get; set; } = new();
        public List<SelectOption> ProductOptions { get; set; } = new();
    }

    public class PurchaseItemInputModel
    {
        [Required(ErrorMessage = "Select a product.")]
        [Display(Name = "Product")]
        public int ProductID { get; set; }

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; } = 1;

        [Required(ErrorMessage = "Unit cost is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit cost must be greater than 0.")]
        [Display(Name = "Unit Cost")]
        public decimal UnitCost { get; set; }
    }

    public class SelectOption
    {
        public int Value { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
