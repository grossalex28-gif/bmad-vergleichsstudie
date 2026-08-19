namespace seed_a_backend.Api.Application.Dtos;

public enum SeatStatus
{
    Free,
    Occupied
}

public record SeatDto(Guid SeatId, string Row, int Column, SeatStatus Status);

public record PriceCategoryDto(Guid Id, string Name, decimal Price);

public record SeatMapDto(Guid EventId, List<string> Rows, int Columns, List<int> AisleColumns, List<SeatDto> Seats, List<PriceCategoryDto> PriceCategories);
