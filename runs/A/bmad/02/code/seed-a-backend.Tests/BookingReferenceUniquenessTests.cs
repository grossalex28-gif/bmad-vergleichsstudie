using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Domain;
using seed_a_backend.Api.Infrastructure;

namespace seed_a_backend.Tests;

/// <summary>
/// Verifiziert die AD-4-Anforderung einer case-insensitiven Unique-Constraint auf
/// `Booking.Reference` gegen die echte SQL-Server-Collation, statt sie nur anzunehmen.
/// </summary>
public class BookingReferenceUniquenessTests : IAsyncLifetime
{
    private readonly SqlServerTestDatabase _database = new();

    public ValueTask InitializeAsync() => _database.InitializeAsync();

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    [Fact]
    public async Task Zwei_Buchungen_mit_gleicher_Referenz_in_unterschiedlicher_Gross_Kleinschreibung_verletzen_die_Unique_Constraint()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();

        db.Bookings.Add(new Booking { Reference = "ABCD2345", Name = "Erika Musterfrau", Email = "erika@example.com" });
        await db.SaveChangesAsync(ct);

        db.Bookings.Add(new Booking { Reference = "abcd2345", Name = "Max Mustermann", Email = "max@example.com" });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task Zwei_Buchungen_mit_unterschiedlicher_Referenz_werden_beide_gespeichert()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();

        db.Bookings.Add(new Booking { Reference = "ABCD2345", Name = "Erika Musterfrau", Email = "erika@example.com" });
        db.Bookings.Add(new Booking { Reference = "EFGH6789", Name = "Max Mustermann", Email = "max@example.com" });
        await db.SaveChangesAsync(ct);

        Assert.Equal(2, await db.Bookings.CountAsync(ct));
    }
}
