using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const int DefaultPageSize = 10;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Categories?search=&page=1
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            var query = _context.Categories
                .Include(c => c.Products)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(c =>
                    c.CategoryName.Contains(search));
            }

            var totalCount = await query.CountAsync();

            page = page < 1 ? 1 : page;

            var categories = await query
                .OrderBy(c => c.CategoryName)
                .Skip((page - 1) * DefaultPageSize)
                .Take(DefaultPageSize)
                .ToListAsync();

            var vm = new CategoryListViewModel
            {
                Categories = categories,
                SearchTerm = search,
                PageNumber = page,
                PageSize = DefaultPageSize,
                TotalCount = totalCount
            };

            return View(vm);
        }

        // GET: /Categories/Create
        public IActionResult Create()
        {
            return View(new CategoryCreateViewModel());
        }

        // POST: /Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var category = new Category
            {
                CategoryName = vm.CategoryName.Trim(),
                Description = vm.Description?.Trim()
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Category created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: /Categories/Details/5
public async Task<IActionResult> Details(int id)
{
    var category = await _context.Categories
        .Include(c => c.Products)
        .FirstOrDefaultAsync(c => c.CategoryID == id);

    if (category == null)
        return NotFound();

    return View(category);
}

// GET: /Categories/Edit/5
public async Task<IActionResult> Edit(int id)
{
    var category = await _context.Categories.FindAsync(id);

    if (category == null)
        return NotFound();

    var vm = new CategoryCreateViewModel
    {
        CategoryName = category.CategoryName,
        Description = category.Description
    };

    ViewBag.CategoryID = category.CategoryID;

    return View(vm);
}

// POST: /Categories/Edit/5
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, CategoryCreateViewModel vm)
{
    if (!ModelState.IsValid)
    {
        ViewBag.CategoryID = id;
        return View(vm);
    }

    var category = await _context.Categories.FindAsync(id);

    if (category == null)
        return NotFound();

    category.CategoryName = vm.CategoryName.Trim();
    category.Description = vm.Description?.Trim();

    await _context.SaveChangesAsync();

    TempData["Success"] = "Category updated successfully.";

    return RedirectToAction(nameof(Index));
}

// GET: /Categories/Delete/5
public async Task<IActionResult> Delete(int id)
{
    var category = await _context.Categories
        .Include(c => c.Products)
        .FirstOrDefaultAsync(c => c.CategoryID == id);

    if (category == null)
        return NotFound();

    return View(category);
}

// POST: /Categories/Delete/5
[HttpPost, ActionName("Delete")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteConfirmed(int id)
{
    var category = await _context.Categories.FindAsync(id);

    if (category == null)
        return NotFound();

    var hasProducts = await _context.Products
        .AnyAsync(p => p.CategoryID == id);

    if (hasProducts)
    {
        TempData["Error"] =
            "Cannot delete this category: it has existing products.";

        return RedirectToAction(nameof(Index));
    }

    _context.Categories.Remove(category);
    await _context.SaveChangesAsync();

    TempData["Success"] = "Category deleted successfully.";

    return RedirectToAction(nameof(Index));
}

// GET: /Categories/Products/5
public async Task<IActionResult> Products(int id)
{
    var category = await _context.Categories
        .FirstOrDefaultAsync(c => c.CategoryID == id);

    if (category == null)
        return NotFound();

    var products = await _context.Products
        .Where(p => p.CategoryID == id)
        .OrderBy(p => p.ProductName)
        .ToListAsync();

    ViewBag.Category = category;

    return View(products);
}
    }

    
}
