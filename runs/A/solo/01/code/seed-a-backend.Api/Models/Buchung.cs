namespace seed_a_backend.Api.Models;

public class Buchung
{
    public Guid Id { get; set; }

    /// Öffentlich sichtbare, kurze Buchungsreferenz zum Wiederaufruf der Buchung.
    public string Referenz { get; set; } = default!;

    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;

    public DateTime ErstelltAm { get; set; }
    public BuchungStatus Status { get; set; }

    public string VeranstaltungId { get; set; } = default!;
    public Veranstaltung Veranstaltung { get; set; } = default!;

    public ICollection<Buchungsposition> Positionen { get; set; } = new List<Buchungsposition>();
}
