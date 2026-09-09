using System.Collections.Generic;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.ViewModels
{
    public class CategoryListViewModel
    {
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();

        public string? SearchTerm { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public int TotalCount { get; set; }

        public int TotalPages =>
            (int)System.Math.Ceiling(TotalCount / (double)PageSize);

        public bool HasPreviousPage =>
            PageNumber > 1;

        public bool HasNextPage =>
            PageNumber < TotalPages;
    }
}