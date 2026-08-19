using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Domain;

namespace seed_b_backend.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Subcategory> Subcategories => Set<Subcategory>();
    public DbSet<SubcategoryProperty> SubcategoryProperties => Set<SubcategoryProperty>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductPropertyValue> ProductPropertyValues => Set<ProductPropertyValue>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
