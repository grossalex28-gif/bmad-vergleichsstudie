namespace seed_a_backend.Api.Controllers.Dtos;

public record SeatMapDto(IReadOnlyList<string> RowLabels, int ColumnCount, IReadOnlyList<int> AisleColumns, IReadOnlyList<SeatMapRowDto> Rows);
