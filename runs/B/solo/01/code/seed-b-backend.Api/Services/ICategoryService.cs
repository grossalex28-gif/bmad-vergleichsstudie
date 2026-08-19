using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Api.Services;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
}
