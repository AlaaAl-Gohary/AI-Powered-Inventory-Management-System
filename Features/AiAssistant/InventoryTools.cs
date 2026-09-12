using System.Text.Json;
using InventoryManagementSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Features.AiAssistant;

public sealed class InventoryTools : IInventoryTools
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly ApplicationDbContext _db;
    public InventoryTools(ApplicationDbContext db) => _db = db;

    public IReadOnlyList<LlmToolDefinition> GetDefinitions() => new List<LlmToolDefinition>
    {
        new() { Name = "get_inventory_summary",
            Description = "KPIs عامة: عدد المنتجات، الفئات، الموردين، إجمالي وحدات المخزون، عدد المنتجات منخفضة المخزون، وإجمالي المصروفات على المشتريات.",
            ParametersSchema = new { type = "object", properties = new { } } },

        new() { Name = "get_low_stock_products",
            Description = "المنتجات اللي كميتها أقل من أو تساوي LowStockThreshold بتاعها.",
            ParametersSchema = new { type = "object", properties = new {
                threshold = new { type = "integer", description = "اختياري: حد مخزون مطلق." } } } },

        new() { Name = "search_products",
            Description = "بحث عن منتجات بالاسم أو الـ SKU.",
            ParametersSchema = new { type = "object", properties = new { query = new { type = "string" } }, required = new[] { "query" } } },

        new() { Name = "get_products_by_category",
            Description = "كل المنتجات اللي تبع فئة معينة.",
            ParametersSchema = new { type = "object", properties = new { categoryName = new { type = "string" } }, required = new[] { "categoryName" } } },

        new() { Name = "get_supplier_info",
            Description = "بيانات التواصل لمورد معيّن + قائمة المنتجات اللي بيورّدها.",
            ParametersSchema = new { type = "object", properties = new { supplierName = new { type = "string" } }, required = new[] { "supplierName" } } },

        new() { Name = "get_purchase_summary",
            Description = "إجمالي عدد المشتريات وقيمتها المالية في مدى زمني.",
            ParametersSchema = new { type = "object", properties = new {
                fromDate = new { type = "string" }, toDate = new { type = "string" } } } },

        new() { Name = "get_top_purchased_products",
            Description = "المنتجات الأكثر شراءً بالكمية.",
            ParametersSchema = new { type = "object", properties = new {
                topN = new { type = "integer" }, fromDate = new { type = "string" }, toDate = new { type = "string" } } } },

        new() { Name = "get_recent_activity",
            Description = "أحدث عمليات الشراء.",
            ParametersSchema = new { type = "object", properties = new { count = new { type = "integer" } } } },

        new() { Name = "recommend_restock",
            Description = "ترشيحات مرتبة لإعادة التخزين بناءً على النقص مقارنة بـ LowStockThreshold.",
            ParametersSchema = new { type = "object", properties = new { topN = new { type = "integer" } } } }
    };

    public async Task<string> InvokeAsync(string name, string argumentsJson, CancellationToken ct = default)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
        var a = doc.RootElement;

        object result = name switch
        {
            "get_inventory_summary"      => await SummaryAsync(ct),
            "get_low_stock_products"     => await LowStockAsync(GetInt(a, "threshold"), ct),
            "search_products"            => await SearchAsync(GetStr(a, "query") ?? "", ct),
            "get_products_by_category"   => await ByCategoryAsync(GetStr(a, "categoryName") ?? "", ct),
            "get_supplier_info"          => await SupplierAsync(GetStr(a, "supplierName") ?? "", ct),
            "get_purchase_summary"       => await PurchaseSummaryAsync(GetDate(a, "fromDate"), GetDate(a, "toDate"), ct),
            "get_top_purchased_products" => await TopPurchasedAsync(GetInt(a, "topN") ?? 5, GetDate(a, "fromDate"), GetDate(a, "toDate"), ct),
            "get_recent_activity"        => await RecentAsync(GetInt(a, "count") ?? 5, ct),
            "recommend_restock"          => await RestockAsync(GetInt(a, "topN") ?? 10, ct),
            _ => new { error = $"Unknown tool '{name}'." }
        };

        return JsonSerializer.Serialize(result, Json);
    }

    private async Task<object> SummaryAsync(CancellationToken ct) => new
    {
        totalProducts    = await _db.Products.CountAsync(ct),
        totalCategories  = await _db.Categories.CountAsync(ct),
        totalSuppliers   = await _db.Suppliers.CountAsync(ct),
        totalStockUnits  = await _db.Products.SumAsync(p => (int?)p.StockQuantity, ct) ?? 0,
        lowStockProducts = await _db.Products.CountAsync(p => p.StockQuantity <= p.LowStockThreshold, ct),
        outOfStock       = await _db.Products.CountAsync(p => p.StockQuantity == 0, ct),
        purchases = new {
            count = await _db.Purchases.CountAsync(ct),
            spend = await _db.Purchases.SumAsync(p => (decimal?)p.TotalAmount, ct) ?? 0m
        }
    };

    private async Task<object> LowStockAsync(int? threshold, CancellationToken ct)
    {
        var q = _db.Products.AsNoTracking().Include(p => p.Category).AsQueryable();
        q = threshold.HasValue
            ? q.Where(p => p.StockQuantity <= threshold.Value)
            : q.Where(p => p.StockQuantity <= p.LowStockThreshold);

        var rows = await q.OrderBy(p => p.StockQuantity)
            .Select(p => new { p.ProductID, p.SKU, p.ProductName, p.StockQuantity, p.LowStockThreshold,
                category = p.Category != null ? p.Category.CategoryName : null, p.UnitPrice })
            .Take(50).ToListAsync(ct);

        return new { thresholdUsed = threshold, count = rows.Count, products = rows };
    }

    private async Task<object> SearchAsync(string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query)) return new { count = 0, products = Array.Empty<object>() };
        var term = query.Trim();
        var rows = await _db.Products.AsNoTracking().Include(p => p.Category)
            .Where(p => EF.Functions.Like(p.ProductName, $"%{term}%") || EF.Functions.Like(p.SKU, $"%{term}%"))
            .OrderBy(p => p.ProductName)
            .Select(p => new { p.ProductID, p.SKU, p.ProductName, p.UnitPrice, p.StockQuantity, p.LowStockThreshold,
                category = p.Category != null ? p.Category.CategoryName : null })
            .Take(25).ToListAsync(ct);
        return new { query = term, count = rows.Count, products = rows };
    }

    private async Task<object> ByCategoryAsync(string categoryName, CancellationToken ct)
    {
        var term = (categoryName ?? "").Trim();
        var rows = await _db.Products.AsNoTracking().Include(p => p.Category)
            .Where(p => p.Category != null && EF.Functions.Like(p.Category.CategoryName, $"%{term}%"))
            .OrderBy(p => p.ProductName)
            .Select(p => new { p.ProductID, p.SKU, p.ProductName, p.UnitPrice, p.StockQuantity,
                category = p.Category!.CategoryName })
            .Take(100).ToListAsync(ct);
        return new { category = term, count = rows.Count, products = rows };
    }

    private async Task<object> SupplierAsync(string supplierName, CancellationToken ct)
    {
        var term = (supplierName ?? "").Trim();
        var supplier = await _db.Suppliers.AsNoTracking()
            .Where(s => EF.Functions.Like(s.SupplierName, $"%{term}%"))
            .Select(s => new { s.SupplierID, s.SupplierName, s.ContactName, s.Phone, s.Email, s.Address,
                products = s.Purchases.SelectMany(p => p.PurchaseItems)
                    .Select(pi => pi.Product!.ProductName).Distinct().ToList() })
            .FirstOrDefaultAsync(ct);
        return supplier is null ? new { error = $"مفيش مورد مطابق لـ {term}." } : supplier;
    }

    private async Task<object> PurchaseSummaryAsync(DateTime? from, DateTime? to, CancellationToken ct)
    {
        var q = _db.Purchases.AsNoTracking().AsQueryable();
        if (from.HasValue) q = q.Where(p => p.PurchaseDate >= from.Value);
        if (to.HasValue)   q = q.Where(p => p.PurchaseDate <= to.Value);
        return new {
            from = from?.ToString("yyyy-MM-dd"), to = to?.ToString("yyyy-MM-dd"),
            count = await q.CountAsync(ct),
            totalSpend = await q.SumAsync(p => (decimal?)p.TotalAmount, ct) ?? 0m,
            unitsPurchased = await q.SelectMany(p => p.PurchaseItems).SumAsync(pi => (int?)pi.Quantity, ct) ?? 0
        };
    }

    private async Task<object> TopPurchasedAsync(int topN, DateTime? from, DateTime? to, CancellationToken ct)
    {
        var q = _db.PurchaseItems.AsNoTracking().AsQueryable();
        if (from.HasValue) q = q.Where(pi => pi.Purchase!.PurchaseDate >= from.Value);
        if (to.HasValue)   q = q.Where(pi => pi.Purchase!.PurchaseDate <= to.Value);
        var rows = await q.GroupBy(pi => new { pi.ProductID, pi.Product!.ProductName, pi.Product.SKU })
            .Select(g => new { productId = g.Key.ProductID, name = g.Key.ProductName, sku = g.Key.SKU,
                quantityBought = g.Sum(x => x.Quantity), totalCost = g.Sum(x => x.Quantity * x.UnitCost) })
            .OrderByDescending(x => x.quantityBought).Take(topN).ToListAsync(ct);
        return new { from = from?.ToString("yyyy-MM-dd"), to = to?.ToString("yyyy-MM-dd"),
            note = "مفيش Sales في الـ schema الحالي — ده ترتيب بالكميات المشتراة.", products = rows };
    }

    private async Task<object> RecentAsync(int count, CancellationToken ct)
    {
        var recentPurchases = await _db.Purchases.AsNoTracking()
            .OrderByDescending(p => p.PurchaseDate).Take(count)
            .Select(p => new { p.PurchaseID, p.PurchaseDate, p.TotalAmount,
                supplier = p.Supplier != null ? p.Supplier.SupplierName : null })
            .ToListAsync(ct);
        return new { recentPurchases };
    }

    private async Task<object> RestockAsync(int topN, CancellationToken ct)
    {
        var products = await _db.Products.AsNoTracking()
            .Select(p => new { p.ProductID, p.SKU, p.ProductName, p.StockQuantity, p.LowStockThreshold })
            .ToListAsync(ct);
        var ranked = products.Select(p => {
                var deficit = Math.Max(0, p.LowStockThreshold - p.StockQuantity);
                return new { p.ProductID, p.SKU, p.ProductName, p.StockQuantity, p.LowStockThreshold,
                    deficit, priorityScore = deficit,
                    reason = deficit > 0 ? $"أقل من الحد بـ {deficit} وحدة." : "المخزون سليم." };
            })
            .Where(x => x.priorityScore > 0).OrderByDescending(x => x.priorityScore).Take(topN).ToList();
        return new { generatedAt = DateTime.UtcNow, basis = "stock deficit vs LowStockThreshold", recommendations = ranked };
    }

    private static string? GetStr(JsonElement e, string k) =>
        e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    private static int? GetInt(JsonElement e, string k) =>
        e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : null;
    private static DateTime? GetDate(JsonElement e, string k) =>
        DateTime.TryParse(GetStr(e, k), out var d) ? d.Date : null;
}
