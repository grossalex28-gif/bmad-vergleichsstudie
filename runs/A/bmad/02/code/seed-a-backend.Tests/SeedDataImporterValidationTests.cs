using seed_a_backend.Api.Infrastructure;
using seed_a_backend.Api.Infrastructure.SeedModels;

namespace seed_a_backend.Tests;

public class SeedDataImporterValidationTests : IAsyncLifetime
{
    private readonly SqlServerTestDatabase _database = new();

    public ValueTask InitializeAsync() => _database.InitializeAsync();

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    private static SeedVenueDto Venue(string id, params SeedRoomDto[] rooms) =>
        new() { Id = id, Name = id, Raeume = rooms.ToList() };

    private static SeedRoomDto Room(string id, int spalten = 5, List<int>? gangSpalten = null) => new()
    {
        Id = id,
        Name = id,
        Reihen = new List<string?> { "A", "B" },
        Spalten = spalten,
        Gang = gangSpalten is null ? null : new SeedAisleDto { Spalten = gangSpalten },
    };

    private static SeedEventDto Event(string id, string spielstaetteId, string raumId, params string[] belegteSitzplaetze) => new()
    {
        Id = id,
        Titel = id,
        Beschreibung = id,
        SpielstaetteId = spielstaetteId,
        RaumId = raumId,
        DauerMinuten = 60,
        Preiskategorien = new(),
        BelegteSitzplaetze = belegteSitzplaetze.Select(s => (string?)s).ToList(),
    };

