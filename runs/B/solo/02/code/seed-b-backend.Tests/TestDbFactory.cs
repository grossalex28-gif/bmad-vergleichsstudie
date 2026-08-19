using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Models;

namespace seed_b_backend.Tests;

public static class TestDbFactory
{
    public static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    public static AppDbContext CreateSeededContext()
    {
        var db = CreateContext();

        var electronics = new Category { Name = "Elektronik" };
        var headphones = new Category
        {
            Name = "Kopfhörer",
            ParentCategory = electronics,
            PropertyNames = ["Bauform", "Kabellos"],
        };

        var supplierA = new Supplier { Name = "NordTech" };
        var supplierB = new Supplier { Name = "Blitzversand" };

        var product1 = new Product
        {
            Name = "Ohrhörer Compact",
            Description = "Kompakte In-Ear-Kopfhörer.",
            Subcategory = headphones,
            Properties = new Dictionary<string, string> { ["Bauform"] = "In-Ear", ["Kabellos"] = "true" },
            ViewCount = 0,
        };
        product1.Offers.Add(new ProductOffer { Product = product1, Supplier = supplierA, Price = 29.99m });
        product1.Offers.Add(new ProductOffer { Product = product1, Supplier = supplierB, Price = 27.50m });

        var product2 = new Product
        {
            Name = "Bügelkopfhörer Studio",
            Description = "Over-Ear-Kopfhörer mit Kabel.",
            Subcategory = headphones,
            Properties = new Dictionary<string, string> { ["Bauform"] = "Over-Ear", ["Kabellos"] = "false" },
            ViewCount = 5,
        };
        product2.Offers.Add(new ProductOffer { Product = product2, Supplier = supplierA, Price = 79.00m });

        db.Categories.AddRange(electronics, headphones);
        db.Suppliers.AddRange(supplierA, supplierB);
        db.Products.AddRange(product1, product2);
        db.SaveChanges();

        return db;
    }
}
