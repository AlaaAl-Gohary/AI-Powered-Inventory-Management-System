using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Controllers
{
    // Person 2 - Suppliers & Purchases
    public class SuppliersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const int DefaultPageSize = 10;

        public SuppliersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Suppliers?search=&page=1
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            var query = _context.Suppliers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(s =>
                    s.SupplierName.Contains(search) ||
                    s.ContactName.Contains(search) ||
                    s.Email.Contains(search) ||
                    s.Phone.Contains(search));
            }

            var totalCount = await query.CountAsync();
            page = page < 1 ? 1 : page;

            var suppliers = await query
                .OrderBy(s => s.SupplierName)
                .Skip((page - 1) * DefaultPageSize)
                .Take(DefaultPageSize)
                .ToListAsync();

            var vm = new SupplierListViewModel
            {
                Suppliers = suppliers,
                SearchTerm = search,
                PageNumber = page,
                PageSize = DefaultPageSize,
                TotalCount = totalCount
            };

            return View(vm);
        }

        // GET: /Suppliers/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var supplier = await _context.Suppliers
                .Include(s => s.Purchases)
                .FirstOrDefaultAsync(s => s.SupplierID == id);

            if (supplier == null) return NotFound();

            return View(supplier);
        }

        // GET: /Suppliers/Products/5  
        
        public async Task<IActionResult> Products(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();

            var products = await _context.PurchaseItems
                .Where(pi => pi.Purchase!.SupplierID == id)
                .Select(pi => pi.Product!)
                .Distinct()
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            ViewBag.Supplier = supplier;
            return View(products);
        }

        // GET: /Suppliers/Create
        public IActionResult Create() => View(new Supplier());

        // POST: /Suppliers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            if (!ModelState.IsValid) return View(supplier);

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Supplier created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Suppliers/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();
            return View(supplier);
        }

        // POST: /Suppliers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Supplier supplier)
        {
            if (id != supplier.SupplierID) return NotFound();
            if (!ModelState.IsValid) return View(supplier);

            try
            {
                _context.Update(supplier);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Suppliers.Any(s => s.SupplierID == id)) return NotFound();
                throw;
            }

            TempData["Success"] = "Supplier updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Suppliers/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();
            return View(supplier);
        }

        // POST: /Suppliers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();

            var hasPurchases = await _context.Purchases.AnyAsync(p => p.SupplierID == id);
            if (hasPurchases)
            {
                TempData["Error"] = "Cannot delete this supplier: they have existing purchase records.";
                return RedirectToAction(nameof(Index));
            }

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Supplier deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
