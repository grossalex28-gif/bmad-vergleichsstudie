using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using seed_b_backend.Api.Controllers;
using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Tests;

public class ProductsControllerTests
{
    private static ProductsController CreateController(Dictionary<string, StringValues>? query = null)
    {
        var db = TestDbFactory.CreateSeededContext();
        var httpContext = new DefaultHttpContext
        {
            Request = { Query = new QueryCollection(query ?? new Dictionary<string, StringValues>()) },
        };
        return new ProductsController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
    }

    [Fact]
    public async Task GetProducts_ReturnsAllSeededProducts_ByDefault()
    {
        var controller = CreateController();

        var result = await controller.GetProducts(null, null, null, null, null, 1);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<PagedResultDto<ProductListItemDto>>(ok.Value);
        Assert.Equal(2, payload.TotalCount);
    }

    [Fact]
    public async Task GetProducts_FiltersByPropertyValue()
    {
        var controller = CreateController(new Dictionary<string, StringValues>
        {
            ["Bauform"] = "Over-Ear",
        });

        var result = await controller.GetProducts(null, null, null, null, null, 1);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<PagedResultDto<ProductListItemDto>>(ok.Value);
        Assert.Single(payload.Items);
        Assert.Equal("Bügelkopfhörer Studio", payload.Items[0].Name);
    }

    [Fact]
    public async Task GetProducts_SortsByPriceDescending()
    {
        var controller = CreateController();

        var result = await controller.GetProducts(null, null, null, "price", "desc", 1);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<PagedResultDto<ProductListItemDto>>(ok.Value);
        Assert.Equal("Bügelkopfhörer Studio", payload.Items[0].Name);
    }

    [Fact]
    public async Task GetProducts_SearchesNameAndDescription()
    {
        var controller = CreateController(new Dictionary<string, StringValues>
        {
            ["search"] = "Kompakte",
        });

        var result = await controller.GetProducts(null, null, "Kompakte", null, null, 1);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<PagedResultDto<ProductListItemDto>>(ok.Value);
        Assert.Single(payload.Items);
        Assert.Equal("Ohrhörer Compact", payload.Items[0].Name);
    }

    [Fact]
    public async Task GetProduct_IncrementsViewCount_OnEachCall()
    {
        var controller = CreateController();

        var first = Assert.IsType<OkObjectResult>((await controller.GetProduct(1)).Result);
        var firstDto = Assert.IsType<ProductDetailDto>(first.Value);
        Assert.Equal(1, firstDto.ViewCount);

        var second = Assert.IsType<OkObjectResult>((await controller.GetProduct(1)).Result);
        var secondDto = Assert.IsType<ProductDetailDto>(second.Value);
        Assert.Equal(2, secondDto.ViewCount);
    }

    [Fact]
    public async Task GetProduct_ReturnsNotFound_ForUnknownId()
    {
        var controller = CreateController();

        var result = await controller.GetProduct(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task AddReview_CreatesNewReview()
    {
        var controller = CreateController();

        var result = await controller.AddReview(1, new ReviewCreateDto("Max", 4));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ReviewResultDto>(ok.Value);
        Assert.Equal(1, dto.RatingCount);
        Assert.Equal(4, dto.AverageRating);
    }

    [Fact]
    public async Task AddReview_BySameAuthor_ReplacesPreviousReview()
    {
        var controller = CreateController();
        await controller.AddReview(1, new ReviewCreateDto("Max", 5));

        var result = await controller.AddReview(1, new ReviewCreateDto("Max", 2));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ReviewResultDto>(ok.Value);
        Assert.Equal(1, dto.RatingCount);
        Assert.Equal(2, dto.AverageRating);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task AddReview_RejectsRatingOutOfRange(int rating)
    {
        var controller = CreateController();

        var result = await controller.AddReview(1, new ReviewCreateDto("Max", rating));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
