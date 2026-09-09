using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Controllers
{
    // Person 2 - Suppliers & Purchases
    public class PurchasesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const int DefaultPageSize = 10;

        
        private const string TempDataFormKey = "Purchase_PendingForm";
        private const string TempDataErrorsKey = "Purchase_PendingErrors";

        public PurchasesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Purchases  -> Purchase History
        public async Task<IActionResult> Index(int page = 1)
        {
            page = page < 1 ? 1 : page;

            var totalCount = await _context.Purchases.CountAsync();

            var purchases = await _context.Purchases
                .Include(p => p.Supplier)
                .OrderByDescending(p => p.PurchaseDate)
                .Skip((page - 1) * DefaultPageSize)
                .Take(DefaultPageSize)
                .ToListAsync();

            ViewBag.PageNumber = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)DefaultPageSize);

            return View(purchases);
        }

        // GET: /Purchases/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var purchase = await _context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.PurchaseItems)
                    .ThenInclude(pi => pi.Product)
                .FirstOrDefaultAsync(p => p.PurchaseID == id);

            if (purchase == null) return NotFound();

            return View(purchase);
        }

        // GET: /Purchases/Create
        public async Task<IActionResult> Create()
        {
            PurchaseCreateViewModel vm;

            
            if (TempData[TempDataFormKey] is string savedForm && !string.IsNullOrEmpty(savedForm))
            {
                vm = JsonSerializer.Deserialize<PurchaseCreateViewModel>(savedForm) ?? new PurchaseCreateViewModel();

                if (TempData[TempDataErrorsKey] is string savedErrors && !string.IsNullOrEmpty(savedErrors))
                {
                    var errors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(savedErrors);
                    if (errors != null)
                    {
                        foreach (var (key, messages) in errors)
                        {
                            foreach (var message in messages)
                            {
                                ModelState.AddModelError(key, message);
                            }
                        }
                    }
                }
            }
            else
            {
                vm = new PurchaseCreateViewModel();
            }

            await RepopulateOptions(vm);
            return View(vm);
        }

        // POST: /Purchases/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseCreateViewModel vm)
        {
            vm.Items = vm.Items?.Where(i => i.ProductID > 0).ToList() ?? new();

            if (vm.Items.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Add at least one purchase item.");
            }

            var productIds = vm.Items.Select(i => i.ProductID).Distinct().ToList();
            var validProductCount = await _context.Products.CountAsync(p => productIds.Contains(p.ProductID));
            if (validProductCount != productIds.Count)
            {
                ModelState.AddModelError(string.Empty, "One or more selected products are invalid.");
            }

            var supplierExists = await _context.Suppliers.AnyAsync(s => s.SupplierID == vm.SupplierID);
            if (!supplierExists)
            {
                ModelState.AddModelError(nameof(vm.SupplierID), "Please select a valid supplier.");
            }

            if (!ModelState.IsValid)
            {
                SavePendingFormToTempData(vm);
                return RedirectToAction(nameof(Create));
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var purchase = new Purchase
                {
                    SupplierID = vm.SupplierID,
                    PurchaseDate = vm.PurchaseDate,
                    TotalAmount = 0m
                };

                foreach (var item in vm.Items)
                {
                    purchase.PurchaseItems.Add(new PurchaseItem
                    {
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        UnitCost = item.UnitCost
                    });
                }

                // Calculate Purchase Total automatically (Quantity * UnitCost per line, summed).
                purchase.TotalAmount = purchase.PurchaseItems.Sum(i => i.Quantity * i.UnitCost);

                _context.Purchases.Add(purchase);
                await _context.SaveChangesAsync();

                foreach (var item in purchase.PurchaseItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductID);
                    if (product != null)
                    {
                        product.StockQuantity += item.Quantity;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"Purchase #{purchase.PurchaseID} created successfully. Stock updated.";
                return RedirectToAction(nameof(Details), new { id = purchase.PurchaseID });
            }
            catch
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Something went wrong while saving the purchase. Please try again.");
                SavePendingFormToTempData(vm);
                return RedirectToAction(nameof(Create));
            }
        }

        private void SavePendingFormToTempData(PurchaseCreateViewModel vm)
        {
            vm.SupplierOptions = new();
            vm.ProductOptions = new();
            TempData[TempDataFormKey] = JsonSerializer.Serialize(vm);

            var errors = ModelState
                .Where(kvp => kvp.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

            TempData[TempDataErrorsKey] = JsonSerializer.Serialize(errors);
        }

        private async Task RepopulateOptions(PurchaseCreateViewModel vm)
        {
            vm.SupplierOptions = await _context.Suppliers
                .OrderBy(s => s.SupplierName)
                .Select(s => new SelectOption { Value = s.SupplierID, Text = s.SupplierName })
                .ToListAsync();

            vm.ProductOptions = await _context.Products
                .OrderBy(p => p.ProductName)
                .Select(p => new SelectOption { Value = p.ProductID, Text = p.ProductName + " (" + p.SKU + ")" })
                .ToListAsync();
        }
    }
}