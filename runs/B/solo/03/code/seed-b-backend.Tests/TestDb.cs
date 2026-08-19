using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Models;

namespace seed_b_backend.Tests;

internal static class TestDb
{
    public static ShopDbContext CreateWithFixtures()
    {
        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new ShopDbContext(options);

        var category = new Category { Id = "K1", Name = "Elektronik" };
        var subCategory = new SubCategory
        {
            Id = "K1a",
            Name = "Kopfhörer",
            CategoryId = category.Id,
            PropertyNames = ["Bauform", "Kabellos"],
        };
        category.SubCategories.Add(subCategory);

        var supplierA = new Supplier { Id = "L1", Name = "NordTech Distribution" };
        var supplierB = new Supplier { Id = "L2", Name = "Blitzversand Elektronik" };

        var product = new Product
        {
            Id = "P1",
            Name = "Ohrhörer Modell Compact",
            Description = "Kompakte In-Ear-Kopfhörer.",
            SubCategoryId = subCategory.Id,
            ViewCount = 0,
        };
        product.Properties.Add(new ProductProperty { ProductId = product.Id, Name = "Bauform", Value = "In-Ear" });
        product.Properties.Add(new ProductProperty { ProductId = product.Id, Name = "Kabellos", Value = "true" });
        product.Offers.Add(new Offer { ProductId = product.Id, SupplierId = supplierA.Id, Price = 29.99m });
        product.Offers.Add(new Offer { ProductId = product.Id, SupplierId = supplierB.Id, Price = 27.50m });

        var product2 = new Product
        {
            Id = "P2",
            Name = "Bügelkopfhörer Studio",
            Description = "Over-Ear-Kopfhörer mit Kabel.",
            SubCategoryId = subCategory.Id,
            ViewCount = 0,
        };
        product2.Properties.Add(new ProductProperty { ProductId = product2.Id, Name = "Bauform", Value = "Over-Ear" });
        product2.Offers.Add(new Offer { ProductId = product2.Id, SupplierId = supplierA.Id, Price = 79.00m });

        db.Categories.Add(category);
        db.Suppliers.AddRange(supplierA, supplierB);
        db.Products.AddRange(product, product2);
        db.SaveChanges();

        return db;
    }
}
