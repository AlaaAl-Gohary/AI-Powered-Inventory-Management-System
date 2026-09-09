using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.ViewModels
{
    public class CategoryCreateViewModel
    {
        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }
}