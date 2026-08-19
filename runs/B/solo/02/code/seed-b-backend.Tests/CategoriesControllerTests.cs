using Microsoft.AspNetCore.Mvc;
using seed_b_backend.Api.Controllers;
using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Tests;

public class CategoriesControllerTests
{
    [Fact]
    public async Task GetCategories_ReturnsTopLevelCategories_WithNestedSubcategoriesAndPropertyNames()
    {
        using var db = TestDbFactory.CreateSeededContext();
        var controller = new CategoriesController(db);

        var result = await controller.GetCategories();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var categories = Assert.IsType<List<CategoryDto>>(ok.Value);
        var electronics = Assert.Single(categories);
        Assert.Equal("Elektronik", electronics.Name);
        var headphones = Assert.Single(electronics.Subcategories);
        Assert.Equal("Kopfhörer", headphones.Name);
        Assert.Equal(["Bauform", "Kabellos"], headphones.PropertyNames);
    }
}
