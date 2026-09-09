using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.Models
{
    
    public class Supplier
    {
        [Key]
        public int SupplierID { get; set; }

        [Required(ErrorMessage = "Supplier name is required.")]
        [StringLength(150, ErrorMessage = "Supplier name cannot exceed 150 characters.")]
        [Display(Name = "Supplier Name")]
        public string SupplierName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact name is required.")]
        [StringLength(100)]
        [Display(Name = "Contact Name")]
        public string ContactName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(30)]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(250)]
        public string Address { get; set; } = string.Empty;

        public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    }
}
