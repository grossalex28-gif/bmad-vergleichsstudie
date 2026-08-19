using seed_b_backend.Api.Dtos;
using seed_b_backend.Api.Services;

namespace seed_b_backend.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task GetDetailAndRegisterViewAsync_IncrementsViewCountOnEachCall()
    {
        using var db = TestDb.CreateWithFixtures();
        var service = new ProductService(db);

        var first = await service.GetDetailAndRegisterViewAsync("P1");
        var second = await service.GetDetailAndRegisterViewAsync("P1");

        Assert.Equal(1, first!.ViewCount);
        Assert.Equal(2, second!.ViewCount);
    }

    [Fact]
    public async Task GetDetailAndRegisterViewAsync_UnknownProduct_ReturnsNull()
    {
        using var db = TestDb.CreateWithFixtures();
        var service = new ProductService(db);

        var result = await service.GetDetailAndRegisterViewAsync("does-not-exist");

        Assert.Null(result);
    }

    [Fact]
    public async Task RateProductAsync_FirstRating_IsStored()
    {
        using var db = TestDb.CreateWithFixtures();
        var service = new ProductService(db);

        var result = await service.RateProductAsync("P1", new CreateRatingRequest("Alice", 4));

        Assert.NotNull(result);
        Assert.Equal(4, result!.Stars);

        var detail = await service.GetDetailAndRegisterViewAsync("P1");
        Assert.Equal(1, detail!.RatingCount);
        Assert.Equal(4, detail.AverageRating);
    }

    [Fact]
    public async Task RateProductAsync_SameAuthorRatesAgain_ReplacesPreviousRatingInsteadOfAdding()
    {
        using var db = TestDb.CreateWithFixtures();
        var service = new ProductService(db);

        await service.RateProductAsync("P1", new CreateRatingRequest("Alice", 5));
        await service.RateProductAsync("P1", new CreateRatingRequest("Alice", 2));

        var detail = await service.GetDetailAndRegisterViewAsync("P1");

        Assert.Equal(1, detail!.RatingCount);
        Assert.Equal(2, detail.AverageRating);
    }

    [Fact]
    public async Task RateProductAsync_UnknownProduct_ReturnsNull()
    {
        using var db = TestDb.CreateWithFixtures();
        var service = new ProductService(db);

        var result = await service.RateProductAsync("does-not-exist", new CreateRatingRequest("Alice", 3));

        Assert.Null(result);
    }

    [Fact]
    public async Task GetListAsync_FiltersByCategorySpecificProperty()
    {
        using var db = TestDb.CreateWithFixtures();
        var service = new ProductService(db);

        var result = await service.GetListAsync(new ProductQuery
        {
            Properties = new Dictionary<string, string> { ["Bauform"] = "Over-Ear" },
        });

        Assert.Single(result.Items);
        Assert.Equal("P2", result.Items[0].Id);
    }

    [Fact]
    public async Task GetListAsync_SortsByPriceAscendingUsingCheapestOffer()
    {
        using var db = TestDb.CreateWithFixtures();
        var service = new ProductService(db);

        var result = await service.GetListAsync(new ProductQuery { Sort = ProductSort.PriceAsc });

        // P1 günstigstes Angebot 27.50 (L2), P2 einziges Angebot 79.00.
        Assert.Equal(["P1", "P2"], result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetListAsync_SearchMatchesNameAndDescription()
    {
        using var db = TestDb.CreateWithFixtures();
        var service = new ProductService(db);

        var result = await service.GetListAsync(new ProductQuery { Search = "Kabel" });

        Assert.Single(result.Items);
        Assert.Equal("P2", result.Items[0].Id);
    }
}
