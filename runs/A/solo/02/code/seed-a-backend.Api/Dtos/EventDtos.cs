namespace seed_a_backend.Api.Dtos;

public record EventListItemDto(
    string Id,
    string Title,
    string VenueId,
    string VenueName,
    DateTime StartsAt
);

public record EventDetailDto(
    string Id,
    string Title,
    string Description,
    int DurationMinutes,
    int AgeRating,
    string VenueId,
    string VenueName,
    string RoomId,
    string RoomName,
    DateTime StartsAt,
    List<PriceCategoryDto> PriceCategories
);

public record PriceCategoryDto(string Id, string Name, decimal Price);
