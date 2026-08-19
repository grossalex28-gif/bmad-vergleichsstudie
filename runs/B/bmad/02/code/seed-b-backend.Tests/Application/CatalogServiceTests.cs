using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Application;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Domain;

namespace seed_b_backend.Tests.Application;

public class CatalogServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly CatalogService _service;

    public CatalogServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        // SQLite's built-in lower() only folds ASCII a-z by default, unlike SQL Server's
        // collation-aware LOWER(). Override it so the test provider matches production
        // behavior for non-ASCII input (e.g. German umlauts) instead of silently diverging.
        _connection.CreateFunction<string?, string?>("lower", s => s?.ToLowerInvariant());

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
        _service = new CatalogService(_context);

        SeedProducts();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private void SeedProducts()
    {
        var category = new Category { Id = "C1", Name = "Testkategorie" };
        var subcategory = new Subcategory { Id = "S1", Name = "Testunterkategorie", CategoryId = "C1" };
        _context.Categories.Add(category);
        _context.Subcategories.Add(subcategory);

        for (var i = 1; i <= 25; i++)
        {
            _context.Products.Add(new Product
            {
                Id = $"T{i:D2}",
                Name = $"Testprodukt {i:D2}",
                Description = "Beschreibung",
                SubcategoryId = "S1"
            });
        }

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetProductsAsync_FirstPage_Returns20Items()
    {
        var result = await _service.GetProductsAsync(1);

        Assert.Equal(20, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(CatalogService.PageSize, result.PageSize);
    }

    [Fact]
    public async Task GetProductsAsync_LastPage_ReturnsRemainder()
    {
        var result = await _service.GetProductsAsync(2);

        Assert.Equal(5, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
    }

    [Fact]
    public async Task GetProductsAsync_PageBeyondRange_ReturnsEmptyList()
    {
        var result = await _service.GetProductsAsync(3);

        Assert.Empty(result.Items);
        Assert.Equal(25, result.TotalCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetProductsAsync_PageZeroOrNegative_ReturnsEmptyList(int page)
    {
        var result = await _service.GetProductsAsync(page);

        Assert.Empty(result.Items);
        Assert.Equal(25, result.TotalCount);
    }

    [Fact]
    public async Task GetProductsAsync_PageCausesSkipOverflow_ReturnsEmptyList()
    {
        var result = await _service.GetProductsAsync(int.MaxValue / 10);

        Assert.Empty(result.Items);
        Assert.Equal(25, result.TotalCount);
    }

    [Fact]
    public async Task GetProductsAsync_OrderingIsStable()
    {
        var first = await _service.GetProductsAsync(1);
        var second = await _service.GetProductsAsync(1);

        Assert.Equal(first.Items.Select(p => p.Id), second.Items.Select(p => p.Id));
    }

    private void SeedCategoryHierarchy()
    {
        var categoryA = new Category { Id = "CA", Name = "Kategorie A" };
        var subA1 = new Subcategory { Id = "SA1", Name = "Unterkategorie A1", CategoryId = "CA" };
        var subA2 = new Subcategory { Id = "SA2", Name = "Unterkategorie A2", CategoryId = "CA" };
        var categoryB = new Category { Id = "CB", Name = "Kategorie B" };
        var subB1 = new Subcategory { Id = "SB1", Name = "Unterkategorie B1", CategoryId = "CB" };

        _context.Categories.AddRange(categoryA, categoryB);
        _context.Subcategories.AddRange(subA1, subA2, subB1);

        _context.Products.Add(new Product { Id = "PA1", Name = "Produkt A1", Description = "Beschreibung", SubcategoryId = "SA1" });
        _context.Products.Add(new Product { Id = "PA2", Name = "Produkt A2", Description = "Beschreibung", SubcategoryId = "SA2" });
        _context.Products.Add(new Product { Id = "PB1", Name = "Produkt B1", Description = "Beschreibung", SubcategoryId = "SB1" });

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetProductsAsync_FilteredBySubcategory_ReturnsOnlyThatSubcategorysProducts()
    {
        SeedCategoryHierarchy();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SA1");

        Assert.Single(result.Items);
        Assert.Equal("PA1", result.Items[0].Id);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetProductsAsync_FilteredByCategory_ReturnsProductsFromAllItsSubcategories()
    {
        SeedCategoryHierarchy();

        var result = await _service.GetProductsAsync(1, categoryId: "CA");

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(new[] { "PA1", "PA2" }, result.Items.Select(p => p.Id));
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetProductsAsync_FilteredByUnknownCategoryId_ReturnsEmptyList()
    {
        var result = await _service.GetProductsAsync(1, categoryId: "UNKNOWN");

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetProductsAsync_FilteredByUnknownSubcategoryId_ReturnsEmptyList()
    {
        var result = await _service.GetProductsAsync(1, subcategoryId: "UNKNOWN");

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetProductsAsync_SubcategoryIdTakesPrecedenceWhenBothProvided()
    {
        SeedCategoryHierarchy();

        var result = await _service.GetProductsAsync(1, categoryId: "CB", subcategoryId: "SA1");

        Assert.Single(result.Items);
        Assert.Equal("PA1", result.Items[0].Id);
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsAllCategoriesWithTheirSubcategories()
    {
        SeedCategoryHierarchy();

        var categories = await _service.GetCategoriesAsync();

        var categoryA = Assert.Single(categories, c => c.Id == "CA");
        Assert.Equal(new[] { "SA1", "SA2" }, categoryA.Subcategories.Select(s => s.Id));

        var categoryB = Assert.Single(categories, c => c.Id == "CB");
        Assert.Equal(new[] { "SB1" }, categoryB.Subcategories.Select(s => s.Id));
    }

    private void SeedProductsWithOffers()
    {
        var category = new Category { Id = "CP", Name = "Preiskategorie" };
        var subcategory = new Subcategory { Id = "SP1", Name = "Preisunterkategorie", CategoryId = "CP" };
        var supplierOne = new Supplier { Id = "LP1", Name = "Lieferant Eins" };
        var supplierTwo = new Supplier { Id = "LP2", Name = "Lieferant Zwei" };
        _context.Categories.Add(category);
        _context.Subcategories.Add(subcategory);
        _context.Suppliers.AddRange(supplierOne, supplierTwo);

        // Name-Reihenfolge (aufsteigend): PP2 Apfel, PP1 Banane, PP4 Mango, PP3 Zitrone
        // Preis-Reihenfolge (aufsteigend, guenstigstes Angebot): PP1 (5.00), PP2 (8.00 von zwei Angeboten), PP3 (12.00), PP4 (kein Angebot -> immer am Ende)
        // ViewCount-Reihenfolge (aufsteigend): PP3 (5), PP2 (10), PP4 (20), PP1 (30) — bewusst abweichend von Name-/Preis-Reihenfolge
        _context.Products.AddRange(
            new Product { Id = "PP1", Name = "Banane", Description = "Beschreibung", SubcategoryId = "SP1", ViewCount = 30 },
            new Product { Id = "PP2", Name = "Apfel", Description = "Beschreibung", SubcategoryId = "SP1", ViewCount = 10 },
            new Product { Id = "PP3", Name = "Zitrone", Description = "Beschreibung", SubcategoryId = "SP1", ViewCount = 5 },
            new Product { Id = "PP4", Name = "Mango", Description = "Beschreibung", SubcategoryId = "SP1", ViewCount = 20 }
        );
        _context.Offers.AddRange(
            new Offer { ProductId = "PP1", SupplierId = "LP1", Price = 5.00m },
            new Offer { ProductId = "PP2", SupplierId = "LP1", Price = 20.00m },
            new Offer { ProductId = "PP2", SupplierId = "LP2", Price = 8.00m },
            new Offer { ProductId = "PP3", SupplierId = "LP1", Price = 12.00m }
        );
        // PP4 bleibt absichtlich ohne Offer-Zeile (AC 4)

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetProductsAsync_SortedByPriceAscending_OrdersByCheapestOfferPrice()
    {
        SeedProductsWithOffers();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SP1", sortBy: "price", sortDirection: "asc");

        Assert.Equal(new[] { "PP1", "PP2", "PP3", "PP4" }, result.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SortedByPriceDescending_OrdersByCheapestOfferPriceDescending()
    {
        SeedProductsWithOffers();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SP1", sortBy: "price", sortDirection: "desc");

        Assert.Equal(new[] { "PP3", "PP2", "PP1", "PP4" }, result.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SortedByNameAscending_OrdersAlphabetically()
    {
        SeedProductsWithOffers();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SP1", sortBy: "name", sortDirection: "asc");

        Assert.Equal(new[] { "PP2", "PP1", "PP4", "PP3" }, result.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SortedByNameDescending_OrdersReverseAlphabetically()
    {
        SeedProductsWithOffers();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SP1", sortBy: "name", sortDirection: "desc");

        Assert.Equal(new[] { "PP3", "PP4", "PP1", "PP2" }, result.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SortedByViewCountAscending_OrdersByViewCount()
    {
        SeedProductsWithOffers();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SP1", sortBy: "viewCount", sortDirection: "asc");

        Assert.Equal(new[] { "PP3", "PP2", "PP4", "PP1" }, result.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SortedByViewCountDescending_OrdersByViewCountDescending()
    {
        SeedProductsWithOffers();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SP1", sortBy: "viewCount", sortDirection: "desc");

        Assert.Equal(new[] { "PP1", "PP4", "PP2", "PP3" }, result.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task GetProductsAsync_UnknownSortBy_FallsBackToDefaultIdOrder()
    {
        SeedProductsWithOffers();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SP1", sortBy: "unbekannt");

        Assert.Equal(new[] { "PP1", "PP2", "PP3", "PP4" }, result.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SortCombinedWithSubcategoryFilter_AppliesBothTogether()
    {
        SeedCategoryHierarchy();
        SeedProductsWithOffers();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SP1", sortBy: "name", sortDirection: "asc");

        Assert.Equal(new[] { "PP2", "PP1", "PP4", "PP3" }, result.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SortCombinedWithCategoryFilter_AppliesBothTogether()
    {
        SeedCategoryHierarchy();
        SeedProductsWithOffers();

        var result = await _service.GetProductsAsync(1, categoryId: "CP", sortBy: "name", sortDirection: "asc");

        Assert.Equal(new[] { "PP2", "PP1", "PP4", "PP3" }, result.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task GetProductsAsync_UnknownSortDirection_FallsBackToAscending()
    {
        SeedProductsWithOffers();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SP1", sortBy: "price", sortDirection: "sideways");

        Assert.Equal(new[] { "PP1", "PP2", "PP3", "PP4" }, result.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SortByIsCaseInsensitive()
    {
        SeedProductsWithOffers();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SP1", sortBy: "PRICE", sortDirection: "asc");

        Assert.Equal(new[] { "PP1", "PP2", "PP3", "PP4" }, result.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SortDirectionIsCaseInsensitive()
    {
        SeedProductsWithOffers();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SP1", sortBy: "price", sortDirection: "DESC");

        Assert.Equal(new[] { "PP3", "PP2", "PP1", "PP4" }, result.Items.Select(p => p.Id));
    }

    private void SeedSearchProducts()
    {
        var category = new Category { Id = "CS", Name = "Suchkategorie" };
        var subcategory = new Subcategory { Id = "SS1", Name = "Suchunterkategorie", CategoryId = "CS" };
        _context.Categories.Add(category);
        _context.Subcategories.Add(subcategory);

        // "Kaffee" steckt im Namen von PS1 und PS3, nicht in PS2/PS4.
        // "Edelstahl" steckt nur in der Beschreibung von PS4 (weder im eigenen noch in einem anderen Namen).
        _context.Products.AddRange(
            new Product { Id = "PS1", Name = "Kaffeemaschine", Description = "Vollautomat für Espresso und Cappuccino", SubcategoryId = "SS1" },
            new Product { Id = "PS2", Name = "Teekanne", Description = "Emaillierte Kanne mit Sieb für losen Tee", SubcategoryId = "SS1" },
            new Product { Id = "PS3", Name = "Kaffeemühle", Description = "Elektrisches Mahlwerk für Kaffeebohnen", SubcategoryId = "SS1" },
            new Product { Id = "PS4", Name = "Wasserkocher", Description = "Schnellkochender Kessel aus Edelstahl", SubcategoryId = "SS1" }
        );

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetProductsAsync_SearchMatchesNameSubstring_ReturnsMatchingProducts()
    {
        SeedSearchProducts();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SS1", search: "Kaffee");

        Assert.Equivalent(new[] { "PS1", "PS3" }, result.Items.Select(p => p.Id).ToList());
    }

    [Fact]
    public async Task GetProductsAsync_SearchIsCaseInsensitive()
    {
        SeedSearchProducts();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SS1", search: "kaffee");

        Assert.Equivalent(new[] { "PS1", "PS3" }, result.Items.Select(p => p.Id).ToList());
    }

    [Fact]
    public async Task GetProductsAsync_SearchIsCaseInsensitiveForGermanUmlauts()
    {
        var category = new Category { Id = "CU", Name = "Umlautkategorie" };
        var subcategory = new Subcategory { Id = "SU1", Name = "Umlautunterkategorie", CategoryId = "CU" };
        _context.Categories.Add(category);
        _context.Subcategories.Add(subcategory);
        _context.Products.Add(new Product { Id = "PU1", Name = "Ölkanne", Description = "Kanne für Öl", SubcategoryId = "SU1" });
        _context.SaveChanges();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SU1", search: "öl");

        Assert.Equivalent(new[] { "PU1" }, result.Items.Select(p => p.Id).ToList());
    }

    [Fact]
    public async Task GetProductsAsync_SearchMatchesDescriptionSubstring_ReturnsMatchingProducts()
    {
        SeedSearchProducts();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SS1", search: "Edelstahl");

        Assert.Equal(new[] { "PS4" }, result.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SearchNoMatch_ReturnsEmptyListNotError()
    {
        SeedSearchProducts();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SS1", search: "xyz-kein-treffer");

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetProductsAsync_SearchWhitespaceOnly_ReturnsAllProducts()
    {
        SeedSearchProducts();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SS1", search: "   ");

        Assert.Equal(4, result.Items.Count);
    }

    [Fact]
    public async Task GetProductsAsync_SearchCombinedWithCategoryFilter_AppliesBothTogether()
    {
        SeedCategoryHierarchy();
        SeedSearchProducts();

        var result = await _service.GetProductsAsync(1, categoryId: "CS", search: "Kaffee");

        Assert.Equivalent(new[] { "PS1", "PS3" }, result.Items.Select(p => p.Id).ToList());
    }

    [Fact]
    public async Task GetProductsAsync_SearchCombinedWithSort_AppliesBothTogether()
    {
        SeedSearchProducts();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SS1", search: "Kaffee", sortBy: "name", sortDirection: "asc");

        Assert.Equal(new[] { "PS1", "PS3" }, result.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task GetProductsAsync_SearchAffectsTotalCount_ForPagination()
    {
        SeedSearchProducts();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SS1", search: "Kaffee");

        Assert.Equal(2, result.TotalCount);
    }

    private void SeedPropertyFilterProducts()
    {
        var category = new Category { Id = "CF", Name = "Filterkategorie" };
        var subcategory = new Subcategory { Id = "SF1", Name = "Filterunterkategorie", CategoryId = "CF" };
        _context.Categories.Add(category);
        _context.Subcategories.Add(subcategory);
        _context.SubcategoryProperties.AddRange(
            new SubcategoryProperty { SubcategoryId = "SF1", Name = "Bauform" },
            new SubcategoryProperty { SubcategoryId = "SF1", Name = "Kabellos" }
        );

        _context.Products.AddRange(
            new Product { Id = "PF1", Name = "Ohrhörer A", Description = "Beschreibung", SubcategoryId = "SF1" },
            new Product { Id = "PF2", Name = "Ohrhörer B", Description = "Beschreibung", SubcategoryId = "SF1" },
            new Product { Id = "PF3", Name = "Kopfhörer C", Description = "Beschreibung", SubcategoryId = "SF1" }
        );
        _context.ProductPropertyValues.AddRange(
            new ProductPropertyValue { ProductId = "PF1", Name = "Bauform", Value = "In-Ear" },
            new ProductPropertyValue { ProductId = "PF1", Name = "Kabellos", Value = "true" },
            new ProductPropertyValue { ProductId = "PF2", Name = "Bauform", Value = "In-Ear" },
            new ProductPropertyValue { ProductId = "PF2", Name = "Kabellos", Value = "false" },
            new ProductPropertyValue { ProductId = "PF3", Name = "Bauform", Value = "Over-Ear" },
            new ProductPropertyValue { ProductId = "PF3", Name = "Kabellos", Value = "true" }
        );

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetProductsAsync_PropertyFilterMatchesExactValue_ReturnsMatchingProducts()
    {
        SeedPropertyFilterProducts();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SF1", propertyFilters: new Dictionary<string, string> { ["Bauform"] = "In-Ear" });

        Assert.Equal(new[] { "PF1", "PF2" }, result.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task GetProductsAsync_PropertyFilterIsCaseSensitive_DifferentCaseYieldsNoMatch()
    {
        SeedPropertyFilterProducts();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SF1", propertyFilters: new Dictionary<string, string> { ["Bauform"] = "in-ear" });

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetProductsAsync_MultiplePropertyFiltersAreAndCombined_ReturnsOnlyProductsMatchingAll()
    {
        SeedPropertyFilterProducts();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SF1", propertyFilters: new Dictionary<string, string> { ["Bauform"] = "In-Ear", ["Kabellos"] = "true" });

        Assert.Equal(new[] { "PF1" }, result.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task GetProductsAsync_PropertyFilterOnNonexistentProperty_ReturnsEmptyListNotError()
    {
        SeedPropertyFilterProducts();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SF1", propertyFilters: new Dictionary<string, string> { ["Farbe"] = "Rot" });

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetProductsAsync_PropertyFilterAffectsTotalCount_ForPagination()
    {
        SeedPropertyFilterProducts();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SF1", propertyFilters: new Dictionary<string, string> { ["Bauform"] = "In-Ear" });

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetProductsAsync_PropertyFilterCombinedWithSearch_AppliesBothTogether()
    {
        SeedPropertyFilterProducts();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SF1", search: "Ohrhörer", propertyFilters: new Dictionary<string, string> { ["Bauform"] = "In-Ear" });

        Assert.Equal(new[] { "PF1", "PF2" }, result.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task GetProductsAsync_PropertyFilterCombinedWithSort_AppliesBothTogether()
    {
        SeedPropertyFilterProducts();

        var result = await _service.GetProductsAsync(1, subcategoryId: "SF1", propertyFilters: new Dictionary<string, string> { ["Bauform"] = "In-Ear" }, sortBy: "name", sortDirection: "asc");

        Assert.Equal(new[] { "PF1", "PF2" }, result.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task GetProductsAsync_PropertyFilterCombinedWithCategoryFilter_AppliesBothTogether()
    {
        SeedCategoryHierarchy();
        SeedPropertyFilterProducts();

        var result = await _service.GetProductsAsync(1, categoryId: "CF", propertyFilters: new Dictionary<string, string> { ["Bauform"] = "In-Ear" });

        Assert.Equal(new[] { "PF1", "PF2" }, result.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task GetCategoriesAsync_IncludesSubcategoryPropertyNamesSortedAlphabetically()
    {
        SeedPropertyFilterProducts();

        var categories = await _service.GetCategoriesAsync();

        var subcategory = Assert.Single(categories, c => c.Id == "CF").Subcategories.Single(s => s.Id == "SF1");
        Assert.Equal(new[] { "Bauform", "Kabellos" }, subcategory.Properties.Select(p => p.Name));
    }

    private void SeedProductDetailData()
    {
        var category = new Category { Id = "CD", Name = "Detailkategorie" };
        var subcategory = new Subcategory { Id = "SD1", Name = "Detailunterkategorie", CategoryId = "CD" };
        _context.Categories.Add(category);
        _context.Subcategories.Add(subcategory);

        var product = new Product { Id = "PD1", Name = "Detailprodukt", Description = "Eine Beschreibung", SubcategoryId = "SD1" };
        _context.Products.Add(product);
        _context.ProductPropertyValues.AddRange(
            new ProductPropertyValue { ProductId = "PD1", Name = "Farbe", Value = "Rot" },
            new ProductPropertyValue { ProductId = "PD1", Name = "Bauform", Value = "Kompakt" }
        );

        _context.Suppliers.AddRange(
            new Supplier { Id = "LD1", Name = "Lieferant Teuer" },
            new Supplier { Id = "LD2", Name = "Lieferant Günstig" }
        );
        _context.Offers.AddRange(
            new Offer { ProductId = "PD1", SupplierId = "LD1", Price = 29.99m },
            new Offer { ProductId = "PD1", SupplierId = "LD2", Price = 19.99m }
        );

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetProductDetailAsync_ReturnsNameDescriptionCategoryAndProperties()
    {
        SeedProductDetailData();

        var result = await _service.GetProductDetailAsync("PD1");

        Assert.NotNull(result);
        Assert.Equal("Detailprodukt", result!.Product.Name);
        Assert.Equal("Eine Beschreibung", result.Product.Description);
        Assert.Equal("SD1", result.Product.Subcategory!.Id);
        Assert.Equal("CD", result.Product.Subcategory.Category!.Id);
        Assert.Equal(2, result.Product.PropertyValues.Count);
    }

    [Fact]
    public async Task GetProductDetailAsync_ReturnsAllOffersWithSupplierAndPrice()
    {
        // Price-ascending ordering (AC 2) is applied in ProductsController, not CatalogService
        // (see ProductDetail story Dev Notes) — this test only verifies the raw offer data
        // the service hands to the controller is complete and correct, not its order.
        SeedProductDetailData();

        var result = await _service.GetProductDetailAsync("PD1");

        var offersBySupplier = result!.Product.Offers.ToDictionary(o => o.SupplierId, o => o.Price);
        Assert.Equal(new Dictionary<string, decimal> { ["LD1"] = 29.99m, ["LD2"] = 19.99m }, offersBySupplier);
    }

    [Fact]
    public async Task GetProductDetailAsync_NoRatings_ReturnsNullAverageAndZeroCount()
    {
        SeedProductDetailData();

        var result = await _service.GetProductDetailAsync("PD1");

        Assert.Null(result!.AverageRating);
        Assert.Equal(0, result.RatingCount);
    }

    [Fact]
    public async Task GetProductDetailAsync_WithRatings_ReturnsAverageAndCount()
    {
        SeedProductDetailData();
        _context.Ratings.AddRange(
            new Rating { ProductId = "PD1", AuthorName = "Anna", Score = 4 },
            new Rating { ProductId = "PD1", AuthorName = "Ben", Score = 2 }
        );
        _context.SaveChanges();

        var result = await _service.GetProductDetailAsync("PD1");

        Assert.Equal(3.0, result!.AverageRating);
        Assert.Equal(2, result.RatingCount);
    }

    [Fact]
    public async Task GetProductDetailAsync_IncrementsViewCountByOne()
    {
        SeedProductDetailData();

        var result = await _service.GetProductDetailAsync("PD1");

        Assert.Equal(1, result!.Product.ViewCount);
    }

    [Fact]
    public async Task GetProductDetailAsync_CalledTwice_ViewCountAccumulatesAcrossCalls()
    {
        SeedProductDetailData();

        await _service.GetProductDetailAsync("PD1");
        var result = await _service.GetProductDetailAsync("PD1");

        Assert.Equal(2, result!.Product.ViewCount);
    }

    [Fact]
    public async Task GetProductDetailAsync_UnknownId_ReturnsNull()
    {
        var result = await _service.GetProductDetailAsync("NICHT-VORHANDEN");

        Assert.Null(result);
    }
}
