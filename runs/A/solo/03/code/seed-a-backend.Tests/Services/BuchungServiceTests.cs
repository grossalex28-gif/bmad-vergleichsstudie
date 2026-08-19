using seed_a_backend.Api.Dtos;
using seed_a_backend.Api.Services;

namespace seed_a_backend.Tests.Services;

[Collection("Datenbank")]
public class BuchungServiceTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task CreateBuchungAsync_ErfolgreicheBuchung_BerechnetGesamtpreisUndBelegtSitzplaetze()
    {
        await using var context = fixture.CreateContext();
        var daten = await TestDataFactory.SeedVeranstaltungAsync(context);
        var service = new BuchungService(context);

        var buchung = await service.CreateBuchungAsync(new CreateBuchungRequestDto(
            daten.VeranstaltungId,
            "Erika Mustermann",
            "erika@example.com",
            [
                new SitzplatzAuswahlDto("A", 1, daten.KategorieAId),
                new SitzplatzAuswahlDto("A", 2, daten.KategorieBId)
            ]));

        Assert.Equal(30m, buchung.Gesamtpreis);
        Assert.Equal(2, buchung.Sitzplaetze.Count);
        Assert.NotEmpty(buchung.Referenz);

        var sitzplan = await new VeranstaltungService(context).GetSitzplanAsync(daten.VeranstaltungId);
        var a1 = sitzplan.Sitzplaetze.Single(s => s.Reihe == "A" && s.Spalte == 1);
        Assert.Equal(SitzplatzStatus.Belegt, a1.Status);
    }

    [Fact]
    public async Task CreateBuchungAsync_BereitsBelegterSitzplatz_LehntGesamteBuchungAb()
    {
        await using var context = fixture.CreateContext();
        var daten = await TestDataFactory.SeedVeranstaltungAsync(context);
        var service = new BuchungService(context);

        await service.CreateBuchungAsync(new CreateBuchungRequestDto(
            daten.VeranstaltungId, "Erste Person", "erste@example.com",
            [new SitzplatzAuswahlDto("A", 1, daten.KategorieAId)]));

        var exception = await Assert.ThrowsAsync<SeatConflictException>(() => service.CreateBuchungAsync(new CreateBuchungRequestDto(
            daten.VeranstaltungId, "Zweite Person", "zweite@example.com",
            [
                new SitzplatzAuswahlDto("A", 2, daten.KategorieAId),
                new SitzplatzAuswahlDto("A", 1, daten.KategorieAId)
            ])));

        Assert.Contains(exception.BelegteSitzplaetze, s => s.Reihe == "A" && s.Spalte == 1);

        // Es darf keine Teilbuchung entstehen: A2 muss weiterhin frei sein.
        var sitzplan = await new VeranstaltungService(context).GetSitzplanAsync(daten.VeranstaltungId);
        var a2 = sitzplan.Sitzplaetze.Single(s => s.Reihe == "A" && s.Spalte == 2);
        Assert.Equal(SitzplatzStatus.Frei, a2.Status);
    }

    [Fact]
    public async Task CreateBuchungAsync_GangAlsSitzplatz_WirftValidierungsfehler()
    {
        await using var context = fixture.CreateContext();
        var daten = await TestDataFactory.SeedVeranstaltungAsync(context);
        var service = new BuchungService(context);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateBuchungAsync(new CreateBuchungRequestDto(
            daten.VeranstaltungId, "Person", "person@example.com",
            [new SitzplatzAuswahlDto("A", 3, daten.KategorieAId)])));
    }

    [Fact]
    public async Task CreateBuchungAsync_UnbekannteVeranstaltung_WirftNotFound()
    {
        await using var context = fixture.CreateContext();
        var service = new BuchungService(context);

        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateBuchungAsync(new CreateBuchungRequestDto(
            "UNBEKANNT", "Person", "person@example.com",
            [new SitzplatzAuswahlDto("A", 1, "UNBEKANNT-A")])));
    }

    [Fact]
    public async Task StornierenAsync_GibtSitzplatzWiederFrei()
    {
        await using var context = fixture.CreateContext();
        var daten = await TestDataFactory.SeedVeranstaltungAsync(context);
        var service = new BuchungService(context);

        var buchung = await service.CreateBuchungAsync(new CreateBuchungRequestDto(
            daten.VeranstaltungId, "Person", "person@example.com",
            [new SitzplatzAuswahlDto("B", 1, daten.KategorieAId)]));

        var storniert = await service.StornierenAsync(buchung.Referenz);
        Assert.Equal(Api.Models.BuchungStatus.Storniert, storniert.Status);

        var sitzplan = await new VeranstaltungService(context).GetSitzplanAsync(daten.VeranstaltungId);
        var b1 = sitzplan.Sitzplaetze.Single(s => s.Reihe == "B" && s.Spalte == 1);
        Assert.Equal(SitzplatzStatus.Frei, b1.Status);
    }

    [Fact]
    public async Task StornierenAsync_BereitsStorniert_WirftValidierungsfehler()
    {
        await using var context = fixture.CreateContext();
        var daten = await TestDataFactory.SeedVeranstaltungAsync(context);
        var service = new BuchungService(context);

        var buchung = await service.CreateBuchungAsync(new CreateBuchungRequestDto(
            daten.VeranstaltungId, "Person", "person@example.com",
            [new SitzplatzAuswahlDto("C", 1, daten.KategorieAId)]));
        await service.StornierenAsync(buchung.Referenz);

        await Assert.ThrowsAsync<ValidationException>(() => service.StornierenAsync(buchung.Referenz));
    }

    [Fact]
    public async Task GetByReferenzAsync_UnbekannteReferenz_WirftNotFound()
    {
        await using var context = fixture.CreateContext();
        var service = new BuchungService(context);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByReferenzAsync("UNBEKANNT-1234"));
    }

    [Fact]
    public async Task CreateBuchungAsync_GleichzeitigeBuchungDesselbenSitzplatzes_NurEineErfolgreich()
    {
        await using var setupContext = fixture.CreateContext();
        var daten = await TestDataFactory.SeedVeranstaltungAsync(setupContext);

        await using var contextA = fixture.CreateContext();
        await using var contextB = fixture.CreateContext();
        var serviceA = new BuchungService(contextA);
        var serviceB = new BuchungService(contextB);

        var request = new CreateBuchungRequestDto(
            daten.VeranstaltungId, "Person A", "a@example.com",
            [new SitzplatzAuswahlDto("A", 1, daten.KategorieAId)]);
        var requestB = request with { Name = "Person B", Email = "b@example.com" };

        var taskA = serviceA.CreateBuchungAsync(request);
        var taskB = serviceB.CreateBuchungAsync(requestB);

        var ergebnisse = await Task.WhenAll(taskA.ContinueWith(TranslateOutcome), taskB.ContinueWith(TranslateOutcome));

        Assert.Single(ergebnisse, r => r == "erfolg");
        Assert.Single(ergebnisse, r => r == "konflikt");
    }

    private static string TranslateOutcome(Task<BuchungDto> task)
    {
        if (task.IsFaulted)
        {
            var inner = task.Exception!.Flatten().InnerException;
            if (inner is SeatConflictException)
            {
                return "konflikt";
            }

            throw inner ?? task.Exception;
        }

        return "erfolg";
    }
}
