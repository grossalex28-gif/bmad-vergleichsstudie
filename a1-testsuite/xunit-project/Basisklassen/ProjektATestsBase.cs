using A1Testsuite.Treiber;
using Xunit;

namespace A1Testsuite.Basisklassen;

/// <summary>
/// Schicht 1 (Testfaelle) fuer Projekt A, gegen die Treiberschnittstelle aus
/// claude/Akzeptanztests_Architektur.md (v2.0, Abschnitt 5). Ein [Fact] je
/// Testfall aus claude/Akzeptanztestfaelle_ProjektA.md (Stand 31.08.2026,
/// TF-A-F13-2 auf Sitzplatz A2 statt A1, siehe dortiger
/// Testreihenfolge-Hinweis). Konkrete Klassen erzeugen im Konstruktor den
/// jeweiligen Adapter und reichen ihn hier durch, siehe
/// a1-testsuite/xunit-project/README.md.
///
/// Veranstaltungen werden nicht ueber die Bezeichnungen "E1"-"E6" aus
/// claude/Anfangsdatenbestand_Spezifikation.md adressiert -- das sind reine
/// Spezifikationslabel, keine echten IDs. Stattdessen loest
/// <see cref="FindeVeranstaltungId"/> die tatsaechliche ID zur Laufzeit ueber
/// den Titel auf (siehe README.md, Abschnitt "Design-Entscheidung").
/// </summary>
public abstract class ProjektATestsBase
{
    private readonly IProjektATreiber _treiber;

    protected ProjektATestsBase(IProjektATreiber treiber)
    {
        _treiber = treiber;
    }

    private async Task<string> FindeVeranstaltungId(string titel)
    {
        var alle = await _treiber.ListeVeranstaltungen();
        var treffer = alle.SingleOrDefault(v => v.Titel == titel);
        Assert.True(treffer is not null, $"Veranstaltung '{titel}' nicht in ListeVeranstaltungen() gefunden.");
        return treffer!.Id;
    }

    private static void AssertPreisNahe(decimal erwartet, decimal tatsaechlich)
        => Assert.True(
            Math.Abs(erwartet - tatsaechlich) <= 0.01m,
            $"Erwartet {erwartet:F2}, war {tatsaechlich:F2} (Toleranz 1 Cent, Abschnitt 7 der Akzeptanztest-Architektur).");

    // A-F1: Veranstaltungsliste mit Titel, Spielstaette, Datum, Uhrzeit.
    [Fact]
    public async Task TF_A_F1_1_Veranstaltungsliste_ohne_Filter()
    {
        var liste = await _treiber.ListeVeranstaltungen();
        Assert.Equal(6, liste.Count);

        void PruefeEintrag(string titel, string spielstaette, DateTime zeitpunkt)
        {
            var eintrag = liste.SingleOrDefault(v => v.Titel == titel);
            Assert.True(eintrag is not null, $"Veranstaltung '{titel}' fehlt in der Liste.");
            Assert.Equal(spielstaette, eintrag!.Spielstaette);
            Assert.Equal(zeitpunkt, eintrag.Zeitpunkt);
        }

        PruefeEintrag("Kammerkonzert Frühling", "Stadthalle Nordpark", new DateTime(2026, 9, 5, 19, 30, 0));
        PruefeEintrag("Sinfonisches Openair-Programm", "Kulturhaus Südtor", new DateTime(2026, 9, 12, 20, 0, 0));
        PruefeEintrag("Comedy-Abend", "Stadthalle Nordpark", new DateTime(2026, 9, 20, 21, 0, 0));
        PruefeEintrag("Kindertheater: Die Reise zum Mond", "Kulturhaus Südtor", new DateTime(2026, 9, 25, 15, 0, 0));
        PruefeEintrag("Jazz-Session", "Stadthalle Nordpark", new DateTime(2026, 10, 3, 20, 30, 0));
        PruefeEintrag("Late-Night-Show", "Kulturhaus Südtor", new DateTime(2026, 10, 10, 22, 0, 0));
    }

    // A-F2: Filter nach Datumsbereich.
    [Fact]
    public async Task TF_A_F2_1_Filter_nach_Datumsbereich()
    {
        var liste = await _treiber.ListeVeranstaltungen(
            vonDatum: new DateTime(2026, 9, 1), bisDatum: new DateTime(2026, 9, 15));

        var titel = liste.Select(v => v.Titel).ToHashSet();
        Assert.Equal(new HashSet<string> { "Kammerkonzert Frühling", "Sinfonisches Openair-Programm" }, titel);
    }

    // A-F3: Filter nach Spielstaette.
    [Fact]
    public async Task TF_A_F3_1_Filter_nach_Spielstaette()
    {
        var liste = await _treiber.ListeVeranstaltungen(spielstaette: "Stadthalle Nordpark");
        var titel = liste.Select(v => v.Titel).ToHashSet();
        Assert.Equal(new HashSet<string> { "Kammerkonzert Frühling", "Comedy-Abend", "Jazz-Session" }, titel);
    }

