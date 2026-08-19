using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data.Entities;

namespace seed_b_backend.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Subcategory> Subcategories => Set<Subcategory>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>()
            .HasMany(c => c.Subcategories)
            .WithOne(s => s.Category)
            .HasForeignKey(s => s.CategoryId);

        modelBuilder.Entity<Subcategory>()
            .HasMany(s => s.Products)
            .WithOne(p => p.Subcategory)
            .HasForeignKey(p => p.SubcategoryId);

        modelBuilder.Entity<Product>()
            .Property(p => p.ViewCount)
            .HasDefaultValue(0);

        modelBuilder.Entity<Offer>(o =>
        {
            o.HasKey(x => new { x.ProductId, x.SupplierId });
            o.Property(x => x.Price).HasColumnType("decimal(18,2)");

            o.HasOne(x => x.Product)
                .WithMany(p => p.Offers)
                .HasForeignKey(x => x.ProductId);

            o.HasOne(x => x.Supplier)
                .WithMany(s => s.Offers)
                .HasForeignKey(x => x.SupplierId);
        });

        modelBuilder.Entity<Rating>(r =>
        {
            r.HasIndex(x => new { x.ProductId, x.AuthorName }).IsUnique();
            r.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId);
        });

        modelBuilder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId);

        modelBuilder.Entity<OrderItem>()
            .Property(i => i.UnitPrice)
            .HasColumnType("decimal(18,2)");
    }
}
