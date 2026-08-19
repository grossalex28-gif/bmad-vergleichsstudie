namespace seed_a_backend.Api.Dtos;

public record VenueDto(string Id, string Name, List<RoomSummaryDto> Rooms);

public record RoomSummaryDto(string Id, string Name);
