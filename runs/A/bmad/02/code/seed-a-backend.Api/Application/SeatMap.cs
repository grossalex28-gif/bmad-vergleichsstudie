namespace seed_a_backend.Api.Application;

public record SeatMap(IReadOnlyList<string> RowLabels, int ColumnCount, IReadOnlyList<int> AisleColumns, IReadOnlyList<SeatMapRow> Rows);
