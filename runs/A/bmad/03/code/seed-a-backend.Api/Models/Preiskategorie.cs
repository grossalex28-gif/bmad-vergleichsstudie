namespace seed_a_backend.Api.Models;

public class Preiskategorie
{
    public required string Id { get; set; }

    public required string Name { get; set; }

    public required decimal Preis { get; set; }

    public required string VeranstaltungId { get; set; }

    public Veranstaltung? Veranstaltung { get; set; }
}
