namespace seed_a_backend.Api.Models;

public class BuchungsPosition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BuchungId { get; set; }
    public Buchung? Buchung { get; set; }

    /// <summary>Denormalisiert für die Eindeutigkeitsprüfung je Veranstaltung/Sitzplatz.</summary>
    public string VeranstaltungId { get; set; } = string.Empty;

    public string Reihe { get; set; } = string.Empty;
    public int Spalte { get; set; }

    public string PreiskategorieId { get; set; } = string.Empty;
    public Preiskategorie? Preiskategorie { get; set; }

    /// <summary>Preis zum Buchungszeitpunkt (Schnappschuss).</summary>
    public decimal Preis { get; set; }

    /// <summary>True solange die Buchung besteht; wird bei Stornierung false, damit der Platz wieder frei wird.</summary>
    public bool Aktiv { get; set; } = true;
}
