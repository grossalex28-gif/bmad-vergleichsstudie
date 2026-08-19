using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Domain;

namespace seed_b_backend.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Subcategory> Subcategories => Set<Subcategory>();
    public DbSet<PropertyDefinition> PropertyDefinitions => Set<PropertyDefinition>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductProperty> ProductProperties => Set<ProductProperty>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
        });

        modelBuilder.Entity<Subcategory>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.CategoryId);
            entity.HasOne(s => s.Category)
                .WithMany(c => c.Subcategories)
                .HasForeignKey(s => s.CategoryId);
        });

        modelBuilder.Entity<PropertyDefinition>(entity =>
        {
            entity.HasIndex(pd => pd.SubcategoryId);
            entity.HasIndex(pd => new { pd.SubcategoryId, pd.Name }).IsUnique();
            entity.HasOne(pd => pd.Subcategory)
                .WithMany(s => s.PropertyDefinitions)
                .HasForeignKey(pd => pd.SubcategoryId);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.SubcategoryId);
            entity.HasOne(p => p.Subcategory)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SubcategoryId);
        });

        modelBuilder.Entity<ProductProperty>(entity =>
        {
            entity.HasIndex(pp => pp.ProductId);
            entity.HasIndex(pp => pp.PropertyDefinitionId);
            entity.HasIndex(pp => new { pp.ProductId, pp.PropertyDefinitionId }).IsUnique();
            entity.HasOne(pp => pp.Product)
                .WithMany(p => p.ProductProperties)
                .HasForeignKey(pp => pp.ProductId);
            entity.HasOne(pp => pp.PropertyDefinition)
                .WithMany(pd => pd.ProductProperties)
                .HasForeignKey(pp => pp.PropertyDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(s => s.Id);
        });

        modelBuilder.Entity<Offer>(entity =>
        {
            entity.HasIndex(o => o.ProductId);
            entity.HasIndex(o => o.SupplierId);
            entity.Property(o => o.Price).HasColumnType("decimal(10,2)");
            entity.HasOne(o => o.Product)
                .WithMany(p => p.Offers)
                .HasForeignKey(o => o.ProductId);
            entity.HasOne(o => o.Supplier)
                .WithMany(s => s.Offers)
                .HasForeignKey(o => o.SupplierId);
        });

        modelBuilder.Entity<Rating>(entity =>
        {
            entity.HasIndex(r => r.ProductId);
            entity.HasIndex(r => new { r.ProductId, r.AuthorName }).IsUnique();
            entity.HasOne(r => r.Product)
                .WithMany(p => p.Ratings)
                .HasForeignKey(r => r.ProductId);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.HasIndex(o => o.PublicId).IsUnique();
            entity.Property(o => o.Status).HasConversion<string>();
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasIndex(oi => oi.OrderId);
            entity.Property(oi => oi.UnitPriceAtOrder).HasColumnType("decimal(10,2)");
            entity.HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId);
            entity.HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(oi => oi.Supplier)
                .WithMany()
                .HasForeignKey(oi => oi.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
