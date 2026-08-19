namespace seed_a_backend.Api.Models;

public enum BuchungStatus
{
    Bestaetigt,
    Storniert
}

public class Buchung
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Referenz { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public string VeranstaltungId { get; set; } = string.Empty;
    public Veranstaltung? Veranstaltung { get; set; }

    public DateTime ErstelltAm { get; set; } = DateTime.UtcNow;
    public BuchungStatus Status { get; set; } = BuchungStatus.Bestaetigt;

    public ICollection<BuchungsPosition> Positionen { get; set; } = new List<BuchungsPosition>();
}
