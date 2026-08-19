using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Data.Entities;
using seed_b_backend.Api.Services;

namespace seed_b_backend.Tests.Services;

public class CategoryQueryServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsCategoriesAndSubcategoriesSortedByName()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C2", Name = "Kategorie 2" });
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1b", Name = "Unterkategorie 1b", CategoryId = "C1" });
        db.Subcategories.Add(new Subcategory { Id = "S1a", Name = "Unterkategorie 1a", CategoryId = "C1" });
        db.Subcategories.Add(new Subcategory { Id = "S2a", Name = "Unterkategorie 2a", CategoryId = "C2" });
        await db.SaveChangesAsync();
        var service = new CategoryQueryService(db);

        var result = await service.GetCategoriesAsync();

        Assert.Equal(2, result.Count);

        Assert.Equal("C1", result[0].Id);
        Assert.Equal("Kategorie 1", result[0].Name);
        Assert.Equal(2, result[0].Subcategories.Count);
        Assert.Equal("S1a", result[0].Subcategories[0].Id);
        Assert.Equal("Unterkategorie 1a", result[0].Subcategories[0].Name);
        Assert.Equal("S1b", result[0].Subcategories[1].Id);
        Assert.Equal("Unterkategorie 1b", result[0].Subcategories[1].Name);

        Assert.Equal("C2", result[1].Id);
        Assert.Equal("Kategorie 2", result[1].Name);
        Assert.Single(result[1].Subcategories);
        Assert.Equal("S2a", result[1].Subcategories[0].Id);
        Assert.Equal("Unterkategorie 2a", result[1].Subcategories[0].Name);
    }

    [Fact]
    public async Task GetCategoriesAsync_BreaksTiesOnDuplicateNameById()
    {
        await using var db = CreateContext();
        db.Categories.Add(new Category { Id = "C2", Name = "Gleicher Name" });
        db.Categories.Add(new Category { Id = "C1", Name = "Gleicher Name" });
        db.Subcategories.Add(new Subcategory { Id = "S1b", Name = "Gleiche Unterkategorie", CategoryId = "C1" });
        db.Subcategories.Add(new Subcategory { Id = "S1a", Name = "Gleiche Unterkategorie", CategoryId = "C1" });
        await db.SaveChangesAsync();
        var service = new CategoryQueryService(db);

        var result = await service.GetCategoriesAsync();

        Assert.Equal(["C1", "C2"], result.Select(c => c.Id));
        Assert.Equal(["S1a", "S1b"], result[0].Subcategories.Select(s => s.Id));
    }
}
