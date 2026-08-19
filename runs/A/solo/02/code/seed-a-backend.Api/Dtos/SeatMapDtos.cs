namespace seed_a_backend.Api.Dtos;

public record SeatMapDto(
    string EventId,
    string RoomId,
    string RoomName,
    List<string> RowLabels,
    int Columns,
    List<SeatRowDto> Rows
);

public record SeatRowDto(string Row, List<SeatDto> Seats);

public record SeatDto(int Column, string Type, string Status);
