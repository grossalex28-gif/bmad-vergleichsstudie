namespace seed_a_backend.Api.Models;

public class Sitzplatzbelegung
{
    public required string VeranstaltungId { get; set; }

    public Veranstaltung? Veranstaltung { get; set; }

    public required string SitzplatzCode { get; set; }

    public string? BuchungId { get; set; }
}
