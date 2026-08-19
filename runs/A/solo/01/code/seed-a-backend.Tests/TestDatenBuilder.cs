using seed_a_backend.Api.Data;
using seed_a_backend.Api.Models;

namespace seed_a_backend.Tests;

public static class TestDatenBuilder
{
    /// Legt eine Veranstaltung mit Raum "R1" (Reihen A-C, 5 Spalten, Gang in Spalte 3)
    /// und zwei Preiskategorien an und speichert sie in der übergebenen Datenbank.
    public static Veranstaltung ErstelleVeranstaltungMitRaum(AppDbContext db)
    {
        var spielstaette = new Spielstaette { Id = "V1", Name = "Stadthalle" };
        var raum = new Raum
        {
            Id = "R1",
            Name = "Großer Saal",
            SpielstaetteId = "V1",
            ReihenCsv = "A,B,C",
            Spalten = 5,
            GangSpaltenCsv = "3",
            GangHinweis = "Mittelgang"
        };
        spielstaette.Raeume.Add(raum);

        var veranstaltung = new Veranstaltung
        {
            Id = "E1",
            Titel = "Testkonzert",
            Beschreibung = "Ein Testkonzert.",
            SpielstaetteId = "V1",
            RaumId = "R1",
            Zeitpunkt = new DateTime(2026, 9, 5, 19, 30, 0, DateTimeKind.Utc),
            DauerMinuten = 90,
            Altersfreigabe = 0
        };
        veranstaltung.Preiskategorien.Add(new Preiskategorie { Id = "E1-A", VeranstaltungId = "E1", Name = "Kategorie A", Preis = 30m });
        veranstaltung.Preiskategorien.Add(new Preiskategorie { Id = "E1-B", VeranstaltungId = "E1", Name = "Kategorie B", Preis = 20m });

        db.Spielstaetten.Add(spielstaette);
        db.Veranstaltungen.Add(veranstaltung);
        db.SaveChanges();

        return veranstaltung;
    }
}
