namespace seed_a_backend.Api.Models;

public class Buchungsposition
{
    public required string BuchungId { get; set; }

    public Buchung? Buchung { get; set; }

    public required string SitzplatzCode { get; set; }

    public required string PreiskategorieId { get; set; }

    public Preiskategorie? Preiskategorie { get; set; }

    public required decimal PreisSnapshot { get; set; }
}
