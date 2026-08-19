namespace seed_a_backend.Api.Application;

public record SeatMapRow(string RowLabel, IReadOnlyList<SeatMapCell> Cells);
