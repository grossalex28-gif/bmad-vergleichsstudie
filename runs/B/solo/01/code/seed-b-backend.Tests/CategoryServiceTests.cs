using seed_b_backend.Api.Services;

namespace seed_b_backend.Tests;

public class CategoryServiceTests
{
    [Fact]
    public async Task GetCategoriesAsync_ReturnsCategoriesWithSubcategoriesAndEigenschaften()
    {
        using var db = TestDbContextFactory.CreateWithSeedData();
        var sut = new CategoryService(db);

        var result = await sut.GetCategoriesAsync();

        var category = Assert.Single(result);
        Assert.Equal("K1", category.Id);
        var subcategory = Assert.Single(category.Unterkategorien);
        Assert.Equal(["Bauform", "Kabellos"], subcategory.Eigenschaften);
    }
}
