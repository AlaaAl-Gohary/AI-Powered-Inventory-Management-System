using System.Collections.Generic;
using System.Reflection.Emit;
using InventoryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Data
{
    
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Person 1
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();

        // Person 2
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Purchase> Purchases => Set<Purchase>();
        public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Purchase>()
                .HasOne(pu => pu.Supplier)
                .WithMany(s => s.Purchases)
                .HasForeignKey(pu => pu.SupplierID)
                .OnDelete(DeleteBehavior.Restrict); 

            modelBuilder.Entity<PurchaseItem>()
                .HasOne(pi => pi.Purchase)
                .WithMany(pu => pu.PurchaseItems)
                .HasForeignKey(pi => pi.PurchaseID)
                .OnDelete(DeleteBehavior.Cascade); 

            modelBuilder.Entity<PurchaseItem>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.PurchaseItems)
                .HasForeignKey(pi => pi.ProductID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Supplier>().ToTable("Suppliers");
            modelBuilder.Entity<Purchase>().ToTable("Purchases");
            modelBuilder.Entity<PurchaseItem>().ToTable("PurchaseItems");
        }
    }
}
