using seed_a_backend.Api.Dtos;
using seed_a_backend.Api.Models;

namespace seed_a_backend.Api.Services;

public static class SeatMapBuilder
{
    public static SeatMapDto Build(Event @event, Room room, IReadOnlyCollection<BookingSeat> activeSeats)
    {
        var occupied = activeSeats
            .Where(s => !s.IsCancelled)
            .Select(s => (s.RowLabel, s.Column))
            .ToHashSet();

        var rows = room.RowLabels.Select(row =>
        {
            var seats = Enumerable.Range(1, room.Columns).Select(column =>
            {
                if (room.AisleColumns.Contains(column))
                {
                    return new SeatDto(column, "aisle", "aisle");
                }

                var status = occupied.Contains((row, column)) ? "occupied" : "free";
                return new SeatDto(column, "seat", status);
            }).ToList();

            return new SeatRowDto(row, seats);
        }).ToList();

        return new SeatMapDto(@event.Id, room.Id, room.Name, room.RowLabels, room.Columns, rows);
    }
}
