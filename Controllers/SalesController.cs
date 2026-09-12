using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.ViewModels;

namespace InventoryManagementSystem.Controllers
{
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const int PageSize = 10;

        public SalesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Sales  -> سجل المبيعات (Sales History) + بحث + Pagination
        public async Task<IActionResult> Index(string search, int page = 1)
        {
            var query = _context.Sales
                .Include(s => s.SaleItems)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s => s.CustomerInfo.Contains(search));
            }

            query = query.OrderByDescending(s => s.SaleDate);

            var totalItems = await query.CountAsync();
            var sales = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)PageSize));
            ViewBag.Search = search;

            return View(sales);
        }

        // GET: Sales/Details/5  -> فاتورة / تفاصيل عملية البيع
        public async Task<IActionResult> Details(int id)
        {
            var sale = await _context.Sales
                .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                .FirstOrDefaultAsync(s => s.SaleID == id);

            if (sale == null) return NotFound();

            return View(sale);
        }

        // GET: Sales/Create
        public IActionResult Create()
        {
            var vm = new SaleCreateViewModel();
            ReloadProductsDropdown();
            return View(vm);
        }

        // POST: Sales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SaleCreateViewModel vm)
        {
            // لازم يكون فيه صنف واحد على الأقل
            if (vm.Items == null || !vm.Items.Any())
            {
                ModelState.AddModelError("", "لازم تضيف منتج واحد على الأقل في عملية البيع");
            }

            // منع تكرار نفس المنتج في أكتر من سطر
            if (vm.Items != null && vm.Items.GroupBy(i => i.ProductID).Any(g => g.Count() > 1))
            {
                ModelState.AddModelError("", "فيه منتج متكرر أكتر من مرة، من فضلك ادمج الكمية في سطر واحد");
            }

            if (!ModelState.IsValid)
            {
                ReloadProductsDropdown();
                return View(vm);
            }

            // بنستخدم Transaction عشان عملية البيع وتحديث المخزون يتم سوا أو يتلغوا سوا
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var sale = new Sale
                {
                    SaleDate = DateTime.Now,
                    CustomerInfo = vm.CustomerInfo
                };

                foreach (var item in vm.Items)
                {
                    // بنجيب المنتج من الداتابيز عشان نتأكد من السعر والمخزون الحقيقي
                    // (متعتمدش على القيم اللي جايه من المتصفح لوحدها لأنها ممكن تتغير)
                    var product = await _context.Products
                        .FirstOrDefaultAsync(p => p.ProductID == item.ProductID);

                    if (product == null)
                    {
                        ModelState.AddModelError("", $"المنتج رقم {item.ProductID} مش موجود");
                        continue;
                    }

                    // ✅ الـ Validation المطلوب: الكمية المباعة متكونش أكبر من الـ Stock المتاح
                    if (item.Quantity > product.StockQuantity)
                    {
                        ModelState.AddModelError("",
                            $"الكمية المطلوبة من \"{product.ProductName}\" ({item.Quantity}) أكبر من المتاح في المخزون ({product.StockQuantity})");
                        continue;
                    }

                    sale.SaleItems.Add(new SaleItem
                    {
                        ProductID = product.ProductID,
                        Quantity = item.Quantity,
                        UnitPrice = product.UnitPrice
                    });

                    // ✅ تقليل الكمية من المخزون تلقائيًا بعد البيع
                    product.StockQuantity -= item.Quantity;
                }

                if (!ModelState.IsValid)
                {
                    await transaction.RollbackAsync();
                    ReloadProductsDropdown();
                    return View(vm);
                }

                // ✅ حساب الإجمالي الكلي أوتوماتيك
                sale.TotalAmount = sale.SaleItems.Sum(si => si.Quantity * si.UnitPrice);

                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "تم تسجيل عملية البيع بنجاح وتحديث المخزون";
                return RedirectToAction(nameof(Details), new { id = sale.SaleID });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "حصل خطأ أثناء حفظ عملية البيع، من فضلك حاول تاني");
                ReloadProductsDropdown();
                return View(vm);
            }
        }

        // GET: Sales/GetProductInfo/5
        // Endpoint بسيط بيرجع سعر ومخزون المنتج بصيغة JSON، بيستخدمه الـ JS في صفحة الإنشاء
        [HttpGet]
        public async Task<IActionResult> GetProductInfo(int id)
        {
            var product = await _context.Products
                .Where(p => p.ProductID == id)
                .Select(p => new { p.ProductID, p.ProductName, p.UnitPrice, p.StockQuantity })
                .FirstOrDefaultAsync();

            if (product == null) return NotFound();

            return Json(product);
        }

        private void ReloadProductsDropdown()
        {
            ViewBag.Products = _context.Products
                .OrderBy(p => p.ProductName)
                .Select(p => new { p.ProductID, p.ProductName, p.UnitPrice, p.StockQuantity })
                .ToList();
        }
    }
}
