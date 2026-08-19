namespace seed_b_backend.Api.Dtos;

public record PagedResultDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages, IReadOnlyList<AttributeFilterOptionDto> AvailableAttributeFilters);