    [Fact]
    public async Task ImportAsync_wirft_bei_unbekannter_SpielstaetteId()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var data = new SeedDataDto
        {
            Spielstaetten = new() { Venue("V1", Room("R1")) },
            Veranstaltungen = new() { Event("E1", "UNBEKANNT", "R1") },
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_bei_doppelter_Raum_Id_in_derselben_Spielstaette()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var data = new SeedDataDto
        {
            Spielstaetten = new() { Venue("V1", Room("R1"), Room("R1")) },
            Veranstaltungen = new(),
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_bei_unbekannter_RaumId()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var data = new SeedDataDto
        {
            Spielstaetten = new() { Venue("V1", Room("R1")) },
            Veranstaltungen = new() { Event("E1", "V1", "UNBEKANNT") },
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_wenn_Sitzplatz_ausserhalb_des_Raum_Grids_liegt()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var data = new SeedDataDto
        {
            Spielstaetten = new() { Venue("V1", Room("R1", spalten: 5)) },
            Veranstaltungen = new() { Event("E1", "V1", "R1", "A9") },
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_wenn_Sitzplatz_unbekannte_Reihe_referenziert()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var data = new SeedDataDto
        {
            Spielstaetten = new() { Venue("V1", Room("R1", spalten: 5)) },
            Veranstaltungen = new() { Event("E1", "V1", "R1", "Z1") },
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_wenn_Sitzplatz_auf_einer_Gang_Spalte_liegt()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var data = new SeedDataDto
        {
            Spielstaetten = new() { Venue("V1", Room("R1", spalten: 5, gangSpalten: new List<int> { 3 })) },
            Veranstaltungen = new() { Event("E1", "V1", "R1", "A3") },
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_bei_doppeltem_Sitzplatz_in_belegteSitzplaetze()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var data = new SeedDataDto
        {
            Spielstaetten = new() { Venue("V1", Room("R1")) },
            Veranstaltungen = new() { Event("E1", "V1", "R1", "A2", "A2") },
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportIfEmptyAsync_wirft_beschreibende_Ausnahme_wenn_Seed_Datei_fehlt()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => importer.ImportIfEmptyAsync(Path.Combine(AppContext.BaseDirectory, "nicht-vorhanden.json"), ct));
        Assert.Contains("nicht-vorhanden.json", exception.Message);
    }

    [Fact]
    public async Task ImportIfEmptyAsync_wirft_beschreibende_Ausnahme_bei_defektem_JSON()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);

        var defekteDatei = Path.Combine(Path.GetTempPath(), $"defekt-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(defekteDatei, "{ das ist kein gueltiges JSON", ct);
        try
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => importer.ImportIfEmptyAsync(defekteDatei, ct));
            Assert.Contains(defekteDatei, exception.Message);
        }
        finally
        {
            File.Delete(defekteDatei);
        }
    }

    [Fact]
    public async Task ImportIfEmptyAsync_wirft_beschreibende_Ausnahme_wenn_ein_Top_Level_Schluessel_fehlt()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);

        var dateiOhneVeranstaltungen = Path.Combine(Path.GetTempPath(), $"ohne-veranstaltungen-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(dateiOhneVeranstaltungen, """{ "spielstaetten": [] }""", ct);
        try
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => importer.ImportIfEmptyAsync(dateiOhneVeranstaltungen, ct));

            // Ein fehlender Pflichtschlüssel ist syntaktisch gültiges, aber unvollständiges JSON —
            // die Meldung darf das nicht fälschlich als "ungültiges JSON" (Syntaxfehler) ausweisen.
            Assert.DoesNotContain("kein gültiges JSON", exception.Message);
            Assert.Contains("Veranstaltungen", exception.Message);
        }
        finally
        {
            File.Delete(dateiOhneVeranstaltungen);
        }
    }

    [Fact]
    public async Task ImportIfEmptyAsync_wirft_wenn_reihen_Schluessel_eines_Raums_fehlt()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);

        var dateiOhneReihen = Path.Combine(Path.GetTempPath(), $"ohne-reihen-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(dateiOhneReihen, """
            {
              "spielstaetten": [
                { "id": "V1", "name": "V1", "raeume": [ { "id": "R1", "name": "R1", "spalten": 5 } ] }
              ],
              "veranstaltungen": []
            }
            """, ct);
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => importer.ImportIfEmptyAsync(dateiOhneReihen, ct));
        }
        finally
        {
            File.Delete(dateiOhneReihen);
        }
    }

    [Fact]
    public async Task ImportAsync_wirft_wenn_reihen_Feld_eines_Raums_null_ist()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var room = Room("R1");
        room.Reihen = null!;
        var data = new SeedDataDto
        {
            Spielstaetten = new() { Venue("V1", room) },
            Veranstaltungen = new(),
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_bei_null_Eintrag_in_reihen()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var room = Room("R1");
        room.Reihen = new List<string?> { "A", null };
        var data = new SeedDataDto
        {
            Spielstaetten = new() { Venue("V1", room) },
            Veranstaltungen = new(),
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_bei_doppelter_Spielstaette_Id()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var data = new SeedDataDto
        {
            Spielstaetten = new() { Venue("V1", Room("R1")), Venue("V1", Room("R2")) },
            Veranstaltungen = new(),
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_wenn_spielstaetten_Feld_null_ist()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var data = new SeedDataDto { Spielstaetten = null!, Veranstaltungen = new() };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_wenn_veranstaltungen_Feld_null_ist()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var data = new SeedDataDto { Spielstaetten = new(), Veranstaltungen = null! };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_wenn_raeume_Feld_einer_Spielstaette_null_ist()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var data = new SeedDataDto
        {
            Spielstaetten = new() { new SeedVenueDto { Id = "V1", Name = "V1", Raeume = null! } },
            Veranstaltungen = new(),
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_wenn_preiskategorien_Feld_einer_Veranstaltung_null_ist()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var eventDto = Event("E1", "V1", "R1");
        eventDto.Preiskategorien = null!;
        var data = new SeedDataDto
        {
            Spielstaetten = new() { Venue("V1", Room("R1")) },
            Veranstaltungen = new() { eventDto },
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_wenn_belegteSitzplaetze_Feld_einer_Veranstaltung_null_ist()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var eventDto = Event("E1", "V1", "R1");
        eventDto.BelegteSitzplaetze = null!;
        var data = new SeedDataDto
        {
            Spielstaetten = new() { Venue("V1", Room("R1")) },
            Veranstaltungen = new() { eventDto },
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_bei_null_Eintrag_in_belegteSitzplaetze()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var eventDto = Event("E1", "V1", "R1");
        eventDto.BelegteSitzplaetze = new List<string?> { null };
        var data = new SeedDataDto
        {
            Spielstaetten = new() { Venue("V1", Room("R1")) },
            Veranstaltungen = new() { eventDto },
        };

        await Assert.ThrowsAsync<FormatException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_bei_negativem_Preis_einer_Preiskategorie()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var eventDto = Event("E1", "V1", "R1");
        eventDto.Preiskategorien.Add(new SeedPriceCategoryDto { Id = "E1-A", Name = "Kategorie A", Preis = -5m });
        var data = new SeedDataDto
        {
            Spielstaetten = new() { Venue("V1", Room("R1")) },
            Veranstaltungen = new() { eventDto },
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_wenn_Gang_Spalte_ausserhalb_des_Sitzplans_liegt()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var data = new SeedDataDto
        {
            Spielstaetten = new() { Venue("V1", Room("R1", spalten: 5, gangSpalten: new List<int> { 99 })) },
            Veranstaltungen = new(),
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_bei_ungueltiger_Spaltenzahl_eines_Raums()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var data = new SeedDataDto
        {
            Spielstaetten = new() { Venue("V1", Room("R1", spalten: 0)) },
            Veranstaltungen = new(),
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_bei_nicht_positiver_Dauer_einer_Veranstaltung()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var eventDto = Event("E1", "V1", "R1");
        eventDto.DauerMinuten = 0;
        var data = new SeedDataDto
        {
            Spielstaetten = new() { Venue("V1", Room("R1")) },
            Veranstaltungen = new() { eventDto },
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_bei_negativer_Altersfreigabe_einer_Veranstaltung()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var eventDto = Event("E1", "V1", "R1");
        eventDto.Altersfreigabe = -1;
        var data = new SeedDataDto
        {
            Spielstaetten = new() { Venue("V1", Room("R1")) },
            Veranstaltungen = new() { eventDto },
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_wenn_Raum_Id_null_ist()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var room = Room("R1");
        room.Id = null!;
        var data = new SeedDataDto
        {
            Spielstaetten = new() { Venue("V1", room) },
            Veranstaltungen = new(),
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_wenn_Raum_Name_null_ist()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var room = Room("R1");
        room.Name = null!;
        var data = new SeedDataDto
        {
            Spielstaetten = new() { Venue("V1", room) },
            Veranstaltungen = new(),
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_wenn_Veranstaltung_Raum_Id_null_ist()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var eventDto = Event("E1", "V1", "R1");
        eventDto.RaumId = null!;
        var data = new SeedDataDto
        {
            Spielstaetten = new() { Venue("V1", Room("R1")) },
            Veranstaltungen = new() { eventDto },
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_wenn_Titel_null_ist()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var eventDto = Event("E1", "V1", "R1");
        eventDto.Titel = null!;
        var data = new SeedDataDto
        {
            Spielstaetten = new() { Venue("V1", Room("R1")) },
            Veranstaltungen = new() { eventDto },
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_wenn_Beschreibung_null_ist()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var eventDto = Event("E1", "V1", "R1");
        eventDto.Beschreibung = null!;
        var data = new SeedDataDto
        {
            Spielstaetten = new() { Venue("V1", Room("R1")) },
            Veranstaltungen = new() { eventDto },
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_wenn_Spielstaette_Id_null_ist()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var venue = Venue("V1", Room("R1"));
        venue.Id = null!;
        var data = new SeedDataDto
        {
            Spielstaetten = new() { venue },
            Veranstaltungen = new(),
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_wenn_Spielstaette_Name_null_ist()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var venue = Venue("V1", Room("R1"));
        venue.Name = null!;
        var data = new SeedDataDto
        {
            Spielstaetten = new() { venue },
            Veranstaltungen = new(),
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_wenn_Veranstaltung_SpielstaetteId_null_ist()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var eventDto = Event("E1", "V1", "R1");
        eventDto.SpielstaetteId = null!;
        var data = new SeedDataDto
        {
            Spielstaetten = new() { Venue("V1", Room("R1")) },
            Veranstaltungen = new() { eventDto },
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_wenn_Preiskategorie_Name_null_ist()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var eventDto = Event("E1", "V1", "R1");
        eventDto.Preiskategorien.Add(new SeedPriceCategoryDto { Id = "E1-A", Name = null!, Preis = 10m });
        var data = new SeedDataDto
        {
            Spielstaetten = new() { Venue("V1", Room("R1")) },
            Veranstaltungen = new() { eventDto },
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }

    [Fact]
    public async Task ImportAsync_wirft_wenn_gleichzeitig_Spielstaette_Id_und_Veranstaltung_SpielstaetteId_null_sind()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var venue = Venue("V1", Room("R1"));
        venue.Id = null!;
        var eventDto = Event("E1", "V1", "R1");
        eventDto.SpielstaetteId = null!;
        var data = new SeedDataDto
        {
            Spielstaetten = new() { venue },
            Veranstaltungen = new() { eventDto },
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => importer.ImportAsync(data, ct));
    }
}
