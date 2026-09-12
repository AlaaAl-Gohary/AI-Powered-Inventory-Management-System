using InventoryManagementSystem.Data;
using InventoryManagementSystem.Features.AiAssistant;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---- Person 5: Generative AI ----
builder.Services.Configure<AiOptions>(
    builder.Configuration.GetSection(AiOptions.SectionName));

builder.Services.AddScoped<IInventoryTools, InventoryTools>();
builder.Services.AddScoped<IAiInventoryAssistant, AiInventoryAssistant>();

if (!string.IsNullOrWhiteSpace(builder.Configuration["Ai:ApiKey"]))
    builder.Services.AddHttpClient<ILlmClient, OpenAiLlmClient>();
else
    builder.Services.AddSingleton<ILlmClient, StubLlmClient>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Suppliers}/{action=Index}/{id?}");

// ==== بيانات تجريبية مؤقتة — امسح الكود ده بعد الديمو ====
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();

    if (!db.Categories.Any())
    {
        var laptops = new InventoryManagementSystem.Models.Category { CategoryName = "Laptops", Description = "Laptop computers" };
        var phones = new InventoryManagementSystem.Models.Category { CategoryName = "Phones", Description = "Mobile phones" };
        db.Categories.AddRange(laptops, phones);
        db.SaveChanges();

        db.Products.AddRange(
            new InventoryManagementSystem.Models.Product { SKU = "LP-001", ProductName = "Dell XPS 15", CategoryID = laptops.CategoryID, UnitPrice = 45000, StockQuantity = 2, LowStockThreshold = 5 },
            new InventoryManagementSystem.Models.Product { SKU = "LP-002", ProductName = "MacBook Pro", CategoryID = laptops.CategoryID, UnitPrice = 55000, StockQuantity = 12, LowStockThreshold = 5 },
            new InventoryManagementSystem.Models.Product { SKU = "PH-001", ProductName = "iPhone 15", CategoryID = phones.CategoryID, UnitPrice = 40000, StockQuantity = 1, LowStockThreshold = 3 },
            new InventoryManagementSystem.Models.Product { SKU = "PH-002", ProductName = "Samsung S24", CategoryID = phones.CategoryID, UnitPrice = 35000, StockQuantity = 20, LowStockThreshold = 5 },
            new InventoryManagementSystem.Models.Product { SKU = "PH-003", ProductName = "Xiaomi 14", CategoryID = phones.CategoryID, UnitPrice = 25000, StockQuantity = 4, LowStockThreshold = 6 }
        );

        db.Suppliers.AddRange(
            new InventoryManagementSystem.Models.Supplier { SupplierName = "TechDistributors", ContactName = "Ahmed Ali", Phone = "01001234567", Email = "ahmed@techdist.com", Address = "Cairo" },
            new InventoryManagementSystem.Models.Supplier { SupplierName = "Global Electronics", ContactName = "Sara Hassan", Phone = "01009876543", Email = "sara@globalelec.com", Address = "Giza" }
        );

        db.SaveChanges();
    }
}

app.Run();