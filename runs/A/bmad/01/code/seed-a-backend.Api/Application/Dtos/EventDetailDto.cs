namespace seed_a_backend.Api.Application.Dtos;

public record EventDetailDto(Guid Id, string Title, string Description, int DurationMinutes, int AgeRating, string VenueName, string RoomName, DateTime StartsAt);
