namespace seed_a_backend.Api.Models;

public class Buchungsposition
{
    public Guid Id { get; set; }

    public Guid BuchungId { get; set; }
    public Buchung Buchung { get; set; } = default!;

    /// Redundant zur Veranstaltung der Buchung gespeichert, damit ein Sitzplatz je
    /// Veranstaltung über einen gefilterten Unique-Index nur einmal aktiv belegt sein kann.
    public string VeranstaltungId { get; set; } = default!;
    public Veranstaltung Veranstaltung { get; set; } = default!;

    public string Reihe { get; set; } = default!;
    public int Spalte { get; set; }

    public string PreiskategorieId { get; set; } = default!;
    public Preiskategorie Preiskategorie { get; set; } = default!;

    /// Preis zum Buchungszeitpunkt (Schnappschuss der Preiskategorie).
    public decimal Preis { get; set; }

    /// Wird beim Stornieren der Buchung mit auf Storniert gesetzt, damit der
    /// gefilterte Unique-Index den Sitzplatz wieder als frei behandelt.
    public BuchungStatus Status { get; set; }
}
