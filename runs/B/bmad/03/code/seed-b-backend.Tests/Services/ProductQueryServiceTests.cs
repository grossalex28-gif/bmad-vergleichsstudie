using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Data.Entities;
using seed_b_backend.Api.Services;

namespace seed_b_backend.Tests.Services;

public class ProductQueryServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static void SeedProducts(AppDbContext db, int count)
    {
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });

        for (var i = 1; i <= count; i++)
        {
            var id = $"P{i:D2}";
            db.Products.Add(new Product
            {
                Id = id,
                Name = $"Produkt {i:D2}",
                Description = "Beschreibung",
                SubcategoryId = "S1",
                Attributes = "{}"
            });
            db.Offers.Add(new Offer { ProductId = id, SupplierId = "L1", Price = 10m + i });
        }

        db.SaveChanges();
    }

    private static void SeedTwoCategories(AppDbContext db)
    {
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Categories.Add(new Category { Id = "C2", Name = "Kategorie 2" });
        db.Subcategories.Add(new Subcategory { Id = "S1a", Name = "Unterkategorie 1a", CategoryId = "C1" });
        db.Subcategories.Add(new Subcategory { Id = "S1b", Name = "Unterkategorie 1b", CategoryId = "C1" });
        db.Subcategories.Add(new Subcategory { Id = "S2a", Name = "Unterkategorie 2a", CategoryId = "C2" });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });

        AddProducts(db, "S1a", 2);
        AddProducts(db, "S1b", 3);
        AddProducts(db, "S2a", 2);

        db.SaveChanges();
    }

    private static void AddProducts(AppDbContext db, string subcategoryId, int count)
    {
        for (var i = 1; i <= count; i++)
        {
            var id = $"P-{subcategoryId}-{i:D2}";
            db.Products.Add(new Product
            {
                Id = id,
                Name = $"Produkt {subcategoryId} {i:D2}",
                Description = "Beschreibung",
                SubcategoryId = subcategoryId,
                Attributes = "{}"
            });
            db.Offers.Add(new Offer { ProductId = id, SupplierId = "L1", Price = 10m + i });
        }
    }

    [Fact]
    public async Task GetProductsAsync_FilteredByCategoryId_ReturnsOnlyProductsFromItsSubcategories()
    {
        await using var db = CreateContext();
        SeedTwoCategories(db);
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, categoryId: "C1");

        Assert.Equal(5, result.TotalCount);
        Assert.All(result.Items, item => Assert.StartsWith("Produkt S1", item.Name));
    }

    [Fact]
    public async Task GetProductsAsync_FilteredBySubcategoryId_ReturnsOnlyProductsFromThatSubcategory()
    {
        await using var db = CreateContext();
        SeedTwoCategories(db);
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, subcategoryId: "S1a");

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.StartsWith("Produkt S1a", item.Name));
    }

    [Fact]
    public async Task GetProductsAsync_NoFilter_ReturnsProductsFromAllCategories()
    {
        await using var db = CreateContext();
        SeedTwoCategories(db);
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1);

        Assert.Equal(7, result.TotalCount);
    }

    [Fact]
    public async Task GetProductsAsync_CategoryIdAndSubcategoryIdBothSet_SubcategoryIdTakesPrecedence()
    {
        await using var db = CreateContext();
        SeedTwoCategories(db);
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, categoryId: "C2", subcategoryId: "S1a");

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.StartsWith("Produkt S1a", item.Name));
    }

    [Fact]
    public async Task GetProductsAsync_UnknownCategoryId_ReturnsEmptyResultWithoutError()
    {
        await using var db = CreateContext();
        SeedTwoCategories(db);
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, categoryId: "C9");

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task GetProductsAsync_UnknownSubcategoryId_ReturnsEmptyResultWithoutError()
    {
        await using var db = CreateContext();
        SeedTwoCategories(db);
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, subcategoryId: "S9");

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task GetProductsAsync_CategoryFilterCombinedWithPagination_SplitsFilteredResultsAcrossTwoPages()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Categories.Add(new Category { Id = "C2", Name = "Kategorie 2" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Subcategories.Add(new Subcategory { Id = "S2", Name = "Unterkategorie 2", CategoryId = "C2" });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        AddProducts(db, "S1", 25);
        AddProducts(db, "S2", 3);
        db.SaveChanges();
        var service = new ProductQueryService(db);

        var page1 = await service.GetProductsAsync(1, categoryId: "C1");
        var page2 = await service.GetProductsAsync(2, categoryId: "C1");

        Assert.Equal(25, page1.TotalCount);
        Assert.Equal(2, page1.TotalPages);
        Assert.Equal(20, page1.Items.Count);
        Assert.Equal(5, page2.Items.Count);
        Assert.Empty(page1.Items.Select(i => i.Id).Intersect(page2.Items.Select(i => i.Id)));
    }

    [Fact]
    public async Task GetProductsAsync_With25Products_Page1HasTwentyItemsAndCorrectMetadata()
    {
        await using var db = CreateContext();
        SeedProducts(db, 25);
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1);

        Assert.Equal(20, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetProductsAsync_With25Products_Page2HasRemainingFiveItems()
    {
        await using var db = CreateContext();
        SeedProducts(db, 25);
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(2);

        Assert.Equal(5, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetProductsAsync_With25Products_PagesAreSortedByNameAndDoNotOverlap()
    {
        await using var db = CreateContext();
        SeedProducts(db, 25);
        var service = new ProductQueryService(db);

        var page1 = await service.GetProductsAsync(1);
        var page2 = await service.GetProductsAsync(2);

        var page1Names = page1.Items.Select(i => i.Name).ToList();
        var page2Names = page2.Items.Select(i => i.Name).ToList();

        Assert.Equal(page1Names.OrderBy(n => n, StringComparer.Ordinal), page1Names);
        Assert.Equal(page2Names.OrderBy(n => n, StringComparer.Ordinal), page2Names);
        Assert.Empty(page1Names.Intersect(page2Names));

        var allNames = page1Names.Concat(page2Names).ToList();
        var expectedNames = Enumerable.Range(1, 25).Select(i => $"Produkt {i:D2}").ToList();
        Assert.Equal(expectedNames, allNames);
    }

    [Fact]
    public async Task GetProductsAsync_ProductWithMultipleOffers_MinPriceIsLowestOffer()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Suppliers.Add(new Supplier { Id = "L2", Name = "Lieferant 2" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 30m });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L2", Price = 12.5m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1);

        Assert.Equal(12.5m, result.Items.Single().MinPrice);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task GetProductsAsync_PageZeroOrNegative_IsTreatedAsPageOne(int page)
    {
        await using var db = CreateContext();
        SeedProducts(db, 25);
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(page);

        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.Items.Count);
    }

    [Fact]
    public async Task GetProductsAsync_PageBeyondLastPage_ReturnsEmptyItemsWithCorrectMetadata()
    {
        await using var db = CreateContext();
        SeedProducts(db, 25);
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(3);

        Assert.Empty(result.Items);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetProductsAsync_With14Products_ReturnsExactlyOnePage()
    {
        await using var db = CreateContext();
        SeedProducts(db, 14);
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1);

        Assert.Equal(14, result.Items.Count);
        Assert.Equal(14, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task GetProductsAsync_ExtremelyLargePage_ReturnsEmptyWithoutThrowing()
    {
        await using var db = CreateContext();
        SeedProducts(db, 25);
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(int.MaxValue);

        Assert.Empty(result.Items);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetProductsAsync_SortByPriceAsc_ItemsAreSortedAscendingByMinPrice()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Products.Add(new Product { Id = "P2", Name = "Produkt 2", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Products.Add(new Product { Id = "P3", Name = "Produkt 3", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 20m });
        db.Offers.Add(new Offer { ProductId = "P2", SupplierId = "L1", Price = 5m });
        db.Offers.Add(new Offer { ProductId = "P3", SupplierId = "L1", Price = 12m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, sortBy: "price", sortDirection: "asc");

        Assert.Equal(new[] { "P2", "P3", "P1" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SortByPriceDesc_ItemsAreSortedDescendingByMinPrice()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Products.Add(new Product { Id = "P2", Name = "Produkt 2", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Products.Add(new Product { Id = "P3", Name = "Produkt 3", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 20m });
        db.Offers.Add(new Offer { ProductId = "P2", SupplierId = "L1", Price = 5m });
        db.Offers.Add(new Offer { ProductId = "P3", SupplierId = "L1", Price = 12m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, sortBy: "price", sortDirection: "desc");

        Assert.Equal(new[] { "P1", "P3", "P2" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SortByPriceWithoutDirection_DefaultsToAscending()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Products.Add(new Product { Id = "P2", Name = "Produkt 2", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 20m });
        db.Offers.Add(new Offer { ProductId = "P2", SupplierId = "L1", Price = 5m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, sortBy: "price");

        Assert.Equal(new[] { "P2", "P1" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SortByNameDesc_ItemsAreSortedDescendingAlphabetically()
    {
        await using var db = CreateContext();
        SeedProducts(db, 3);
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, sortBy: "name", sortDirection: "desc");

        Assert.Equal(new[] { "Produkt 03", "Produkt 02", "Produkt 01" }, result.Items.Select(i => i.Name));
    }

    [Fact]
    public async Task GetProductsAsync_SortByNameAsc_IsCaseInsensitive()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P1", Name = "Banane", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Products.Add(new Product { Id = "P2", Name = "apfel", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Products.Add(new Product { Id = "P3", Name = "zitrone", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 1m });
        db.Offers.Add(new Offer { ProductId = "P2", SupplierId = "L1", Price = 1m });
        db.Offers.Add(new Offer { ProductId = "P3", SupplierId = "L1", Price = 1m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, sortBy: "name", sortDirection: "asc");

        Assert.Equal(new[] { "apfel", "Banane", "zitrone" }, result.Items.Select(i => i.Name));
    }

    [Fact]
    public async Task GetProductsAsync_NoSortSpecified_DefaultsToNameAscending()
    {
        await using var db = CreateContext();
        SeedProducts(db, 3);
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1);

        Assert.Equal(new[] { "Produkt 01", "Produkt 02", "Produkt 03" }, result.Items.Select(i => i.Name));
    }

    [Fact]
    public async Task GetProductsAsync_UnknownSortBy_FallsBackToNameSort()
    {
        await using var db = CreateContext();
        SeedProducts(db, 3);
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, sortBy: "unbekannt");

        Assert.Equal(new[] { "Produkt 01", "Produkt 02", "Produkt 03" }, result.Items.Select(i => i.Name));
    }

    [Fact]
    public async Task GetProductsAsync_UnknownSortDirection_FallsBackToAscending()
    {
        await using var db = CreateContext();
        SeedProducts(db, 3);
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, sortBy: "name", sortDirection: "hoch");

        Assert.Equal(new[] { "Produkt 01", "Produkt 02", "Produkt 03" }, result.Items.Select(i => i.Name));
    }

    [Fact]
    public async Task GetProductsAsync_SortByPriceCombinedWithCategoryFilter_ReturnsOnlyFilteredProductsSorted()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Categories.Add(new Category { Id = "C2", Name = "Kategorie 2" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Subcategories.Add(new Subcategory { Id = "S2", Name = "Unterkategorie 2", CategoryId = "C2" });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Products.Add(new Product { Id = "P2", Name = "Produkt 2", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Products.Add(new Product { Id = "P3", Name = "Produkt 3", Description = "d", SubcategoryId = "S2", Attributes = "{}" });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 5m });
        db.Offers.Add(new Offer { ProductId = "P2", SupplierId = "L1", Price = 20m });
        db.Offers.Add(new Offer { ProductId = "P3", SupplierId = "L1", Price = 1m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, categoryId: "C1", sortBy: "price", sortDirection: "desc");

        Assert.Equal(new[] { "P2", "P1" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SortByPriceCombinedWithPagination_PagesAreSortedAndDoNotOverlap()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });

        for (var i = 1; i <= 25; i++)
        {
            var id = $"P{i:D2}";
            db.Products.Add(new Product
            {
                Id = id,
                Name = $"Produkt {i:D2}",
                Description = "Beschreibung",
                SubcategoryId = "S1",
                Attributes = "{}"
            });
            db.Offers.Add(new Offer { ProductId = id, SupplierId = "L1", Price = 30m - i });
        }

        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var page1 = await service.GetProductsAsync(1, sortBy: "price", sortDirection: "asc");
        var page2 = await service.GetProductsAsync(2, sortBy: "price", sortDirection: "asc");

        var page1Prices = page1.Items.Select(i => i.MinPrice).ToList();
        var page2Prices = page2.Items.Select(i => i.MinPrice).ToList();

        Assert.Equal(page1Prices.OrderBy(p => p), page1Prices);
        Assert.Equal(page2Prices.OrderBy(p => p), page2Prices);
        Assert.True(page1Prices.Max() <= page2Prices.Min());
        Assert.Empty(page1.Items.Select(i => i.Id).Intersect(page2.Items.Select(i => i.Id)));
    }

    [Fact]
    public async Task GetProductsAsync_SortByPriceAsc_UsesMinimumOfMultipleOffersPerProduct()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Suppliers.Add(new Supplier { Id = "L2", Name = "Lieferant 2" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Products.Add(new Product { Id = "P2", Name = "Produkt 2", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        // P1's cheapest offer (8) undercuts P2's only offer (10), even though P1's other offer (30) is far pricier.
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 30m });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L2", Price = 8m });
        db.Offers.Add(new Offer { ProductId = "P2", SupplierId = "L1", Price = 10m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, sortBy: "price", sortDirection: "asc");

        Assert.Equal(new[] { "P1", "P2" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SortByPriceAsc_TiedMinPriceBreaksTieById()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P3", Name = "Produkt 3", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Products.Add(new Product { Id = "P2", Name = "Produkt 2", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Offers.Add(new Offer { ProductId = "P3", SupplierId = "L1", Price = 15m });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 15m });
        db.Offers.Add(new Offer { ProductId = "P2", SupplierId = "L1", Price = 15m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, sortBy: "price", sortDirection: "asc");

        Assert.Equal(new[] { "P1", "P2", "P3" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_ProductsWithDuplicateNames_PagesDoNotOverlapOrDropItems()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });

        for (var i = 1; i <= 25; i++)
        {
            var id = $"P{i:D2}";
            db.Products.Add(new Product
            {
                Id = id,
                Name = "Gleicher Name",
                Description = "Beschreibung",
                SubcategoryId = "S1",
                Attributes = "{}"
            });
            db.Offers.Add(new Offer { ProductId = id, SupplierId = "L1", Price = 10m + i });
        }

        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var page1 = await service.GetProductsAsync(1);
        var page2 = await service.GetProductsAsync(2);

        var page1Ids = page1.Items.Select(i => i.Id).ToList();
        var page2Ids = page2.Items.Select(i => i.Id).ToList();

        Assert.Equal(20, page1Ids.Count);
        Assert.Equal(5, page2Ids.Count);
        Assert.Empty(page1Ids.Intersect(page2Ids));
        Assert.Equal(page1Ids.OrderBy(id => id, StringComparer.Ordinal), page1Ids);
        Assert.Equal(page2Ids.OrderBy(id => id, StringComparer.Ordinal), page2Ids);
    }

    [Fact]
    public async Task GetProductsAsync_SearchMatchesNameCaseInsensitively_ReturnsProduct()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P1", Name = "Zitrusreiniger", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 5m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, search: "ZITRUS");

        Assert.Equal(new[] { "P1" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SearchMatchesDescriptionOnly_ReturnsProduct()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "Enthält Zitrusextrakt", SubcategoryId = "S1", Attributes = "{}" });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 5m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, search: "zitrusextrakt");

        Assert.Equal(new[] { "P1" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SearchWithNoMatch_ExcludesNonMatchingProductsButKeepsOthers()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P1", Name = "Zitrusreiniger", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Products.Add(new Product { Id = "P2", Name = "Staubsauger", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 5m });
        db.Offers.Add(new Offer { ProductId = "P2", SupplierId = "L1", Price = 5m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, search: "zitrus");

        Assert.Equal(new[] { "P1" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_EmptySearch_ReturnsSameResultAsNullSearch()
    {
        await using var db = CreateContext();
        SeedProducts(db, 3);
        var service = new ProductQueryService(db);

        var withEmptySearch = await service.GetProductsAsync(1, search: "");
        var withNullSearch = await service.GetProductsAsync(1, search: null);

        Assert.Equal(withNullSearch.TotalCount, withEmptySearch.TotalCount);
        Assert.Equal(withNullSearch.Items.Select(i => i.Id), withEmptySearch.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_NullSearch_BehavesLikeNoSearchArgument()
    {
        await using var db = CreateContext();
        SeedProducts(db, 3);
        var service = new ProductQueryService(db);

        var withExplicitNull = await service.GetProductsAsync(1, search: null);
        var withoutArgument = await service.GetProductsAsync(1);

        Assert.Equal(withoutArgument.TotalCount, withExplicitNull.TotalCount);
        Assert.Equal(withoutArgument.Items.Select(i => i.Id), withExplicitNull.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SearchCombinedWithCategoryId_ReturnsOnlyMatchesFromThatCategory()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Categories.Add(new Category { Id = "C2", Name = "Kategorie 2" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Subcategories.Add(new Subcategory { Id = "S2", Name = "Unterkategorie 2", CategoryId = "C2" });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P1", Name = "Zitrusreiniger", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Products.Add(new Product { Id = "P2", Name = "Zitrusseife", Description = "d", SubcategoryId = "S2", Attributes = "{}" });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 5m });
        db.Offers.Add(new Offer { ProductId = "P2", SupplierId = "L1", Price = 5m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, categoryId: "C1", search: "zitrus");

        Assert.Equal(new[] { "P1" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SearchCombinedWithSort_FilteredResultsRemainSorted()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P1", Name = "Zitrus A", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Products.Add(new Product { Id = "P2", Name = "Zitrus B", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Products.Add(new Product { Id = "P3", Name = "Anderes Produkt", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 20m });
        db.Offers.Add(new Offer { ProductId = "P2", SupplierId = "L1", Price = 5m });
        db.Offers.Add(new Offer { ProductId = "P3", SupplierId = "L1", Price = 1m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, sortBy: "price", sortDirection: "asc", search: "zitrus");

        Assert.Equal(new[] { "P2", "P1" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SearchCombinedWithPagination_SplitsMatchesAcrossTwoPagesWithoutOverlap()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });

        for (var i = 1; i <= 25; i++)
        {
            var id = $"P{i:D2}";
            db.Products.Add(new Product
            {
                Id = id,
                Name = $"Zitrusprodukt {i:D2}",
                Description = "Beschreibung",
                SubcategoryId = "S1",
                Attributes = "{}"
            });
            db.Offers.Add(new Offer { ProductId = id, SupplierId = "L1", Price = 10m + i });
        }

        db.Products.Add(new Product { Id = "PX", Name = "Unbeteiligtes Produkt", Description = "Beschreibung", SubcategoryId = "S1", Attributes = "{}" });
        db.Offers.Add(new Offer { ProductId = "PX", SupplierId = "L1", Price = 50m });

        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var page1 = await service.GetProductsAsync(1, search: "zitrus");
        var page2 = await service.GetProductsAsync(2, search: "zitrus");

        Assert.Equal(25, page1.TotalCount);
        Assert.Equal(20, page1.Items.Count);
        Assert.Equal(5, page2.Items.Count);
        Assert.Empty(page1.Items.Select(i => i.Id).Intersect(page2.Items.Select(i => i.Id)));
        Assert.DoesNotContain("PX", page1.Items.Select(i => i.Id).Concat(page2.Items.Select(i => i.Id)));
    }

    [Fact]
    public async Task GetProductsAsync_WhitespaceOnlySearch_ReturnsSameResultAsNullSearch()
    {
        await using var db = CreateContext();
        SeedProducts(db, 3);
        var service = new ProductQueryService(db);

        var withWhitespaceSearch = await service.GetProductsAsync(1, search: "   ");
        var withNullSearch = await service.GetProductsAsync(1, search: null);

        Assert.Equal(withNullSearch.TotalCount, withWhitespaceSearch.TotalCount);
        Assert.Equal(withNullSearch.Items.Select(i => i.Id), withWhitespaceSearch.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SearchWithLeadingAndTrailingWhitespace_StillMatchesProduct()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P1", Name = "Zitrusreiniger", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 5m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, search: "  zitrus  ");

        Assert.Equal(new[] { "P1" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_AttributeFilter_ReturnsOnlyProductWithMatchingValue()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1", AttributeNames = ["Bauform"] });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = """{"Bauform":"In-Ear"}""" });
        db.Products.Add(new Product { Id = "P2", Name = "Produkt 2", Description = "d", SubcategoryId = "S1", Attributes = """{"Bauform":"Over-Ear"}""" });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 5m });
        db.Offers.Add(new Offer { ProductId = "P2", SupplierId = "L1", Price = 5m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, attr: ["Bauform:In-Ear"]);

        Assert.Equal(new[] { "P1" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_TwoAttributeFiltersOnDifferentNames_AreCombinedWithAnd()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1", AttributeNames = ["Bauform", "Kabellos"] });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = """{"Bauform":"In-Ear","Kabellos":true}""" });
        db.Products.Add(new Product { Id = "P2", Name = "Produkt 2", Description = "d", SubcategoryId = "S1", Attributes = """{"Bauform":"In-Ear","Kabellos":false}""" });
        db.Products.Add(new Product { Id = "P3", Name = "Produkt 3", Description = "d", SubcategoryId = "S1", Attributes = """{"Bauform":"Over-Ear","Kabellos":true}""" });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 5m });
        db.Offers.Add(new Offer { ProductId = "P2", SupplierId = "L1", Price = 5m });
        db.Offers.Add(new Offer { ProductId = "P3", SupplierId = "L1", Price = 5m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, attr: ["Bauform:In-Ear", "Kabellos:true"]);

        Assert.Equal(new[] { "P1" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_AttributeFilterMatchingNoProduct_ReturnsEmptyResultWithoutError()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1", AttributeNames = ["Bauform"] });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = """{"Bauform":"In-Ear"}""" });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 5m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, attr: ["Bauform:Nicht-Vorhanden"]);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetProductsAsync_AttributeFilterOnMissingJsonKey_ExcludesProductWithoutThrowing()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1", AttributeNames = ["Speicherkapazitaet"] });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P4", Name = "Produkt 4", Description = "d", SubcategoryId = "S1", Attributes = """{}""" });
        db.Products.Add(new Product { Id = "P5", Name = "Produkt 5", Description = "d", SubcategoryId = "S1", Attributes = """{"Speicherkapazitaet":"64GB"}""" });
        db.Offers.Add(new Offer { ProductId = "P4", SupplierId = "L1", Price = 5m });
        db.Offers.Add(new Offer { ProductId = "P5", SupplierId = "L1", Price = 5m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, attr: ["Speicherkapazitaet:64GB"]);

        Assert.Equal(new[] { "P5" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_BooleanAttributeFilter_MatchesOnlyExactBooleanValue()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1", AttributeNames = ["Kabellos"] });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = """{"Kabellos":true}""" });
        db.Products.Add(new Product { Id = "P2", Name = "Produkt 2", Description = "d", SubcategoryId = "S1", Attributes = """{"Kabellos":false}""" });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 5m });
        db.Offers.Add(new Offer { ProductId = "P2", SupplierId = "L1", Price = 5m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, attr: ["Kabellos:true"]);

        Assert.Equal(new[] { "P1" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_NumericAttributeFilter_MatchesExactTextNotNumericEquivalence()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1", AttributeNames = ["KapazitaetLiter"] });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = """{"KapazitaetLiter":1.50}""" });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 5m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var matching = await service.GetProductsAsync(1, attr: ["KapazitaetLiter:1.50"]);
        var nonMatching = await service.GetProductsAsync(1, attr: ["KapazitaetLiter:1.5"]);

        Assert.Equal(new[] { "P1" }, matching.Items.Select(i => i.Id));
        Assert.Empty(nonMatching.Items);
    }

    [Fact]
    public async Task GetProductsAsync_AvailableAttributeFiltersWithSubcategoryId_ReturnsDistinctSortedValuesPerAttribute()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1", AttributeNames = ["Bauform", "Kabellos"] });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = """{"Bauform":"In-Ear","Kabellos":true}""" });
        db.Products.Add(new Product { Id = "P2", Name = "Produkt 2", Description = "d", SubcategoryId = "S1", Attributes = """{"Bauform":"Over-Ear","Kabellos":true}""" });
        db.Products.Add(new Product { Id = "P3", Name = "Produkt 3", Description = "d", SubcategoryId = "S1", Attributes = """{"Bauform":"In-Ear","Kabellos":false}""" });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 5m });
        db.Offers.Add(new Offer { ProductId = "P2", SupplierId = "L1", Price = 5m });
        db.Offers.Add(new Offer { ProductId = "P3", SupplierId = "L1", Price = 5m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, subcategoryId: "S1");

        var bauform = Assert.Single(result.AvailableAttributeFilters, f => f.Name == "Bauform");
        Assert.Equal(new[] { "In-Ear", "Over-Ear" }, bauform.Values);
    }

    [Fact]
    public async Task GetProductsAsync_AvailableAttributeFiltersWithAttributeNameNoProductHasValueFor_ReturnsEmptyValuesList()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1", AttributeNames = ["Speicherkapazitaet"] });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = """{}""" });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 5m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, subcategoryId: "S1");

        var option = Assert.Single(result.AvailableAttributeFilters);
        Assert.Equal("Speicherkapazitaet", option.Name);
        Assert.Empty(option.Values);
    }

    [Fact]
    public async Task GetProductsAsync_AvailableAttributeFiltersIsEmpty_WhenOnlyCategoryIdSetOrNoScopeSet()
    {
        await using var db = CreateContext();
        SeedTwoCategories(db);
        var service = new ProductQueryService(db);

        var withCategoryOnly = await service.GetProductsAsync(1, categoryId: "C1");
        var withNoScope = await service.GetProductsAsync(1);

        Assert.Empty(withCategoryOnly.AvailableAttributeFilters);
        Assert.Empty(withNoScope.AvailableAttributeFilters);
    }

    [Fact]
    public async Task GetProductsAsync_AttributeFilterRoundTripFromAvailableFilters_MatchesSourceProduct()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1", AttributeNames = ["KapazitaetLiter"] });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = """{"KapazitaetLiter":1.50}""" });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 5m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var initial = await service.GetProductsAsync(1, subcategoryId: "S1");
        var option = Assert.Single(initial.AvailableAttributeFilters);
        var value = Assert.Single(option.Values);

        var filtered = await service.GetProductsAsync(1, subcategoryId: "S1", attr: [$"{option.Name}:{value}"]);

        Assert.Equal(new[] { "P1" }, filtered.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_AttributeFilterCombinedWithSearch_ResultsAreAnded()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1", AttributeNames = ["Bauform"] });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P1", Name = "Zitrus In-Ear", Description = "d", SubcategoryId = "S1", Attributes = """{"Bauform":"In-Ear"}""" });
        db.Products.Add(new Product { Id = "P2", Name = "Zitrus Over-Ear", Description = "d", SubcategoryId = "S1", Attributes = """{"Bauform":"Over-Ear"}""" });
        db.Products.Add(new Product { Id = "P3", Name = "Anderes In-Ear", Description = "d", SubcategoryId = "S1", Attributes = """{"Bauform":"In-Ear"}""" });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 5m });
        db.Offers.Add(new Offer { ProductId = "P2", SupplierId = "L1", Price = 5m });
        db.Offers.Add(new Offer { ProductId = "P3", SupplierId = "L1", Price = 5m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, search: "zitrus", attr: ["Bauform:In-Ear"]);

        Assert.Equal(new[] { "P1" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_AttributeFilterCombinedWithSort_FilteredResultsRemainSorted()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1", AttributeNames = ["Bauform"] });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = """{"Bauform":"In-Ear"}""" });
        db.Products.Add(new Product { Id = "P2", Name = "Produkt 2", Description = "d", SubcategoryId = "S1", Attributes = """{"Bauform":"In-Ear"}""" });
        db.Products.Add(new Product { Id = "P3", Name = "Produkt 3", Description = "d", SubcategoryId = "S1", Attributes = """{"Bauform":"Over-Ear"}""" });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 20m });
        db.Offers.Add(new Offer { ProductId = "P2", SupplierId = "L1", Price = 5m });
        db.Offers.Add(new Offer { ProductId = "P3", SupplierId = "L1", Price = 1m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, sortBy: "price", sortDirection: "asc", attr: ["Bauform:In-Ear"]);

        Assert.Equal(new[] { "P2", "P1" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_AttributeFilterCombinedWithPagination_TotalCountReflectsFilteredSet()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1", AttributeNames = ["Bauform"] });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });

        for (var i = 1; i <= 22; i++)
        {
            var id = $"P{i:D2}";
            db.Products.Add(new Product
            {
                Id = id,
                Name = $"Produkt {i:D2}",
                Description = "d",
                SubcategoryId = "S1",
                Attributes = """{"Bauform":"In-Ear"}"""
            });
            db.Offers.Add(new Offer { ProductId = id, SupplierId = "L1", Price = 10m + i });
        }

        db.Products.Add(new Product { Id = "PX", Name = "Unbeteiligt", Description = "d", SubcategoryId = "S1", Attributes = """{"Bauform":"Over-Ear"}""" });
        db.Offers.Add(new Offer { ProductId = "PX", SupplierId = "L1", Price = 50m });

        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var page1 = await service.GetProductsAsync(1, attr: ["Bauform:In-Ear"]);
        var page2 = await service.GetProductsAsync(2, attr: ["Bauform:In-Ear"]);

        Assert.Equal(22, page1.TotalCount);
        Assert.Equal(2, page1.TotalPages);
        Assert.Equal(20, page1.Items.Count);
        Assert.Equal(2, page2.Items.Count);
        Assert.DoesNotContain("PX", page1.Items.Select(i => i.Id).Concat(page2.Items.Select(i => i.Id)));
    }

    [Fact]
    public async Task GetProductsAsync_MalformedAttributeFilterEntryWithoutColon_IsIgnoredWithoutError()
    {
        await using var db = CreateContext();
        SeedProducts(db, 3);
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, attr: ["BauformInEar"]);

        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task GetProductsAsync_NullAttributeFilter_BehavesLikeNoAttributeFilterArgument()
    {
        await using var db = CreateContext();
        SeedProducts(db, 3);
        var service = new ProductQueryService(db);

        var withExplicitNull = await service.GetProductsAsync(1, attr: null);
        var withoutArgument = await service.GetProductsAsync(1);

        Assert.Equal(withoutArgument.TotalCount, withExplicitNull.TotalCount);
        Assert.Equal(withoutArgument.Items.Select(i => i.Id), withExplicitNull.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SortByViews_ItemsAreSortedDescendingByViewCount()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = "{}", ViewCount = 5 });
        db.Products.Add(new Product { Id = "P2", Name = "Produkt 2", Description = "d", SubcategoryId = "S1", Attributes = "{}", ViewCount = 20 });
        db.Products.Add(new Product { Id = "P3", Name = "Produkt 3", Description = "d", SubcategoryId = "S1", Attributes = "{}", ViewCount = 1 });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 10m });
        db.Offers.Add(new Offer { ProductId = "P2", SupplierId = "L1", Price = 10m });
        db.Offers.Add(new Offer { ProductId = "P3", SupplierId = "L1", Price = 10m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, sortBy: "views");

        Assert.Equal(new[] { "P2", "P1", "P3" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SortByViewsWithAscDirection_SortDirectionIsIgnored()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = "{}", ViewCount = 5 });
        db.Products.Add(new Product { Id = "P2", Name = "Produkt 2", Description = "d", SubcategoryId = "S1", Attributes = "{}", ViewCount = 20 });
        db.Products.Add(new Product { Id = "P3", Name = "Produkt 3", Description = "d", SubcategoryId = "S1", Attributes = "{}", ViewCount = 1 });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 10m });
        db.Offers.Add(new Offer { ProductId = "P2", SupplierId = "L1", Price = 10m });
        db.Offers.Add(new Offer { ProductId = "P3", SupplierId = "L1", Price = 10m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var withoutDirection = await service.GetProductsAsync(1, sortBy: "views");
        var withAscDirection = await service.GetProductsAsync(1, sortBy: "views", sortDirection: "asc");

        Assert.Equal(new[] { "P2", "P1", "P3" }, withoutDirection.Items.Select(i => i.Id));
        Assert.Equal(withoutDirection.Items.Select(i => i.Id), withAscDirection.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SortByViewsCombinedWithCategoryFilter_ReturnsOnlyFilteredProductsSorted()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Categories.Add(new Category { Id = "C2", Name = "Kategorie 2" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Subcategories.Add(new Subcategory { Id = "S2", Name = "Unterkategorie 2", CategoryId = "C2" });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = "{}", ViewCount = 5 });
        db.Products.Add(new Product { Id = "P2", Name = "Produkt 2", Description = "d", SubcategoryId = "S1", Attributes = "{}", ViewCount = 20 });
        db.Products.Add(new Product { Id = "P3", Name = "Produkt 3", Description = "d", SubcategoryId = "S2", Attributes = "{}", ViewCount = 99 });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 10m });
        db.Offers.Add(new Offer { ProductId = "P2", SupplierId = "L1", Price = 10m });
        db.Offers.Add(new Offer { ProductId = "P3", SupplierId = "L1", Price = 10m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, categoryId: "C1", sortBy: "views");

        Assert.Equal(new[] { "P2", "P1" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SortByViewsCombinedWithSearch_ReturnsOnlyMatchesSorted()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P1", Name = "Zitrus A", Description = "d", SubcategoryId = "S1", Attributes = "{}", ViewCount = 5 });
        db.Products.Add(new Product { Id = "P2", Name = "Zitrus B", Description = "d", SubcategoryId = "S1", Attributes = "{}", ViewCount = 20 });
        db.Products.Add(new Product { Id = "P3", Name = "Anderes Produkt", Description = "d", SubcategoryId = "S1", Attributes = "{}", ViewCount = 99 });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 10m });
        db.Offers.Add(new Offer { ProductId = "P2", SupplierId = "L1", Price = 10m });
        db.Offers.Add(new Offer { ProductId = "P3", SupplierId = "L1", Price = 10m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, sortBy: "views", search: "zitrus");

        Assert.Equal(new[] { "P2", "P1" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SortByViewsCombinedWithAttributeFilter_ReturnsOnlyMatchesSorted()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1", AttributeNames = ["Bauform"] });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = """{"Bauform":"In-Ear"}""", ViewCount = 5 });
        db.Products.Add(new Product { Id = "P2", Name = "Produkt 2", Description = "d", SubcategoryId = "S1", Attributes = """{"Bauform":"In-Ear"}""", ViewCount = 20 });
        db.Products.Add(new Product { Id = "P3", Name = "Produkt 3", Description = "d", SubcategoryId = "S1", Attributes = """{"Bauform":"Over-Ear"}""", ViewCount = 99 });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 10m });
        db.Offers.Add(new Offer { ProductId = "P2", SupplierId = "L1", Price = 10m });
        db.Offers.Add(new Offer { ProductId = "P3", SupplierId = "L1", Price = 10m });
        await db.SaveChangesAsync();
        var service = new ProductQueryService(db);

        var result = await service.GetProductsAsync(1, sortBy: "views", attr: ["Bauform:In-Ear"]);

        Assert.Equal(new[] { "P2", "P1" }, result.Items.Select(i => i.Id));
    }
}
