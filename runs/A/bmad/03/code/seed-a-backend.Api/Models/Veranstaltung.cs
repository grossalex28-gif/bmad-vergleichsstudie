namespace seed_a_backend.Api.Models;

public class Veranstaltung
{
    public required string Id { get; set; }

    public required string Titel { get; set; }

    public required string Beschreibung { get; set; }

    public required string RaumId { get; set; }

    public Raum? Raum { get; set; }

    public required int DauerMinuten { get; set; }

    public required int Altersfreigabe { get; set; }

    public required DateTimeOffset Zeitpunkt { get; set; }

    public ICollection<Preiskategorie> Preiskategorien { get; set; } = new List<Preiskategorie>();
}
