namespace seed_a_backend.Api.Application.Dtos;

public record EventListItemDto(Guid Id, string Title, string VenueName, DateTime StartsAt);
