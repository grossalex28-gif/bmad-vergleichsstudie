using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using seed_a_backend.Api.Application.Dtos;
using seed_a_backend.Api.Data;
using seed_a_backend.Api.Domain;

namespace seed_a_backend.Api.Application;

public class CreateBookingResult
{
    public BookingDto? Booking { get; private init; }
    public List<string>? InvalidFields { get; private init; }
    public string? ValidationDetail { get; private init; }
    public List<string>? ConflictingSeats { get; private init; }

    public static CreateBookingResult Success(BookingDto booking) => new() { Booking = booking };
    public static CreateBookingResult Invalid(List<string> fields, string detail) =>
        new() { InvalidFields = fields, ValidationDetail = detail };
    public static CreateBookingResult Conflict(List<string> seatLabels) =>
        new() { ConflictingSeats = seatLabels };
}

public class CancelBookingResult
{
    public BookingDto? Booking { get; private init; }
    public bool NotFound { get; private init; }
    public bool AlreadyCancelled { get; private init; }

    public static CancelBookingResult Success(BookingDto booking) => new() { Booking = booking };
    public static CancelBookingResult NotFoundResult() => new() { NotFound = true };
    public static CancelBookingResult AlreadyCancelledResult() => new() { AlreadyCancelled = true };
}

public class BookingService(AppDbContext dbContext)
{
    public async Task<CreateBookingResult> CreateBookingAsync(CreateBookingRequestDto request)
    {
        var invalidFields = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Name)) invalidFields.Add("name");
        if (string.IsNullOrWhiteSpace(request.Email)) invalidFields.Add("email");
        if (request.Seats is null || request.Seats.Count == 0) invalidFields.Add("seats");
        if (invalidFields.Count > 0)
        {
            return CreateBookingResult.Invalid(invalidFields, "Pflichtangaben fehlen.");
        }

        var seatIds = request.Seats!.Select(s => s.SeatId).ToList();
        if (seatIds.Distinct().Count() != seatIds.Count)
        {
            return CreateBookingResult.Invalid(["seats"], "Ein Sitzplatz wurde mehrfach angefragt.");
        }

        var room = await dbContext.Events
            .Where(e => e.Id == request.EventId)
            .Select(e => (Guid?)e.RoomId)
            .FirstOrDefaultAsync();
        if (room is null)
        {
            return CreateBookingResult.Invalid(["seats"], "Unbekannte Veranstaltung.");
        }

        var validSeatIds = await dbContext.Seats
            .Where(s => s.RoomId == room.Value && seatIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();
        var validCategoryIds = await dbContext.PriceCategories
            .Where(pc => pc.EventId == request.EventId)
            .Select(pc => pc.Id)
            .ToListAsync();
        var hasInvalidSeatOrCategory = request.Seats!.Any(s =>
            !validSeatIds.Contains(s.SeatId) || !validCategoryIds.Contains(s.PriceCategoryId));
        if (hasInvalidSeatOrCategory)
        {
            return CreateBookingResult.Invalid(["seats"], "Ein Sitzplatz oder eine Preiskategorie ist ungültig.");
        }

        // EF Core InMemory (Unit-Tests, AC7) unterstützt keine echten Transaktionen — nur bei
        // relationalen Providern (SQL Server) wird eine echte Transaktion gestartet.
        await using IDbContextTransaction? transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted)
            : null;

        // Schritt 1 (AD-1): aktive Konflikte lesen, BEVOR irgendetwas eingefügt wird.
        var conflictingSeatIds = await dbContext.BookingSeats
            .Where(bs => bs.EventId == request.EventId && bs.IsActive && seatIds.Contains(bs.SeatId))
            .Select(bs => bs.SeatId)
            .ToListAsync();

        if (conflictingSeatIds.Count > 0)
        {
            if (transaction is not null) await transaction.RollbackAsync();
            return CreateBookingResult.Conflict(await SeatLabelsAsync(conflictingSeatIds));
        }

        var reference = await GenerateUniqueReferenceAsync();
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            Reference = reference,
            EventId = request.EventId,
            Name = request.Name!.Trim(),
            Email = request.Email!.Trim(),
            Status = BookingStatus.Active
        };
        dbContext.Bookings.Add(booking);

        foreach (var seat in request.Seats!)
        {
            dbContext.BookingSeats.Add(new BookingSeat
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                EventId = request.EventId,
                SeatId = seat.SeatId,
                PriceCategoryId = seat.PriceCategoryId,
                IsActive = true
            });
        }

        try
        {
            // Schritt 2 (AD-1): Insert — der filtered unique index fängt eine echte Race
            // zwischen Schritt 1 und 2 zweier gleichzeitiger Transaktionen ab.
            await dbContext.SaveChangesAsync();
            if (transaction is not null) await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            if (transaction is not null) await transaction.RollbackAsync();
            dbContext.ChangeTracker.Clear();
            var stillConflicting = await dbContext.BookingSeats
                .Where(bs => bs.EventId == request.EventId && bs.IsActive && seatIds.Contains(bs.SeatId))
                .Select(bs => bs.SeatId)
                .ToListAsync();
            if (stillConflicting.Count == 0)
            {
                // Der SaveChanges-Fehlschlag kam nicht vom Sitzplatz-Unique-Index (AD-1) —
                // z. B. eine Referenz-Kollision (AD-3) oder ein transienter DB-Fehler. Als
                // Sitzplatzkonflikt zu melden wäre irreführend (leere conflictingSeats-Liste).
                throw;
            }
            return CreateBookingResult.Conflict(await SeatLabelsAsync(stillConflicting));
        }

        return CreateBookingResult.Success(await BuildBookingDtoAsync(booking.Id));
    }

    private async Task<List<string>> SeatLabelsAsync(List<Guid> seatIds) =>
        await dbContext.Seats
            .Where(s => seatIds.Contains(s.Id))
            .Select(s => s.Row + s.Column.ToString())
            .ToListAsync();

    private async Task<string> GenerateUniqueReferenceAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = BookingReferenceGenerator.Generate();
            if (!await dbContext.Bookings.AnyAsync(b => b.Reference == candidate))
            {
                return candidate;
            }
        }
        throw new InvalidOperationException("Konnte keine eindeutige Buchungsreferenz erzeugen.");
    }

    public async Task<BookingDto?> GetBookingByReferenceAsync(string reference)
    {
        var trimmedReference = reference.Trim();
        var bookingId = await dbContext.Bookings
            .Where(b => b.Reference == trimmedReference)
            .Select(b => (Guid?)b.Id)
            .FirstOrDefaultAsync();

        return bookingId is null ? null : await BuildBookingDtoAsync(bookingId.Value);
    }

    public async Task<CancelBookingResult> CancelBookingAsync(string reference)
    {
        var trimmedReference = reference.Trim();
        var booking = await dbContext.Bookings
            .Include(b => b.Seats)
            .FirstOrDefaultAsync(b => b.Reference == trimmedReference);

        if (booking is null)
        {
            return CancelBookingResult.NotFoundResult();
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            return CancelBookingResult.AlreadyCancelledResult();
        }

        await using IDbContextTransaction? transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted)
            : null;

        booking.Status = BookingStatus.Cancelled;
        foreach (var seat in booking.Seats)
        {
            seat.IsActive = false;
        }

        await dbContext.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();

        return CancelBookingResult.Success(await BuildBookingDtoAsync(booking.Id));
    }

    private async Task<BookingDto> BuildBookingDtoAsync(Guid bookingId)
    {
        var booking = await dbContext.Bookings
            .Where(b => b.Id == bookingId)
            .Select(b => new
            {
                b.Reference,
                b.EventId,
                b.Status,
                EventTitle = b.Event!.Titel,
                VenueName = b.Event!.Venue!.Name,
                StartsAt = b.Event!.Zeitpunkt
            })
            .SingleAsync();

        var seats = await dbContext.BookingSeats
            .Where(bs => bs.BookingId == bookingId)
            .Join(dbContext.Seats, bs => bs.SeatId, s => s.Id, (bs, s) => new { bs, s })
            .Join(dbContext.PriceCategories, x => x.bs.PriceCategoryId, pc => pc.Id, (x, pc) =>
                new BookingSeatDto(x.s.Id, x.s.Row, x.s.Column, pc.Id, pc.Name, pc.Preis))
            .ToListAsync();

        var totalCents = seats.Sum(s => (long)Math.Round(s.Price * 100));
        return new BookingDto(booking.Reference, booking.EventId, booking.EventTitle, booking.VenueName, booking.StartsAt, booking.Status.ToString(), seats, totalCents / 100m);
    }
}