    // A-F4: Detailansicht mit Beschreibung, Dauer, Altersfreigabe, Spielstaette, Raum, Zeitpunkt.
    [Fact]
    public async Task TF_A_F4_1_Detailansicht()
    {
        var id = await FindeVeranstaltungId("Comedy-Abend");
        var detail = await _treiber.HoleVeranstaltung(id);

        Assert.False(string.IsNullOrWhiteSpace(detail.Beschreibung));
        Assert.Equal(100, detail.DauerMinuten);
        Assert.Equal(16, detail.Altersfreigabe);
        Assert.Equal("Stadthalle Nordpark", detail.Spielstaette);
        Assert.Equal("Großer Saal", detail.Raum);
        Assert.Equal(new DateTime(2026, 9, 20, 21, 0, 0), detail.Zeitpunkt);
    }

    // A-F5: Sitzplan aus Reihen/Spalten, Positionen optional als Gang. Konzertsaal (E2): 12x16, Seitengaenge Spalte 1 und 16.
    [Fact]
    public async Task TF_A_F5_1_Sitzplan_mit_beidseitigen_Seitengaengen()
    {
        var id = await FindeVeranstaltungId("Sinfonisches Openair-Programm");
        var plan = await _treiber.HoleSitzplan(id);

        Assert.Equal(12, plan.AnzahlReihen);
        Assert.Equal(16, plan.AnzahlSpalten);
        Assert.Equal(12 * 16, plan.Positionen.Count);

        foreach (var pos in plan.Positionen)
        {
            var erwarteterTyp = pos.Spalte is 1 or 16 ? PositionsTyp.Gang : PositionsTyp.Sitz;
            Assert.True(pos.Typ == erwarteterTyp, $"Position {pos.Bezeichnung}: erwartet {erwarteterTyp}, war {pos.Typ}.");
        }
    }

    // A-F5: Studio (E4), 4x8, bewusst ohne Gang.
    [Fact]
    public async Task TF_A_F5_2_Sitzplan_ohne_Gang()
    {
        var id = await FindeVeranstaltungId("Kindertheater: Die Reise zum Mond");
        var plan = await _treiber.HoleSitzplan(id);

        Assert.Equal(4, plan.AnzahlReihen);
        Assert.Equal(8, plan.AnzahlSpalten);
        Assert.All(plan.Positionen, p => Assert.Equal(PositionsTyp.Sitz, p.Typ));
    }

    // A-F6: Datengrundlage fuer frei/belegt/Gang. Kleiner Saal (E1), 6x10, Mittelgang Spalte 6, B3/B4/C7 vorbelegt.
    [Fact]
    public async Task TF_A_F6_1_Belegte_und_freie_Plaetze_sowie_Gang()
    {
        var id = await FindeVeranstaltungId("Kammerkonzert Frühling");
        var plan = await _treiber.HoleSitzplan(id);

        Assert.Equal(6, plan.AnzahlReihen);
        Assert.Equal(10, plan.AnzahlSpalten);

        var belegt = new HashSet<string> { "B3", "B4", "C7" };

        foreach (var pos in plan.Positionen)
        {
            if (pos.Spalte == 6)
            {
                Assert.Equal(PositionsTyp.Gang, pos.Typ);
                continue;
            }

            Assert.Equal(PositionsTyp.Sitz, pos.Typ);
            var erwarteterStatus = belegt.Contains(pos.Bezeichnung) ? PositionsStatus.Belegt : PositionsStatus.Frei;
            Assert.Equal(erwarteterStatus, pos.Status);
        }
    }

    // A-F7: Auswahl freier Sitzplaetze (Berechnung ohne Fehler, beide Positionen akzeptiert).
    [Fact]
    public async Task TF_A_F7_1_Auswahl_freier_Sitzplaetze()
    {
        var id = await FindeVeranstaltungId("Kammerkonzert Frühling");
        var auswahl = new[]
        {
            new SitzplatzAuswahl("A1", "Kategorie A"),
            new SitzplatzAuswahl("A2", "Kategorie B"),
        };

        var preis = await _treiber.BerechnePreis(id, auswahl);
        Assert.True(preis > 0m);
    }

    // A-F8: Preiskategorien je Veranstaltung.
    [Fact]
    public async Task TF_A_F8_1_Preiskategorien()
    {
        var id = await FindeVeranstaltungId("Kammerkonzert Frühling");
        var kategorien = await _treiber.HolePreiskategorien(id);

        Assert.Equal(2, kategorien.Count);
        var a = kategorien.SingleOrDefault(k => k.Bezeichnung == "Kategorie A");
        var b = kategorien.SingleOrDefault(k => k.Bezeichnung == "Kategorie B");
        Assert.True(a is not null, "Kategorie A fehlt.");
        Assert.True(b is not null, "Kategorie B fehlt.");
        AssertPreisNahe(32.00m, a!.Preis);
        AssertPreisNahe(22.00m, b!.Preis);
    }

