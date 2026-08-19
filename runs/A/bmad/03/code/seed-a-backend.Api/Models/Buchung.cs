namespace seed_a_backend.Api.Models;

public enum BuchungStatus
{
    Aktiv,
    Storniert,
}

public class Buchung
{
    public required string Id { get; set; }

    public required string Referenz { get; set; }

    public required string VeranstaltungId { get; set; }

    public Veranstaltung? Veranstaltung { get; set; }

    public required string Name { get; set; }

    public required string Email { get; set; }

    public required BuchungStatus Status { get; set; }

    public required decimal Gesamtpreis { get; set; }

    public ICollection<Buchungsposition> Buchungspositionen { get; set; } = new List<Buchungsposition>();
}
