using Microsoft.Data.Sqlite;
using seed_b_backend.Api.Application;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Domain;

namespace seed_b_backend.Tests;

public class ProductQueryServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly ProductQueryService _sut;

    public ProductQueryServiceTests()
    {
        _db = TestDbContextFactory.CreateSqliteInMemory(out _connection);
        _sut = new ProductQueryService(_db);

        var category = new Category { Id = "K1", Name = "Kategorie 1" };
        var subcategory = new Subcategory { Id = "K1a", Name = "Unterkategorie 1", Category = category };
        _db.Categories.Add(category);
        _db.Subcategories.Add(subcategory);

        for (var i = 1; i <= 25; i++)
        {
            var id = $"P{i:D2}";
            _db.Products.Add(new Product
            {
                Id = id,
                Name = $"Produkt {i:D2}",
                Description = "Beschreibung",
                Subcategory = subcategory
            });
        }

        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetProductsAsync_DefaultPage_ReturnsAtMost20ItemsWithTotals()
    {
        var result = await _sut.GetProductsAsync();

        Assert.Equal(20, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(2, result.PageCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }

    [Fact]
    public async Task GetProductsAsync_Page2_ReturnsRemainingItemsWithoutOverlappingPage1()
    {
        var page1 = await _sut.GetProductsAsync(1);
        var page2 = await _sut.GetProductsAsync(2);

        Assert.Equal(5, page2.Items.Count);

        var page1Ids = page1.Items.Select(i => i.Id).ToHashSet();
        var page2Ids = page2.Items.Select(i => i.Id).ToHashSet();
        Assert.Empty(page1Ids.Intersect(page2Ids));
    }

    [Fact]
    public async Task GetProductsAsync_PageBeyondPageCount_IsClampedToLastPage()
    {
        var result = await _sut.GetProductsAsync(int.MaxValue);

        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.Items.Count);
    }

    [Fact]
    public async Task GetProductsAsync_CategoryIdFilter_ReturnsProductsFromAllSubcategoriesOfThatCategory()
    {
        var category2 = new Category { Id = "K2", Name = "Kategorie 2" };
        var sub2A = new Subcategory { Id = "K2a", Name = "Unterkategorie 2a", Category = category2 };
        var sub2B = new Subcategory { Id = "K2b", Name = "Unterkategorie 2b", Category = category2 };
        _db.Categories.Add(category2);
        _db.Subcategories.AddRange(sub2A, sub2B);
        _db.Products.Add(new Product { Id = "P2A", Name = "Produkt 2a", Description = "Beschreibung", Subcategory = sub2A });
        _db.Products.Add(new Product { Id = "P2B", Name = "Produkt 2b", Description = "Beschreibung", Subcategory = sub2B });
        _db.SaveChanges();

        var result = await _sut.GetProductsAsync(categoryId: "K2");

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(new[] { "P2A", "P2B" }, result.Items.Select(i => i.Id).OrderBy(id => id));
    }

    [Fact]
    public async Task GetProductsAsync_SubcategoryIdFilter_ReturnsOnlyProductsOfThatSubcategory()
    {
        var category2 = new Category { Id = "K2", Name = "Kategorie 2" };
        var sub2A = new Subcategory { Id = "K2a", Name = "Unterkategorie 2a", Category = category2 };
        var sub2B = new Subcategory { Id = "K2b", Name = "Unterkategorie 2b", Category = category2 };
        _db.Categories.Add(category2);
        _db.Subcategories.AddRange(sub2A, sub2B);
        _db.Products.Add(new Product { Id = "P2A", Name = "Produkt 2a", Description = "Beschreibung", Subcategory = sub2A });
        _db.Products.Add(new Product { Id = "P2B", Name = "Produkt 2b", Description = "Beschreibung", Subcategory = sub2B });
        _db.SaveChanges();

        var result = await _sut.GetProductsAsync(subcategoryId: "K2a");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("P2A", result.Items.Single().Id);
    }

    [Fact]
    public async Task GetProductsAsync_SubcategoryIdAndCategoryId_SubcategoryIdTakesPrecedence()
    {
        var category2 = new Category { Id = "K2", Name = "Kategorie 2" };
        var sub2A = new Subcategory { Id = "K2a", Name = "Unterkategorie 2a", Category = category2 };
        _db.Categories.Add(category2);
        _db.Subcategories.Add(sub2A);
        _db.Products.Add(new Product { Id = "P2A", Name = "Produkt 2a", Description = "Beschreibung", Subcategory = sub2A });
        _db.SaveChanges();

        var result = await _sut.GetProductsAsync(categoryId: "K1", subcategoryId: "K2a");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("P2A", result.Items.Single().Id);
    }

    [Fact]
    public async Task GetProductsAsync_FilteredPageBeyondFilteredPageCount_IsClampedAgainstFilteredCount()
    {
        var category2 = new Category { Id = "K2", Name = "Kategorie 2" };
        var sub2A = new Subcategory { Id = "K2a", Name = "Unterkategorie 2a", Category = category2 };
        _db.Categories.Add(category2);
        _db.Subcategories.Add(sub2A);
        _db.Products.Add(new Product { Id = "P2A", Name = "Produkt 2a", Description = "Beschreibung", Subcategory = sub2A });
        _db.SaveChanges();

        var result = await _sut.GetProductsAsync(page: int.MaxValue, subcategoryId: "K2a");

        Assert.Equal(1, result.Page);
        Assert.Equal(1, result.PageCount);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetProductsAsync_WhitespaceOnlySubcategoryId_IsTreatedAsNoFilter()
    {
        var result = await _sut.GetProductsAsync(subcategoryId: "   ");

        Assert.Equal(25, result.TotalCount);
    }

    private void AddPriceSortFixture(out Category category2, out Subcategory sub2)
    {
        category2 = new Category { Id = "K2", Name = "Kategorie 2" };
        sub2 = new Subcategory { Id = "K2a", Name = "Unterkategorie 2a", Category = category2 };
        _db.Categories.Add(category2);
        _db.Subcategories.Add(sub2);

        var supplier1 = new Supplier { Id = "S1", Name = "Lieferant 1" };
        var supplier2 = new Supplier { Id = "S2", Name = "Lieferant 2" };
        _db.Suppliers.AddRange(supplier1, supplier2);

        var productA = new Product { Id = "PA", Name = "Produkt A", Description = "Beschreibung", Subcategory = sub2 };
        var productB = new Product { Id = "PB", Name = "Produkt B", Description = "Beschreibung", Subcategory = sub2 };
        var productC = new Product { Id = "PC", Name = "Produkt C", Description = "Beschreibung", Subcategory = sub2 };
        _db.Products.AddRange(productA, productB, productC);

        // Niedrigster Angebotspreis: PB=5 (min aus 10/5), PC=20, PA=30
        _db.Offers.Add(new Offer { ProductId = "PA", SupplierId = "S1", Price = 30m });
        _db.Offers.Add(new Offer { ProductId = "PB", SupplierId = "S1", Price = 10m });
        _db.Offers.Add(new Offer { ProductId = "PB", SupplierId = "S2", Price = 5m });
        _db.Offers.Add(new Offer { ProductId = "PC", SupplierId = "S1", Price = 20m });
        _db.SaveChanges();
    }

    [Fact]
    public async Task GetProductsAsync_SortByPriceAsc_OrdersByLowestAvailableOfferPriceAscending()
    {
        AddPriceSortFixture(out _, out var sub2);

        var result = await _sut.GetProductsAsync(subcategoryId: sub2.Id, sortBy: "price", sortDir: "asc");

        Assert.Equal(new[] { "PB", "PC", "PA" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SortByPriceDesc_ReversesAscendingOrder()
    {
        AddPriceSortFixture(out _, out var sub2);

        var result = await _sut.GetProductsAsync(subcategoryId: sub2.Id, sortBy: "price", sortDir: "desc");

        Assert.Equal(new[] { "PA", "PC", "PB" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SortByNameAsc_OrdersAlphabeticallyAscending()
    {
        AddPriceSortFixture(out _, out var sub2);

        var result = await _sut.GetProductsAsync(subcategoryId: sub2.Id, sortBy: "NAME", sortDir: "ASC");

        Assert.Equal(new[] { "PA", "PB", "PC" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SortByNameDesc_OrdersAlphabeticallyDescending()
    {
        AddPriceSortFixture(out _, out var sub2);

        var result = await _sut.GetProductsAsync(subcategoryId: sub2.Id, sortBy: "name", sortDir: "desc");

        Assert.Equal(new[] { "PC", "PB", "PA" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_UnknownSortBy_FallsBackToDefaultIdOrder()
    {
        var result = await _sut.GetProductsAsync(sortBy: "foo", sortDir: "asc");

        Assert.Equal("P01", result.Items.First().Id);
        Assert.Equal(20, result.Items.Count);
    }

    [Fact]
    public async Task GetProductsAsync_SortDirWithoutSortBy_HasNoEffectAndUsesDefaultIdOrder()
    {
        var result = await _sut.GetProductsAsync(sortDir: "desc");

        Assert.Equal("P01", result.Items.First().Id);
    }

    [Fact]
    public async Task GetProductsAsync_CategoryIdCombinedWithSort_OrdersOnlyProductsOfThatCategory()
    {
        AddPriceSortFixture(out var category2, out _);

        var result = await _sut.GetProductsAsync(categoryId: category2.Id, sortBy: "name", sortDir: "desc");

        Assert.Equal(new[] { "PC", "PB", "PA" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SortByName_OrderIsStableAndNonOverlappingAcrossPageBoundary()
    {
        var page1 = await _sut.GetProductsAsync(page: 1, sortBy: "name", sortDir: "asc");
        var page2 = await _sut.GetProductsAsync(page: 2, sortBy: "name", sortDir: "asc");

        var expectedNames = Enumerable.Range(1, 25).Select(i => $"Produkt {i:D2}").ToArray();
        Assert.Equal(expectedNames[..20], page1.Items.Select(i => i.Name));
        Assert.Equal(expectedNames[20..], page2.Items.Select(i => i.Name));
    }

    [Fact]
    public async Task GetProductsAsync_QMatchesSubstringInName_IsCaseInsensitive()
    {
        var result = await _sut.GetProductsAsync(q: "produkt 05");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("P05", result.Items.Single().Id);
    }

    [Fact]
    public async Task GetProductsAsync_QMatchesSubstringInDescription_ReturnsOnlyThatProduct()
    {
        _db.Products.Add(new Product
        {
            Id = "PDESC",
            Name = "Sonderprodukt",
            Description = "Enthält einen einzigartigen Suchbegriff Zauberwort im Text",
            Subcategory = _db.Subcategories.Single()
        });
        _db.SaveChanges();

        var result = await _sut.GetProductsAsync(q: "zauberwort");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("PDESC", result.Items.Single().Id);
    }

    [Fact]
    public async Task GetProductsAsync_QWithoutMatch_ReturnsEmptyListWithZeroTotalCount()
    {
        var result = await _sut.GetProductsAsync(q: "kein-treffer-xyz");

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetProductsAsync_WhitespaceOnlyQ_IsTreatedAsNoFilter()
    {
        var result = await _sut.GetProductsAsync(q: "   ");

        Assert.Equal(25, result.TotalCount);
    }

    [Fact]
    public async Task GetProductsAsync_QCombinedWithCategoryAndSort_ReturnsOnlyIntersectionInSortedOrder()
    {
        AddPriceSortFixture(out var category2, out _);

        var result = await _sut.GetProductsAsync(categoryId: category2.Id, q: "produkt", sortBy: "name", sortDir: "desc");

        Assert.Equal(new[] { "PC", "PB", "PA" }, result.Items.Select(i => i.Id));
    }

    private PropertyDefinition AddPropertyFilterFixture()
    {
        var subcategory = _db.Subcategories.Single();
        var farbe = new PropertyDefinition { SubcategoryId = subcategory.Id, Name = "Farbe" };
        var material = new PropertyDefinition { SubcategoryId = subcategory.Id, Name = "Material" };
        _db.PropertyDefinitions.AddRange(farbe, material);
        _db.SaveChanges();

        _db.ProductProperties.AddRange(
            new ProductProperty { ProductId = "P01", PropertyDefinitionId = farbe.Id, Value = "Rot" },
            new ProductProperty { ProductId = "P01", PropertyDefinitionId = material.Id, Value = "Holz" },
            new ProductProperty { ProductId = "P02", PropertyDefinitionId = farbe.Id, Value = "Blau" },
            new ProductProperty { ProductId = "P02", PropertyDefinitionId = material.Id, Value = "Holz" },
            new ProductProperty { ProductId = "P03", PropertyDefinitionId = farbe.Id, Value = "Rot" },
            new ProductProperty { ProductId = "P03", PropertyDefinitionId = material.Id, Value = "Metall" });
        _db.SaveChanges();

        return farbe;
    }

    [Fact]
    public async Task GetProductsAsync_PropertyFilter_ReturnsOnlyProductsWithExactMatchingValue()
    {
        AddPropertyFilterFixture();

        var result = await _sut.GetProductsAsync(property: ["Farbe:Rot"]);

        Assert.Equal(new[] { "P01", "P03" }, result.Items.Select(i => i.Id).OrderBy(id => id));
    }

    [Fact]
    public async Task GetProductsAsync_PropertyFiltersOnDifferentProperties_CombineWithAnd()
    {
        AddPropertyFilterFixture();

        var result = await _sut.GetProductsAsync(property: ["Farbe:Rot", "Material:Holz"]);

        Assert.Equal("P01", result.Items.Single().Id);
    }

    [Fact]
    public async Task GetProductsAsync_PropertyFiltersOnSameProperty_CombineWithOr()
    {
        AddPropertyFilterFixture();

        var result = await _sut.GetProductsAsync(property: ["Farbe:Rot", "Farbe:Blau"]);

        Assert.Equal(new[] { "P01", "P02", "P03" }, result.Items.Select(i => i.Id).OrderBy(id => id));
    }

    [Fact]
    public async Task GetProductsAsync_MalformedPropertyEntryWithoutColon_IsIgnored()
    {
        AddPropertyFilterFixture();

        var result = await _sut.GetProductsAsync(property: ["FarbeOhneDoppelpunkt"]);

        Assert.Equal(25, result.TotalCount);
    }

    [Fact]
    public async Task GetProductsAsync_PropertyCombinedWithSubcategoryQAndSort_ReturnsCorrectlySortedIntersection()
    {
        AddPropertyFilterFixture();

        var result = await _sut.GetProductsAsync(
            subcategoryId: "K1a",
            q: "produkt",
            property: ["Farbe:Rot"],
            sortBy: "name",
            sortDir: "desc");

        Assert.Equal(new[] { "P03", "P01" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_PropertyFilterOnUnknownName_ReturnsEmptyList()
    {
        AddPropertyFilterFixture();

        var result = await _sut.GetProductsAsync(property: ["Groesse:XL"]);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    private void AddViewCountSortFixture(out Subcategory sub2)
    {
        var category2 = new Category { Id = "K2", Name = "Kategorie 2" };
        sub2 = new Subcategory { Id = "K2a", Name = "Unterkategorie 2a", Category = category2 };
        _db.Categories.Add(category2);
        _db.Subcategories.Add(sub2);

        _db.Products.Add(new Product { Id = "PA", Name = "Produkt A", Description = "Beschreibung", Subcategory = sub2, ViewCount = 5 });
        _db.Products.Add(new Product { Id = "PB", Name = "Produkt B", Description = "Beschreibung", Subcategory = sub2, ViewCount = 20 });
        _db.Products.Add(new Product { Id = "PC", Name = "Produkt C", Description = "Beschreibung", Subcategory = sub2, ViewCount = 1 });
        _db.SaveChanges();
    }

    [Fact]
    public async Task GetProductsAsync_SortByViewCountDesc_OrdersByViewCountDescending()
    {
        AddViewCountSortFixture(out var sub2);

        var result = await _sut.GetProductsAsync(subcategoryId: sub2.Id, sortBy: "viewCount", sortDir: "desc");

        Assert.Equal(new[] { "PB", "PA", "PC" }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SortByViewCountAsc_OrdersByViewCountAscending()
    {
        AddViewCountSortFixture(out var sub2);

        var result = await _sut.GetProductsAsync(subcategoryId: sub2.Id, sortBy: "viewCount", sortDir: "asc");

        Assert.Equal(new[] { "PC", "PA", "PB" }, result.Items.Select(i => i.Id));
    }
}
