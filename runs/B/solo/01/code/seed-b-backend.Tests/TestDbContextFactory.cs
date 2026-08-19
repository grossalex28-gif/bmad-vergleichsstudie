using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Models;

namespace seed_b_backend.Tests;

public static class TestDbContextFactory
{
    public static AppDbContext CreateWithSeedData()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AppDbContext(options);

        var category = new Category { Id = "K1", Name = "Elektronik" };
        var subcategory = new Subcategory
        {
            Id = "K1a",
            Name = "Kopfhörer",
            CategoryId = "K1",
            Eigenschaften = ["Bauform", "Kabellos"]
        };
        db.Categories.Add(category);
        db.Subcategories.Add(subcategory);

        var supplierA = new Supplier { Id = "L1", Name = "Lieferant A" };
        var supplierB = new Supplier { Id = "L2", Name = "Lieferant B" };
        db.Suppliers.AddRange(supplierA, supplierB);

        var product1 = new Product
        {
            Id = "P1",
            Name = "Ohrhörer Compact",
            Beschreibung = "Kompakte In-Ear-Kopfhörer für unterwegs.",
            SubcategoryId = "K1a",
            EigenschaftenJson = """{"Bauform":"In-Ear","Kabellos":true}""",
            Aufrufe = 0
        };
        var product2 = new Product
        {
            Id = "P2",
            Name = "Bügelkopfhörer Studio",
            Beschreibung = "Over-Ear-Kopfhörer mit Kabel.",
            SubcategoryId = "K1a",
            EigenschaftenJson = """{"Bauform":"Over-Ear","Kabellos":false}""",
            Aufrufe = 5
        };
        db.Products.AddRange(product1, product2);

        db.Offers.AddRange(
            new Offer { ProductId = "P1", SupplierId = "L1", Preis = 29.99m },
            new Offer { ProductId = "P1", SupplierId = "L2", Preis = 27.50m },
            new Offer { ProductId = "P2", SupplierId = "L1", Preis = 79.00m });

        db.SaveChanges();

        return db;
    }
}