    // A-F9: Gesamtpreisberechnung, BerechnePreis als eigenstaendiger Aufruf.
    [Fact]
    public async Task TF_A_F9_1_Gesamtpreisberechnung()
    {
        var id = await FindeVeranstaltungId("Kammerkonzert Frühling");
        var auswahl = new[]
        {
            new SitzplatzAuswahl("A1", "Kategorie A"),
            new SitzplatzAuswahl("A2", "Kategorie B"),
        };

        var preis = await _treiber.BerechnePreis(id, auswahl);
        AssertPreisNahe(54.00m, preis);
    }

    // A-F10 bis A-F12: Buchung anlegen, ueber Referenz abrufen, stornieren.
    // Als eine zusammenhaengende Testmethode implementiert (Abschnitt 7 der
    // Akzeptanztest-Architektur, Konvention zu verketteten Testfaellen).
    [Fact]
    public async Task TF_A_F10_1_bis_TF_A_F12_1_Buchung_anlegen_abrufen_stornieren()
    {
        var id = await FindeVeranstaltungId("Kammerkonzert Frühling");
        var auswahl = new[] { new SitzplatzAuswahl("A1", "Kategorie A") };

        // TF-A-F10-1
        var referenz = await _treiber.LegeBuchungAn(id, auswahl, "Max Mustermann", "max.mustermann@example.com");
        Assert.False(string.IsNullOrWhiteSpace(referenz));

        var planNachBuchung = await _treiber.HoleSitzplan(id);
        Assert.Equal(PositionsStatus.Belegt, planNachBuchung.Positionen.Single(p => p.Bezeichnung == "A1").Status);

        // TF-A-F11-1
        var buchung = await _treiber.HoleBuchung(referenz);
        var position = Assert.Single(buchung.Positionen);
        Assert.Equal("A1", position.SitzplatzBezeichnung);
        Assert.Equal("Kategorie A", position.KategorieBezeichnung);
        AssertPreisNahe(32.00m, buchung.Gesamtpreis);
        Assert.False(string.IsNullOrWhiteSpace(buchung.Status));

        // TF-A-F12-1
        await _treiber.StorniereBuchung(referenz);

        var planNachStorno = await _treiber.HoleSitzplan(id);
        Assert.Equal(PositionsStatus.Frei, planNachStorno.Positionen.Single(p => p.Bezeichnung == "A1").Status);
    }

    // A-F13, positiver Fall: beide gewaehlten Plaetze frei, Buchung wird angelegt.
    [Fact]
    public async Task TF_A_F13_1_Buchung_mit_zwei_freien_Plaetzen_wird_angelegt()
    {
        var id = await FindeVeranstaltungId("Jazz-Session");
        var auswahl = new[]
        {
            new SitzplatzAuswahl("A1", "Kategorie A"),
            new SitzplatzAuswahl("A2", "Kategorie B"),
        };

        var referenz = await _treiber.LegeBuchungAn(id, auswahl, "Anna Beispiel", "anna.beispiel@example.com");
        Assert.False(string.IsNullOrWhiteSpace(referenz));

        var plan = await _treiber.HoleSitzplan(id);
        Assert.Equal(PositionsStatus.Belegt, plan.Positionen.Single(p => p.Bezeichnung == "A1").Status);
        Assert.Equal(PositionsStatus.Belegt, plan.Positionen.Single(p => p.Bezeichnung == "A2").Status);
    }

    // A-F13, negativer Fall: B3 bereits belegt -> Ablehnung, A2 bleibt frei.
    // Sitzplatz A2 statt A1 (siehe Testreihenfolge-Hinweis in
    // claude/Akzeptanztestfaelle_ProjektA.md, Stand 31.08.2026): A1 wird von
    // TF_A_F10_1_bis_TF_A_F12_1 tatsaechlich gebucht/storniert, xUnit
    // garantiert innerhalb einer Klasse keine Reihenfolge zwischen
    // unabhaengigen Testmethoden. A2 wird von keiner anderen Testmethode
    // zustandsveraendernd angefasst.
    [Fact]
    public async Task TF_A_F13_2_Ablehnung_bei_bereits_belegtem_Platz()
    {
        var id = await FindeVeranstaltungId("Kammerkonzert Frühling");
        var auswahl = new[]
        {
            new SitzplatzAuswahl("A2", "Kategorie A"),
            new SitzplatzAuswahl("B3", "Kategorie A"),
        };

        await Assert.ThrowsAsync<BuchungAbgelehntException>(
            () => _treiber.LegeBuchungAn(id, auswahl, "Peter Konflikt", "peter.konflikt@example.com"));

        var plan = await _treiber.HoleSitzplan(id);
        Assert.Equal(PositionsStatus.Frei, plan.Positionen.Single(p => p.Bezeichnung == "A2").Status);
    }
}
