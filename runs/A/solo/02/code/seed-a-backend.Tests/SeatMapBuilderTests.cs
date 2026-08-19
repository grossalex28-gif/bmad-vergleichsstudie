using seed_a_backend.Api.Models;
using seed_a_backend.Api.Services;

namespace seed_a_backend.Tests;

public class SeatMapBuilderTests
{
    private static (Event Event, Room Room) BuildRoom()
    {
        var room = new Room
        {
            Id = "R1",
            Name = "Kleiner Saal",
            VenueId = "V1",
            RowLabels = ["A", "B"],
            Columns = 3,
            AisleColumns = [2]
        };
        var @event = new Event { Id = "E1", Title = "Test", Description = "Test", VenueId = "V1", RoomId = "R1" };
        return (@event, room);
    }

    [Fact]
    public void Build_MarksConfiguredColumnAsAisleOnEveryRow()
    {
        var (@event, room) = BuildRoom();

        var map = SeatMapBuilder.Build(@event, room, []);

        Assert.All(map.Rows, row => Assert.Equal("aisle", row.Seats[1].Type));
    }

    [Fact]
    public void Build_MarksSeatWithActiveBookingAsOccupied()
    {
        var (@event, room) = BuildRoom();
        var seats = new List<BookingSeat>
        {
            new() { EventId = "E1", RowLabel = "A", Column = 1, IsCancelled = false }
        };

        var map = SeatMapBuilder.Build(@event, room, seats);

        var seatA1 = map.Rows.Single(r => r.Row == "A").Seats.Single(s => s.Column == 1);
        Assert.Equal("occupied", seatA1.Status);
    }

    [Fact]
    public void Build_TreatsCancelledBookingSeatAsFree()
    {
        var (@event, room) = BuildRoom();
        var seats = new List<BookingSeat>
        {
            new() { EventId = "E1", RowLabel = "A", Column = 1, IsCancelled = true }
        };

        var map = SeatMapBuilder.Build(@event, room, seats);

        var seatA1 = map.Rows.Single(r => r.Row == "A").Seats.Single(s => s.Column == 1);
        Assert.Equal("free", seatA1.Status);
    }

    [Fact]
    public void Build_FreeSeatNotInAisle_HasFreeStatus()
    {
        var (@event, room) = BuildRoom();

        var map = SeatMapBuilder.Build(@event, room, []);

        var seatB3 = map.Rows.Single(r => r.Row == "B").Seats.Single(s => s.Column == 3);
        Assert.Equal("seat", seatB3.Type);
        Assert.Equal("free", seatB3.Status);
    }
}
