using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const int DefaultPageSize = 10;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Products?search=&categoryId=&status=&page=1
        public async Task<IActionResult> Index(
            string? search,
            int? categoryId,
            string? status,
            int page = 1)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(p =>
                    p.ProductName.Contains(search) ||
                    p.SKU.Contains(search));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p =>
                    p.CategoryID == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                status = status.Trim();

                if (status == "In Stock")
                {
                    query = query.Where(p =>
                        p.StockQuantity > p.LowStockThreshold);
                }
                else if (status == "Low Stock")
                {
                    query = query.Where(p =>
                        p.StockQuantity > 0 &&
                        p.StockQuantity <= p.LowStockThreshold);
                }
                else if (status == "Out of Stock")
                {
                    query = query.Where(p =>
                        p.StockQuantity == 0);
                }
            }

            var totalCount = await query.CountAsync();

            page = page < 1 ? 1 : page;

            var products = await query
                .OrderBy(p => p.ProductName)
                .Skip((page - 1) * DefaultPageSize)
                .Take(DefaultPageSize)
                .ToListAsync();

            var categories = await _context.Categories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            var vm = new ProductListViewModel
            {
                Products = products,
                SearchTerm = search,
                CategoryId = categoryId,
                Status = status,
                PageNumber = page,
                PageSize = DefaultPageSize,
                TotalCount = totalCount
            };

            ViewBag.Categories = categories;

            return View(vm);
        }

        // GET: /Products/Details/5
public async Task<IActionResult> Details(int id)
{
    var product = await _context.Products
        .Include(p => p.Category)
        .FirstOrDefaultAsync(p => p.ProductID == id);

    if (product == null)
        return NotFound();

    return View(product);
}

// GET: /Products/Edit/5
public async Task<IActionResult> Edit(int id)
{
    var product = await _context.Products.FindAsync(id);

    if (product == null)
        return NotFound();

    var categories = await _context.Categories
        .OrderBy(c => c.CategoryName)
        .ToListAsync();

    ViewBag.Categories = categories;

    var vm = new ProductCreateViewModel
    {
        SKU = product.SKU,
        ProductName = product.ProductName,
        CategoryID = product.CategoryID,
        UnitPrice = product.UnitPrice,
        StockQuantity = product.StockQuantity,
        LowStockThreshold = product.LowStockThreshold
    };

    ViewBag.ProductID = product.ProductID;

    return View(vm);
}

// POST: /Products/Edit/5
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, ProductCreateViewModel vm)
{
    if (!ModelState.IsValid)
    {
        ViewBag.Categories = await _context.Categories
            .OrderBy(c => c.CategoryName)
            .ToListAsync();

        ViewBag.ProductID = id;

        return View(vm);
    }

    var product = await _context.Products.FindAsync(id);

    if (product == null)
        return NotFound();

    product.SKU = vm.SKU.Trim();
    product.ProductName = vm.ProductName.Trim();
    product.CategoryID = vm.CategoryID;
    product.UnitPrice = vm.UnitPrice;
    product.StockQuantity = vm.StockQuantity;
    product.LowStockThreshold = vm.LowStockThreshold;

    await _context.SaveChangesAsync();

    TempData["Success"] = "Product updated successfully.";

    return RedirectToAction(nameof(Index));
}

// GET: /Products/Delete/5
public async Task<IActionResult> Delete(int id)
{
    var product = await _context.Products
        .Include(p => p.Category)
        .FirstOrDefaultAsync(p => p.ProductID == id);

    if (product == null)
        return NotFound();

    return View(product);
}

// POST: /Products/Delete/5
[HttpPost, ActionName("Delete")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteConfirmed(int id)
{
    var product = await _context.Products.FindAsync(id);

    if (product == null)
        return NotFound();

    var hasPurchaseItems = await _context.PurchaseItems
        .AnyAsync(pi => pi.ProductID == id);

    if (hasPurchaseItems)
    {
        TempData["Error"] =
            "Cannot delete this product: it has existing purchase records.";

        return RedirectToAction(nameof(Index));
    }

    _context.Products.Remove(product);
    await _context.SaveChangesAsync();

    TempData["Success"] = "Product deleted successfully.";

    return RedirectToAction(nameof(Index));
}

        // GET: /Products/Create
public async Task<IActionResult> Create()
{
    var categories = await _context.Categories
        .OrderBy(c => c.CategoryName)
        .ToListAsync();

    ViewBag.Categories = categories;

    return View(new ProductCreateViewModel());
}

// POST: /Products/Create
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(ProductCreateViewModel vm)
{
    if (!ModelState.IsValid)
    {
        ViewBag.Categories = await _context.Categories
            .OrderBy(c => c.CategoryName)
            .ToListAsync();

        return View(vm);
    }

    var product = new Product
    {
        SKU = vm.SKU.Trim(),
        ProductName = vm.ProductName.Trim(),
        CategoryID = vm.CategoryID,
        UnitPrice = vm.UnitPrice,
        StockQuantity = vm.StockQuantity,
        LowStockThreshold = vm.LowStockThreshold
    };

    _context.Products.Add(product);
    await _context.SaveChangesAsync();

    TempData["Success"] = "Product created successfully.";

    return RedirectToAction(nameof(Index));
}
    }

    

    
}