namespace seed_a_backend.Api.Controllers.Dtos;

public record SeatMapRowDto(string RowLabel, IReadOnlyList<SeatMapCellDto> Cells);
