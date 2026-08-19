namespace seed_a_backend.Api.Models;

public class Preiskategorie
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Preis { get; set; }

    public string VeranstaltungId { get; set; } = string.Empty;
    public Veranstaltung? Veranstaltung { get; set; }
}
