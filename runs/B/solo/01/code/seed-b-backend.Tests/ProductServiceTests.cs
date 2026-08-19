using seed_b_backend.Api.Services;

namespace seed_b_backend.Tests;

public class ProductServiceTests
{
    private static ProductQuery EmptyQuery(
        string? search = null,
        string? categoryId = null,
        string? subcategoryId = null,
        ProductSort sort = ProductSort.NameAsc,
        int page = 1,
        Dictionary<string, string>? eigenschaften = null) =>
        new(search, categoryId, subcategoryId, sort, page, eigenschaften ?? new());

    [Fact]
    public async Task GetProductsAsync_FiltersBySubcategory()
    {
        using var db = TestDbContextFactory.CreateWithSeedData();
        var sut = new ProductService(db);

        var result = await sut.GetProductsAsync(EmptyQuery(subcategoryId: "K1a"));

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetProductsAsync_FiltersBySearchTermAcrossNameAndDescription_CaseInsensitive()
    {
        using var db = TestDbContextFactory.CreateWithSeedData();
        var sut = new ProductService(db);

        var result = await sut.GetProductsAsync(EmptyQuery(search: "OVER-EAR"));

        var item = Assert.Single(result.Items);
        Assert.Equal("P2", item.Id);
    }

    [Fact]
    public async Task GetProductsAsync_FiltersByEigenschaft()
    {
        using var db = TestDbContextFactory.CreateWithSeedData();
        var sut = new ProductService(db);

        var result = await sut.GetProductsAsync(EmptyQuery(eigenschaften: new() { ["Kabellos"] = "true" }));

        var item = Assert.Single(result.Items);
        Assert.Equal("P1", item.Id);
    }

    [Fact]
    public async Task GetProductsAsync_SortsByPriceAscending_UsingMinimumOfferPrice()
    {
        using var db = TestDbContextFactory.CreateWithSeedData();
        var sut = new ProductService(db);

        var result = await sut.GetProductsAsync(EmptyQuery(sort: ProductSort.PriceAsc));

        Assert.Equal(["P1", "P2"], result.Items.Select(p => p.Id));
        Assert.Equal(27.50m, result.Items[0].MinPreis);
    }

    [Fact]
    public async Task GetProductsAsync_SortsByViewsDescending()
    {
        using var db = TestDbContextFactory.CreateWithSeedData();
        var sut = new ProductService(db);

        var result = await sut.GetProductsAsync(EmptyQuery(sort: ProductSort.ViewsDesc));

        Assert.Equal(["P2", "P1"], result.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task GetProductDetailAsync_IncrementsAufrufeOnEachCall()
    {
        using var db = TestDbContextFactory.CreateWithSeedData();
        var sut = new ProductService(db);

        var first = await sut.GetProductDetailAsync("P1");
        var second = await sut.GetProductDetailAsync("P1");

        Assert.Equal(1, first!.Aufrufe);
        Assert.Equal(2, second!.Aufrufe);
    }

    [Fact]
    public async Task GetProductDetailAsync_ReturnsNull_ForUnknownProduct()
    {
        using var db = TestDbContextFactory.CreateWithSeedData();
        var sut = new ProductService(db);

        var result = await sut.GetProductDetailAsync("does-not-exist");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductDetailAsync_ReturnsAllOffersSortedByPrice()
    {
        using var db = TestDbContextFactory.CreateWithSeedData();
        var sut = new ProductService(db);

        var result = await sut.GetProductDetailAsync("P1");

        Assert.Equal(["L2", "L1"], result!.Angebote.Select(o => o.LieferantId));
    }

    [Fact]
    public async Task AddOrReplaceRatingAsync_AddsNewRating()
    {
        using var db = TestDbContextFactory.CreateWithSeedData();
        var sut = new ProductService(db);

        var result = await sut.AddOrReplaceRatingAsync("P1", "Anna", 5);

        Assert.Equal(5, result!.DurchschnittsBewertung);
        Assert.Equal(1, result.AnzahlBewertungen);
    }

    [Fact]
    public async Task AddOrReplaceRatingAsync_ReplacesPreviousRating_BySameAuthor()
    {
        using var db = TestDbContextFactory.CreateWithSeedData();
        var sut = new ProductService(db);

        await sut.AddOrReplaceRatingAsync("P1", "Anna", 5);
        var result = await sut.AddOrReplaceRatingAsync("P1", "anna", 1);

        Assert.Equal(1, result!.DurchschnittsBewertung);
        Assert.Equal(1, result.AnzahlBewertungen);
    }

    [Fact]
    public async Task AddOrReplaceRatingAsync_ReturnsNull_ForUnknownProduct()
    {
        using var db = TestDbContextFactory.CreateWithSeedData();
        var sut = new ProductService(db);

        var result = await sut.AddOrReplaceRatingAsync("does-not-exist", "Anna", 5);

        Assert.Null(result);
    }
}
