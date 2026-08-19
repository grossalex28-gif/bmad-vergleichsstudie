namespace seed_a_backend.Api.Models;

public class Preiskategorie
{
    public string Id { get; set; } = default!;

    public string VeranstaltungId { get; set; } = default!;
    public Veranstaltung Veranstaltung { get; set; } = default!;

    public string Name { get; set; } = default!;
    public decimal Preis { get; set; }
}
